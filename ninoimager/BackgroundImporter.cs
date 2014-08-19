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
	public class BackgroundImporter
	{
		public BackgroundImporter()
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

		public void SetOriginalSettings(Stream mapStr, Stream imgStr, Stream palStr)
		{
			// Set original palette settings
			if (palStr != null) {
				Nclr nclr = new Nclr(palStr);
				this.Quantization = new FixedPaletteQuantization(nclr.GetPalette(0));
				this.ExtendedPalette = nclr.Extended;
			}

			// Set original image settings if the file is not compressed
			if (imgStr != null && imgStr.ReadByte() == 0x52) {
				imgStr.Position -= 1;

				Ncgr ncgr = new Ncgr(imgStr);
				this.DispCnt       = ncgr.RegDispcnt;
				this.UnknownChar   = ncgr.Unknown;
				this.Format        = ncgr.Format;
				this.IncludeCpos   = ncgr.HasCpos;
				this.PixelEncoding = ncgr.PixelEncoding;
			}

			// Set original map settings
			if (mapStr != null && mapStr.ReadByte() == 0x52) {
				mapStr.Position -= 1;

				Nscr nscr = new Nscr(mapStr);
				this.BgMode      = nscr.BgMode;
				this.PaletteMode = nscr.PaletteMode;
				this.TileSize    = nscr.TileSize;
			}
		}

		/// <summary>
		/// Import a background image creating and writing a NSCR, NCGR and NCLR files to the streams passed.
		/// </summary>
		/// <param name="imgPath">Image path.</param>
		/// <param name="mapStr">Map stream output.</param>
		/// <param name="imgStr">Image stream output.</param>
		/// <param name="palStr">Pal strream output.</param>
		public void ImportBackground(EmguImage newImg, Stream mapStr, Stream imgStr, Stream palStr)
		{
			if (newImg == null)
				throw new ArgumentNullException();

			int width  = newImg.Width;
			int height = newImg.Height;
			int maxColors = 1 << this.Format.Bpp();

			Pixel[] pixels;
			Color[] palette;
			List<int> mapPalette = new List<int>();
			bool is16ColFixed = (PaletteMode == PaletteMode.Palette16_16) && (Quantization is FixedPaletteQuantization);
			if (!is16ColFixed) {
				// Quantizate image -> get pixels and palette
				this.Quantization.Quantizate(newImg);
				pixels  = this.Quantization.GetPixels(this.PixelEncoding);
				palette = this.Quantization.Palette;
				if (palette.Length > maxColors)
					throw new FormatException(string.Format("The image has more than {0} colors", maxColors));
			} else {
				palette = this.Quantization.Palette;
				ManyFixedPaletteQuantization quant = new ManyFixedPaletteQuantization(
					this.Quantization.Palette.Split(16).ToArray());
					
				List<Pixel> pixelList = new List<Pixel>();
				for (int y = 0; y < newImg.Height; y += this.TileSize.Height) {
					for (int x = 0; x < newImg.Width; x += this.TileSize.Width) {
						Rectangle subArea  = new Rectangle(x, y, this.TileSize.Width, this.TileSize.Height);
						EmguImage subImage = newImg.Copy(subArea);
						quant.Quantizate(subImage);
						mapPalette.Add(quant.SelectedPalette);
						pixelList.AddRange(quant.GetPixels(PixelEncoding.Lineal));
					}
				}

				pixels = pixelList.ToArray();
			}

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

			if (!is16ColFixed) {
				nscr.PaletteMode = (this.Format == ColorFormat.Indexed_4bpp) ?
					PaletteMode.Palette16_16 : PaletteMode.Palette256_1;
				pixels = nscr.CreateMap(pixels);
			} else {
				nscr.PaletteMode = PaletteMode.Palette16_16;
				pixels = nscr.CreateMap(pixels, mapPalette.ToArray());
			}

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
			if (palStr != null)
				nclr.Write(palStr);
			if (imgStr != null)
				ncgr.Write(imgStr);
			if (mapStr != null)
				nscr.Write(mapStr);
		}

		public void ImportBackgroundShareImage(EmguImage newImg, Pixel[] fullImg, Stream mapStr)
		{
			if (newImg == null || mapStr == null)
				throw new ArgumentNullException();

			int width  = newImg.Width;
			int height = newImg.Height;
			int maxColors = 1 << this.Format.Bpp();

			// Quantizate image -> get pixels
			this.Quantization.Quantizate(newImg);
            Pixel[] pixels  = this.Quantization.GetPixels(this.PixelEncoding);
			if (this.Quantization.Palette.Length > maxColors)
				throw new FormatException(string.Format("The image has more than {0} colors", maxColors));

			// Create map
			Nscr nscr = new Nscr() {
				TileSize    = this.TileSize,
				Width       = width,
				Height      = height,
				BgMode      = this.BgMode,
				PaletteMode = PaletteMode.Palette256_1,
				Mapping = new MatchMapping(fullImg)
			};
			nscr.CreateMap(pixels);
			nscr.Write(mapStr);
		}
	}
}

