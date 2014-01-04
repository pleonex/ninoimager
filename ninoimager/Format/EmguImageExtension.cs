// -----------------------------------------------------------------------
// <copyright file="EmguImageExtension.cs" company="none">
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
// <date>04/01/2014</date>
// -----------------------------------------------------------------------
using System;
using Rectangle = System.Drawing.Rectangle;
using Color     = Emgu.CV.Structure.Rgba;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.Format
{
	public static class EmguImageExtension
	{
		public static void SetPixel(this EmguImage img, int x, int y, Color color)
		{
			img.Data[y, x, 0] = (byte)color.Red;
			img.Data[y, x, 1] = (byte)color.Green;
			img.Data[y, x, 2] = (byte)color.Blue;
			img.Data[y, x, 3] = (byte)color.Alpha;
		}

		public static void SetPixels(this EmguImage img, int x, int y, int width, int height, Color[] pixels)
		{
			img.SetPixels(new Rectangle(x, y, width, height), pixels);
		}

		public static void SetPixels(this EmguImage img, Rectangle area, Color[] pixels)
		{
			// Area dimensions
			int xStart = area.X;
			int xEnd   = xStart + area.Width;
			int yStart = area.Y;
			int yEnd   = yStart + area.Height;

			// Area checks
			if (xStart < 0 || xEnd > img.Width || yStart < 0 || yEnd > img.Height)
				throw new ArgumentOutOfRangeException("area", area, "The are does not fill in the image");

			if (area.Width * area.Height != pixels.Length)
				throw new ArgumentOutOfRangeException("pixels", pixels, "Invalid number of pixels");

			// Data, it's faster not to iterate over a property
			byte[,,] data = img.Data;

			// Assign color to each pixel
			for (int y = yStart; y < yEnd; y++) {
				for (int x = xStart; x < xEnd; x++) {
					int cIdx = (y - yStart) * area.Width + (x - xStart);
					data[y, x, 0] = (byte)pixels[cIdx].Red;
					data[y, x, 1] = (byte)pixels[cIdx].Green;
					data[y, x, 2] = (byte)pixels[cIdx].Blue;
					data[y, x, 3] = (byte)pixels[cIdx].Alpha;
				}
			}

			// Assign new data
			img.Data = data;
		}
	}
}

