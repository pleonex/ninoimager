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
		protected Dithering<byte> dithering;
		protected Pixel[] pixels;
		protected Color[] palette;

		public ColorQuantization()
		{
			this.dithering = new NoDithering<byte>();
			this.PixelEncoding = PixelEncoding.HorizontalTiles;
			this.TileSize = new Size(8, 8);
		}

		public PixelEncoding PixelEncoding {
			get;
			set;
		}

		public Size TileSize {
			get;
			set;
		}

		public abstract void Quantizate(EmguImage image);

		public Pixel[] GetPixels()
		{
			return this.pixels;
		}

		public Color[] GetPalette()
		{
			return this.palette;
		}
	}
}

