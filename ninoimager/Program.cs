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

			if (args.Length != 3)
				return;

			if (args[0] == "-t1")
				PaletteInfo(args[1], args[2]);
			else if (args[0] == "-c1")
				TestReadWriteFormat(args[1], args[2]);
			else if (args[0] == "-s1")
				SearchVersion(args[1], args[2]);
			else if (args[0] == "-ss")
				SpecificSearch(args[1], args[2]);
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
				Activator.CreateInstance(type, file);	// Read file
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

		private static void SearchVersion(string dir, string version)
		{
			foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
				try {
				Nclr palette = new Nclr(file);
				if (palette.NitroData.VersionS != version)
					Console.WriteLine("* {0} -> {1}", palette.NitroData.VersionS, file);
				} catch (Exception ex) {
					Console.WriteLine("ERROR at {0}", file);
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
					Console.ReadKey(true);
					Console.WriteLine();
				}
			}
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
