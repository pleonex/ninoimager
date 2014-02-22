// -----------------------------------------------------------------------
// <copyright file="BasicQuantization.cs" company="none">
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
// <date>02/22/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Ninoimager.Format;
using Color     = Emgu.CV.Structure.Rgba;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	public class BasicQuantization : ColorQuantization
	{
		public override void Quantizate(EmguImage image)
		{
			List<Color> listColor = new List<Color>();

			int width = image.Width;
			int height = image.Height;
			this.pixels = new Pixel[width * height];

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					// Get the color without the alpha channel and add to the list
					Color color   = image[y, x];
					Color noTrans = color;
					noTrans.Alpha = 255;

					if (!listColor.Contains(noTrans))
						listColor.Add(noTrans);

					int colorIndex = listColor.IndexOf(noTrans);

					// Finally set the color
					int index = this.PixelEncoding.GetIndex(x, y, width, height, this.TileSize);
					this.pixels[index] = new Pixel((uint)colorIndex, (uint)color.Alpha, true);
				}
			}

			this.palette = listColor.ToArray();
		}
	}
}

