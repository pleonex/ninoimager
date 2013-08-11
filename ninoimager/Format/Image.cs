// -----------------------------------------------------------------------
// <copyright file="ImageData.cs" company="none">
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
// <date>11/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Drawing;

namespace Ninoimager.Format
{
	public enum ColorFormat {
		Unknown,
		BGR555_4bpp = 3,
		BGR555_8bpp = 4
	}

	public enum PixelEncoding {
		Unknown,
		Lineal,
		HorizontalTiles,
		VerticalTiles
	}

	public class Image
	{
		// Image data will be independent of the value of "format" and "pixelEnc" doing a conversion to lineal pixel
		// encoding and to 8BPP index if the image is indexed and to ABGR32 otherwise.
		// Doing so, operations and transformations will be easier to implement since there will be only two formats to
		// work. The conversion will take place at the initialization and when the data is required (still to do).
		private uint[,] data;

		private ColorFormat format;
		private PixelEncoding pixelEnc;

		private int width;
		private int height;

		public Image()
		{
			this.data     = new uint[0, 0];
			this.format   = ColorFormat.Unknown;
			this.pixelEnc = PixelEncoding.Unknown;
			this.width    = 0;
			this.height   = 0;
		}

		public bool IsIndexed {
			get {
				switch (this.format) {
				case ColorFormat.BGR555_4bpp:
				case ColorFormat.BGR555_8bpp:
					return true;

				default:
					return false;
				}
			}
		}

		public int Width {
			get { return this.width; }
		}

		public int Height {
			get { return this.height; }
		}

		public ColorFormat Format {
			get { return this.format; }
		}

		public PixelEncoding PixelEncoding {
			get { return pixelEnc; }
		}

		public Bitmap CreateBitmap()
		{
			if (this.IsIndexed)
				throw new ArgumentException("A palette is required.");

			Bitmap bmp = new Bitmap(this.width, this.height);

			for (int h = 0; h < this.height; h++) {
				for (int w = 0; w < this.width; w++) {
					Color color = Color.FromArgb((int)this.data[w, h]);
					bmp.SetPixel(w, h, color);
				}
			}

			return bmp;
		}

		public Bitmap CreateBitmap(Palette palette, int paletteIndex)
		{
			if (!this.IsIndexed) {
				Console.WriteLine("##WARNING## The palette is not required.");
				return this.CreateBitmap();
			}

			Bitmap bmp = new Bitmap(this.width, this.height);
			Color[] imgColors = palette.GetPalette(paletteIndex);

			for (int h = 0; h < this.height; h++) {
				for (int w = 0; w < this.width; w++) {
					uint colorIndex = this.data[w, h];
					if (colorIndex >= imgColors.Length)
						throw new IndexOutOfRangeException("Color index out of range");

					bmp.SetPixel(w, h, imgColors[colorIndex]);
				}
			}

			return bmp;
		}

		public void SetData(byte[] rawData, PixelEncoding pixelEnc, ColorFormat format)
		{
			this.pixelEnc = pixelEnc;
			this.format   = format;

			// UNDONE: Convert rawData
			throw new NotImplementedException();
		}

		public byte[] GetData()
		{
			// UNDONE: Convert data
			throw new NotImplementedException();
		}
	}
}

