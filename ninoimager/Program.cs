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
		private static readonly Regex SpRegex = new Regex(@"(.+)_3\.ncer_(.+)m\.png$", RegexOptions.Compiled);

		public static void Main(string[] args)
		{
            string offset = "UI/Common/";
            args = new string[] {
                "-isp",
                "/home/benito/Dropbox/Ninokuni español/Imágenes/Originales definitivas/" + offset,
                "/home/benito/Dropbox/Ninokuni español/Imágenes/N2D/" + offset,
                "modime.xml"
            };

			Console.WriteLine("ninoimager ~~ Image importer and exporter for Ni no kuni DS");
			Console.WriteLine("V {0} ~~ by pleoNeX ~~", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine();

			Stopwatch watch = new Stopwatch();
			watch.Start();

			if ((args.Length == 4 || args.Length == 5) && args[0] == "-ibg")
				SearchAndImportBg(args[1], args[2], args[3], (args.Length == 5) ? args[4] : string.Empty);
			else if (args.Length == 4 && args[0] == "-isp")
				SearchAndImportSp(args[1], args[2], args[3]);
			else if (args.Length == 2 && args[0] == "-efr")
				SearchAndExportBg(args[1]);
			else
				Tests.RunTest(args);

			watch.Stop();
			Console.WriteLine();
			Console.WriteLine("It tooks: {0}", watch.Elapsed);
		}

		private static void SearchAndImportSp(string baseDir, string outDir, string modimeXml)
		{
			Console.WriteLine("@ Batch import");
			Console.WriteLine("From: {0}", baseDir);
			Console.WriteLine("To:   {0}", outDir);
			Console.WriteLine();

			List<string> imported = new List<string>();

			// Import single images
			SingleImportSp(baseDir, outDir, imported);

			// Create a new XML document with data of the modime XMLs
			CreateModimeXml(imported.ToArray(), modimeXml);
		}

		private static void SingleImportSp(string baseDir, string outputDir, List<string> importedList)
		{
            Dictionary<string, SortedList<int, string>> spriteGroups = 
                new Dictionary<string, SortedList<int, string>>();

			foreach (string imgFile in Directory.EnumerateFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = SpRegex.Match(imgFile);
				if (!match.Success)
					continue;

				// Get relative path
				string imagePath = match.Groups[1].Value;
                int imageIndex   = Convert.ToInt32(match.Groups[2].Value);
				string relative  = imagePath.Replace(baseDir, "");
				if (relative[0] == Path.DirectorySeparatorChar)
					relative = relative.Substring(1);

				if (!spriteGroups.ContainsKey(relative))
                    spriteGroups.Add(relative, new SortedList<int, string>());
                spriteGroups[relative].Add(imageIndex, imgFile);
			}

			foreach (string relative in spriteGroups.Keys) {
				// Get output paths
				string original = Path.Combine(outputDir, relative) + ".n2d";
				string outFile  = Path.Combine(outputDir, relative) + "_new.n2d";

				// Check if it has been already imported
				if (importedList.Contains(relative))
					continue;

				// Try to import
				try {
					Npck ori  = new Npck(original);
                    Npck npck = Npck.ImportSpriteImage(spriteGroups[relative].Values.ToArray(), ori);

					npck.Write(outFile);
					npck.CloseAll();
                    ori.CloseAll();
				} catch (Exception ex) {
					Console.WriteLine("## Error ## Importing:  {0}", relative);
					Console.WriteLine("\t" + ex.Message);
                    #if DEBUG
                    Console.WriteLine(ex.ToString());
                    #endif
					continue;
				}

				importedList.Add(relative);
				Console.WriteLine("Written with success... {0}", relative);
			}
		}

		private static void SearchAndImportBg(string baseDir, string outDir, string modimeXml, string multiXml)
		{
			Console.WriteLine("@ Batch import");
			Console.WriteLine("From: {0}", baseDir);
			Console.WriteLine("To:   {0}", outDir);
			Console.WriteLine("Modime XML:       {0}", modimeXml);
			Console.WriteLine("MultiImport from: {0}", multiXml);
			Console.WriteLine();

			List<string> imported = new List<string>();

			// First import "special files" that share palette and images data with other N2D files
			MultiImportBg(baseDir, outDir, imported, multiXml);

			// Then import other images
			SingleImportBg(baseDir, outDir, imported);

			// Create a new XML document with data of the modime XMLs
			CreateModimeXml(imported.ToArray(), modimeXml);
		}

		private static void MultiImportBg(string baseDir, string outputDir, List<string> importedList, string xml)
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

				// Import
				Npck originalPack = new Npck(outPaths[0] + ".n2d");
				Npck[] packs = null;
				if (mode == "SharePalette")
					packs = Npck.ImportBackgroundImageSharePalette(imgPaths.ToArray(), originalPack);
				else if (mode == "ShareImage")
					packs = Npck.ImportBackgroundImageShareImage(imgPaths.ToArray(), originalPack);
				else
					throw new FormatException(string.Format("Unsopported mode \"{0}\"", mode)); 

				// Write output
				originalPack.CloseAll();
				for (int i = 0; i < outPaths.Count; i++) {
					packs[i].Write(outPaths[i] + "_new.n2d");
					packs[i].CloseAll();
				}

				Console.WriteLine("MultiImport done!");
				foreach (string image in relatives) {
					importedList.Add(image);
					Console.WriteLine("\tWritten with success... {0}", image);
				}
			}
		}

		private static void SingleImportBg(string baseDir, string outputDir, List<string> importedList)
		{
			foreach (string imgFile in Directory.EnumerateFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
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

				// Try to import
				try {
					// Import with original palette and settings
					Npck original = new Npck(oriFile);
					Npck npck = Npck.ImportBackgroundImage(imgFile, original);

					npck.Write(outFile);

					original.CloseAll();
					npck.CloseAll();
				} catch (Exception ex) {
					Console.WriteLine("## Error ## Importing:  {0}", relative);
					Console.WriteLine("\t" + ex.Message);
					continue;
				}

				importedList.Add(relative);
				Console.WriteLine("Written with success... {0}", relative);
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

		private static void SearchAndExportBg(string baseDir)
		{
			foreach (string file in Directory.EnumerateFiles(baseDir, "*.n2d", SearchOption.AllDirectories)) {	
				string relativePath = file.Replace(baseDir, "");
				string imageName = Path.GetRandomFileName().Substring(0, 8);
				string imagePath = Path.Combine(baseDir, imageName + ".png");

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
