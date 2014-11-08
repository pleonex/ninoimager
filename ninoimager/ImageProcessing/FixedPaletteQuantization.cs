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
		private EmguImage rgbImg;
        private Emgu.CV.Image<LabColor, byte> labImg;
        private LabColor[] labPalette;

		public FixedPaletteQuantization(Color[] fixedPalette)
		{
			this.nearestNeighbour = new ExhaustivePaletteSearch();
            this.Palette = fixedPalette;
		}

        protected override void PreQuantization(EmguImage image)
        {
            // Convert image to Lab color space and get palette
			this.rgbImg = image;
            this.labImg = image.Convert<LabColor, byte>();
            this.labPalette = ColorConversion.ToLabPalette<Color>(this.Palette);
            this.nearestNeighbour.Initialize(labPalette);
        }

        protected override Pixel QuantizatePixel(int x, int y)
        {
            // Get nearest color from palette
            int colorIndex = nearestNeighbour.Search(labImg[y, x]);

            // Apply dithering algorithm for each channel
            LabColor oldPixel = labImg[y, x];
            LabColor newPixel = labPalette[colorIndex];
            this.Dithering.ApplyDithering(labImg.Data, x, y, 0, oldPixel.X - newPixel.X);
            this.Dithering.ApplyDithering(labImg.Data, x, y, 1, oldPixel.Y - newPixel.Y);
            this.Dithering.ApplyDithering(labImg.Data, x, y, 2, oldPixel.Z - newPixel.Z);

			// If it's a transparent color, set the first palette color
			if (this.rgbImg[y, x].Alpha == 0)
				return new Pixel(0, (uint)this.rgbImg[y, x].Alpha, true);
			else
            	return new Pixel((uint)colorIndex, (uint)this.Palette[colorIndex].Alpha, true);
        }

        protected override void PostQuantization()
        {
        }
	}
}

