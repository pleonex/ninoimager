// -----------------------------------------------------------------------
// <copyright file="FixedPalette.cs" company="none">
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
using Ninoimager.Format;
using Color     = Emgu.CV.Structure.Bgra;
using LabColor  = Emgu.CV.Structure.Lab;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	public class FixedPaletteQuantization : ColorQuantization
	{
		private NearestNeighbour<LabColor> nearestNeighbour;

		public FixedPaletteQuantization(Color[] fixedPalette)
		{
			this.nearestNeighbour = new ExhaustivePaletteSearch();
			this.palette = fixedPalette;
		}

		public override void Quantizate(EmguImage image)
		{
			int width  = image.Width;
			int height = image.Height;
			this.pixels = new Pixel[width * height];

			// Convert image to Lab color space and get palette
			Emgu.CV.Image<LabColor, byte> labImg = image.Convert<LabColor, byte>();
			LabColor[] labPalette = ColorConversion.ToLabPalette<Color>(this.palette);
			this.nearestNeighbour.Initialize(labPalette);

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					// Get nearest color from palette
					int colorIndex = nearestNeighbour.Search(labImg[y, x]);

					// Apply dithering algorithm for each channel
					LabColor oldPixel = labImg[y, x];
					LabColor newPixel = labPalette[colorIndex];
					this.dithering.ApplyDithering(labImg.Data, x, y, 0, oldPixel.X - newPixel.X);
					this.dithering.ApplyDithering(labImg.Data, x, y, 1, oldPixel.Y - newPixel.Y);
					this.dithering.ApplyDithering(labImg.Data, x, y, 2, oldPixel.Z - newPixel.Z);

					// Finally set the new pixel into the array
					int index = this.PixelEncoding.GetIndex(x, y, width, height, this.TileSize);
					this.pixels[index]  = new Pixel((uint)colorIndex, (uint)this.palette[colorIndex].Alpha, true);
				}
			}
		}
	}
}

