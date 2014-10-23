// -----------------------------------------------------------------------
// <copyright file="Btx0.cs" company="none">
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
// <date>19/09/2013</date>
// -----------------------------------------------------------------------
using System;
using System.IO;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.Format
{
	public class Btx0
	{
		private static Type[] BlockTypes = { typeof(Btx0.Tex0), typeof(Btx0.Mdl0) };
		private NitroFile nitro;
		private Tex0 tex0;
		private Image[] images;

		public Btx0()
		{
			this.nitro = new NitroFile("BTX0", "1.0", BlockTypes) { HasOffsets = true };
			this.tex0 = new Tex0(this.nitro);
			this.nitro.Blocks.Add(this.tex0);
			this.images  = new Image[0];
		}

		public Btx0(string file)
		{
			this.nitro = new NitroFile(file, true, BlockTypes);
			this.GetInfo();
		}

		public Btx0(Stream str)
		{
			this.nitro = new NitroFile(str, true, BlockTypes);
			this.GetInfo();
		}

		public NitroFile NitroData {
			get { return this.nitro; }
		}

		public int NumTextures {
			get { return this.tex0.TextureInfo.NumObjects; }
		}

		public void Write(string fileOut)
		{
			this.nitro.Write(fileOut);
		}

		public void Write(Stream strOut)
		{
			this.nitro.Write(strOut);
		}

		private void GetInfo()
		{
			this.tex0 = this.nitro.GetBlock<Tex0>(0);
			this.images  = new Image[this.tex0.TextureInfo.NumObjects];
			for (int i = 0; i < tex0.TextureInfo.NumObjects; i++) {
				this.images[i] = new Image();
				this.images[i].Width  = (int)tex0.TextureInfo.Data[i].Width;
				this.images[i].Height = (int)tex0.TextureInfo.Data[i].Height;
				this.images[i].SetData(
					tex0.TextureData[i],
					Ninoimager.Format.PixelEncoding.Lineal,
					tex0.TextureInfo.Data[i].Format
				);
			}
		}

		public EmguImage CreateBitmap(int texIdx)
        {
			string texName = this.tex0.TextureInfo.Data[texIdx].Name;

			int palIdx = -1;
			for (int i = 0; i < this.tex0.PaletteInfo.NumObjects && palIdx == -1; i++) {
				string palName = this.tex0.PaletteInfo.Data[i].Name.Replace("_pl", "");
				if (palName == texName || palName.Replace("_pl", "") == texName)
					palIdx = i;
			}

			if (palIdx == -1)
				palIdx = 0;

			return this.CreateBitmap(texIdx, palIdx);
        }

		public EmguImage CreateBitmap(int texIdx, int palIdx)
		{
			// Get palette
			int numColors = 1 << this.tex0.TextureInfo.Data[texIdx].Format.Bpp();
			Palette palette = this.tex0.GetPalette(palIdx, numColors);

			// Get image
			EmguImage img = this.images[texIdx].CreateBitmap(palette, 0);

			// Set transparent color
			if (this.tex0.TextureInfo.Data[texIdx].Color0 == 1) {
				Color transparent = palette.GetPalette(0)[0];
				var mask = img.InRange(transparent, transparent);
				img.SetValue(0, mask);
			}
			return img;
		}

		private class Mdl0 : NitroBlock
		{
			public Mdl0(NitroFile nitro)
				: base(nitro)
			{
			}

			public byte[] Data { get; set; }

			protected override void ReadData(Stream strIn)
			{
				this.Data = new byte[this.Size - 8];
				strIn.Read(this.Data, 0, this.Data.Length);
			}

			protected override void WriteData(Stream strOut)
			{
				strOut.Write(this.Data, 0, this.Data.Length);
			}

			protected override void UpdateSize()
			{
				this.Size = 8 + this.Data.Length;
			}
		}

		/**
		 * TEX0 Block structure:
		 * |- Header
		 * |- DataInfo[]: texture
		 * |- DataInfo[]: texel texture
		 * |- DataInfo[]: palette
		 * |- Texture Data
		 * |- Texel Texture Data
		 * |- Palette Data
		 */

		private class Tex0 : NitroBlock
		{
			public Tex0(NitroFile nitro)
				: base(nitro)
			{
			}

			public uint Unknown1 { get; set; }
            public uint Unknown2 { get; set; }
            public uint Unknown3 { get; set; }
            public uint Unknown4 { get; set; }
            public uint Unknown5 { get; set; }

			public InfoCollection<TextureDataInfo> TextureInfo { get; set; }
			public InfoCollection<PaletteDataInfo> PaletteInfo { get; set; }

            public byte[][] TextureData               { get; set; }
            public byte[]   TextureCompressedData     { get; set; }
            public byte[]   TextureCompressedInfoData { get; set; }
            public byte[]   PaletteData               { get; set; }

			protected override void ReadData(Stream strIn)
			{
				long blockOffset = strIn.Position - 8;
				BinaryReader br  = new BinaryReader(strIn);

				// Offset and size section
				this.Unknown1       = br.ReadUInt32();
				uint texDataSize    = (uint)(br.ReadUInt16() << 3);
				uint texInfoOffset  = br.ReadUInt16();
				this.Unknown2       = br.ReadUInt32();
				uint texDataOffset  = br.ReadUInt32();

				this.Unknown3              = br.ReadUInt32();
				uint texTexelDataSize       = (uint)(br.ReadUInt16() << 3);
				uint texTexelInfoOffset     = br.ReadUInt16();
				this.Unknown4               = br.ReadUInt32();
				uint texTexelDataOffset     = br.ReadUInt32();
				uint texTexelInfoDataOffset = br.ReadUInt32();

				this.Unknown5      = br.ReadUInt32();
				uint palDataSize   = br.ReadUInt32() << 3;
				uint palInfoOffset = br.ReadUInt32();
				uint palDataOffset = br.ReadUInt32();

                // Read Info 3D: texture
                strIn.Position = blockOffset + texInfoOffset;
				this.TextureInfo = new InfoCollection<TextureDataInfo>();
				this.TextureInfo.ReadData(strIn);

                // Read Info 3D: palette
                strIn.Position = blockOffset + palInfoOffset;
				this.PaletteInfo = new InfoCollection<PaletteDataInfo>();
                this.PaletteInfo.ReadData(strIn);

                // TODO: Read Info 3D: texel texture                         

				// Get palette data
				strIn.Position   = blockOffset + palDataOffset;
				this.PaletteData = br.ReadBytes((int)palDataSize);

				// Get texture data
				this.TextureData = new byte[this.TextureInfo.NumObjects][];
				for (int i = 0; i < this.TextureInfo.NumObjects; i++) {
					strIn.Position = blockOffset + texDataOffset + this.TextureInfo.Data[i].TextureOffset;
					this.TextureData[i] = br.ReadBytes(this.TextureInfo.Data[i].Length);
                }

				// TODO: Convert image from texel texture to indexed color                
			}

			public Palette GetPalette(int idxPalette, int numColors)
			{
				// The palette will be "numColors * 2" or in case this is bigger, as
				// much data as possible
				int length = numColors * 2;
				int offset = (int)this.PaletteInfo.Data[idxPalette].Offset;
				if (offset + numColors * 2 > this.PaletteData.Length)
					length = this.PaletteData.Length - offset;

				byte[] subPalette = new byte[length];
				Array.Copy(
					this.PaletteData,
					this.PaletteInfo.Data[idxPalette].Offset,
					subPalette,
					0,
					length);

				return new Palette(subPalette.ToBgr555Colors());
			}

			protected override void WriteData(Stream strOut)
			{
				throw new NotImplementedException ();
			}

			protected override void UpdateSize()
			{
				throw new NotImplementedException ();
			}

			public class InfoCollection<T> where T : DataInfo, new()
			{
				public byte NumObjects { get; set; }
				public T[]  Data       { get; set; }

				public void ReadData(Stream strIn)
				{
					BinaryReader br = new BinaryReader(strIn);

					br.ReadByte();	// Dummy
					this.NumObjects = br.ReadByte();
					br.ReadUInt16();	// SectionSize
					long objStart = strIn.Position;

					// Read just for fun
					#if DEBUG
					// Unknown Block header
					br.ReadUInt16();	// Unknown Block HeaderSize
					br.ReadUInt16();	// Unknown Block SectionSize
					br.ReadUInt32();	// Unknown Block Constant

					// Info Block header
					strIn.Position = this.NumObjects * 4;
					br.ReadUInt16();	// Info Block HeaderSize
					br.ReadUInt16();	// Info Block SectionSize
					#endif

					this.Data = new T[this.NumObjects];
					for (int i = 0; i < this.NumObjects; i++) {
						strIn.Position = objStart;
						this.Data[i] = new T();
						this.Data[i].ReadData(strIn, i, this.NumObjects);
					}
				}
			}

            public abstract class DataInfo
            {
				public ushort UnknownData1 { get; set; }  // Unkonwn data 1 from Unknown
				public ushort UnknownData2 { get; set; }  // Unknown data 2 from Unknown

				public string Name { get; set; }

				public void ReadData(Stream strIn, int index, int numObjs)
                {
					BinaryReader br = new BinaryReader(strIn);
					long unknownStart = strIn.Position + 8;
					long infoStart = unknownStart + numObjs * 4 + 4;
					long nameStart = infoStart + numObjs * this.GetInfoSize();

					strIn.Position = unknownStart + index * 4;
					this.UnknownData1 = br.ReadUInt16();
					this.UnknownData2 = br.ReadUInt16();
                    
					strIn.Position = infoStart + index * this.GetInfoSize();
					this.ReadInfo(strIn);

					strIn.Position = nameStart + index * 0x10;
					this.Name = new string(br.ReadChars(0x10)).Replace("\0", "");
                }

				protected abstract void ReadInfo(Stream strIn);

				protected abstract int GetInfoSize();
            }

			public class TextureDataInfo
				: DataInfo
			{
				public uint TextureOffset { get; set; }
				public uint Width    { get; set; }
				public byte Unknown1 { get; set; }
				public byte Unknown2 { get; set; }
				public byte Unknown3 { get; set; }

				public byte CoordinateTransformation { get; set; }
				public byte Color0 { get; set; }
				public ColorFormat Format { get; set; }
				public uint Height { get; set; }
				public byte Width2 { get; set; }
				public byte FlipY { get; set; }
				public byte FlipX { get; set; }
				public byte RepeatY { get; set; }
				public byte RepeatX { get; set; }

				public int Length { get; set; }

				protected override void ReadInfo(Stream strIn) 
				{
					BinaryReader br = new BinaryReader(strIn);

					this.TextureOffset = (uint)(br.ReadUInt16() << 3);
					ushort parameters  = br.ReadUInt16();
					this.Width    = br.ReadByte();
					this.Unknown1 = br.ReadByte();
					this.Unknown2 = br.ReadByte();
					this.Unknown3 = br.ReadByte();

					// Now let's get the information inside Parameters
					this.CoordinateTransformation = (byte)(parameters & 14);
					this.Color0  = (byte)((parameters >> 13) & 1);
					this.Format  = (ColorFormat)((parameters >> 10) & 7);
					this.Height  = (byte)(8 << ((parameters >> 7) & 7));
					this.Width2  = (byte)(8 << ((parameters >> 4) & 7));
					this.FlipY   = (byte)((parameters >> 3) & 1);
					this.FlipX   = (byte)((parameters >> 2) & 1);
					this.RepeatY = (byte)((parameters >> 1) & 1);
					this.RepeatX = (byte)(parameters & 1);

					// In the case of width is zero
					if (this.Width == 0x00) {
						switch (this.Unknown1 & 0x3) {
							case 2:  this.Width = 0x200; break;
							default: this.Width = 0x100; break;
						}
					}

					// In the case of the height is zero
					if (this.Height == 0x00) {
						switch ((this.Unknown1 >> 4) & 0x3) {
							case 2:  this.Height = 0x200; break;
							default: this.Height = 0x100; break;
						}
					}
						
					// Calculate texture length
					this.Length = (int)((this.Format.Bpp() * this.Height * this.Width) / 8);       
				}

				protected override int GetInfoSize()
				{
					return 8;
				}
			}

			public class PaletteDataInfo
				: DataInfo
			{
				public uint   Offset  { get; set; }
				public ushort Unknown { get; set; }

				protected override void ReadInfo(Stream strIn)
				{
					BinaryReader br = new BinaryReader(strIn);
					this.Offset  = (uint)(br.ReadUInt16() << 3);
					this.Unknown = br.ReadUInt16();
				}

				protected override int GetInfoSize()
				{
					return 4;
				}
			}
		}
	}
}
