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
using System.IO;
using System.Reflection;
using System.Text;
using Ninoimager.Format;

namespace Ninoimager
{
	public static class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("ninoimager ~~ Image importer and exporter for Ni no kuni DS");
			Console.WriteLine("V {0} ~~ by pleoNeX ~~", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine();

			//args = new string[] { "-ti", "title_logo_6.nscr", "title_logo_1.ncgr", "title_logo_0.nclr" };

			if (args.Length < 3)
				return;

			// Nitro files tests
			if (args[0] == "-t1")
				PaletteInfo(args[1], args[2]);
			else if (args[0] == "-t2" && args.Length == 4)
				ImageInfo(args[1], args[2], args[3]);
			else if (args[0] == "-t3" && args.Length == 5)
				MapInfo(args[1], args[2], args[3], args[4]);
			else if (args[0] == "-c1")
				TestReadWriteFormat(args[1], args[2]);
			else if (args[0] == "-s1" && args.Length == 4)
				SearchVersion(args[1], args[2], args[3]);
			else if (args[0] == "-ss")
				SpecificSearch(args[1], args[2]);
			else if (args[0] == "-p1")
				SelectImagesFiles(args[1], args[2]);
			else if (args[0] == "-ti" && args.Length == 4)
				ImportTest(args[1], args[2], args[3]);
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

		private static void ImportTest(string mapFile, string imgFile, string palFile)
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
			System.Drawing.Bitmap bmp = nscr.CreateBitmap(ncgr, nclr);
			bmp.Save(mapFile + ".png");

			Importer importer = new Importer();
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
