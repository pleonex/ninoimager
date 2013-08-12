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
		Unknown,
		Lineal,
		HorizontalTiles,
		VerticalTiles
	}

	public class Image
	{
		// Image data will be independent of the value of "format" and "pixelEnc" doing a conversion to lineal pixel
		// encoding and to 24BPP index + 8 bits of alpha component if the image is indexed and to ABGR32 otherwise.
		// Doing so, operations and transformations will be easier to implement since there will be only two formats to
		// work. The conversion will take place at the initialization and when the data is required (still to do).
		private uint[,] data;
		private uint[,] originalData;	// Copy used when updating dimensions

		private ColorFormat format;
		private PixelEncoding pixelEnc;

		private Size tileSize = new Size(8, 8);
		private int width;
		private int height;

		public Image()
		{
			this.data     = null;
			this.format   = ColorFormat.Unknown;
			this.pixelEnc = PixelEncoding.Unknown;
			this.width    = 0;
			this.height   = 0;
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
					return true;

				case ColorFormat.Texeled_4x4:
				case ColorFormat.ABGR555_16bpp:
				case ColorFormat.BGRA_32bpp:
				case ColorFormat.ABGR_32bpp:
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
			set {
				this.width = value;
				if (this.data != null)
					this.ChangeDimension();
			}
		}

		public int Height {
			get { return this.height; }
			set {
				this.height = value;
				if (this.data != null)
					this.ChangeDimension();
			}
		}

		public ColorFormat Format {
			get { return this.format; }
			set { this.format = value; }
		}

		public PixelEncoding PixelEncoding {
			get { return pixelEnc; }
			set { this.pixelEnc = value; }
		}

		public Size TileSize {
			get { return this.tileSize; }
			set { this.tileSize = value; }
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

					uint alpha      = this.data[w, h] >> 24;
					uint colorIndex = this.data[w, h] & 0x00FFFFFF;
					if (colorIndex >= imgColors.Length)
						throw new IndexOutOfRangeException("Color index out of range");

					Color color = imgColors[colorIndex];
					color = Color.FromArgb((int)alpha, color);
					bmp.SetPixel(w, h, color);
				}
			}

			return bmp;
		}

		public void SetData(byte[] rawData, PixelEncoding pixelEnc, ColorFormat format)
		{
			if (this.width == 0 || this.height == 0)
				throw new ArgumentOutOfRangeException("Width and Height have not been specified.");

			this.pixelEnc = pixelEnc;
			this.format   = format;

			// First convert to 24bpp index + 8 bits alpha if it's indexed or ARGB32 otherwise.
			// normalizeData contains information about 1 pixel (index or color)
			uint[] normalizedData = new uint[rawData.Length * 8 / this.Bpp];

			int rawPos = 0;
			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = GetValue(rawData, ref rawPos, this.Bpp);	// Get pixel info from raw data
				normalizedData[i] = UnpackColor(info, this.format);		// Get color from pixel info (unpack info)
			}

			// Then convert to lineal pixel encoding
			this.data = new uint[this.width, this.height];
			this.LinealCodec(normalizedData, true);
			this.originalData = (uint[,])this.data.Clone();
		}

		public byte[] GetData()
		{
			// Inverse operation of SetData

			// First convert to one-dimension array (encode pixels)
			uint[] normalizedData = new uint[this.width * this.height];
			this.LinealCodec(normalizedData, false);

			// Then code normalized data to its format and write to final buffer
			byte[] buffer = new byte[normalizedData.Length * this.Bpp / 8];
			int bufferPos = 0;

			for (int i = 0; i < normalizedData.Length; i++) {
				uint info = PackColor(normalizedData[i], this.format);
				SetValue(buffer, ref bufferPos, this.Bpp, info);
			}

			return buffer;
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

			if (bitPos + size > data.Length)
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

		/// <summary>
		/// Encode and decode "pixel encoding"
		/// </summary>
		/// <param name="linealData">lineal data after or before conversion. Must be initialize.</param>
		/// <param name="decoding">True if the method must decode, false if encode.</param> 
		private void LinealCodec(uint[] linealData, bool decoding)
		{
			if (this.pixelEnc != PixelEncoding.Lineal && this.pixelEnc != PixelEncoding.HorizontalTiles &&
			    this.pixelEnc != PixelEncoding.VerticalTiles)
				throw new NotSupportedException();

			if (linealData == null || linealData.Length != this.width * this.height)
				throw new ArgumentNullException();

			// Little trick to use the same equations
			if (this.pixelEnc == PixelEncoding.Lineal)
				this.tileSize = new Size(this.width, this.height);

			int tileLength = this.tileSize.Width * this.tileSize.Height;
			int numTilesX = this.width / this.tileSize.Width;
			int numTilesY = this.height / this.tileSize.Height;

			for (int h = 0; h < this.height; h++) {
				for (int w = 0; w < this.width; w++) {
					// Get lineal index
					Point pixelPos = new Point(w % this.tileSize.Width, h % this.tileSize.Height); // Pos. pixel in tile
					Point tilePos  = new Point(w / this.tileSize.Width, h / this.tileSize.Height); // Pos. tile in image
					int index = 0;

					if (this.pixelEnc == PixelEncoding.HorizontalTiles)
						index = tilePos.Y * numTilesX * tileLength + tilePos.X * tileLength;	// Absolute tile pos.
					else if (this.pixelEnc == PixelEncoding.VerticalTiles)
						index = tilePos.X * numTilesY * tileLength + tilePos.Y * tileLength;	// Absolute tile pos.

					index += pixelPos.Y * this.tileSize.Width + pixelPos.X;	// Add pos. of pixel inside tile

					if (decoding)
						this.data[w, h] = linealData[index];
					else
						linealData[index] = this.data[w, h];
				}
			}
		}

		/// <summary>
		/// Update data variable to new dimension.
		/// </summary>
		private void ChangeDimension()
		{
			int originalWidth  = this.originalData.GetLength(0);
			int originalHeight = this.originalData.GetLength(1);

			this.data = new uint[this.height, this.width];
			for (int h = 0; h < this.height; h++) {
				for (int w = 0; w < this.width; w++) {

					if (w < originalWidth && h < originalHeight)
						this.data[w, h] = this.originalData[w, h];
					else
						this.data[w, h] = 0;	// If new pixel is outside of the original img -> transparent color
				}
			}
		}
	}
}

