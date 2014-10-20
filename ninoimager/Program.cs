// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="none">
// Copyright (C) 2013
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see "http://www.gnu.org/licenses/". 
// </copyright>
// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>29/07/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Ninoimager.Format;

namespace Ninoimager
{
	public static class MainClass
	{
		private static readonly string M = "m";
		private static readonly Regex RegexLin = new Regex(@"(.+)\/(.+)" + M + @"\.png$", RegexOptions.Compiled);
		private static readonly Regex RegexWin = new Regex(@"(.+)\\(.+)" + M + @"\.png$", RegexOptions.Compiled);

		public static void Main(string[] args)
		{
            Console.WriteLine("ninoimager ~~ Image importer and exporter for Ni no kuni DS");
			Console.WriteLine("V {0} ~~ by pleoNeX ~~", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine();

			Stopwatch watch = new Stopwatch();
			watch.Start();

			RunCommand(args);

			watch.Stop();
			Console.WriteLine("It tooks: {0}", watch.Elapsed);
		}

		private static void RunCommand(string[] args)
		{
			// Three argument commands
			if (args.Length < 3)
				return;

			string command = args[0];
			string baseDir = args[1];
			string outputDir = args[2];

			if (command == "-ebg" && args.Length == 3)
				SearchAndExportBg(baseDir, outputDir);

			if (command == "-etx" && args.Length == 3) {
				Btx0 tex = new Btx0(args[1]);
				for (int i = 0; i < tex.NumObjects; i++)
					tex.CreateBitmap(i).Save(args[2] + i.ToString() + ".png");
			}

			// Five argument commands
			if (args.Length < 5)
				return;
				
			string infoPath = args[3];
			string editPath = args[4];
			string multiXml = (args.Length == 6) ? args[5] : string.Empty;

			if (command == "-i" && (args.Length == 5 || args.Length == 6))
				SearchAndImport(baseDir, outputDir, infoPath, editPath, multiXml, false);

			if (command == "-ir" && (args.Length == 5 || args.Length == 6))
				SearchAndImport(baseDir, outputDir, infoPath, editPath, multiXml, true);
		}
		
		private static void SearchAndImport(string baseDir, string outDir,
			string infoPath, string editPath, string multiXml, bool filterDate)
		{
			Console.WriteLine("@ Batch import");
			Console.WriteLine("From: {0}", baseDir);
			Console.WriteLine("To:   {0}", outDir);
			Console.WriteLine("Info XML:    {0}", infoPath);
			Console.WriteLine("Edit XML:    {0}", editPath);
			Console.WriteLine("MultiImport: {0}", multiXml);
			Console.WriteLine();

			List<string> imported = new List<string>();

			// First import "special files" that share palette and images data with other N2D files
			Console.WriteLine("## Starting MultiImport ##");
			MultiImport(baseDir, outDir, imported, multiXml, filterDate);
			Console.WriteLine();

			// Then import other images
			Console.WriteLine("## Starting SingleImport ##");
			SingleImport(baseDir, outDir, imported, filterDate);
			Console.WriteLine();

			// Create a new XML document with data of the modime XMLs
			UpdateModimeXml(imported.ToArray(), infoPath, editPath);
		}

		private static void MultiImport(string baseDir, string outputDir, 
			List<string> importedList, string xml, bool filterDate)
		{
			if (string.IsNullOrEmpty(xml))
				return;

			XDocument doc = XDocument.Load(xml);
			foreach (XElement entry in doc.Root.Elements("Pack")) {
				// Get mode
				string mode = entry.Element("Mode").Value;

				// Get paths
				bool existImages = true;
				List<string> relatives = new List<string>();
				List<string> imgPaths  = new List<string>();
				List<string> outPaths  = new List<string>();
				foreach (XElement ximg in entry.Element("Images").Elements("Image")) {
					relatives.Add(ximg.Value);
					imgPaths.Add(Path.Combine(baseDir, ximg.Value) + "_6.nscr" + M + ".png");
					outPaths.Add(Path.Combine(outputDir, ximg.Value));

					if (!File.Exists(imgPaths[imgPaths.Count - 1])) {
						existImages = false;
						break;
					}
				}

				// Images still not translated
				if (!existImages)
					continue;

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outPaths[0] + "_new.n2d");
					if (!imgPaths.Any(f => File.GetLastWriteTime(f) > referenceDate))
						continue;
				}

				// Import
				Console.Write("|-Importing {0,-45} | ", relatives[0]);
				for (int i = 1; i < relatives.Count; i++) {
					Console.WriteLine();
					Console.Write("            {0,-45} | ", relatives[i]);
				}

				Npck originalPack = new Npck(outPaths[0] + ".n2d");
				Npck[] packs = null;
				if (mode == "SharePalette")
					packs = NpckFactory.FromBackgroundImageSharePalette(imgPaths.ToArray(), originalPack);
				else if (mode == "ShareImage")
					packs = NpckFactory.FromBackgroundImageShareImage(imgPaths.ToArray(), originalPack);
				else if (mode == "SharePaletteChangeDepth")
					packs = NpckFactory.FromBackgroundImageSharePaletteChangeDepth(imgPaths.ToArray(), originalPack, true);
				else
					throw new FormatException(string.Format("Unsopported mode \"{0}\"", mode)); 

				// Write output
				originalPack.CloseAll();
				for (int i = 0; i < outPaths.Count; i++) {
					packs[i].Write(outPaths[i] + "_new.n2d");
					packs[i].CloseAll();
				}

				importedList.AddRange(relatives);
				Console.WriteLine("Successfully");
			}
		}
			
		private static void SingleImport(string baseDir, string outputDir,
			List<string> importedList, bool filterDate)
		{
			int count = 0;
			Dictionary<string, int> errors = new Dictionary<string, int>();
			errors.Add("skipImgs",   0);
			errors.Add("errorPack",  0); 	errors.Add("errorImgs",  0);
			errors.Add("noN2DPack",  0);	errors.Add("noN2DImgs",  0);
			errors.Add("noModPack",  0);	errors.Add("noModImgs",  0);
			errors.Add("inListPack", 0);	errors.Add("inListImgs", 0);
			errors.Add("noSuffPack", 0);    errors.Add("noSuffImgs", 0);

			Dictionary<string, SortedList<int, string>> imageGroups = 
				new Dictionary<string, SortedList<int, string>>();
				
			PlatformID osId = Environment.OSVersion.Platform;
			Regex regex = (osId == PlatformID.Unix || osId == PlatformID.MacOSX) ? RegexLin : RegexWin;

			Console.Write("Searching for images... ");
			foreach (string imgFile in Directory.GetFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = regex.Match(imgFile);
				if (!match.Success)
					continue;

				// Gets real name and frame index
				string filename = match.Groups[2].Value;
				int frameIdx = 0;	// BG index is always 0 (only importing one image)

				// If it's match old format (suffix '_6.nscrm' and '_3.ncer_Xm')
				if (filename.EndsWith("_6.nscr")) {
					filename = filename.Substring(0, filename.Length - 7);
				} else if (filename.Contains("_3.ncer_")) {
					try {
						int nameIdx = filename.IndexOf("_3.ncer_");
						frameIdx = Convert.ToInt32(filename.Substring(nameIdx + 8));
						filename = filename.Substring(0, nameIdx);
					} catch {
						Console.WriteLine("\t\t{0} skipped", imgFile);
						errors["skipImgs"]++;
						continue;
					}
				}

				// Else, the new format does not have the suffixes
				// If the .n2d file exists with that file name we got it
				string testN2D = match.Groups[1].Value + Path.DirectorySeparatorChar;
				testN2D = testN2D.Replace(baseDir, outputDir);
				if (!File.Exists(testN2D + filename + ".n2d") && filename.Contains("_")) {
					// Else, try to get frame index, if exception it should be texture so skip
					try {
						int nameIdx = filename.LastIndexOf("_");
						frameIdx = Convert.ToInt32(filename.Substring(nameIdx + 1));
						filename = filename.Substring(0, nameIdx);
					} catch {
						errors["skipImgs"]++;
						continue;
					}
				}

				// Get relative path
				string imagePath = match.Groups[1].Value + Path.DirectorySeparatorChar + filename;
				string relative  = imagePath.Replace(baseDir, "");
				if (relative[0] == Path.DirectorySeparatorChar)
					relative = relative.Substring(1);

				if (!imageGroups.ContainsKey(relative))
					imageGroups.Add(relative, new SortedList<int, string>());
				imageGroups[relative].Add(frameIdx, imgFile);
				count++;
			}

			Console.WriteLine("\tFound {0} images in {1} groups", count, imageGroups.Count);
			Console.WriteLine("\t\t\t\tSkipped {0} images", errors["skipImgs"]);
			Console.WriteLine("Starting importing...");
			foreach (string relative in imageGroups.Keys) {
				// Get output paths
				string original = Path.Combine(outputDir, relative) + ".n2d";
				string outFile  = Path.Combine(outputDir, relative) + "_new.n2d";
				string[] imgs   = imageGroups[relative].Values.ToArray();

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outFile);
					if (!imgs.Any(f => File.GetLastWriteTime(f) > referenceDate)) {
						Console.WriteLine("|+ Skipped (date filter) {0}", relative);
						errors["noModPack"]++;
						errors["noModImgs"] += imgs.Length;
						count -= imgs.Length;
						continue;
					}
				}

				// Check if it has been already imported
				if (importedList.Contains(relative)) {
					Console.WriteLine("|+ Skipped (already imported) {0}", relative);
					errors["inListPack"]++;
					errors["inListImgs"] += imgs.Length;
					count -= imgs.Length;
					continue;
				}

				// If original file does not exist, skip
				// Odd way to import manually images and skip them here
				if (!File.Exists(original) && File.Exists(outFile)) {
					Console.WriteLine("|+ Skipped (manual mode) {0}", relative);
					errors["noN2DPack"]++;
					errors["noN2DImgs"] += imgs.Length;
					count -= imgs.Length;
					continue;
				}

				// If the original file does not exists AND there isn't any manual import
				if (!File.Exists(original)) {
					Console.WriteLine("|+ Skipped (invalid suffix) {0}", relative);
					errors["noSuffPack"]++;
					errors["noSuffImgs"] += imgs.Length;
					count -= imgs.Length;
					continue;
				}

				// Try to import
				Console.Write("|-Importing {0,-45} {1,2} | ", relative, imgs.Length);
				try {
					Npck ori  = new Npck(original);
					Npck npck;
					if (ori.IsSprite)
						npck = NpckFactory.FromSpriteImage(imgs, imageGroups[relative].Keys.ToArray(), ori);
					else if (ori.IsBackground && imgs.Length == 1)
						npck = NpckFactory.FromBackgroundImage(imgs[0], ori);
					else
						throw new FormatException("Image format not supported");

					npck.Write(outFile);
					npck.CloseAll();
					ori.CloseAll();
				} catch (Exception ex) {
					Console.WriteLine("Error: {0}", ex.Message);
					#if DEBUG
					Console.WriteLine(ex.ToString());
					#endif
					errors["errorPack"]++;
					errors["errorImgs"] += imgs.Length;
					count -= imgs.Length;
					continue;
				}

				importedList.Add(relative);
				Console.WriteLine("Successfully");
			}

			Console.WriteLine();
			Console.WriteLine("# Statistics #");
			Console.WriteLine("\tErrors in {0} packages ({1} images)",
				errors["errorPack"], errors["errorImgs"]);
			Console.WriteLine("\tNo N2D file found for {0} packages ({1} images)",
				errors["noN2DPack"], errors["noN2DImgs"]);
			Console.WriteLine("\tInvalid file suffix for {0} packages ({1} images)",
				errors["noSuffPack"], errors["noSuffImgs"]);
			Console.WriteLine("\tFilter skipped {0} packages ({1} images)",
				errors["noModPack"], errors["noModImgs"]);
			Console.WriteLine("\tAlready imported {0} packages ({1} images)",
				errors["inListPack"], errors["inListImgs"]);
			Console.WriteLine("\tImported {0} images successfully!", count);
		}

		private static void CreateModimeXml(string[] relativePaths, string outputXml)
		{
			const string RootName    = "/Ninokuni.nds";
			const string BaseRomPath = "/Ninokuni.nds/ROM/data/";

			XDocument xml   = new XDocument();
			xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
			XElement root   = new XElement("NinoImport");
			xml.Add(root);

			XElement gameInfo = new XElement("GameInfo");
			XElement editInfo = new XElement("EditInfo");
			root.Add(gameInfo);
			root.Add(editInfo);

			foreach (string relative in relativePaths) {
				// Add game info
				XElement xgame = new XElement("File");
				xgame.Add(new XElement("Path", Path.Combine(BaseRomPath, relative) + ".n2d"));
				xgame.Add(new XElement("Import", "{$ImagePath}/" + relative + "_new.n2d"));
				gameInfo.Add(xgame);

				// Add edit info
				XElement xedit = new XElement("FileInfo");
				xedit.Add(new XElement("Path", Path.Combine(BaseRomPath, relative) + ".n2d"));
				xedit.Add(new XElement("Type", "Common.Replace"));
				xedit.Add(new XElement("DependsOn", RootName));
				editInfo.Add(xedit);
			}

			xml.Save(outputXml);
		}

		private static void UpdateModimeXml(string[] relativePaths, string infoPath, string editPath)
		{
			const string RootName    = "/Ninokuni.nds";
			const string BaseRomPath = "/Ninokuni.nds/ROM/data/";

			// Open Info XML
			XDocument infoXml  = XDocument.Load(infoPath);
			XElement  infoRoot = infoXml.Root.Element("Files");

			// Open Edit XML
			XDocument editXml  = XDocument.Load(editPath);
			XElement  editRoot = editXml.Root.Element("Files");

			// For each entry to add
			foreach (string relative in relativePaths) {
				string path = Path.Combine(BaseRomPath, relative) + ".n2d";
				path = path.Replace('\\', '/');
				
				// If it's not there, add it
				if (infoRoot.Elements().Count(e => e.Element("Path").Value == path) == 0) {
					XElement xedit = new XElement("FileInfo");
					xedit.Add(new XElement("Path", path));
					xedit.Add(new XElement("Type", "Common.Replace"));
					xedit.Add(new XElement("DependsOn", RootName));
					infoRoot.Add(xedit);
				}

				// If it's not there, add it
				if (editRoot.Elements().Count(e => e.Element("Path").Value == path) == 0) {
					XElement xgame = new XElement("File");
					xgame.Add(new XElement("Path", path));
					xgame.Add(new XElement("Import", "{$ImagePath}/" + relative.Replace('\\', '/') + "_new.n2d"));
					editRoot.Add(xgame);
				}
			}

			infoXml.Save(infoPath);
			editXml.Save(editPath);
		}

		private static void SearchAndExportBg(string baseDir, string outputDir)
		{
			foreach (string file in Directory.GetFiles(outputDir, "*.n2d", SearchOption.AllDirectories)) {	
				string relativePath = file.Replace(outputDir, "");
				string imageName = Path.GetFileNameWithoutExtension(file);
				string imagePath = Path.Combine(baseDir, relativePath, imageName + ".png");

				try {
					Npck pack = new Npck(file);
					pack.GetBackgroundImage().Save(imagePath);
				} catch (Exception ex) {
					Console.WriteLine("Error trying to export: {0} to {1}", relativePath, imagePath);
					Console.WriteLine("\t" + ex.ToString());
					continue;
				}

				Console.WriteLine("Exported {0} -> {1}", relativePath, imageName);
			}
		}
	}
}
