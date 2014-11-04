// -----------------------------------------------------------------------
// <copyright file="TextureImporter.cs" company="none">
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
// <date>04/11/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninoimager.Format;
using Ninoimager.ImageProcessing;
using Size      = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using Color     = Emgu.CV.Structure.Bgra;
using LabColor  = Emgu.CV.Structure.Lab;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager
{
	public class TextureImporter
	{
		public TextureImporter(Btx0 texture)
		{
			this.Texture = texture;
			this.Format  = ColorFormat.Indexed_8bpp;
			this.Quantization = new NdsQuantization();
		}

		public Btx0 Texture {
			get;
			set;
		}

		public ColorQuantization Quantization {
			get;
			set;
		}

		public ColorFormat Format {
			get;
			set;
		}

		public void AddImage(EmguImage newImg, string name)
		{
			if (newImg == null)
				throw new ArgumentNullException();

			// Quantizate image -> get pixels and palette
			this.Quantization.Quantizate(newImg);
			Pixel[] pixels = this.Quantization.GetPixels(PixelEncoding.Lineal);
			Color[] colors = this.Quantization.Palette;

			int maxColors = 1 << this.Format.Bpp();
			if (colors.Length > maxColors)
				throw new FormatException(string.Format("The image has more than {0} colors", maxColors));

			// Create image format
			Image image = new Image();
			image.Width = newImg.Width;
			image.Height = newImg.Height;
			image.SetData(pixels, PixelEncoding.Lineal, this.Format);

			// Create the palette
			Palette palette = new Palette(colors);

			// Add the image
			this.Texture.AddImage(image, palette, name);
		}

		public void RemoveImages()
		{
			this.Texture.RemoveImages();
		}
	}
}

