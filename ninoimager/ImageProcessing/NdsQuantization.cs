// -----------------------------------------------------------------------
// <copyright file="NdsQuantization.cs" company="none">
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
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	/// <summary>
	/// Aproximation to the Nitro SDK quantization
	/// </summary>
	public class NdsQuantization : BasicQuantization
	{
		private ColorFormat format;

		public NdsQuantization()
		{
			this.BackdropColor = new Color(0, 0, 0, 255);	// Black
			this.Format        = ColorFormat.Indexed_8bpp;
		}

        protected override void PostQuantization()
        {
            base.PostQuantization();

            // Normalize palette
            this.SortPalette();
            this.AddBackdropColor();
            this.FillPalette(); // Especially useful when format is 16/16 to divide palettes.
        }

		public Color BackdropColor {
			get;
			set;
		}

		public ColorFormat Format {
			get { return this.format; }
			set {
				this.format = value;
				this.MaxColors = (1 << this.format.Bpp()) - 1;	// Reserve space for BackdropColor
			}
		}

		private void AddBackdropColor()
		{
			// Add the color to the first place of the palette...
            Color[] palette = this.Palette;
			Array.Resize(ref palette, palette.Length + 1);
			for (int i = palette.Length - 1; i >= 1; i--)
				palette[i] = palette[i - 1];

			palette[0] = BackdropColor;
            this.Palette = palette;

			// and increment the index of every pixels by 1
            Pixel[] pixels = this.GetPixels(PixelEncoding.Lineal);
			for (int i = 0; i < pixels.Length; i++) {
				pixels[i] = pixels[i].ChangeInfo(pixels[i].Info + 1);
			}

            pixels = this.GetPixels(PixelEncoding.HorizontalTiles);
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = pixels[i].ChangeInfo(pixels[i].Info + 1);
            }
		}

		private void SortPalette()
		{
			// Sort palette
            Color[] palette = this.Palette;
			Color[] messyPalette = (Color[])palette.Clone();
			Array.Sort<Color>(palette, (c1, c2) => c1.CompareTo(c2));
            this.Palette = palette;

			// Update pixel index
            Pixel[] pixels = this.GetPixels(PixelEncoding.Lineal);
			for (int i = 0; i < pixels.Length; i++) {
				Color oldColor = messyPalette[pixels[i].Info];
				int newIndex = Array.FindIndex<Color>(palette, c => c.Equals(oldColor));

				pixels[i] = pixels[i].ChangeInfo((uint)newIndex);
			}

            pixels = this.GetPixels(PixelEncoding.HorizontalTiles);
            for (int i = 0; i < pixels.Length; i++) {
                Color oldColor = messyPalette[pixels[i].Info];
                int newIndex = Array.FindIndex<Color>(palette, c => c.Equals(oldColor));

                pixels[i] = pixels[i].ChangeInfo((uint)newIndex);
            }
		}

		private void FillPalette()
		{
			// Default color is black, so we only need to resize the palette.
            Color[] palette = this.Palette;
			Array.Resize(ref palette, 1 << this.Format.Bpp());
            this.Palette = palette;
		}
	}
}

