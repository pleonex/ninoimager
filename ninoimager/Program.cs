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

			if (args.Length == 3 && args[0] == "-ibg")
				SearchAndImportBg(args[1], args[2]);
			else
				Tests.RunTest(args);
		}

		private static void SearchAndImportBg(string baseDir, string xmlList)
		{
			XDocument xml   = new XDocument();
			xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
			XElement root   = new XElement("NinoImport");
			xml.Add(root);

			foreach (string file in Directory.EnumerateFiles(baseDir, "*.png", SearchOption.AllDirectories)) {
				Match match = BgRegex.Match(file);
				if (!match.Success)
					continue;

				string packFile = match.Groups[1] + ".n2d";
				Npck npck = Npck.ImportBackgroundImage(file);
				npck.Write(packFile);

				XElement xfile = new XElement("File");
				xfile.SetAttributeValue("ID", "");
				xfile.Value = packFile.Replace(baseDir, "");
				root.Add(xfile);
			}

			xml.Save(xmlList);
		}
	}
}
