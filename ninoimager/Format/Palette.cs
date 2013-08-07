// -----------------------------------------------------------------------
// <copyright file="Palette.cs" company="none">
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
// <date>06/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Collections;
using System.Drawing;

namespace Ninoimager.Format
{
	public class Palette
	{
		private Color[][] palette;

		public Palette()
		{
			this.palette = new Color[0][];
		}

		public Palette(Color[] palette)
		{
			this.SetPalette(palette);
		}

		public Palette(Color[][] palette)
		{
			this.SetPalette(palette);
		}

		public int NumPalettes {
			get { return this.palette.Length; }
		}

		public IEnumerable Palettes {
			get {
				foreach (Color[] pal in this.palette) {
					yield return (Color[])pal.Clone();
				}

				yield break;
			}
		}

		public Color[] GetPalette(int index)
		{
			if (index < 0 || index >= this.palette.Length)
				throw new IndexOutOfRangeException();

			return (Color[])this.palette[index].Clone();
		}

		public void SetPalette(Color[] palette)
		{
			this.palette = new Color[1][];
			this.palette[0] = new Color[palette.Length];
			Array.Copy(palette, this.palette[0], palette.Length);
		}

		public void SetPalette(Color[][] palette)
		{
			this.palette = new Color[palette.Length][];
			for (int i = 0; i < palette.Length; i++) {
				this.palette[i] = new Color[palette[i].Length];
				Array.Copy(palette[i], this.palette[i], palette[i].Length);
			}
		}

		public Bitmap CreateBitmap(int index)
		{
			if (index < 0 || index >= this.palette.Length)
				throw new IndexOutOfRangeException();

			return CreateBitmap(this.palette[index]);
		}

		public static Bitmap CreateBitmap(Color[] colors)
		{
			int height = (colors.Length / 0x10);
			if (colors.Length % 0x10 != 0)
				height++;

			Bitmap palette = new Bitmap(160, height * 10);

			bool end = false;
			for (int i = 0; i < 16 & !end; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					if (colors.Length <= j + 16 * i)
					{
						end = true;
						break;
					}

					for (int k = 0; k < 10; k++)
						for (int q = 0; q < 10; q++)
							palette.SetPixel((j * 10 + q), (i * 10 + k), colors[j + 16 * i]);
				}
			}

			return palette;
		}

		public static Color FromBGR555(ushort value)
		{
			int red   = ((value & 0x001F) >> 00) * 8;
			int green = ((value & 0x03E0) >> 05) * 8;
			int blue  = ((value & 0x7C00) >> 10) * 8;

			return Color.FromArgb(red, green, blue);
		}

		public static Color[] FromBGR555(byte[] values)
		{
			if (values.Length % 2 != 0)
				throw new ArgumentException("Length must be even.");

			Color[] colors = new Color[values.Length / 2];
			for (int i = 0; i < values.Length; i += 2) {
				colors[i] = FromBGR555(BitConverter.ToUInt16(values, i));
			}

			return colors;
		}

		public static ushort ToBGR555(Color color)
		{
			int red   = color.R / 8;
			int green = color.G / 8;
			int blue  = color.B / 8;

			ushort bgr = (ushort)((red << 0) | (green << 5) | (blue << 10));
			return bgr;
		}

		public static byte[] ToBGR555(Color[] colors)
		{
			byte[] values = new byte[colors.Length * 2];

			for (int i = 0; i < colors.Length; i++) {
				ushort bgr = ToBGR555(colors[i]);
				Array.Copy(BitConverter.GetBytes(bgr), 0, values, i * 2, 2);
			}

			return values;
		}
	}
}
