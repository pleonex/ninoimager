// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="none">
// Copyright (C) 2019
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
// <date>28/05/2019</date>
// -----------------------------------------------------------------------
namespace Ninoimager
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Ninoimager.Cli;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ninoimager ~~ Image importer and exporter for Ni no kuni DS");
            Console.WriteLine("V {0} ~~ by pleoNeX ~~", Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine();

            if (args.Length == 0) {
                Console.WriteLine("Missing file with configuration settings");
                Environment.Exit(1);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (args[0] == "-n") {
                NinokuniImporter.RunCommand(args);
            } else {
                string yaml = args[0];
                Options options = new DeserializerBuilder()
                    .WithNamingConvention(new UnderscoredNamingConvention())
                    .Build()
                    .Deserialize<Options>(File.ReadAllText(yaml));

                var runner = new Runner(options);
                runner.Run();
            }

            watch.Stop();
            Console.WriteLine("It tooks: {0}", watch.Elapsed);
        }
    }
}