// -----------------------------------------------------------------------
// <copyright file="Ncgr.cs" company="none">
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
// <date>12/08/2013</date>
// -----------------------------------------------------------------------

//#define VERBOSE
using System;
using System.IO;

namespace Ninoimager.Format
{
	public class Ncgr : Image
	{
		private static Type[] BlockTypes = { typeof(Ncgr.CHAR), typeof(Ncgr.Cpos) };
		private NitroFile nitro;
		private CHAR charBlock;
		private Cpos cpos;

		public Ncgr()
		{
			this.nitro = new NitroFile("NCGR", "1.1", BlockTypes);
			this.charBlock = new CHAR(this.nitro);
			this.nitro.Blocks.Add(this.charBlock);
		}

		public Ncgr(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.GetInfo();
		}

		public Ncgr(Stream str)
		{
			this.nitro = new NitroFile(str, BlockTypes);
			this.GetInfo();
		}

		public NitroFile NitroData {
			get { return this.nitro; }
		}

		public uint RegDispcnt {
			get { return this.charBlock.RegDispcnt; }
			set { this.charBlock.RegDispcnt = value; }
		}

		public uint Unknown {
			get { return this.charBlock.Unknown; }
			set { this.charBlock.Unknown = value; }
		}

		public bool InvalidSize {
			 get;
			 set;
		 }

		public bool HasCpos {
			get { return this.cpos != null; }
		}

		public void Write(string fileOut)
		{
			this.SetInfo();
			this.nitro.Write(fileOut);
		}

		public void Write(Stream strOut)
		{
			this.SetInfo();
			this.nitro.Write(strOut);
		}

		private void GetInfo()
		{
			this.charBlock = this.nitro.GetBlock<CHAR>(0);
			if (this.nitro.Blocks.ContainsType("CPOS"))
				this.cpos = this.nitro.GetBlock<Cpos>(0);

			this.Format = this.charBlock.Format;	// To get BPP
			int numPixels = this.charBlock.ImageData.Length * 8 / this.Format.Bpp();
			int defaultWidth = this.TileSize.Width;
			int defaultHeight = numPixels / defaultWidth;

			if (this.charBlock.Width == 0xFFFF && this.charBlock.Height == 0xFFFF) {
				// Since these images can be tiled used with OAMs or MAPs files, the width must be a multiple of 
				// tile size to do correctly the lineal transformation.
				this.Width  = defaultWidth;
				this.Height = defaultHeight;
				this.InvalidSize = true;
			} else {
				this.Width  = this.charBlock.Width * 8;		// It indicates the number of tiles in X axis
				this.Height = this.charBlock.Height * 8;	// It indicates the number of tiles in Y axis
				this.InvalidSize = false;
			}

			// It's "Digimon" game developper fault
			if (this.Width * this.Height != numPixels) {
				this.Width  = defaultWidth;
				this.Height = defaultHeight;
			}

			// It's "Donkey Kong - Jungle Climber" game developper fault (and its damn dummy files)
			// I will left it to throw the error since there is no image and a bitmap with width or height
			// 0 can not be created.
			if (this.Width == 0 || this.Height == 0)
				this.Width = this.Height = 0;

			this.SetData(this.charBlock.ImageData, this.charBlock.PixelEncoding, this.charBlock.Format);
		}

		private void SetInfo()
		{
			this.charBlock.Format        = this.Format;
			this.charBlock.PixelEncoding = this.PixelEncoding;
			this.charBlock.ImageData     = this.GetData();
			if (this.InvalidSize) {
				this.charBlock.Width  = 0xFFFF;
				this.charBlock.Height = 0xFFFF;
			} else {
				this.charBlock.Height = (ushort)(this.Height / 8);
				this.charBlock.Width  = (ushort)(this.Width / 8);
			}
		}

		private class CHAR : NitroBlock
		{
			public CHAR(NitroFile nitro)
				: base(nitro)
			{
			}

			public ushort Height {
				get;
				set;
			}

			public ushort Width {
				get;
				set;
			}

			public ColorFormat Format {
				get;
				set;
			}

			/// <summary>
			/// Video register 4000000h, it can set only bits: 4, 20 and 21.
			/// More info at: <seealso cref="http://nocash.emubase.de/gbatek.htm#dsvideobgmodescontrol"/>
			/// </summary>
			/// <value>The register DISPCNT</value>
			public uint RegDispcnt {
				get;
				set;
			}

			public PixelEncoding PixelEncoding {
				get;
				set;
			}

			public uint Unknown {
				get;
				set;
			}

			public byte[] ImageData {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);
				long blockPos   = strIn.Position;

				this.Height     = br.ReadUInt16();
				this.Width      = br.ReadUInt16();
				uint format     = br.ReadUInt32();
				this.Format     = (ColorFormat)format;
				this.RegDispcnt = br.ReadUInt32();

				// It seems to be something like this
				uint unknown       = br.ReadUInt32();
				this.PixelEncoding = (PixelEncoding)(unknown & 0xFF);
				this.Unknown       = (unknown >> 8);

				uint dataLength = br.ReadUInt32();
				uint dataOffset = br.ReadUInt32();

#if VERBOSE
				if (this.Height == 0 || this.Height == 0xFFFF)
					Console.WriteLine("\t* Invalid height value.");
				if (this.Width == 0 || this.Width == 0xFFFF)
					Console.WriteLine("\t* Invalid width value.");
				if (this.Unknown == 1)
					Console.WriteLine("\t* Unknown set to 1.");
				if (dataLength == 0)
					Console.WriteLine("\t* Image data null.");
				if (dataOffset != 0x18)
					Console.WriteLine("\t* Different data offset.");
#endif

				// Try to fix values
				if (dataLength == 0)
					dataLength = (uint)(this.Size - 0x18);

				// Finally read data
				strIn.Position  = blockPos + dataOffset;
				this.ImageData  = br.ReadBytes((int)dataLength);
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);

				uint unknown = (uint)this.PixelEncoding | (this.Unknown << 8);

				bw.Write(this.Height);
				bw.Write(this.Width);
				bw.Write((uint)this.Format);
				bw.Write(this.RegDispcnt);
				bw.Write(unknown);
				bw.Write((uint)this.ImageData.Length);
				bw.Write(0x18);
				bw.Write(this.ImageData);
			}

			protected override void UpdateSize()
			{
				this.Size = 0x08 + 0x18 + this.ImageData.Length;;
			}
		}

		private class Cpos : NitroBlock
		{
			public Cpos(NitroFile nitro)
				: base(nitro)
			{
			}

			public uint Unknown1 {
				get;
				set;
			}

			public ushort Unknown2 {
				get;
				set;
			}

			public ushort Unknown3 {
				get;
				set;
			}
			
			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);
				this.Unknown1 = br.ReadUInt32();
				this.Unknown2 = br.ReadUInt16();
				this.Unknown3 = br.ReadUInt16();
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write(this.Unknown1);
				bw.Write(this.Unknown2);
				bw.Write(this.Unknown3);
			}

			protected override void UpdateSize()
			{
				this.Size = 0x08 + 0x08;
			}
		}
	}
}

