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

//#define DITHERING
using System;
using System.Collections.Generic;
using System.IO;
using Ninoimager.Format;
using Ninoimager.ImageProcessing;
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
			this.BgMode      = BgMode.Text;
			this.Format      = ColorFormat.Indexed_8bpp;
			this.TileSize    = new Size(8, 8);
			this.PixelEncoding = PixelEncoding.HorizontalTiles;

			this.Quantization = new NdsQuantization();
		}

		#region Importer parameters
		public ColorQuantization Quantization {
			get;
			set;
		}

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

		public BgMode BgMode {
			get;
			set;
		}

		public ColorFormat Format {
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

		public bool ExtendedPalette {
			get;
			set;
		}

		public PaletteMode PaletteMode {
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
			int maxColors = 1 << this.Format.Bpp();

			// Quantizate image -> get pixels and palette
			this.Quantization.Quantizate(newImg);
			Pixel[] pixels  = this.Quantization.GetPixels();
			Color[] palette = this.Quantization.GetPalette();
			if (palette.Length > maxColors)
				throw new FormatException(string.Format("The image has more than {0} colors", maxColors));

			// Create palette format
			Nclr nclr = new Nclr() {
				Extended = this.ExtendedPalette
			};
			nclr.SetData(palette, this.Format);

			// Create map from pixels
			Nscr nscr = new Nscr() { 
				TileSize    = this.TileSize,
				Width       = width, 
				Height      = height,
				BgMode      = this.BgMode,
				PaletteMode = this.PaletteMode
			};
			nscr.PaletteMode = (this.Format == ColorFormat.Indexed_4bpp) ?
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
			ncgr.SetData(pixels, this.PixelEncoding, this.Format, this.TileSize);

			// Write data
			nclr.Write(palStr);
			ncgr.Write(imgStr);
			nscr.Write(mapStr);
		}
	}
}

