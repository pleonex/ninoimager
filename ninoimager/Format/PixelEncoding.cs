// -----------------------------------------------------------------------
// <copyright file="PixelEncoding.cs" company="none">
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
// <date>20/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Drawing;

namespace Ninoimager.Format
{
	public enum PixelEncoding {
		HorizontalTiles = 0,
		Lineal = 1,
		VerticalTiles,
		Unknown,
	}

	public static class PixelEncodingExtension
	{
		public static bool IsTiled(this PixelEncoding enc)
		{
			switch (enc) {
			case PixelEncoding.HorizontalTiles:
			case PixelEncoding.VerticalTiles:
				return true;

			case PixelEncoding.Lineal:
				return false;

			default:
				throw new FormatException();
			}
		}

        public static void Codec<T>(this PixelEncoding pxEnc, T[] dataIn, T[] dataOut, bool decoding,
		                               int width, int height, Size tileSize)
		{
			if (pxEnc != PixelEncoding.Lineal && pxEnc != PixelEncoding.HorizontalTiles && 
			    pxEnc != PixelEncoding.VerticalTiles)
				throw new NotSupportedException();

			if (dataIn == null || dataOut == null || dataIn.Length > dataOut.Length)
				throw new ArgumentNullException();

			if ((width % tileSize.Width != 0) && 
			    (pxEnc == PixelEncoding.HorizontalTiles || pxEnc == PixelEncoding.VerticalTiles))
				throw new FormatException("Width must be a multiple of tile width to use Tiled pixel encoding.");

			// Little trick to use the same equations
			if (pxEnc == PixelEncoding.Lineal)
				tileSize = new Size(width, height);

			for (int linealIndex = 0; linealIndex < dataOut.Length; linealIndex++) {
				int tiledIndex = pxEnc.GetIndex(linealIndex % width, linealIndex / width, width, height, tileSize);

				if (decoding) {
					// As the new data is lineal, and in the last row of tiles in the dataIn can be incompleted
					// the output array can contains null pixels in the middle of the array.
					if (tiledIndex >= dataIn.Length)
                        dataOut[linealIndex] = default(T);	// Null pixel
					else
						dataOut[linealIndex] = dataIn[tiledIndex];
				} else {
					// As this index will increment lineally, we can stop, there isn't more data to code
					if (linealIndex >= dataIn.Length)
						break;
					dataOut[tiledIndex] = dataIn[linealIndex];
				}
			}
		}

		public static int GetIndex(this PixelEncoding pxEnc, int x, int y, int width, int height, Size tileSize)
		{
			if (pxEnc == PixelEncoding.Lineal)
				return y * width + x;

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
	}
}

