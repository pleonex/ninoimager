// -----------------------------------------------------------------------
// <copyright file="ColorExtension.cs" company="none">
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
// <date>20/08/2013</date>
// -----------------------------------------------------------------------
using System;
using Color = Emgu.CV.Structure.Rgba;

namespace Ninoimager.Format
{
	public static class ColorExtension
	{
		public static int CompareTo(this Color c1, Color c2)
		{
			if (c1.Red == c2.Red) {
				if (c1.Green == c2.Green) {
					return c1.Blue.CompareTo(c2.Blue);
				} else {
					return c1.Green.CompareTo(c2.Green);
				}
			} else {
				return c1.Red.CompareTo(c2.Red);
			}
		}
	}
}

