// -----------------------------------------------------------------------
// <copyright file="Importer.cs" company="none">
// Copyright (C) 2013
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
// <date>17/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Ninoimager.Format;
using Size      = System.Drawing.Size;
using Color     = Emgu.CV.Structure.Rgba;
using LabColor  = Emgu.CV.Structure.Lab;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager
{
	public class Importer
	{
		public Importer()
		{
			/* Default parameters: 
				+ Image will be HorizontalTiled
				+ Depth will be 8bpp (256/1).
				+ No PCMP block will be added
				+ No CPOS block will be added
				+ Register DISPCNT will be set to 0 (since it's a BG and those bits aren't used)
				+ Unknown value from CHAR block will be set to 0 (since it seems to be used only with OBJ)
				+ BG Mode will be "Text" (most used)
				+ Transparent color will be magenta: (R:248, G:0, B:248) 
			*/
			this.IncludePcmp = false;
			this.IncludeCpos = false;
			this.DispCnt     = 0;
			this.UnknownChar = 0;
			this.TransparentColor = new Color(248, 0, 248, 255);	// Magenta
			this.BackdropColor = new Color(0, 0, 0, 255);			// Black
			this.BgMode        = BgMode.Text;
			this.DefaultFormat = ColorFormat.Indexed_8bpp;
			this.PixelEncoding = PixelEncoding.HorizontalTiles;
			this.TileSize = new Size(8, 8);
			this.Palette  = null;
		}

		#region Importer parameters
		public bool IncludePcmp {
			get;
			set;
		}

		public bool IncludeCpos {
			get;
			set;
		}

		public uint DispCnt {
			get;
			set;
		}

		public uint UnknownChar {
			get;
			set;
		}

		public Color TransparentColor {
			get;
			set;
		}

		public Color BackdropColor {
			get;
			set;
		}

		public BgMode BgMode {
			get;
			set;
		}

		public ColorFormat DefaultFormat {
			get;
			set;
		}

		public PixelEncoding PixelEncoding {
			get;
			set;
		}

		public Size TileSize {
			get;
			set;
		}

		public Color[] Palette {
			get;
			set;
		}
		#endregion

		/// <summary>
		/// Import a background image creating and writing a NSCR, NCGR and NCLR files to the streams passed.
		/// </summary>
		/// <param name="imgPath">Image path.</param>
		/// <param name="mapStr">Map stream output.</param>
		/// <param name="imgStr">Image stream output.</param>
		/// <param name="palStr">Pal strream output.</param>
		public void ImportBackground(EmguImage newImg, Stream mapStr, Stream imgStr, Stream palStr)
		{
			if (newImg == null || mapStr == null || imgStr == null || palStr == null)
				throw new ArgumentNullException();

			int width  = newImg.Width;
			int height = newImg.Height;
			int maxColors = 1 << this.DefaultFormat.Bpp();

			Pixel[] pixels;
			Color[] palette;

			// If there is no fixed palette, then get palette from the image
			if (this.Palette == null) {
				this.GetIndexImage(newImg, out pixels, out palette);
				if (palette.Length >= maxColors)
					throw new FormatException(string.Format("The image has more than {0} colors", maxColors));
			
				// Normalize palette
				this.AddBackdropColor(pixels, ref palette);
				this.SortPalette(pixels, palette);
				this.FillPalette(ref palette);
			} else {
				this.GetFixedPaletteImage(newImg, out pixels);
				palette = (Color[])this.Palette.Clone();
				if (this.Palette.Length >= maxColors)
					throw new FormatException(string.Format("The fixed palette has more than {0} colors", maxColors));
			}

			// Create palette format
			Nclr nclr = new Nclr() {
				Extended = false
			};
			nclr.SetData(palette, this.DefaultFormat);

			// Create map from pixels
			Nscr nscr = new Nscr() { 
				TileSize = this.TileSize,
				Width    = width, 
				Height   = height,
				BgMode   = this.BgMode
			};
			nscr.PaletteMode = (this.DefaultFormat == ColorFormat.Indexed_4bpp) ?
				PaletteMode.Palette16_16 : PaletteMode.Palette256_1;
			pixels = nscr.CreateMap(pixels);

			// Create image format
			Ncgr ncgr = new Ncgr() {
				RegDispcnt = this.DispCnt,
				Unknown    = this.UnknownChar
			};
			ncgr.Width  = (pixels.Length > 256) ? 256 : pixels.Length;
			ncgr.Height = (int)Math.Ceiling(pixels.Length / (double)ncgr.Width);
			if (ncgr.Height % this.TileSize.Height != 0)
				ncgr.Height += this.TileSize.Height - (ncgr.Height % this.TileSize.Height);
			ncgr.SetData(pixels, this.PixelEncoding, this.DefaultFormat, this.TileSize);

			// Write data
			nclr.Write(palStr);
			ncgr.Write(imgStr);
			nscr.Write(mapStr);
		}

		private void GetIndexImage(EmguImage image, out Pixel[] pixels, out Color[] palette)
		{
			List<Color> listColor = new List<Color>();
			int width  = image.Width;
			int height = image.Height;
			pixels = new Pixel[width * height];

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int index = this.PixelEncoding.GetIndex(x, y, width, height, this.TileSize);

					Color color   = image[y, x];
					Color noTrans = color;
					noTrans.Alpha = 255;

					if (!listColor.Contains(noTrans))
						listColor.Add(noTrans);

					int colorIndex = listColor.IndexOf(noTrans);
					pixels[index] = new Pixel((uint)colorIndex, (uint)color.Alpha, true);
				}
			}

			palette = listColor.ToArray();
		}

		/// <summary>
		/// Gets an indexed image from a fixed palette. Floyd-Steinberg algorithm is applied
		/// for dithering. (https://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering)
		/// </summary>
		/// <param name="image">The source image.</param>
		/// <param name="pixels">The output indexed pixels.</param>
		/// <param name="palette">The fixed palette.</param>
		private void GetFixedPaletteImage(EmguImage image, out Pixel[] pixels)
		{
			int width  = image.Width;
			int height = image.Height;
			pixels = new Pixel[width * height];

			// Convert image to Lab color space and get palette
			Emgu.CV.Image<LabColor, byte> labImg = image.Convert<LabColor, byte>();
			FixedPalette fixedPalette = FixedPalette.FromAnyColor<Color>(this.Palette);

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int index = this.PixelEncoding.GetIndex(x, y, width, height, this.TileSize);

					// Get nearest color from palette
					int colorIndex = fixedPalette.GetNearestIndex(labImg[y, x]);

					// UNDONE: Apply Floyd-Steinberg algorithm
					pixels[index]  = new Pixel((uint)colorIndex, (uint)this.Palette[colorIndex].Alpha, true);
				}
			}
		}

		private void AddBackdropColor(Pixel[] pixels, ref Color[] palette)
		{
			// Add the color to the first place of the palette...
			Array.Resize(ref palette, palette.Length + 1);
			for (int i = palette.Length - 1; i >= 1; i--)
				palette[i] = palette[i - 1];
			palette[0] = this.BackdropColor;

			// and increment the index of every pixels by 1
			for (int i = 0; i < pixels.Length; i++) {
				pixels[i] = pixels[i].ChangeInfo(pixels[i].Info + 1);
			}
		}

		private void SortPalette(Pixel[] pixels, Color[] palette)
		{
			Color[] messyPalette = (Color[])palette.Clone();
			Array.Sort<Color>(palette, (c1, c2) => c1.CompareTo(c2));

			for (int i = 0; i < pixels.Length; i++) {
				Color oldColor = messyPalette[pixels[i].Info];
				int newIndex = Array.FindIndex<Color>(palette, c => c.Equals(oldColor));

				pixels[i] = pixels[i].ChangeInfo((uint)newIndex);
			}
		}

		private void FillPalette(ref Color[] palette)
		{
			// Default color is black, so we only need to resize it.
			Array.Resize(ref palette, 1 << this.DefaultFormat.Bpp());
		}
	}
}

