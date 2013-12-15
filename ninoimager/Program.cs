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

			DateTime start = DateTime.Now;

			if (args.Length == 3 && args[0] == "-ibg")
				SearchAndImportBg(args[1], args[2]);
			else if (args.Length == 2 && args[0] == "-efr")
				SearchAndExport(args[1]);
			else
				Tests.RunTest(args);

			DateTime end = DateTime.Now;
			Console.WriteLine("It tooks: {0}:{1}", (end - start).Minutes, (end - start).Seconds);
		}

		private static void SearchAndImportBg(string baseDir, string xmlList)
		{
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

				string packFile = match.Groups[1] + ".n2d";
				string relative = packFile.Replace(baseDir, "");
				string infoFile = Path.Combine(Path.GetDirectoryName(file), ".info.xml");
				if (!File.Exists(infoFile))
					continue;

				try {
					Npck npck = Npck.ImportBackgroundImage(file);
					npck.Write(packFile);
				} catch (Exception ex) {
					Console.WriteLine("Error trying to import: {0}", relative);
					Console.WriteLine("\t" + ex.Message);
					continue;
				}

				XDocument xinfo = XDocument.Load(infoFile);
				string id = xinfo.Element("RomInfo").Descendants("File")
							.First(f => f.Value == Path.GetFileName(packFile)).Attribute("Id").Value;

				// Add game info
				XElement xgame = new XElement("File");
				xgame.Add(new XElement("Path", relative));
				xgame.Add(new XElement("Import", relative));
				gameInfo.Add(xgame);

				// Add edit info
				XElement xedit = new XElement("FileInfo");
				xedit.Add(new XElement("Path", relative));
				xedit.Add(new XElement("Type", "Common.Replace"));
				xedit.Add(new XElement("DependsOn", "."));	// No idea?
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
