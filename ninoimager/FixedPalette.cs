// -----------------------------------------------------------------------
// <copyright file="FixedPalette.cs" company="none">
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
// <date>01/08/2014</date>
// -----------------------------------------------------------------------
using System;
using Emgu.CV.Structure;

namespace Ninoimager
{
	public class FixedPalette
	{
		private Lab[] palette;

		public FixedPalette(Lab[] palette)
		{
			this.palette = (Lab[])palette.Clone();
		}

		public static FixedPalette FromAnyColor<TColor>(TColor[] palette)
			where TColor : struct, Emgu.CV.IColor
		{
			return new FixedPalette(ColorConversion.ConvertColors<TColor, Lab>(palette));
		}

		public Lab GetColor(int index)
		{
			if (index < 0 || index >= this.palette.Length)
				throw new ArgumentOutOfRangeException("index", index, "Index out of range");

			return this.palette[index];
		}

		/// <summary>
		/// Get the palette index of the nearest color by using k-d tree algorithm
		/// (http://en.wikipedia.org/wiki/K-d_tree) to search.
		/// </summary>
		/// <returns>The nearest color palette index.</returns>
		/// <param name="color">Color to get its nearest palette color.</param>
		public int GetNearestIndex(Lab color)
		{
			throw new NotImplementedException();
		}
	}
}

