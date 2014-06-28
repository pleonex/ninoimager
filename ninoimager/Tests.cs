// -----------------------------------------------------------------------
// <copyright file="Tests.cs" company="none">
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
// <date>21/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Ninoimager.Format;
using Ninoimager.ImageProcessing;
using Emgu.CV.Structure;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;
using Color     = Emgu.CV.Structure.Bgra;

namespace Ninoimager
{
	public static class Tests
	{
		public static void RunTest(string[] args)
		{
			string pathFile = "/home/benito/tests/";
            /*args = new string[] {
                "-tis",
                pathFile + "button_K_3.ncer",
                pathFile + "button_K_1.ncgr",
                pathFile + "button_K_0.nclr"
            };*/
			/*args = new string[] {
				"-tis",
				pathFile + "ug_hero.NCER",
				pathFile + "ug_boygirl.NCGR",
				pathFile + "ug_boygirl.NCLR"
			};*/
			/*args = new string[] {
				"-tis",
				pathFile + "m2d_ability_menu_obj_00.NCER",
				pathFile + "m2d_ability_menu_obj_00.NCGR.lz",
				pathFile + "m2d_common.NCLR"
			};*/
			/*args = new string[] {
				"-tis",
				pathFile + "c2d_99_00_11.NCER",
				pathFile + "c2d_99_00_11.NCGR.lz",
				pathFile + "m2d_common.NCLR"
			};*/
            /*args = new string[] {
				"-tis",
				pathFile + "titlebutton_3.ncer",
				pathFile + "titlebutton_1.ncgr",
				pathFile + "titlebutton_0.nclr"
            };*/
			args = new string[] {
				"-tis",
				pathFile + "CESA02_3.ncer",
				pathFile + "CESA02_1.ncgr",
				pathFile + "CESA02_0.nclr"
			};

			if (args.Length < 3)
				return;

			// Nitro files tests
			if (args[0] == "-t1")
				PaletteInfo(args[1], args[2]);
			else if (args[0] == "-t2" && args.Length == 4)
				ImageInfo(args[1], args[2], args[3]);
			else if (args[0] == "-t3" && args.Length == 5)
				MapInfo(args[1], args[2], args[3], args[4]);
			else if (args[0] == "-t4" && args.Length == 5)
				SpriteInfo(args[1], args[2], args[3], args[4]);
			else if (args[0] == "-c1")
				TestReadWriteFormat(args[1], args[2]);
			else if (args[0] == "-s1" && args.Length == 4)
				SearchVersion(args[1], args[2], args[3]);
			else if (args[0] == "-ss")
				SpecificSearch(args[1], args[2]);
			else if (args[0] == "-p1")
				SelectImagesFiles(args[1], args[2]);
			else if (args[0] == "-tib" && args.Length == 4)
				ImportTestBackground(args[1], args[2], args[3]);
			else if (args[0] == "-tis" && args.Length == 4)
				ImportTestSprite(args[1], args[2], args[3]);
			else if (args[0] == "-pe")
				ExtractPack(args[1], args[2]);
			else if (args[0] == "-pi")
				ImportPack(args[1], args[2]);
			else if (args[0] == "-tc")
				TestConvertColors(args[1], args[2]);
			else
				Console.WriteLine("Invalid program arguments");
		}

		private static void SpecificSearch(string dir, string format)
		{
			Type type = Type.GetType(format, true, false);

			// Log into a file
			StreamWriter writer = File.CreateText("log.txt");
			writer.AutoFlush = true;

			StringBuilder sb = new StringBuilder();
			TextWriter tw = new StringWriter(sb);
			Console.SetOut(tw);

			foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
				try {
					Activator.CreateInstance(type, file);	// Read file
				} catch (Exception ex) {
					Console.WriteLine("ERROR on file: {0}", file);
					Console.WriteLine(ex.ToString());
				}

				writer.WriteLine("# {0}:", file);
				writer.WriteLine(sb.ToString());
				sb.Clear();
			}

			writer.Flush();
			writer.Close();
		}

		private static void TestReadWriteFormat(string dir, string format)
		{
			Type type = Type.GetType(format, true, false);
			MethodInfo writeMethod = type.GetMethod("Write", new Type[] { typeof(Stream) });
			if (writeMethod == null)
				throw new Exception("Invalid test");

			foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
				FileStream fs = new FileStream(file, FileMode.Open);
				MemoryStream ms = new MemoryStream();
				Object obj = null;
				try {
					obj = Activator.CreateInstance(type, fs);		// Constructor -> Read
					writeMethod.Invoke(obj, new object[] { ms });	// Write

					if (!Compare(fs, ms)) {
						Console.WriteLine("Different files! -> {0}", file);
					}

				} catch (Exception ex) {
					Console.WriteLine("ERROR on file: {0}", file);
					Console.WriteLine("{0}", ex.ToString());
					Console.ReadKey(true);
				} finally {
					fs.Close();
					ms.Close();
				}
			}
		}

		private static void SearchVersion(string dir, string format, string version)
		{
			Type type = Type.GetType(format, true, false);
			PropertyInfo nitroProp = type.GetProperty("NitroData");

			foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
				try {
					Object obj = Activator.CreateInstance(type, file);
					string objVersion = ((NitroFile)nitroProp.GetValue(obj, null)).VersionS;
					if (objVersion == version)
						Console.WriteLine("* {0} -> {1}", objVersion, file);
				} catch (Exception ex) {
					Console.WriteLine("ERROR at {0}", file);
					Console.WriteLine(ex.ToString());
					Console.ReadKey(true);
					Console.WriteLine();
				}
			}
		}

		private static void SelectImagesFiles(string indir, string outDir)
		{
			const int FilesPerType = 10000;
			Dictionary<string, List<string>> types = new Dictionary<string, List<string>>();
			types.Add("Error", new List<string>());
			types.Add("Register", new List<string>());
			types.Add("Unknown", new List<string>());

			// Log into a file
			StreamWriter writer = File.CreateText(Path.Combine(outDir, "log.txt"));
			writer.AutoFlush = true;

			foreach (string file in Directory.GetFiles(indir, "*.*", SearchOption.AllDirectories)) {
				Ncgr ncgr = null;

				// Check for error
				try { ncgr = new Ncgr(file); }
				catch (Exception ex) {
					if (types["Error"].Count < FilesPerType) {
						writer.WriteLine("Error: " + file);
						writer.WriteLine(ex.ToString());
						types["Error"].Add(file);
					}
					continue;
				}

				// Check for unknown1
				if (ncgr.RegDispcnt != 0) {
					if (types["Register"].Count < FilesPerType) {
						writer.WriteLine("Register: " + file);
						types["Register"].Add(file);
					}
				}

				if ((ncgr.Unknown >> 8) != 0) {
					if (types["Unknown"].Count < FilesPerType) {
						writer.WriteLine("Unknown: " + file);
						types["Unknown"].Add(file);
					}
				}


				// Have we got all the files already?
				bool finished = true;
				foreach (string key in types.Keys) {
					if (types[key].Count < FilesPerType)
						finished = false;
				}
				if (finished)
					break;
			}

			writer.Flush();
			writer.Close();

			// Copy selected files
			foreach (string key in types.Keys) {
				string dir = Path.Combine(outDir, key);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				foreach (string file in types[key]) {
					string copyFile = Path.Combine(dir, Path.GetFileName(file));
					if (File.Exists(copyFile))
						copyFile += Path.GetRandomFileName();
					File.Copy(file, copyFile);
				}
			}
		}

		private static void PaletteInfo(string file, string outputDir)
		{
			Console.WriteLine("Reading {0} as NCLR palette...", file);
			Nclr palette = new Nclr(file);

			Console.WriteLine("\t* Version:               {0}", palette.NitroData.VersionS);
			Console.WriteLine("\t* Contains PCMP section: {0}", palette.NitroData.Blocks.ContainsType("PCMP"));
			Console.WriteLine("\t* Number of palettes:    {0}", palette.NumPalettes);

			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);
			for (int i = 0; i < palette.NumPalettes; i++) {
				Console.WriteLine("\t+ Palette {0}: {1} colors", i, palette.GetPalette(i).Length);

				if (!string.IsNullOrEmpty(outputDir)) {
					string outputFile = Path.Combine(outputDir, "Palette" + i.ToString() + ".png");
					if (File.Exists(outputFile))
						File.Delete(outputFile);
					palette.CreateBitmap(i).Save(outputFile);
				}
			}
		}

		private static void ImageInfo(string imgFile, string palFile, string outputFile)
		{
			Console.WriteLine("Reading {0} as NCLR palette...", palFile);
			Nclr palette = new Nclr(palFile);

			Console.WriteLine("Reading {0} as NCGR image...", imgFile);
			Ncgr image = new Ncgr(imgFile);

			Console.WriteLine("\t* Version:               {0}", image.NitroData.VersionS);
			Console.WriteLine("\t* Contains CPOS section: {0}", image.NitroData.Blocks.ContainsType("CPOS"));
			Console.WriteLine("\t* Height:                {0}", image.Height);
			Console.WriteLine("\t* Width:                 {0}", image.Width);
			Console.WriteLine("\t* Format:                {0}", image.Format);
			Console.WriteLine("\t* Pixel encoding:        {0}", image.PixelEncoding);

			image.CreateBitmap(palette, 0).Save(outputFile);
		}

		private static void MapInfo(string mapFile, string imgFile, string palFile, string outputFile)
		{
			Console.WriteLine("Reading {0} as NCLR palette...", palFile);
			Nclr palette = new Nclr(palFile);

			Console.WriteLine("Reading {0} as NCGR image...", imgFile);
			Ncgr image = new Ncgr(imgFile);

			Console.WriteLine("Reading {0} as NSCR map...", mapFile);
			Nscr map = new Nscr(mapFile);

			Console.WriteLine("\t* Version:      {0}", map.NitroData.VersionS);
			Console.WriteLine("\t* Height:       {0}", map.Height);
			Console.WriteLine("\t* Width:        {0}", map.Width);
			Console.WriteLine("\t* BG Mode:      {0}", map.BgMode);
			Console.WriteLine("\t* Palette Mode: {0}", map.PaletteMode);

			map.CreateBitmap(image, palette).Save(outputFile);
		}

		private static void SpriteInfo(string spriteFile, string imgFile, string palFile, string outputDir)
		{
			Console.WriteLine("Reading {0} as NCLR palette...", palFile);
			Nclr palette = new Nclr(palFile);

			Console.WriteLine("Reading {0} as NCGR image...", imgFile);
			Ncgr image = new Ncgr(imgFile);

			Console.WriteLine("Reading {0} as NCER sprite...", spriteFile);
			Ncer sprite = new Ncer(spriteFile);

			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			for (int i = 0; i < sprite.NumFrames; i++) {
				if (!string.IsNullOrEmpty(outputDir)) {
					string outputFile = Path.Combine(outputDir, "Sprite" + i.ToString() + ".png");
					if (File.Exists(outputFile))
						File.Delete(outputFile);
					sprite.CreateBitmap(i, image, palette).Save(outputFile);
				}
			}
		}

		private static void ExtractPack(string packFile, string outputImage)
		{
			Npck npck = new Npck(packFile);
			npck.GetBackgroundImage().Save(outputImage);
		}

		private static void ImportPack(string inputImage, string outputPack)
		{
			Npck npck = Npck.ImportBackgroundImage(inputImage);
			npck.Write(outputPack);
		}

		private static void ImportTestBackground(string mapFile, string imgFile, string palFile)
		{
			FileStream   oldPalStr = new FileStream(palFile, FileMode.Open);
			FileStream   oldImgStr = new FileStream(imgFile, FileMode.Open);
			FileStream   oldMapStr = new FileStream(mapFile, FileMode.Open);
			MemoryStream newPalStr = new MemoryStream();
			MemoryStream newImgStr = new MemoryStream();
			MemoryStream newMapStr = new MemoryStream();

			Nclr nclr = new Nclr(oldPalStr);
			Ncgr ncgr = new Ncgr(oldImgStr);
			Nscr nscr = new Nscr(oldMapStr);
			EmguImage bmp = nscr.CreateBitmap(ncgr, nclr);
			bmp.Save(mapFile + ".png");

			BackgroundImporter importer = new BackgroundImporter();
			importer.ImportBackground(bmp, newMapStr, newImgStr, newPalStr);

			if (!Compare(oldPalStr, newPalStr)) {
				string newPalFile = palFile + ".new";
				WriteStream(newPalFile, newPalStr);
				Console.WriteLine("Palette different... Written to {0}", newPalFile);
			}
			if (!Compare(oldImgStr, newImgStr)) {
				string newImgFile = imgFile + ".new";
				WriteStream(newImgFile, newImgStr);
				Console.WriteLine("Image different...   Written to {0}", newImgFile);
			}
			if (!Compare(oldMapStr, newMapStr)) {
				string newMapFile = mapFile + ".new";
				WriteStream(newMapFile, newMapStr);
				Console.WriteLine("Map different...     Written to {0}", newMapFile);
			}

			newPalStr.Position = newImgStr.Position = newMapStr.Position = 0;
			nclr = new Nclr(newPalStr);
			ncgr = new Ncgr(newImgStr);
			nscr = new Nscr(newMapStr);
			nscr.CreateBitmap(ncgr, nclr).Save(mapFile + "m.png");

			oldPalStr.Close();
			oldImgStr.Close();
			oldMapStr.Close();
			newPalStr.Close();
			newImgStr.Close();
			newMapStr.Close();
		}

		private static void ImportTestSprite(string sprFile, string imgFile, string palFile)
		{
			FileStream   oldPalStr = new FileStream(palFile, FileMode.Open);
			FileStream   oldImgStr = new FileStream(imgFile, FileMode.Open);
			FileStream   oldSprStr = new FileStream(sprFile, FileMode.Open);
			MemoryStream newPalStr = new MemoryStream();
            MemoryStream newImgLinealStr = new MemoryStream();
            MemoryStream newImgTiledStr = new MemoryStream();
            MemoryStream newSprStr = new MemoryStream();

			Nclr nclr = new Nclr(oldPalStr);
			Ncgr ncgr = new Ncgr(oldImgStr);
			Ncer ncer = new Ncer(oldSprStr);

			SpriteImporter importer = new SpriteImporter();
			importer.Format = ColorFormat.Indexed_4bpp;
			importer.ObjectMode    = ObjMode.Normal;
			importer.PaletteMode   = PaletteMode.Palette16_16;
			importer.TileSize      = new System.Drawing.Size(64, 64);
			importer.TransparentColor = new Color(128, 0, 128, 255);
			importer.Quantization     = new NdsQuantization() { 
				BackdropColor = importer.TransparentColor,
				Format = ColorFormat.Indexed_4bpp
			};
			importer.Reducer  = new SimilarDistanceReducer();
			importer.Splitter = new NdsSplitter(1);

			for (int i = 0; i < ncer.NumFrames; i++) {
				EmguImage bmp = ncer.CreateBitmap(i, ncgr, nclr);
				bmp.Save(sprFile + i.ToString() + ".png");
				importer.AddFrame(bmp);
			}

            importer.Generate(newPalStr, newImgLinealStr, newImgTiledStr, newSprStr);

			/*
			if (!Compare(oldPalStr, newPalStr)) {
				string newPalFile = palFile + ".new";
				WriteStream(newPalFile, newPalStr);
				Console.WriteLine("Palette different... Written to {0}", newPalFile);
			}
			if (!Compare(oldImgStr, newImgStr)) {
				string newImgFile = imgFile + ".new";
				WriteStream(newImgFile, newImgStr);
				Console.WriteLine("Image different...   Written to {0}", newImgFile);
			}
			if (!Compare(oldSprStr, newSprStr)) {
				string newSprFile = sprFile + ".new";
				WriteStream(newSprFile, newSprStr);
				Console.WriteLine("Sprite different...  Written to {0}", newSprFile);
			}
			*/

            newPalStr.Position = newImgLinealStr.Position = newImgTiledStr.Position = newSprStr.Position = 0;
			nclr = new Nclr(newPalStr);
            ncgr = new Ncgr(newImgTiledStr);
			ncer = new Ncer(newSprStr);
			for (int i = 0; i < ncer.NumFrames; i++)
				ncer.CreateBitmap(i, ncgr, nclr).Save(sprFile + i.ToString() + "m.png");

			oldPalStr.Close();
			oldImgStr.Close();
			oldSprStr.Close();
			newPalStr.Close();
            newImgTiledStr.Close();
            newImgLinealStr.Close();
			newSprStr.Close();
		}

		private static void TestConvertColors(string inputImage, string outputImage)
		{
			// Get colors of the input image
			var img = new Emgu.CV.Image<Bgr, byte>(inputImage);
			Bgr[] colors = new Bgr[img.Width * img.Height];
			for (int x = 0; x < img.Width; x++)
				for (int y = 0; y < img.Height; y++)
					colors[y * img.Width + x] = img[y, x];

			// Convert
			Lab[] newColors  = ColorConversion.ConvertColors<Bgr, Lab>(colors);
			Bgr[] newColors2 = ColorConversion.ConvertColors<Lab, Bgr>(newColors);

			// Set colors of output image
			var img2 = new Emgu.CV.Image<Bgr, byte>(img.Width, img.Height);
			for (int x = 0; x < img2.Width; x++)
				for (int y = 0; y < img2.Height; y++)
					img2[y, x] = newColors2[y * img2.Width + x];

			img2.Save(outputImage);
		}

		private static void WriteStream(string path, MemoryStream data)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			data.WriteTo(fs);
			fs.Flush();
			fs.Close();
		}

		private static bool Compare(Stream str1, Stream str2)
		{
			if (str1.Length != str2.Length)
				return false;

			str1.Position = str2.Position = 0;
			while (str1.Position != str1.Length) {
				if (str1.ReadByte() != str2.ReadByte())
					return false;
			}

			return true;
		}

	}
}

