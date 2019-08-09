// -----------------------------------------------------------------------
// <copyright file="ImportMultiNscr.cs" company="none">
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
    using System.Collections.Generic;

    public class ImportMultiNscr
    {
        public string[] Inputs {
            get;
            set;
        }

        public string InputPalette {
            get;
            set;
        }

        public string ReferenceTiles {
            get;
            set;
        }

        public string OutputTiles {
            get;
            set;
        }

        public string ReferenceMap {
            get;
            set;
        }

        public string[] OutputMaps {
            get;
            set;
        }
    }
}