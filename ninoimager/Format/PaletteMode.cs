// -----------------------------------------------------------------------
// <copyright file="PaletteMode.cs" company="none">
// Copyright (C) 2014 
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
// <date>03/20/2014</date>
// -----------------------------------------------------------------------
using System;

namespace Ninoimager.Format
{
	// For more info see GbaTek
	public enum PaletteMode {
		Palette16_16 = 0,	// 16 colors / 16 palettes
		Palette256_1 = 1,	// 256 colors / 1 palette
		Extended = 2,		// 256 colors / 16 palettes
	}
}

