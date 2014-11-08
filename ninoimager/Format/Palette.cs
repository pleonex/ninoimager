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
using System.IO;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

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

		public Color GetColor(int paletteIndex, int colorIndex)
		{
			if ((paletteIndex < 0 || paletteIndex >= this.palette.Length) ||
			    (colorIndex < 0 || colorIndex >= this.palette[paletteIndex].Length))
			    throw new IndexOutOfRangeException();

			return this.palette[paletteIndex][colorIndex];
		}

		public Color[] GetPalette(int index)
		{
			if (index < 0 || index >= this.palette.Length)
				throw new IndexOutOfRangeException();

			return (Color[])this.palette[index].Clone();
		}

		public Color[][] GetPalettes()
		{
			return (Color[][])this.palette.Clone();
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

		public EmguImage CreateBitmap(int index)
		{
			if (index < 0 || index >= this.palette.Length)
				throw new IndexOutOfRangeException();

			return CreateBitmap(this.palette[index]);
		}

		public static EmguImage CreateBitmap(Color[] colors)
		{
			int height = (colors.Length / 0x10);
			if (colors.Length % 0x10 != 0)
				height++;

			EmguImage palette = new EmguImage(160, height * 10);

			bool end = false;
			for (int i = 0; i < 16 & !end; i++) {
				for (int j = 0; j < 16; j++) {
					// Check if we have reached the end.
					// A palette image can be incompleted (number of colors not padded to 16)
					if (colors.Length <= j + 16 * i) {
						end = true;
						break;
					}

					for (int k = 0; k < 10; k++)
						for (int q = 0; q < 10; q++)
							palette.SetPixel(j * 10 + q, i * 10 + k, colors[j + 16 * i]);
				}
			}

			return palette;
		}

		public void ToWinPaletteFormat(string outPath, int index, bool gimpCompability)
		{
			FileStream stream = new FileStream(outPath, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(stream);

			bw.Write(new char[] { 'R', 'I', 'F', 'F' });            // "RIFF"
			bw.Write((uint)(0x10 + palette[index].Length * 4));     // file_length - 8
			bw.Write(new char[] { 'P', 'A', 'L', ' ' });            // "PAL "
			bw.Write(new char[] { 'd', 'a', 't', 'a' });            // "data"
			bw.Write((uint)palette[index].Length * 4 + 4);          // data_size = file_length - 0x14
			bw.Write((ushort)0x0300);                               // version = 00 03
			bw.Write((ushort)(palette[index].Length));                  // num_colors
			if (gimpCompability)
				bw.Write((uint)0x00);                   			// Error in Gimp 2.8

			for (int i = 0; i < palette[index].Length; i++)
			{
				bw.Write((byte)palette[index][i].Red);
				bw.Write((byte)palette[index][i].Green);
				bw.Write((byte)palette[index][i].Blue);
				bw.Write((byte)0x00);
				bw.Flush();
			}

			stream.Close();
		}

		public void ToAcoFormat(string outPath, int index)
		{
			FileStream stream = new FileStream(outPath, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(stream);

			bw.Write((ushort)0x00);         					// Version 0
			bw.Write(ToBytesBE((ushort)palette[index].Length));	// Number of colors

			for (int i = 0; i < palette[index].Length; i++) {
				bw.Write((ushort)0x00);         						// Color spec set to 0
				bw.Write(ToBytesBE((ushort)palette[index][i].Red));     // Red component
				bw.Write(ToBytesBE((ushort)palette[index][i].Green));   // Green component
				bw.Write(ToBytesBE((ushort)palette[index][i].Blue));    // Blue component
				bw.Write((ushort)0x00);         						// Always 0x00, not used
			}

			stream.Close();
		}

		private static byte[] ToBytesBE(ushort value)
		{
			byte[] data = BitConverter.GetBytes(value);
			return new byte[] { data[1], data[0] };
		}
	}
}
