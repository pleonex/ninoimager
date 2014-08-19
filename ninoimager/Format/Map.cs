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
using System.Collections.Generic;
using Size      = System.Drawing.Size;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.Format
{
	public class Map
	{
		private MapInfo[] info;
		private Mapable mapping;

		private Size tileSize;
		private int width;
		private int height;
		private BgMode bgMode;

		public Map()
		{
			this.info     = null;
			this.mapping  = new CompressMapping();
			this.tileSize = new Size(0, 0);
			this.width    = 0;
			this.height   = 0;
		}

		public Mapable Mapping {
			get { return this.mapping; }
			set { 
				this.mapping = value;
				this.mapping.TileSize = this.tileSize;
			 }
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
			set { 
				this.tileSize = value;
				this.mapping.TileSize = value; 
			}
		}

		public BgMode BgMode {
			get { return this.bgMode; }
			set { this.bgMode = value; }
		}

		public EmguImage CreateBitmap(Image image, Palette palette)
		{
			// UNDONE: Support Text mode and pixel areas
			if (this.bgMode == BgMode.Text && (this.width > 256 || this.height > 256))
				throw new NotSupportedException("Text modes with multiple pixel ares not supported.");

			// TODO: Try to change the tile size of the image
			if (this.tileSize != image.TileSize)
				throw new FormatException("Image with different tile size");

			// TODO: Try to convert image to tiled
			if (!image.PixelEncoding.IsTiled())
				throw new FormatException("Image not tiled.");

			Pixel[] mapImage = new Pixel[this.width * this.height];
			uint[] tmpIndex = new uint[this.width * this.height];

			int count = 0;
			foreach (MapInfo info in this.info) {
				Pixel[] tile = image.GetTile(info.TileIndex);
				if (info.FlipX)
					tile.FlipX(this.tileSize);
				if (info.FlipY)
					tile.FlipY(this.tileSize);

				tile.CopyTo(mapImage, count);

				for (int i = 0; i < tile.Length; i++)
					tmpIndex[count + i] = (uint)info.PaletteIndex;

				count += tile.Length;
			}

			// Palette Index must be lineal but it's tiled, convert it.
			uint[] palIndex = new uint[this.width * this.height];
			image.PixelEncoding.Codec(tmpIndex, palIndex, true, this.width, this.height, image.TileSize);

			Image finalImg = new Image(
				mapImage,
			    this.width,
			    this.height,
			    image.PixelEncoding,
			    image.Format,
			    image.TileSize
			);
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

		public Pixel[] CreateMap(Pixel[] pixels)
		{
			this.mapping.Map(pixels);

			this.SetMapInfo(this.mapping.GetMapInfo());
			return this.mapping.GetMappedImage();
		}

		public Pixel[] CreateMap(Pixel[] pixels, int[] palettes)
		{
			this.mapping.Map(pixels, palettes);

			this.SetMapInfo(this.mapping.GetMapInfo());
			return this.mapping.GetMappedImage();
		}
	}
}
