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
using Point     = System.Drawing.Point;
using Size      = System.Drawing.Size;
using Color     = Emgu.CV.Structure.Rgba;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.Format
{
	public class Image
	{
		// Image data will be independent of the value of "format" and "pixelEnc" doing a conversion to lineal pixel
		// encoding and to 24BPP index + 8 bits of alpha component if the image is indexed and to ABGR32 otherwise.
		// Doing so, operations and transformations will be easier to implement since there will be only two formats to
		// work. The conversion will take place at the initialization and when the data is required.
		// CHECK: What about changing the type to Pixel?
		private uint[] data;
		private ColorFormat format;
		private PixelEncoding pixelEnc;

		private Size tileSize;
		private int width;
		private int height;

		public Image()
		{
			this.data     = null;
			this.format   = ColorFormat.Unknown;
			this.pixelEnc = PixelEncoding.Unknown;
			this.tileSize = new Size(8, 8);
			this.width    = 0;
			this.height   = 0;
		}

		public Image(Pixel[] pixels, int width, int height, PixelEncoding pxEnc, ColorFormat format, Size tileSize)
		{
			this.width = width;
			this.height = height;
			this.SetData(pixels, pxEnc, format, tileSize);
		}

		private Image(Image img, uint[] data, int width, int height)
		{
			this.data = data;
			this.width = width;
			this.height = height;
			this.format = img.Format;
			this.pixelEnc = img.PixelEncoding;
			this.tileSize = img.TileSize;
		}

		public int Width {
			get { return this.width; }
			set { this.width = value; }
		}

		public int Height {
			get { return this.height; }
			set { this.height = value; }
		}

		public ColorFormat Format {
			get { return this.format; }
			set { this.format = value; }
		}

		public PixelEncoding PixelEncoding {
			get { return pixelEnc; }
			private set { this.pixelEnc = value; }
		}

		public Size TileSize {
			get { return this.tileSize; }
			private set { this.tileSize = value; }
		}

		public EmguImage CreateBitmap()
		{
			if (this.Format.IsIndexed())
				throw new ArgumentException("A palette is required.");

			EmguImage bmp = new EmguImage(this.width, this.height);
			bmp.SetPixels(0, 0, this.width, this.height, this.data.ToArgbColors());

			return bmp;
		}

		public EmguImage CreateBitmap(Palette palette, int paletteIndex)
		{
			if (!this.Format.IsIndexed()) {
				Console.WriteLine("##WARNING## The palette is not required.");
				return this.CreateBitmap();
			}

			EmguImage bmp = new EmguImage(this.width, this.height);
			bmp.SetPixels(
				0,
				0,
				this.width,
				this.height, 
				InfoToIndexedColors(this.data, palette.GetPalette(paletteIndex))
			);

			return bmp;
		}

		public EmguImage CreateBitmap(Palette palette, uint[] paletteIndex)
		{
			if (!this.Format.IsIndexed()) {
				Console.WriteLine("##WARNING## The palette is not required.");
				return this.CreateBitmap();
			}

			EmguImage bmp = new EmguImage(this.width, this.height);
			bmp.SetPixels(
				0,
				0,
				this.width,
				this.height,
				InfoToIndexedColors(this.data, palette.GetPalettes(), paletteIndex)
			);

			return bmp;
		}

		public EmguImage CreateSubImage(int x, int y, int width, int height)
		{
			// Get pixel of subimage and create using private constructor to pass internal data
			throw new NotImplementedException();
		}

		public Pixel[] GetTile(int index)
		{
			Pixel[] tile = new Pixel[this.tileSize.Width * this.tileSize.Height];

			bool isIndexed = this.Format.IsIndexed();
			int numTilesX = this.width / this.tileSize.Width;
			Point tilePos = new Point(index % numTilesX, index / numTilesX);

			for (int y = 0; y < this.tileSize.Height; y++) {
				for (int x = 0; x < this.tileSize.Width; x++) {
					uint px = this.data[(y + tilePos.Y * tileSize.Height) * this.Width + (x + tilePos.X * tileSize.Width)];
					tile[y * this.tileSize.Width + x] = new Pixel(
						px & 0x00FFFFFF,
						(px >> 24) & 0xFF,
						isIndexed);
				}
			}

			return tile;
		}

		public void SetData(byte[] rawData, PixelEncoding pixelEnc, ColorFormat format)
		{
			this.SetData(rawData, pixelEnc, format, new Size(8, 8));
		}

		public void SetData(byte[] rawData, PixelEncoding pixelEnc, ColorFormat format, Size tileSize)
		{
			if (this.width == 0 || this.height == 0)
				throw new ArgumentOutOfRangeException("Width and Height have not been specified.");

			this.pixelEnc = pixelEnc;
			this.format   = format;
			this.tileSize = tileSize;

			// First convert to 24bpp index + 8 bits alpha if it's indexed or ARGB32 otherwise.
			// normalizeData contains information about 1 pixel (index or color)
			uint[] normalizedData = new uint[rawData.Length * 8 / this.Format.Bpp()];

			int rawPos = 0;
			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = rawData.GetBits(ref rawPos, this.Format.Bpp());	// Get pixel info from raw data
				normalizedData[i] = this.format.UnpackColor(info);			// Get color from pixel info (unpack info)
			}

			// Then convert to lineal pixel encoding
			this.data = new uint[this.width * this.height];
			this.pixelEnc.Codec(normalizedData, this.data, true, this.width, this.height, this.tileSize);
		}

		public void SetData(Pixel[] pixels, PixelEncoding pixelEnc, ColorFormat format, Size tileSize)
		{
			if (this.width == 0 || this.height == 0)
				throw new ArgumentOutOfRangeException("Width and Height have not been specified.");

			this.pixelEnc = pixelEnc;
			this.format   = format;
			this.tileSize = tileSize;

			uint[] normalizedData = new uint[pixels.Length];
			for (int i = 0; i < pixels.Length; i++)
				normalizedData[i] = (uint)(pixels[i].Alpha << 24) | (uint)pixels[i].Info;

			this.data = new uint[this.width * this.height];
			this.pixelEnc.Codec(normalizedData, this.data, true, this.width, this.height, this.tileSize);
		}

		public byte[] GetData()
		{
			// Inverse operation of SetData

			// First convert to one-dimension array (encode pixels)
			uint[] normalizedData = new uint[this.width * this.height];
			this.pixelEnc.Codec(this.data, normalizedData, false, this.width, this.height, this.tileSize);

			// Then code normalized data to its format and write to final buffer
			byte[] buffer = new byte[normalizedData.Length * this.Format.Bpp() / 8];
			int bufferPos = 0;

			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = this.format.PackColor(normalizedData[i]);
				buffer.SetBits(ref bufferPos, this.Format.Bpp(), info);
			}

			return buffer;
		}

		private static Color[] InfoToIndexedColors(uint[] colorInfo, Color[] palette)
		{
			return InfoToIndexedColors(
				colorInfo,
				new Color[][] { palette },
				new uint[colorInfo.Length]	// By default is filled with 0
			);
		}

		private static Color[] InfoToIndexedColors(uint[] colorInfo, Color[][] palettes, uint[] palIdx)
		{
			Color[] colors = new Color[colorInfo.Length];
			for (int i = 0; i < colorInfo.Length; i++)
				colors[i] = InfoToIndexedColor(colorInfo[i], palettes[palIdx[i]]);
			return colors;
		}

		private static Color InfoToIndexedColor(uint colorInfo, Color[] palette)
		{
			uint alpha      = colorInfo >> 24;
			uint colorIndex = colorInfo & 0x00FFFFFF;
			if (colorIndex >= palette.Length)
				throw new IndexOutOfRangeException("Color index out of palette");

			Color color = palette[colorIndex];
			color.Alpha = (double)alpha;

			return color;
		}
	}
}
