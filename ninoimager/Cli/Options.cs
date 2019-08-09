// -----------------------------------------------------------------------
// <copyright file="Options.cs" company="none">
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
namespace Ninoimager.Cli
{
    using YamlDotNet.Serialization;

    public class Options
    {
        public string Name {
            get;
            set;
        }

        [YamlMember(Alias = "work_dir", ApplyNamingConventions = false)]
        public string WorkingDirectory {
            get;
            set;
        }

        public ExportMultiNscr[] ExportMultiNscr {
            get;
            set;
        }

        public ImportMultiNscr[] ImportMultiNscr {
            get;
            set;
        }
    }
}