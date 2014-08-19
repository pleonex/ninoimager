// -----------------------------------------------------------------------
// <copyright file="MatchMapping.cs" company="none">
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
// <date>02/23/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Ninoimager.Format
{
	public class MatchMapping : CompressMapping
	{
		public MatchMapping(Pixel[] mappedImage)
		{
			this.mappedImage = mappedImage;
		}

		public override void Map(Pixel[] image)
		{
			List<Pixel[]> tiles = new List<Pixel[]>();
			List<MapInfo> infos = new List<MapInfo>();
			int tileLength = this.TileSize.Width * this.TileSize.Height;

			// Get tiles
			for (int i = 0; i < mappedImage.Length; i += tileLength) {
				Pixel[] tile = new Pixel[tileLength];
				Array.Copy(mappedImage, i, tile, 0, tileLength);
				tiles.Add(tile);
			}

			// Perfom search
			for (int i = 0; i < image.Length; i += tileLength) {
				// Get tile
				Pixel[] tile = new Pixel[tileLength];
				Array.Copy(image, i, tile, 0, tileLength);

				bool flipX;
				bool flipY;
				int index = CompressMapping.Search(tile, tiles, this.TileSize, out flipX, out flipY);

				if (index == -1)
					throw new Exception("Tile not found.");

				// Finally create map info
				infos.Add(new MapInfo(index, 0, flipX, flipY));
			}

			this.mapInfo = infos.ToArray();
		}
	}
}

