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

		public void AddImage(EmguImage newImg, string name, Color[] palOut = null)
		{
			if (newImg == null)
				throw new ArgumentNullException();

			// First, let's be sure the dimension are power of 2.
			if (!IsPowerOfTwo((uint)newImg.Width)) {
				int width = 1 << (int)(Math.Log(newImg.Width, 2) + 1);
				EmguImage blankVertical = new EmguImage(width - newImg.Width, newImg.Height);
				newImg = newImg.ConcateHorizontal(blankVertical);
				Console.Write("(Warning: Width not power of 2) ");
			}

			if (!IsPowerOfTwo((uint)newImg.Height)) {
				int height = 1 << (int)(Math.Log(newImg.Height, 2) + 1);
				EmguImage blankHorizontal = new EmguImage(newImg.Width, height - newImg.Height);
				newImg = newImg.ConcateVertical(blankHorizontal);
				Console.Write("(Warning: Height not power of 2) ");
			}

			// Quantizate image -> get pixels and palette
			this.Quantization.Quantizate(newImg);
			Pixel[] pixels = this.Quantization.GetPixels(PixelEncoding.Lineal);
			Color[] colors = (palOut == null) ? this.Quantization.Palette : palOut;

			int maxColors = 1 << this.Format.Bpp();
			if (colors.Length > maxColors)
				throw new FormatException(string.Format("The image has more than {0} colors", maxColors));

			// Create image format
			Image image = new Image();
			image.Width = newImg.Width;
			image.Height = newImg.Height;
			image.SetData(pixels, PixelEncoding.Lineal, this.Format, new Size(1, 1));

			// Create the palette
			Palette palette = new Palette(colors);

			// Add the image
			this.Texture.AddImage(image, palette, name);
		}

		public void AddImage(EmguImage newImg, string name, int[] texUnk, int[] palUnk,
			Color[] palOut = null)
		{
			// Add image
			this.AddImage(newImg, name, palOut);

			// Set unknowns values
			this.Texture.SetTextureUnknowns(this.Texture.NumTextures - 1, texUnk);
			this.Texture.SetPaletteUnknowns(this.Texture.NumPalettes - 1, palUnk);
		}
			
		public void RemoveImages()
		{
			this.Texture.RemoveImages();
		}

		private static bool IsPowerOfTwo(ulong x)
		{
			return (x != 0) && ((x & (x - 1)) == 0);
		}
	}
}

