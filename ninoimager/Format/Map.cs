// -----------------------------------------------------------------------
// <copyright file="Map.cs" company="none">
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
// <date>15/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Drawing;

namespace Ninoimager.Format
{
	// For more info see GbaTek
	public enum BgMode {
		Text     = 0,
		Affine   = 1,	// Palette must be 8bpp
		Extended = 2,	// Extended mode -> Text | Affine, not bitmap
	}

	public struct MapInfo
	{
		public MapInfo(int tileIndex, int paletteIndex, bool flipX, bool flipY)
			: this()
		{
			this.TileIndex = tileIndex;
			this.PaletteIndex = paletteIndex;
			this.FlipX = flipX;
			this.FlipY = flipY;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ninoimager.Format.MapInfo"/> struct.
		/// Text and extended mode.
		/// </summary>
		/// <param name="value">Value.</param>
		public MapInfo(ushort value)
			: this()
		{
			this.TileIndex    = (value >> 00) & 0x3FF;
			this.PaletteIndex = (value >> 12) & 0x0F;
			this.FlipX        = ((value >> 10) & 0x01) == 1;
			this.FlipY        = ((value >> 11) & 0x01) == 1;

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ninoimager.Format.MapInfo"/> struct.
		/// Affine (rotation / scaling) mode.
		/// </summary>
		/// <param name="value">Value.</param>
		public MapInfo(byte value)
			: this()
		{
			this.TileIndex    = value;
			this.PaletteIndex = 0;
			this.FlipX        = false;
			this.FlipY        = false;
		}

		public int TileIndex {
			get;
			private set;
		}

		public int PaletteIndex {
			get;
			private set;
		}

		public bool FlipX {
			get;
			private set;
		}

		public bool FlipY {
			get;
			private set;
		}

		public byte ToByte()
		{
			return (byte)this.TileIndex;
		}

		public ushort ToUInt16()
		{
			return (ushort)(
				(this.TileIndex << 00) |
				(this.PaletteIndex << 12) |
				((this.FlipX ? 1 : 0) << 10) |
				((this.FlipY ? 1 : 0) << 11));
		}
	}

	public class Map
	{
		private MapInfo[] info;

		private Size tileSize;
		private int width;
		private int height;
		private BgMode bgMode;

		public Map()
		{
			this.info     = null;
			this.tileSize = new Size(0, 0);
			this.width    = 0;
			this.height   = 0;
		}

		public int Width {
			get { return this.width; }
			set {
				if ((value % this.tileSize.Width) != 0)
					throw new ArgumentException("Width must be a multiple of the tile width");

				this.width = value;
			}
		}

		public int Height {
			get { return this.height; }
			set {
				if ((value % this.tileSize.Height) != 0)
					throw new ArgumentException("Height must be a multiple of the tile height.");

				this.height = value;
			}
		}

		public Size TileSize {
			get { return this.tileSize; }
			set { this.tileSize = value; }
		}

		public BgMode BgMode {
			get { return this.bgMode; }
			set { this.bgMode = value; }
		}

		public Bitmap CreateBitmap(Image image, Palette palette)
		{
			// UNDONE: Support Text mode and pixel areas
			if (this.bgMode == BgMode.Text && (this.width > 256 || this.height > 256))
				throw new NotSupportedException("Text modes with multiple pixel ares not supported.");

			// TODO: Try to change the tile size of the image
			if (this.tileSize != image.TileSize)
				throw new FormatException("Image with different tile size");

			// TODO: Try to convert image to tiled
			if (!image.IsTiled)
				throw new FormatException("Image not tiled.");

			Pixel[] mapImage = new Pixel[this.width * this.height];
			uint[] tmpIndex = new uint[this.width * this.height];

			int count = 0;
			foreach (MapInfo info in this.info) {
				Pixel[] tile = image.GetTile(info.TileIndex);
				if (info.FlipX)
					FlipX(tile, this.tileSize);
				if (info.FlipY)
					FlipY(tile, this.tileSize);

				tile.CopyTo(mapImage, count);

				for (int i = 0; i < tile.Length; i++)
					tmpIndex[count + i] = (uint)info.PaletteIndex;

				count += tile.Length;
			}

			// Palette Index must be lineal but it's tiled, convert it.
			uint[] palIndex = new uint[this.width * this.height];
			Image.LinealCodec(tmpIndex, palIndex, true, image.PixelEncoding, this.width, this.height, image.TileSize);

			Image finalImg = new Image(
				mapImage,
			    this.width,
			    this.height,
			    image.PixelEncoding,
			    image.Format,
			    image.TileSize);
			return finalImg.CreateBitmap(palette, palIndex);
		}

		public void SetMapInfo(MapInfo[] mapInfo)
		{
			this.info = (MapInfo[])mapInfo.Clone();
		}

		public MapInfo[] GetMapInfo()
		{
			return (MapInfo[])this.info.Clone();
		}

		private static void FlipX(Pixel[] tile, Size tileSize)
		{
			for (int y = 0; y < tileSize.Height; y++) {
				for (int x = 0; x < tileSize.Width / 2; x++) {
					int t1 = y * tileSize.Width + x;
					int t2 = y * tileSize.Width + (tileSize.Width - 1 - x);

					Pixel swap = tile[t1];
					tile[t1] = tile[t2];
					tile[t2] = swap;
				}
			}
		}

		private static void FlipY(Pixel[] tile, Size tileSize)
		{
			for (int x = 0; x < tileSize.Width; x++) {
				for (int y = 0; y < tileSize.Height / 2; y++) {
					int t1 = x + tileSize.Width * y;
					int t2 = x + tileSize.Width * (tileSize.Height - 1 - y);

					Pixel swap = tile[t1];
					tile[t1] = tile[t2];
					tile[t2] = swap;
				}
			}

		}
	}
}
