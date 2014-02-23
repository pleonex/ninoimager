// -----------------------------------------------------------------------
// <copyright file="BasicMapping.cs" company="none">
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
	public class SinglePaletteMapping : Mapable
	{
		public override void Map(Pixel[] image)
		{
			List<Pixel[]> tiles = new List<Pixel[]>();
			List<MapInfo> infos = new List<MapInfo>();
			int tileLength = this.TileSize.Width * this.TileSize.Height;

			for (int i = 0; i < image.Length; i += tileLength) {
				// Get tile
				Pixel[] tile = new Pixel[tileLength];
				Array.Copy(image, i, tile, 0, tileLength);

				// TODO: Clean code with flips
				// what about creating a new structure Tile and writing those methods there?
				bool flipX = false;
				bool flipY = false;

				// Check if it's already in the list
				int index = Mapable.Search(tile, tiles);

				// Check flip X
				if (index == -1) {
					Pixel[] tileFlipX = (Pixel[])tile.Clone();
					tileFlipX.FlipX(this.TileSize);
					index = Mapable.Search(tileFlipX, tiles);
					flipX = true;
					flipY = false;

					// Check flip Y
					if (index == -1) {
						Pixel[] tileFlipY = (Pixel[])tile.Clone();
						tileFlipY.FlipY(this.TileSize);
						index = Mapable.Search(tileFlipY, tiles);
						flipX = false;
						flipY = true;
					}

					// Check flip X & Y
					if (index == -1) {
						tileFlipX.FlipY(this.TileSize);
						index = Mapable.Search(tileFlipX, tiles);
						flipX = true;
						flipY = true;
					}
				}

				// Otherwise add
				if (index == -1) {
					tiles.Add(tile);
					index = tiles.Count - 1;
					flipX = false;
					flipY = false;
				}

				// Finally create map info
				infos.Add(new MapInfo(index, 0, flipX, flipY));
			}

			// Get an array of pixels instead of tiles
			Pixel[] linPixels = new Pixel[tiles.Count * tileLength];
			for (int i = 0; i < tiles.Count; i++)
				tiles[i].CopyTo(linPixels, i * tileLength);

			// Set data
			this.mappedImage = linPixels;
			this.mapInfo = infos.ToArray();
		}
	}
}

