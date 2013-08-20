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
		// Texture NDS formats from http://nocash.emubase.de/gbatek.htm#ds3dtextureformats
		Indexed_A3I5  = 1,	// 8  bits-> 0-4: index; 5-7: alpha
		Indexed_2bpp  = 2,	// 2  bits for 4   colors
		Indexed_4bpp  = 3,	// 4  bits for 16  colors
		Indexed_8bpp  = 4,	// 8  bits for 256 colors          
		Texeled_4x4   = 5,	// 32 bits-> 2 bits per texel (only in textures)
		Indexed_A5I3  = 6,	// 8  bits-> 0-2: index; 3-7: alpha
		ABGR555_16bpp = 7,	// 16 bits BGR555 color with alpha component
        // Also common formats
		Indexed_1bpp,		// 1  bit  for 2   colors
		Indexed_A4I4,  		// 8  bits-> 0-3: index; 4-7: alpha
		BGRA_32bpp, 		// 32 bits BGRA color
		ABGR_32bpp, 		// 32 bits ABGR color
	}

	public enum PixelEncoding {
		HorizontalTiles = 0,
		Lineal = 1,
		VerticalTiles,
		Unknown,
	}

	public struct Pixel
	{
		public Pixel(uint info, uint alpha, bool isIndexed) : this()
		{
			this.IsIndexed = isIndexed;
			this.Info = info;
			this.Alpha = (byte)alpha;
		}

		public bool IsIndexed {
			get;
			private set;
		}

		/// <summary>
		/// Gets the pixel info.
		/// If it's indexed it returns the color index otherwise, it returns a 32bit BGR value.
		/// </summary>
		/// <value>The pixel info.</value>
		public uint Info {
			get;
			private set;
		}

		public byte Alpha {
			get;
			private set;
		}
	}

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

		public bool IsIndexed {
			get {
				switch (this.format) {
				case ColorFormat.Indexed_1bpp:
				case ColorFormat.Indexed_2bpp:
				case ColorFormat.Indexed_4bpp:
				case ColorFormat.Indexed_8bpp:
				case ColorFormat.Indexed_A3I5:
				case ColorFormat.Indexed_A4I4:
				case ColorFormat.Indexed_A5I3:
				case ColorFormat.Texeled_4x4:
					return true;

				case ColorFormat.ABGR555_16bpp:
				case ColorFormat.BGRA_32bpp:
				case ColorFormat.ABGR_32bpp:
					return false;

				default:
					throw new FormatException();
				}
			}
		}

		public bool IsTiled {
			get {
				switch (this.pixelEnc) {
				case PixelEncoding.HorizontalTiles:
				case PixelEncoding.VerticalTiles:
					return true;

				case PixelEncoding.Lineal:
					return false;

				default:
					throw new FormatException();
				}
			}
		}

		public int Bpp {
			get {
				switch (this.format) {
				case ColorFormat.Indexed_1bpp:  return 1;
				case ColorFormat.Indexed_2bpp:  return 2;
				case ColorFormat.Indexed_4bpp:  return 4;
				case ColorFormat.Indexed_8bpp:  return 8;
				case ColorFormat.Indexed_A3I5:  return 8;
				case ColorFormat.Indexed_A4I4:  return 8;
				case ColorFormat.Indexed_A5I3:  return 8;
				case ColorFormat.ABGR555_16bpp: return 16;
				case ColorFormat.BGRA_32bpp:    return 32;
				case ColorFormat.ABGR_32bpp:    return 32;
				case ColorFormat.Texeled_4x4:   return 2;

				default:
					throw new FormatException();
				}
			}
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

		public Bitmap CreateBitmap()
		{
			if (this.IsIndexed)
				throw new ArgumentException("A palette is required.");

			Bitmap bmp = new Bitmap(this.width, this.height);

			for (int i = 0; i < this.data.Length; i++) {
				Color color = Color.FromArgb((int)this.data[i]);
				bmp.SetPixel(i % this.width, i / this.width, color);
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

			for (int i = 0; i < this.data.Length; i++) {
				uint alpha      = this.data[i] >> 24;
				uint colorIndex = this.data[i] & 0x00FFFFFF;
				if (colorIndex >= imgColors.Length)
					throw new IndexOutOfRangeException("Color index out of range");

				Color color = imgColors[colorIndex];
				color = Color.FromArgb((int)alpha, color);
				bmp.SetPixel(i % this.width, i / this.width, color);
			}

			return bmp;
		}

		public Bitmap CreateBitmap(Palette palette, uint[] paletteIndex)
		{
			if (!this.IsIndexed) {
				Console.WriteLine("##WARNING## The palette is not required.");
				return this.CreateBitmap();
			}

			Bitmap bmp = new Bitmap(this.width, this.height);

			for (int i = 0; i < this.data.Length; i++) {
				uint alpha = this.data[i] >> 24;
				uint colorIndex = this.data[i] & 0x00FFFFFF;

				Color color = palette.GetColor((int)paletteIndex[i], (int)colorIndex);
				color = Color.FromArgb((int)alpha, color);
				bmp.SetPixel(i % this.width, i / this.width, color);
			}

			return bmp;
		}

		public Image CreateSubImage(int x, int y, int width, int height)
		{
			// Get pixel of subimage and create using private constructor to pass internal data
			throw new NotImplementedException();
		}

		public Pixel[] GetTile(int index)
		{
			Pixel[] tile = new Pixel[this.tileSize.Width * this.tileSize.Height];

			bool isIndexed = this.IsIndexed;
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
			uint[] normalizedData = new uint[rawData.Length * 8 / this.Bpp];

			int rawPos = 0;
			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = GetValue(rawData, ref rawPos, this.Bpp);	// Get pixel info from raw data
				normalizedData[i] = UnpackColor(info, this.format);		// Get color from pixel info (unpack info)
			}

			// Then convert to lineal pixel encoding
			this.data = new uint[this.width * this.height];
			LinealCodec(normalizedData, this.data, true, this.pixelEnc, this.width, this.height, this.tileSize);
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
			LinealCodec(normalizedData, this.data, true, this.pixelEnc, this.width, this.height, this.tileSize);
		}

		public byte[] GetData()
		{
			// Inverse operation of SetData

			// First convert to one-dimension array (encode pixels)
			uint[] normalizedData = new uint[this.width * this.height];
			LinealCodec(this.data, normalizedData, false, this.pixelEnc, this.width, this.height, this.tileSize);

			// Then code normalized data to its format and write to final buffer
			byte[] buffer = new byte[normalizedData.Length * this.Bpp / 8];
			int bufferPos = 0;

			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = PackColor(normalizedData[i], this.format);
				SetValue(buffer, ref bufferPos, this.Bpp, info);
			}

			return buffer;
		}

		public static void LinealCodec(uint[] dataIn, uint[] dataOut, bool decoding, PixelEncoding pxEnc,
		                               int width, int height, Size tileSize)
		{
			if (pxEnc != PixelEncoding.Lineal && pxEnc != PixelEncoding.HorizontalTiles && 
			    pxEnc != PixelEncoding.VerticalTiles)
				throw new NotSupportedException();

			if (dataIn == null || dataOut == null || dataIn.Length != dataOut.Length)
				throw new ArgumentNullException();

			if ((width % tileSize.Width != 0) && 
			    (pxEnc == PixelEncoding.HorizontalTiles || pxEnc == PixelEncoding.VerticalTiles))
				throw new FormatException("Width must be a multiple of tile width to use Tiled pixel encoding.");

			// Little trick to use the same equations
			if (pxEnc == PixelEncoding.Lineal)
				tileSize = new Size(width, height);

			for (int linealIndex = 0; linealIndex < dataOut.Length; linealIndex++) {
				int tiledIndex = CalculateTiledIndex(
					linealIndex % width, linealIndex / width,
					pxEnc, width, height, tileSize);

				if (decoding)
					dataOut[linealIndex] = dataIn[tiledIndex];
				else
					dataOut[tiledIndex] = dataIn[linealIndex];
			}
		}

		public static int CalculateTiledIndex(int x, int y, PixelEncoding pxEnc, int width, int height, Size tileSize)
		{
			int tileLength = tileSize.Width * tileSize.Height;
			int numTilesX = width / tileSize.Width;
			int numTilesY = height / tileSize.Height;

			// Get lineal index
			Point pixelPos = new Point(x % tileSize.Width, y % tileSize.Height); // Pos. pixel in tile
			Point tilePos  = new Point(x / tileSize.Width, y / tileSize.Height); // Pos. tile in image
			int index = 0;

			if (pxEnc == PixelEncoding.HorizontalTiles)
				index = tilePos.Y * numTilesX * tileLength + tilePos.X * tileLength;	// Absolute tile pos.
			else if (pxEnc == PixelEncoding.VerticalTiles)
				index = tilePos.X * numTilesY * tileLength + tilePos.Y * tileLength;	// Absolute tile pos.

			index += pixelPos.Y * tileSize.Width + pixelPos.X;	// Add pos. of pixel inside tile

			return index;
		}

		private static uint GetValue(byte[] data, ref int bitPos, int size)
		{
			if (size < 0 || size > 32)
				throw new ArgumentOutOfRangeException("Size is too big");

			if (bitPos + size > data.Length * 8)
				throw new IndexOutOfRangeException();

			uint value = 0;
			for (int s = 0; s < size; s++, bitPos++) {
				uint bit = data[bitPos / 8];
				bit >>= (bitPos % 8);
				bit &= 1;

				value |= bit << s;
			}

			return value;
		}

		private static void SetValue(byte[] data, ref int bitPos, int size, uint value)
		{
			if (size < 0 || size > 32)
				throw new ArgumentOutOfRangeException("Size is too big");

			if (bitPos + size > data.Length * 8)
				throw new IndexOutOfRangeException();

			for (int s = 0; s < size; s++, bitPos++) {
				uint bit = (value >> s) & 1;

				uint dByte = data[bitPos / 8];
				dByte |= bit << (bitPos % 8);
				data[bitPos / 8] = (byte)dByte;
			}
		}

		private static uint UnpackColor(uint info, ColorFormat format)
		{
			switch (format) {
			// 100% alpha, no transparency
			case ColorFormat.Indexed_1bpp:
			case ColorFormat.Indexed_2bpp:
			case ColorFormat.Indexed_4bpp:
			case ColorFormat.Indexed_8bpp:
				return (0xFFu << 24) | info;

			case ColorFormat.Indexed_A3I5:
				return (((info >> 5) * 0xFF / 0x07) << 24) | (info & 0x1F);
			case ColorFormat.Indexed_A4I4:
				return (((info >> 4) * 0xFF / 0x0F) << 24) | (info & 0x0F);
			case ColorFormat.Indexed_A5I3:
				return (((info >> 3) * 0xFF / 0x1F) << 24) | (info & 0x07);

			case ColorFormat.ABGR555_16bpp:
				return 
					((((info >> 15) & 0x01) * 0xFF / 0x01) << 24) |	// alpha, 1 bit
					((((info >> 10) & 0x1F) * 0xFF / 0x1F) << 16) | // blue,  5 bits
					((((info >> 05) & 0x1F) * 0xFF / 0x1F) << 08) | // green, 5 bits
					((((info >> 00) & 0x1F) * 0xFF / 0x1F) << 00);	// red,   5 bits
			case ColorFormat.ABGR_32bpp:
				return info;
			case ColorFormat.BGRA_32bpp:
				return ((info & 0x0F) << 24) | (info >> 8);

			default:
				throw new NotSupportedException();
			}
		}

		private static uint PackColor(uint pxInfo, ColorFormat format)
		{
			switch (format) {
				// No transparency
			case ColorFormat.Indexed_1bpp:
			case ColorFormat.Indexed_2bpp:
			case ColorFormat.Indexed_4bpp:
			case ColorFormat.Indexed_8bpp:
				return pxInfo & 0x00FFFFFF;

			case ColorFormat.Indexed_A3I5:
				return (((pxInfo >> 24) * 0x07 / 0xFF) << 5) | (pxInfo & 0x1F);
			case ColorFormat.Indexed_A4I4:
				return (((pxInfo >> 24) * 0x0F / 0xFF) << 4) | (pxInfo & 0x0F);
			case ColorFormat.Indexed_A5I3:
				return (((pxInfo >> 24) * 0x1F / 0xFF) << 3) | (pxInfo & 0x07);

			case ColorFormat.ABGR555_16bpp:
				return
					((((pxInfo >> 24) & 0xFF) * 0x01 / 0xFF) << 15) |	// alpha, 1 bit
					((((pxInfo >> 16) & 0xFF) * 0x1F / 0xFF) << 10) |	// blue,  5 bits
					((((pxInfo >> 08) & 0xFF) * 0x1F / 0xFF) << 05) |	// green, 5 bits
					((((pxInfo >> 00) & 0xFF) * 0x1F / 0xFF) << 00);	// red,   5 bits
			case ColorFormat.ABGR_32bpp:
				return pxInfo;
			case ColorFormat.BGRA_32bpp:
				return ((pxInfo >> 24) & 0xFF) | (pxInfo << 8);

			default:
				throw new NotSupportedException();
			}
		}
	}
}
