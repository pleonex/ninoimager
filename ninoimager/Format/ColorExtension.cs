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
using System.Drawing;

namespace Ninoimager.Format
{
	public static class ColorExtension
	{
		public static int Compare(this Color c1, Color c2)
		{
			if (c1.R == c2.R) {
				if (c1.G == c2.G) {
					return c1.B.CompareTo(c2.B);
				} else {
					return c1.G.CompareTo(c2.G);
				}
			} else {
				return c1.R.CompareTo(c2.R);
			}
		}

		public static ushort ToBgr555(this Color color)
		{
			int red   = color.R / 8;
			int green = color.G / 8;
			int blue  = color.B / 8;
	
			return (ushort)((red << 0) | (green << 5) | (blue << 10));
		}
		
		public static byte[] ToBgr555(this Color[] colors)
		{
			byte[] values = new byte[colors.Length * 2];

			for (int i = 0; i < colors.Length; i++) {
				ushort bgr = ToBgr555(colors[i]);
				Array.Copy(BitConverter.GetBytes(bgr), 0, values, i * 2, 2);
			}

			return values;
		}
	}
}

