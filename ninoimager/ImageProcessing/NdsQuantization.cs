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
using Color     = Emgu.CV.Structure.Rgba;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

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

		public override void Quantizate(EmguImage image)
		{
			base.Quantizate(image);

			// Normalize palette
			this.SortPalette();
			this.AddBackdropColor();
			//this.FillPalette();
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
			Array.Resize(ref this.palette, this.palette.Length + 1);
			for (int i = this.palette.Length - 1; i >= 1; i--)
				this.palette[i] = this.palette[i - 1];
			this.palette[0] = this.BackdropColor;

			// and increment the index of every pixels by 1
			for (int i = 0; i < this.pixels.Length; i++) {
				this.pixels[i] = this.pixels[i].ChangeInfo(pixels[i].Info + 1);
			}
		}

		private void SortPalette()
		{
			// Sort palette
			Color[] messyPalette = (Color[])this.palette.Clone();
			Array.Sort<Color>(this.palette, (c1, c2) => c1.CompareTo(c2));

			// Update pixel index
			for (int i = 0; i < this.pixels.Length; i++) {
				Color oldColor = messyPalette[this.pixels[i].Info];
				int newIndex = Array.FindIndex<Color>(this.palette, c => c.Equals(oldColor));

				this.pixels[i] = this.pixels[i].ChangeInfo((uint)newIndex);
			}
		}

		private void FillPalette()
		{
			// Default color is black, so we only need to resize the palette.
			Array.Resize(ref this.palette, 1 << this.Format.Bpp());
		}
	}
}

