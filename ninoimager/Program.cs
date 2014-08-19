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
		private static readonly Regex BgRegex = new Regex(@"(.+)_6\.nscrm\.png$", RegexOptions.Compiled);
		private static readonly Regex SpRegex = new Regex(@"(.+)_3?(\.ncer)?_(\d+)m\.png$", RegexOptions.Compiled);

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

			// Five argument commands
			if (args.Length < 5)
				return;
				
			string infoPath = args[3];
			string editPath = args[4];

			if (command == "-ibg" && (args.Length == 5 || args.Length == 6))
				SearchAndImportBg(baseDir, outputDir, infoPath, editPath,
					(args.Length == 6) ? args[5] : string.Empty, false);

			if (command == "-irbg" && (args.Length == 5 || args.Length == 6))
				SearchAndImportBg(baseDir, outputDir, infoPath, editPath,
					(args.Length == 6) ? args[5] : string.Empty, true);

			if (command == "-isp" && args.Length == 5)
				SearchAndImportSp(baseDir, outputDir, infoPath, editPath, false);

			if (command == "-irsp" && args.Length == 5)
				SearchAndImportSp(baseDir, outputDir, infoPath, editPath, true);
		}

		private static void SearchAndImportSp(string baseDir, string outDir,
			string infoPath, string editPath, bool filterDate)
		{
			Console.WriteLine("@ Batch import");
			Console.WriteLine("From: {0}", baseDir);
			Console.WriteLine("To:   {0}", outDir);
			Console.WriteLine("Info XML: {0}", infoPath);
			Console.WriteLine("Edit XML: {0}", editPath);
			Console.WriteLine();

			List<string> imported = new List<string>();

			// Import single images
			SingleImportSp(baseDir, outDir, imported, filterDate);

			// Create a new XML document with data of the modime XMLs
			UpdateModimeXml(imported.ToArray(), infoPath, editPath);
		}

		private static void SingleImportSp(string baseDir, string outputDir,
			List<string> importedList, bool filterDate)
		{
            int count = 0;
			int errorsPack = 0;
			int errorsImgs = 0;
            Dictionary<string, SortedList<int, string>> spriteGroups = 
                new Dictionary<string, SortedList<int, string>>();

            Console.Write("Searching for images... ");
			foreach (string imgFile in Directory.GetFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = SpRegex.Match(imgFile);
				if (!match.Success)
					continue;

				// Get relative path
				string imagePath = match.Groups[1].Value;
                int imageIndex   = Convert.ToInt32(match.Groups[3].Value);
				string relative  = imagePath.Replace(baseDir, "");
				if (relative[0] == Path.DirectorySeparatorChar)
					relative = relative.Substring(1);

				if (!spriteGroups.ContainsKey(relative))
                    spriteGroups.Add(relative, new SortedList<int, string>());
                spriteGroups[relative].Add(imageIndex, imgFile);
                count++;
			}

            Console.WriteLine("Found {0} images", count);
            Console.WriteLine("Starting importing...");
			foreach (string relative in spriteGroups.Keys) {
				// Get output paths
				string original = Path.Combine(outputDir, relative) + ".n2d";
				string outFile  = Path.Combine(outputDir, relative) + "_new.n2d";
                string[] imgs   = spriteGroups[relative].Values.ToArray();

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outFile);
					if (!imgs.Any(f => File.GetLastWriteTime(f) > referenceDate)) {
						count -= imgs.Length;
						continue;
					}
				}

				// Check if it has been already imported
				if (importedList.Contains(relative)) {
					count -= imgs.Length;
					continue;
				}

                // If original file does not exist, skip
                // Odd way to import manually images and skip them here
				if (!File.Exists(original)) {
					count -= imgs.Length;
					continue;
				}

				// Try to import
                Console.Write("|-Importing {0,-45} {1,2} | ", relative, imgs.Length);
				try {
                    Npck ori  = new Npck(original);
                    Npck npck = NpckFactory.FromSpriteImage(imgs, spriteGroups[relative].Keys.ToArray(), ori);
                    npck.Write(outFile);

					npck.CloseAll();
                    ori.CloseAll();
				} catch (Exception ex) {
                    Console.WriteLine("Error: {0}", ex.Message);
                    #if DEBUG
                    Console.WriteLine(ex.ToString());
                    #endif
					errorsPack++;
					errorsImgs += imgs.Length;
                    count -= imgs.Length;
					continue;
				}

				importedList.Add(relative);
                Console.WriteLine("Successfully");
			}

            Console.WriteLine();
			Console.WriteLine("Errors in {0} packages ({1} images)", errorsPack, errorsImgs);
            Console.WriteLine("Imported {0} images successfully!", count);
		}

		private static void SearchAndImportBg(string baseDir, string outDir,
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
			MultiImportBg(baseDir, outDir, imported, multiXml, filterDate);

			// Then import other images
			SingleImportBg(baseDir, outDir, imported, filterDate);

			// Create a new XML document with data of the modime XMLs
			UpdateModimeXml(imported.ToArray(), infoPath, editPath);

			// Since here it's one image -> one pack, we can count them like that
			Console.WriteLine();
			Console.WriteLine("Imported {0} images successfully!", imported.Count);
		}

		private static void MultiImportBg(string baseDir, string outputDir, 
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
					imgPaths.Add(Path.Combine(baseDir, ximg.Value) + "_6.nscrm.png");
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

		private static void SingleImportBg(string baseDir, string outputDir,
			List<string> importedList, bool filterDate)
		{
			foreach (string imgFile in Directory.GetFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = BgRegex.Match(imgFile);
				if (!match.Success)
					continue;

				// Get paths
				string imagePath = match.Groups[1].Value;
				string relative  = imagePath.Replace(baseDir, "");
				if (relative[0] == Path.DirectorySeparatorChar)
					relative = relative.Substring(1);
				string oriFile = Path.Combine(outputDir, relative) + ".n2d";
				string outFile = Path.Combine(outputDir, relative) + "_new.n2d";

				// Check if it has been already imported
				if (importedList.Contains(relative))
					continue;

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outFile);
					if (File.GetLastWriteTime(imgFile) <= referenceDate)
						continue;
				}

				// Try to import
				Console.Write("|-Importing {0,-45} | ", relative);
				try {
					// Import with original palette and settings
					Npck original = new Npck(oriFile);
					Npck npck = NpckFactory.FromBackgroundImage(imgFile, original);
					npck.Write(outFile);

					original.CloseAll();
					npck.CloseAll();
				} catch (Exception ex) {
					Console.WriteLine("Error: {0}", ex.Message);
					#if DEBUG
					Console.WriteLine(ex.ToString());
					#endif
					continue;
				}

				importedList.Add(relative);
				Console.WriteLine("Successfully");
			}
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
