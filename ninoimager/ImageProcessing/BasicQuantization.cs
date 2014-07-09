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
using Color     = Emgu.CV.Structure.Bgra;
using LabColor  = Emgu.CV.Structure.Lab;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	public class BasicQuantization : ColorQuantization
	{
        private List<Color> listColor;
        private NearestNeighbour<LabColor> nearestNeighbour;
        private EmguImage image;

		public BasicQuantization()
		{
			this.MaxColors = 256;
		}

		public int MaxColors {
			get;
			set;
		}

        protected override void PreQuantization(EmguImage image)
        {
            this.listColor = new List<Color>();
            this.nearestNeighbour = null;
            this.image = image;
        }

        protected override Pixel QuantizatePixel(int x, int y)
        {
            // Get the color and add to the list
            Color color = image[y, x];

            int colorIndex;
            if (listColor.Count < this.MaxColors) {
                if (!listColor.Contains(color))
                    listColor.Add(color);
                colorIndex = listColor.IndexOf(color);
            } else {
                // Create the labpalette if so
                if (nearestNeighbour == null) {
                    LabColor[] labPalette = ColorConversion.ToLabPalette<Color>(listColor.ToArray());
                    nearestNeighbour = new ExhaustivePaletteSearch();
                    nearestNeighbour.Initialize(labPalette);
                }

                LabColor labNoTrans = ColorConversion.ToLabPalette<Color>(new Color[] { color })[0];
                colorIndex = nearestNeighbour.Search(labNoTrans);
            }

            return new Pixel((uint)colorIndex, (uint)color.Alpha, true);
        }

        protected override void PostQuantization()
        {
            this.Palette = listColor.ToArray();
        }
	}
}

