// -----------------------------------------------------------------------
// <copyright file="PaletteQuantization.cs" company="none">
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
using Size      = System.Drawing.Size;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	public abstract class ColorQuantization
	{
        private Pixel[] pixelsLineal;
        private Pixel[] pixelsHorizontal;
        private int width;
        private int height;

		public ColorQuantization()
		{
            this.Dithering = new NoDithering<byte>();
			this.TileSize = new Size(8, 8);
		}

		public Size TileSize {
			get;
			set;
		}

        protected Dithering<byte> Dithering {
            get;
            set;
        }

		public void Quantizate(EmguImage image)
        {
            this.width  = image.Width;
            this.height = image.Height;

            this.pixelsLineal     = new Pixel[width * height];
            this.pixelsHorizontal = new Pixel[width * height];
            this.PreQuantization(image);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Pixel px = this.QuantizatePixel(x, y);
                    this.SetPixel(x, y, px);
                }
            }

            this.PostQuantization();
        }

        protected abstract void PreQuantization(EmguImage image);

        protected abstract Pixel QuantizatePixel(int x, int y);

        protected abstract void PostQuantization();

        public Pixel[] GetPixels(PixelEncoding enc)
		{
            if (enc == PixelEncoding.HorizontalTiles)
                return this.pixelsHorizontal;
            else if (enc == PixelEncoding.Lineal)
                return this.pixelsLineal;
            else
                return null;
		}

        private void SetPixel(int x, int y, Pixel px)
        {
            int horizoIdx = PixelEncoding.HorizontalTiles.GetIndex(x, y, width, height, this.TileSize);
            this.pixelsHorizontal[horizoIdx] = px;

            int linealIdx = PixelEncoding.Lineal.GetIndex(x, y, width, height, new Size(width, height));
            this.pixelsLineal[linealIdx] = px;
        }

        public Color[] Palette {
            get;
            protected set;
        }
	}
}

