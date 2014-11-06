// -----------------------------------------------------------------------
// <copyright file="Mapable.cs" company="none">
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
using Size = System.Drawing.Size;

namespace Ninoimager.Format
{
	public abstract class Mapable
	{
		protected Pixel[] mappedImage;
		protected MapInfo[] mapInfo;

		public Mapable()
		{
			this.TileSize = new Size(0, 0);
		}

		public Size TileSize {
			get;
			set;
		}

		public abstract void Map(Pixel[] image);

		public abstract void Map(Pixel[] image, int[] palettes);

		public Pixel[] GetMappedImage()
		{
			return this.mappedImage;
		}

		public MapInfo[] GetMapInfo()
		{
			return this.mapInfo;
		}

		protected static int Search(Pixel[] tile, List<Pixel[]> tiles)
		{
			for (int k = 0; k < tiles.Count; k++) {
				bool result = true;
				for (int i = 0; i < tiles[k].Length && result; i++)
					result = (tile[i].Equals(tiles[k][i]));

				if (result)
					return k;
			}

			return -1;
		}
	}
}

