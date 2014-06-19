// -----------------------------------------------------------------------
// <copyright file="SpriteImporter.cs" company="none">
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
// <date>03/27/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Ninoimager.Format;
using Ninoimager.ImageProcessing;
using Size      = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager
{
	/// <summary>
	/// Generate a sprite file format from several frames.
	/// </summary>
	/// <description>
	/// The process of image to frame conversion will take the following steps:
	///  1 Generate frames
	/// 	* Input an image file (EmguImage)
	/// 	* Split the image into objects -> create the objects
	///	 2 Generate palettes
	/// 	* Quantizate each frame image
	/// 	* Reduce to only 16 palettes
	///  3 Generate output files
	/// 	* Concatenate all the frame images and use similar method as background importer.
	/// </description>
	public class SpriteImporter
	{
		private List<Tuple<Frame, EmguImage>> frameData;
        private ColorFormat format;

		public SpriteImporter()
		{
			this.frameData = new List<Tuple<Frame, EmguImage>>();

			// Default settings
			this.BgMode = BgMode.Text;
			this.Format = ColorFormat.Indexed_4bpp;
            this.DispCnt       = 0x00200010;
			this.ObjectMode    = ObjMode.Normal;
			this.PaletteMode   = PaletteMode.Palette16_16;
			this.TileSize      = new System.Drawing.Size(64, 64);
            this.TransparentColor   = new Color(248, 0, 248, 255);
			this.UseRectangularArea = true;
			this.Quantization     = new NdsQuantization() { 
				BackdropColor = this.TransparentColor,
                Format = this.Format
			};
			this.Reducer       = new SimilarDistanceReducer();
			this.Splitter      = new NdsSplitter(1);
		}

		#region Propiedades

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
            get { return this.format; }
            set {
                this.format = value;
                NdsQuantization ndsQuant = this.Quantization as NdsQuantization;
                if (ndsQuant != null)
                    ndsQuant.Format = value;
            }
		}

		public Size TileSize {
			get;
			set;
		}

		public ObjMode ObjectMode {
			get;
			set;
		}

		public PaletteMode PaletteMode {
			get;
			set;
		}

		public Color TransparentColor {
			get;
			set;
		}

		public bool UseRectangularArea {
			get;
			set;
		}

		public ISplitable Splitter {
			get;
			set;
		}

		public PaletteReducer Reducer {
			get;
			set;
		}

		public ColorQuantization Quantization {
			get;
			set;
		}

		#endregion

		public void AddFrame(EmguImage image)
		{
			Frame frame    = this.Splitter.Split(image);
			frame.TileSize = 128;//this.TileSize.Width * this.TileSize.Height;
			this.frameData.Add(Tuple.Create(frame, image));

			var mask = image.InRange(new Color(0, 0, 0, 0), new Color(255, 255, 255, 0));
			image.SetValue(this.TransparentColor, mask);
		}

        public void Generate(Stream paletteStr, Stream imgLinealStr, Stream imgTiledStr, Stream spriteStr)
		{
            Pixel[] pixelsLin;
            Pixel[] pixelsHori;
			Color[][] palettes;
            this.CreateData(out pixelsLin, out pixelsHori, out palettes);

			// Get frame list
			Frame[] frames = new Frame[this.frameData.Count];
			for (int i = 0; i < this.frameData.Count; i++)
				frames[i] = this.frameData[i].Item1;

			// Create palette format
			Nclr nclr = new Nclr() {
				Extended = false,
				Format   = this.Format
			};
			nclr.SetPalette(palettes);

			// Create image format
            Ncgr ncgrLineal = new Ncgr() {
				RegDispcnt  = this.DispCnt,
				Unknown     = this.UnknownChar,
				InvalidSize = true
			};
            ncgrLineal.Width  = (pixelsLin.Length > 256) ? 256 : pixelsLin.Length;
            ncgrLineal.Height = (int)Math.Ceiling(pixelsLin.Length / (double)ncgrLineal.Width);
            ncgrLineal.SetData(pixelsLin, PixelEncoding.Lineal, this.Format, this.TileSize);

            Ncgr ncgrTiled = new Ncgr() {
                RegDispcnt  = this.DispCnt,
                Unknown     = this.UnknownChar,
                InvalidSize = true
            };
            ncgrTiled.Width  = ncgrLineal.Width;
            ncgrTiled.Height = ncgrLineal.Height;
            if (ncgrTiled.Height % this.TileSize.Height != 0)
                ncgrTiled.Height += this.TileSize.Height - (ncgrTiled.Height % this.TileSize.Height);
            ncgrTiled.SetData(pixelsHori, PixelEncoding.HorizontalTiles, this.Format, this.TileSize);

			// Create sprite format
			Ncer ncer = new Ncer() {
				TileSize = 128,
				IsRectangularArea = this.UseRectangularArea
			};
			ncer.SetFrames(frames);

			// Write data
			if (paletteStr != null)
				nclr.Write(paletteStr);
            if (imgLinealStr != null)
                ncgrLineal.Write(imgLinealStr);
            if (imgTiledStr != null)
                ncgrTiled.Write(imgTiledStr);
			if (spriteStr != null)
				ncer.Write(spriteStr);
		}

		/// <summary>
		/// Generate pixels, palette and update objects in frames.
		/// </summary>
		/// <param name="pixels">Pixels of frames.</param>
		/// <param name="palettes">Palettes of frames.</param>
        private void CreateData(out Pixel[] pixelsLin, out Pixel[] pixelsHori, out Color[][] palettes)
		{
			int tileSize = 128;
            int maxColors   = 1 << this.Format.Bpp();
            int numPalettes = (maxColors <= 16) ? 16 : 1;

			// Create the ObjData. Quantizate images.
			List<Color[]> palettesList = new List<Color[]>();
			List<ObjectData> data = new List<ObjectData>();
			foreach (Tuple<Frame, EmguImage> frame in this.frameData) {
				EmguImage frameImg = frame.Item2;
				Obj[] objects = frame.Item1.GetObjects();

				foreach (Obj obj in objects) {
					ObjectData objData = new ObjectData();
					objData.Object = obj;

					Rectangle area = obj.GetArea();
					area.Offset(256, 128);
					objData.Image = frameImg.Copy(area);

					// Update object
					objData.Object.Mode        = this.ObjectMode;
					objData.Object.PaletteMode = this.PaletteMode;

					// Quantizate
					this.Quantization.Quantizate(objData.Image);
                    objData.PixelsLineal     = this.Quantization.GetPixels(PixelEncoding.Lineal);
                    objData.PixelsHorizontal = this.Quantization.GetPixels(PixelEncoding.HorizontalTiles);
					objData.Palette = this.Quantization.Palette;
					if (objData.Palette.Length > maxColors)
						throw new FormatException(string.Format("The image has more than {0} colors", maxColors));

					palettesList.Add(objData.Palette);

					data.Add(objData);
				}
			}

			// Reduce palettes
			this.Reducer.Clear();
			this.Reducer.AddPaletteRange(palettesList.ToArray());
            this.Reducer.Reduce(numPalettes);
			palettes = this.Reducer.ReducedPalettes;

			// Approximate palettes removed and get the pixel array
            List<Pixel> pixelLinList  = new List<Pixel>();
            List<Pixel> pixelHoriList = new List<Pixel>();
			for (int i = 0; i < data.Count; i++) {
				int paletteIdx = this.Reducer.PaletteApproximation[i];
				if (paletteIdx != -1) {
					// Quantizate again the image with the new palette
					Color[] newPalette = palettes[paletteIdx];
					FixedPaletteQuantization quantization = new FixedPaletteQuantization(newPalette);
					quantization.Quantizate(data[i].Image);

					// Get the pixel
                    data[i].PixelsLineal     = quantization.GetPixels(PixelEncoding.Lineal);
                    data[i].PixelsHorizontal = quantization.GetPixels(PixelEncoding.HorizontalTiles);
				} else {
					paletteIdx = Array.FindIndex(palettes, p => p == palettesList[i] );
				}

				// Update object
				data[i].Object.PaletteIndex = (byte)paletteIdx;

				// Add pixels to the list
                data[i].Object.TileNumber = (ushort)(pixelLinList.Count / tileSize);
                pixelLinList.AddRange(data[i].PixelsLineal);
                pixelHoriList.AddRange(data[i].PixelsHorizontal);
			}

            pixelsLin  = pixelLinList.ToArray();
            pixelsHori = pixelHoriList.ToArray();
		}

		private class ObjectData
		{
			public Obj Object {
				get;
				set;
			}

			public EmguImage Image {
				get;
				set;
			}

            public Pixel[] PixelsLineal {
				get;
				set;
			}

            public Pixel[] PixelsHorizontal {
                get;
                set;
            }

			public Color[] Palette {
				get;
				set;
			}
		}
	}
}

