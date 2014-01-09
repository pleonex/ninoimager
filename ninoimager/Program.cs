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

		public static void Main(string[] args)
		{
			Console.WriteLine("ninoimager ~~ Image importer and exporter for Ni no kuni DS");
			Console.WriteLine("V {0} ~~ by pleoNeX ~~", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine();

			Stopwatch watch = new Stopwatch();
			watch.Start();

			if (args.Length == 4 && args[0] == "-ibg")
				SearchAndImportBg(args[1], args[2], args[3]);
			else if (args.Length == 2 && args[0] == "-efr")
				SearchAndExport(args[1]);
			else
				Tests.RunTest(args);

			watch.Stop();
			Console.WriteLine("It tooks: {0}", watch.Elapsed);
		}

		private static void SearchAndImportBg(string baseDir, string outputDir, string xmlList)
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

			foreach (string file in Directory.EnumerateFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = BgRegex.Match(file);
				if (!match.Success)
					continue;

				// Get paths
				string imagePath = match.Groups[1].Value;
				string relative  = imagePath.Replace(baseDir, "");
				if (relative[0] == Path.DirectorySeparatorChar)
					relative = relative.Substring(1);
				string packFile  = Path.Combine(outputDir, relative) + ".n2d";
				string outFile   = Path.Combine(outputDir, relative) + "_new.n2d";
				string infoFile  = Path.Combine(Path.GetDirectoryName(packFile), ".info.xml");
				if (!File.Exists(infoFile)) {
					Console.WriteLine("## Warning ## '.info.xml' file not found.");
					continue;
				}

				// Try to import
				try {
					Npck npck;
					Npck original = new Npck(packFile);

					// If there is palette, keeps old palette
					if (original[0] != null) {
						npck = Npck.ImportBackgroundImage(file, original[0]);
					} else {
						// Create pack with new palette but not save it
						Console.WriteLine("## Info ## Pack file without palette");
						npck = Npck.ImportBackgroundImage(file);
						npck[0] = null;
					}

					npck.Write(outFile);
					original.CloseAll();
					npck.CloseAll();
				} catch (Exception ex) {
					Console.WriteLine("## Error ## Importing:  {0}", relative);
					Console.WriteLine("\t" + ex.Message);
					continue;
				}

				// Read info file to get ROM id
				XDocument xinfo = XDocument.Load(infoFile);
				string id = xinfo.Element("RomInfo").Descendants("File")
							.First(f => f.Value == Path.GetFileName(packFile)).Attribute("Id").Value;

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

				Console.WriteLine("Written with success... {0}", relative);
			}

			xml.Save(xmlList);
		}

		private static void SearchAndExport(string baseDir)
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
