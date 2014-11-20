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
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

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

			if (command == "-etx" && args.Length == 3)
				ExportTexture(baseDir, outputDir);

			// Four argument commands
			if (args.Length < 4)
				return;

			if (command == "-etxcolor" && args.Length == 4)
				ExportTextureWithColors(baseDir, outputDir, args[3]);

			if (command == "-stxpal" && args.Length == 4)
				SearchPalette(args[1], args[2], Convert.ToInt32(args[3]));

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

			List<ImageInfo> imported = new List<ImageInfo>();

			// "Fix" paths
			if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar)
				baseDir += Path.DirectorySeparatorChar;

			if (outDir[outDir.Length - 1] != Path.DirectorySeparatorChar)
				outDir += Path.DirectorySeparatorChar;

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
			List<ImageInfo> importedList, string xml, bool filterDate)
		{
			if (string.IsNullOrEmpty(xml))
				return;

			XDocument doc = XDocument.Load(xml);
			foreach (XElement entry in doc.Root.Elements("Pack")) {
				// Get mode
				string mode = entry.Element("Mode").Value;

				// Get paths
				bool existImages = true;
				List<string> images   = new List<string>();
				List<ImageInfo> infos = new List<ImageInfo>();
				foreach (XElement ximg in entry.Element("Images").Elements("Image")) {
					string frame = (ximg.Attribute("frame") != null) ? 
						"_" + ximg.Attribute("frame").Value : "" ;

					ImageInfo info = new ImageInfo();
					info.AbsoluteImage = Path.Combine(baseDir, ximg.Value) + frame + M + ".png";
					info.Filename      = Path.GetFileNameWithoutExtension(ximg.Value);
					info.PackExtension = (mode != "TextureWithPalette") ? ".n2d" : ".n3d";
					info.RelativePath  = Path.GetDirectoryName(ximg.Value) + Path.DirectorySeparatorChar;
					infos.Add(info);

					images.Add(info.AbsoluteImage);

					if (!File.Exists(info.AbsoluteImage)) {
						existImages = false;
						break;
					}
				}

				// Import
				Console.Write("|-Importing {0,-45} | ", infos[0].RelativeImage);
				for (int i = 1; i < infos.Count; i++) {
					Console.WriteLine();
					Console.Write("            {0,-45} | ", infos[i].RelativeImage);
				}

				// Images still not translated
				if (!existImages) {
					Console.WriteLine("Skipped: Images not found");
					continue;
				}

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outputDir + infos[0].RelativeNewPack);
					if (!images.Any(f => File.GetLastWriteTime(f) > referenceDate)) {
						Console.WriteLine("Skipped: date filter");
						continue;
					}
				}

				Npck originalPack = new Npck(outputDir + infos[0].RelativePack);
				Npck[] packs = null;
				if (mode == "SharePalette")
					packs = NpckFactory.FromBackgroundImageSharePalette(images.ToArray(), originalPack);
				else if (mode == "ShareImage")
					packs = NpckFactory.FromBackgroundImageShareImage(images.ToArray(), originalPack);
				else if (mode == "SharePaletteChangeDepth")
					packs = NpckFactory.FromBackgroundImageSharePaletteChangeDepth(images.ToArray(), originalPack, true);
				else if (mode == "TextureWithPalette") {
					// Get frames
					string frame = entry.Element("Images").Elements("Image").First().Attribute("frame").Value;
					List<int> frames = new List<int>() { Convert.ToInt32(frame) };

					// Create palette
					XElement[] xcolors = entry.Element("Palette").Elements("Color").ToArray();
					Color[] colors = new Color[xcolors.Length];
					for (int i = 0; i < colors.Length; i++) {
						colors[i] = new Color();
						colors[i].Red   = Convert.ToInt32(xcolors[i].Attribute("red").Value);
						colors[i].Green = Convert.ToInt32(xcolors[i].Attribute("green").Value);
						colors[i].Blue  = Convert.ToInt32(xcolors[i].Attribute("blue").Value);
					}
					Palette palette = new Palette(colors);

					// Generate pack file
					packs = new Npck[1];
					packs[0] = NpckFactory.ChangeTextureImages(images.ToArray(), frames.ToArray(), palette, originalPack);
				} else
					throw new FormatException(string.Format("Unsopported mode \"{0}\"", mode)); 

				// Write output
				originalPack.CloseAll();
				for (int i = 0; i < infos.Count; i++) {
					packs[i].Write(outputDir + infos[i].RelativeNewPack);
					packs[i].CloseAll();
				}

				importedList.AddRange(infos);
				Console.WriteLine("Successfully");
			}
		}
			
		private static void SingleImport(string imgDir, string outputDir,
			List<ImageInfo> importedList, bool filterDate)
		{
			int count = 0;
			Dictionary<string, int> errors = new Dictionary<string, int>();
			errors.Add("errorPack",  0); 	errors.Add("errorImgs",  0);
			errors.Add("noN2DPack",  0);	errors.Add("noN2DImgs",  0);
			errors.Add("noModPack",  0);	errors.Add("noModImgs",  0);
			errors.Add("inListPack", 0);	errors.Add("inListImgs", 0);
			errors.Add("noSuffPack", 0);    errors.Add("noSuffImgs", 0);

			// Search image: Group of images with same prefix ordered by frame index.
			Dictionary<string, SortedList<int, ImageInfo>> imageGroups =
				SearchImages(imgDir, outputDir);

			// Import!
			Console.WriteLine("Starting importing...");
			foreach (string relative in imageGroups.Keys) {
				// Get paths
				IList<ImageInfo> infos = imageGroups[relative].Values;
				string outFile = outputDir + infos[0].RelativeNewPack;
				string oriFile = outputDir + infos[0].RelativePack;
				string[] imgs  = infos.Select(i => i.AbsoluteImage).ToArray();
				int[] frameIdx = infos.Select(i => i.FrameIndex).ToArray();
				//string[] internalNames = infos.Select(i => i.InternalName).ToArray();

				// If don't match the filter, skip
				if (filterDate) {
					DateTime referenceDate = File.GetLastWriteTime(outFile);
					if (!imgs.Any(f => File.GetLastWriteTime(f) > referenceDate)) {
						Console.WriteLine("|+ Skipped (date filter) {0}", relative);
						errors["noModPack"]++;
						errors["noModImgs"] += imgs.Length;
						continue;
					}
				}

				// Check if it has been already imported
				if (importedList.Any(i => i.RelativeImage == relative)) {
					Console.WriteLine("|+ Skipped (already imported) {0}", relative);
					errors["inListPack"]++;
					errors["inListImgs"] += imgs.Length;
					continue;
				}

				// If original file does not exist, skip
				// Odd way to import manually images and skip them here
				if (!File.Exists(oriFile) && File.Exists(outFile)) {
					Console.WriteLine("|+ Skipped (manual mode) {0}", relative);
					errors["noN2DPack"]++;
					errors["noN2DImgs"] += imgs.Length;
					continue;
				}

				// If the original file does not exists AND there isn't any manual import
				if (!File.Exists(oriFile)) {
					Console.WriteLine("|+ Skipped (invalid suffix) {0}", relative);
					errors["noSuffPack"]++;
					errors["noSuffImgs"] += imgs.Length;
					continue;
				}

				// Try to import
				Console.Write("|-Importing {0,-45} {1,2} | ", relative, imgs.Length);
				try {
					Npck ori  = new Npck(oriFile);
					Npck npck;
					if (ori.IsSprite)
						npck = NpckFactory.FromSpriteImage(imgs, frameIdx, ori);
					else if (ori.IsBackground && imgs.Length == 1)
						npck = NpckFactory.FromBackgroundImage(imgs[0], ori);
					else if (ori.IsTexture)
						npck = NpckFactory.ChangeTextureImages(imgs, frameIdx, ori);
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
					continue;
				}

				count += imgs.Length;
				importedList.AddRange(infos);
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

		private static Dictionary<string, SortedList<int, ImageInfo>> SearchImages(
			string imgDir, string outputDir)
		{
			int count   = 0;
			int skipped = 0;
			Dictionary<string, SortedList<int, ImageInfo>> imageGroups = 
				new Dictionary<string, SortedList<int, ImageInfo>>();

			// Select regular expression by platform since they are pre-compiled.
			PlatformID osId = Environment.OSVersion.Platform;
			Regex regex = (osId == PlatformID.Unix || osId == PlatformID.MacOSX) ? RegexLin : RegexWin;

			Console.Write("Searching for images... ");
			foreach (string imgFile in Directory.GetFiles(imgDir, "*.png", SearchOption.AllDirectories)) {
				Match match = regex.Match(imgFile);
				if (!match.Success)
					continue;

				// Gets file info
				ImageInfo info = new ImageInfo();
				info.Matched       = false;
				info.Filename      = match.Groups[2].Value;
				info.PackExtension = ".n2d";
				info.FrameIndex    = 0;
				info.AbsoluteImage = imgFile;
				info.InternalName  = info.Filename;
				info.RelativePath  = match.Groups[1].Value + Path.DirectorySeparatorChar;
				info.RelativePath  = info.RelativePath.Replace(imgDir, "");
				if (info.RelativePath[0] == Path.DirectorySeparatorChar)
					info.RelativePath = info.RelativePath.Substring(1);

				// Dectect texture
				string[] fields = info.Filename.Split('_');
				if (fields.Count() >= 2) {
					// Get texture name (find one last '_' chars)
					int nameIdx = 0;
					for (int i = 0; i < fields.Count() - 1; i++)
						nameIdx = info.Filename.IndexOf('_', nameIdx + 1);

					// Check if it exists a N3D with the texture name
					string texName = info.Filename.Substring(0, nameIdx);
					if (File.Exists(outputDir + info.RelativePath + texName + ".n3d")) {
						try {
							//info.InternalName = fields[fields.Count() - 2];
							info.FrameIndex = Convert.ToInt32(fields[fields.Count() - 1]);
							info.Matched    = true;
							info.Filename   = texName;
							info.PackExtension = ".n3d";
						} catch {
							Console.WriteLine("\t\t{0} skipped", imgFile);
							skipped++;
							continue;
						}
					}
				}

				// If it matches old format (suffix '_6.nscr' and '_3.ncer_X')
				if (!info.Matched && info.Filename.EndsWith("_6.nscr")) {
					info.Matched  = true;
					info.Filename = info.Filename.Substring(0, info.Filename.Length - 7);
				} else if (!info.Matched && info.Filename.Contains("_3.ncer_")) {
					try {
						int nameIdx = info.Filename.IndexOf("_3.ncer_");
						info.FrameIndex = Convert.ToInt32(info.Filename.Substring(nameIdx + 8));
						info.Filename   = info.Filename.Substring(0, nameIdx);
						info.Matched = true;
					} catch {
						Console.WriteLine("\t\t{0} skipped", imgFile);
						skipped++;
						continue;
					}
				}

				// Else, the new format does not have the suffixes
				// If the .n2d file exists with that file name we got it
				if (!info.Matched && File.Exists(outputDir + info.RelativePack)) {
					// It's a background image
					info.Matched = true;
				} else if (!info.Matched && info.Filename.Contains("_")) {
					// It's a sprite image
					try {
						int nameIdx = info.Filename.LastIndexOf("_");
						info.FrameIndex = Convert.ToInt32(info.Filename.Substring(nameIdx + 1));
						info.Filename   = info.Filename.Substring(0, nameIdx);
						//if (File.Exists(outputDir + info.RelativePack))
						info.Matched = true;
					} catch {
						Console.WriteLine("\t\t{0} skipped", imgFile);
						skipped++;
						continue;
					}
				}

				if (info.Matched) {
					if (!imageGroups.ContainsKey(info.RelativeImage))
						imageGroups.Add(info.RelativeImage, new SortedList<int, ImageInfo>());
					imageGroups[info.RelativeImage].Add(info.FrameIndex, info);
					count++;
				} else {
					Console.WriteLine("\t\t{0} skipped", imgFile);
					skipped++;
				}
			}

			Console.WriteLine("\tFound {0} images in {1} groups", count, imageGroups.Count);
			Console.WriteLine("\t\t\t\tSkipped {0} images", skipped);

			return imageGroups;
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

		private static void UpdateModimeXml(ImageInfo[] relativePaths, string infoPath, string editPath)
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
			foreach (ImageInfo info in relativePaths) {
				string path = BaseRomPath + info.RelativePack;
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
					xgame.Add(new XElement("Import", "{$ImagePath}/" + info.RelativeNewPack.Replace("\\", "/")));
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

		private static void ExportTexture(string n3dPath, string outPath)
		{
			string filename = Path.GetFileNameWithoutExtension(n3dPath);

			// Get texture file
			Npck pack = new Npck(n3dPath);
			Btx0 texture = new Btx0(pack[0]);

			// Export images and palettes
			for (int i = 0; i < texture.NumTextures; i++) {
				string name = filename + "_" + i.ToString();
				string path = Path.Combine(outPath, name);

				texture.CreateBitmap(i).Save(path + ".png");
				texture.GetPalette(i).ToWinPaletteFormat(path + "_gimp.pal", 0, true);
				texture.GetPalette(i).ToWinPaletteFormat(path + ".pal", 0, false);
				texture.GetPalette(i).ToAcoFormat(path + ".aco", 0);
			}
		}

		private static void ExportTextureWithColors(string n3dPath, string outPath, string colors)
		{
			string name = Path.GetFileNameWithoutExtension(n3dPath);
			string path = Path.Combine(outPath, name);

			// Get texture file
			Npck pack = new Npck(n3dPath);
			Btx0 texture = new Btx0(pack[0]);

			// Parse colors
			string[] strColors = colors.Split(' ');
			Color[] newColors = new Color[strColors.Length];
			for (int i = 0; i < strColors.Length; i++) {
				int hexColor = Convert.ToInt32(strColors[i], 16);
				newColors[i] = new Color();
				newColors[i].Alpha = 255;
				newColors[i].Red   = (hexColor >> 00) & 0xFF;
				newColors[i].Green = (hexColor >> 08) & 0xFF;
				newColors[i].Blue  = (hexColor >> 16) & 0xFF;
			}

			// Create and export palette
			Palette palette = new Palette(newColors);
			palette.ToWinPaletteFormat(path + "_palGimp.pal", 0, true);
			palette.ToWinPaletteFormat(path + "_pal.pal", 0, false);
			palette.CreateBitmap(0).Save(path + "_pal.png");

			// For each image, set new palette and export it
			for (int i = 0; i < texture.NumTextures; i++)
				texture.CreateBitmap(i, palette).Save(path + "_" + i.ToString() + ".png");
		}

		private static void SearchPalette(string packPath, string imgPath, int idx)
		{
			// Get new image
			EmguImage newImg = new EmguImage(imgPath);

			// Get original image
			Npck pack = new Npck(packPath);
			Btx0 texture = new Btx0(pack[0]);
			Image originalImg = texture.GetImage(idx);
			Pixel[] pixels = originalImg.GetPixels();

			// For each pixel, set palette color in the position given by original image
			Color[] palette = new Color[originalImg.Format.MaxColors()];
			for (int y = 0; y < newImg.Height; y++) {
				for (int x = 0; x < newImg.Width; x++) {
					// Get target color
					Color px = newImg[y, x];

					// Get palette color index
					uint index = pixels[y * newImg.Width + x].Info;

					// If we have already set this color, and it does not match with
					// this pixel... Error!
					if (palette[index].Alpha != 0 && !palette[index].Equals(px)) {
						Console.WriteLine("Can not find a valid color combination");
						return;
					}

					// If the color has not been set, set it!
					if (palette[index].Alpha == 0)
						palette[index] = px;
				}
			}

			// Print palette
			Console.WriteLine("Palette found");
			string xmlColor = "          <Color red=\"{0}\" green=\"{1}\" blue=\"{2}\" />";
			foreach (Color c in palette)
				Console.WriteLine(xmlColor, c.Red, c.Green, c.Blue);
		}

		private struct ImageInfo
		{
			public bool   Matched { get; set; }
			public string RelativePath  { get; set; }
			public string Filename      { get; set; }
			public string PackExtension { get; set; }
			public int    FrameIndex    { get; set; }
			public string InternalName  { get; set; }
			public string AbsoluteImage { get; set; }
			public string RelativeImage { get { return RelativePath + Filename; } }
			public string RelativePack  { get { return RelativePath + Filename + PackExtension; }}
			public string RelativeNewPack { get { return RelativePath + Filename + "_new" + PackExtension; }}
		}
	}
}
