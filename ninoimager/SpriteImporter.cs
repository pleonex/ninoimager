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
using Size = System.Drawing.Size;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

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

		public SpriteImporter()
		{
			this.frameData = new List<Tuple<Frame, EmguImage>>();
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

		public int BlockSize {
			get;
			set;
		}

		public ISplitable Splitter {
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
			Frame frame = this.Splitter.Split(image);
			this.frameData.Add(Tuple.Create(frame, image));
		}

		public void Generate(Stream paletteStr, Stream imageStr, Stream spriteStr)
		{
			throw new NotImplementedException();
		}
	}
}

