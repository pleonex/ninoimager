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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.Format
{
	public class Btx0
	{
		private static Type[] BlockTypes = { typeof(Btx0.Tex0), typeof(Btx0.Mdl0) };
		private NitroFile nitro;
		private Tex0 tex0;
		private List<Image> images;

		public Btx0()
		{
			this.nitro = new NitroFile("BTX0", "1.0", BlockTypes) { HasOffsets = true };
			this.tex0  = new Tex0(this.nitro);
			this.nitro.Blocks.Add(this.tex0);
			this.images  = new List<Image>();
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

		public int NumPalettes {
			get {
				return this.tex0.TextureInfo.NumObjects;
			}
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
			this.images  = new List<Image>(this.tex0.TextureInfo.NumObjects);
			for (int i = 0; i < tex0.TextureInfo.NumObjects; i++) {
				Image img = new Image();
				img.Width  = (int)tex0.TextureInfo.Data[i].Width;
				img.Height = (int)tex0.TextureInfo.Data[i].Height;
				img.SetData(
					tex0.TextureData[i],
					Ninoimager.Format.PixelEncoding.Lineal,
					tex0.TextureInfo.Data[i].Format
				);
				this.images.Add(img);
			}
		}

		public string GetTextureName(int texIdx)
		{
			return this.tex0.TextureInfo.Data[texIdx].Name;
		}

		private int SearchPaletteIdx(int texIdx)
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

			return palIdx;
		}

		public EmguImage CreateBitmap(int texIdx)
        {
			return this.CreateBitmap(texIdx, this.SearchPaletteIdx(texIdx));
        }

		public EmguImage CreateBitmap(int texIdx, int palIdx)
		{
			// Get palette
			Palette palette = this.GetPalette(texIdx);

			// Get image
			EmguImage img = this.images[texIdx].CreateBitmap(palette, 0);

			// Set transparent color
			if (this.tex0.TextureInfo.Data[texIdx].Color0) {
				Color transparent = palette.GetPalette(0)[0];
				var mask = img.InRange(transparent, transparent);
				img.SetValue(0, mask);
			}
			return img;
		}

		public Palette GetPalette(int texIdx)
		{
			int palIdx = this.SearchPaletteIdx(texIdx);
			int numColors = 1 << this.tex0.TextureInfo.Data[texIdx].Format.Bpp();
			return this.tex0.GetPalette(palIdx, numColors);
		}

		public Image GetImage(int texIdx)
		{
			return this.images[texIdx];
		}

		public void RemoveImages()
		{
			this.tex0.TextureData.Clear();
			this.tex0.TextureInfo.Clear();
			this.tex0.PaletteData.Clear();
			this.tex0.PaletteInfo.Clear();
		}

		public void AddImage(Image img, Palette palette, string name)
		{
			this.AddImage(img, name);
			this.AddPalette(palette, name + "_pl");
		}

		public void AddImage(Image img, string name)
		{
			this.images.Add(img);
			this.tex0.AddImage(img, name);
		}

		public void AddTexelImage(Image img, string name)
		{
			throw new NotSupportedException();
		}

		public void AddPalette(Palette palette, string name)
		{
			this.tex0.AddPalette(palette, name);
		}

		public int[] GetTextureUnknowns(int id)
		{
			int[] unknowns = new int[5];
			unknowns[0] = this.tex0.TextureInfo.Data[id].UnknownData1;
			unknowns[1] = this.tex0.TextureInfo.Data[id].UnknownData2;
			unknowns[2] = this.tex0.TextureInfo.Data[id].Unknown1;
			unknowns[3] = this.tex0.TextureInfo.Data[id].Unknown2;
			unknowns[4] = this.tex0.TextureInfo.Data[id].Unknown3;
			return unknowns;
		}

		public int[] GetPaletteUnknowns(int id)
		{
			int[] unknowns = new int[2];
			unknowns[0] = this.tex0.PaletteInfo.Data[id].UnknownData1;
			unknowns[1] = this.tex0.PaletteInfo.Data[id].UnknownData2;
			return unknowns;
		}

		public void SetTextureUnknowns(int id, int[] unknowns)
		{
			this.tex0.TextureInfo.Data[id].UnknownData1 = (ushort)unknowns[0];
			this.tex0.TextureInfo.Data[id].UnknownData2 = (ushort)unknowns[1];
			this.tex0.TextureInfo.Data[id].Unknown1 = (byte)unknowns[2];
			this.tex0.TextureInfo.Data[id].Unknown2 = (byte)unknowns[3];
			this.tex0.TextureInfo.Data[id].Unknown3 = (byte)unknowns[4];
		}

		public void SetPaletteUnknowns(int id, int[] unknowns)
		{
			this.tex0.PaletteInfo.Data[id].UnknownData1 = (ushort)unknowns[0];
			this.tex0.PaletteInfo.Data[id].UnknownData2 = (ushort)unknowns[1];
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
				this.TextureInfo = new InfoCollection<TextureDataInfo>();
				this.PaletteInfo = new InfoCollection<PaletteDataInfo>();
				this.TextureData = new List<byte[]>();
				this.PaletteData = new List<byte>();
			}

			public uint Unknown1 { get; set; }
            public uint Unknown2 { get; set; }
            public uint Unknown3 { get; set; }
            public uint Unknown4 { get; set; }
            public uint Unknown5 { get; set; }

			public InfoCollection<TextureDataInfo> TextureInfo { get; set; }
			public InfoCollection<PaletteDataInfo> PaletteInfo { get; set; }

            public List<byte[]> TextureData               { get; set; }
            public byte[]       TextureCompressedData     { get; set; }
            public byte[]       TextureCompressedInfoData { get; set; }
            public List<byte>   PaletteData               { get; set; }

			protected override void ReadData(Stream strIn)
			{
				long blockOffset = strIn.Position - 8;
				BinaryReader br  = new BinaryReader(strIn);

				// Offset and size section
				this.Unknown1       = br.ReadUInt32();
				br.ReadUInt16();	// Texture Data Size (must be multiplied by 8)
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
				this.TextureInfo.ReadData(strIn);

                // Read Info 3D: palette
                strIn.Position = blockOffset + palInfoOffset;
                this.PaletteInfo.ReadData(strIn);

                // TODO: Read Info 3D: texel texture                         

				// Get palette data
				strIn.Position   = blockOffset + palDataOffset;
				this.PaletteData.AddRange(br.ReadBytes((int)palDataSize));

				// Get texture data
				this.TextureData = new List<byte[]>(this.TextureInfo.NumObjects);
				for (int i = 0; i < this.TextureInfo.NumObjects; i++) {
					strIn.Position = blockOffset + texDataOffset + this.TextureInfo.Data[i].TextureOffset;
					this.TextureData.Add(br.ReadBytes(this.TextureInfo.Data[i].Length));
                }

				// TODO: Convert image from texel texture to indexed color                
			}

			public Palette GetPalette(int idxPalette, int numColors)
			{
				// The palette will be "numColors * 2" or in case this is bigger, as
				// much data as possible
				int length = numColors * 2;
				int offset = (int)this.PaletteInfo.Data[idxPalette].Offset;
				if (offset + numColors * 2 > this.PaletteData.Count)
					length = this.PaletteData.Count - offset;

				byte[] subPalette = this.PaletteData.GetRange(
					(int)this.PaletteInfo.Data[idxPalette].Offset, length).ToArray();
				return new Palette(subPalette.ToBgr555Colors());
			}

			public void AddPalette(Palette palette, string name)
			{
				// Get palette data and add it
				List<byte> palData = new List<byte>(palette.GetPalette(0).ToBgr555());
				int offset = SubArraySearch(this.PaletteData, palData);
				int subOffset = palData.Count - (this.PaletteData.Count - offset);
				if (subOffset > 0)
					palData = palData.GetRange(this.PaletteData.Count - offset, subOffset);

				this.PaletteData.AddRange(palData);
				while (this.PaletteData.Count % 8 != 0)
					this.PaletteData.Add(0x00);

				// Create palette info and add it
				Tex0.PaletteDataInfo palInfo = new Tex0.PaletteDataInfo();
				palInfo.Name = name;
				palInfo.Offset = (uint)offset;
				this.PaletteInfo.AddElement(palInfo);
			}

			private static int SubArraySearch(List<byte> array, List<byte> subarray)
			{
				for (int idx = 0; idx < array.Count; idx++) {
					bool found = true;
					for (int subIdx = 0; subIdx < subarray.Count &&
						idx + subIdx < array.Count && found; subIdx++)
					{
						if (array[idx + subIdx] != subarray[subIdx])
							found = false;
					}

					if (found)
						return idx;
				}

				return array.Count;
			}

			public void AddImage(Image image, string name)
			{
				// Get image data and add it
				byte[] texData = image.GetData();
				this.TextureData.Add(texData);

				// Create texture info and add it
				Tex0.TextureDataInfo texInfo = new Tex0.TextureDataInfo();
				texInfo.Width  = image.Width;
				texInfo.Height = image.Height;
				texInfo.Color0 = true;
				texInfo.Format = image.Format;
				texInfo.Name   = name;
				this.TextureInfo.AddElement(texInfo);
			}

			private byte[] CompressTextureData()
			{
				List<byte> data = new List<byte>();
				for (int i = 0; i < this.TextureData.Count; i++) {
					// Get offset
					// MORE COMPRESSION:
					//uint offset = (uint)SubArraySearch(data, this.TextureData[i].ToList());
					// ORIGINAL COMPRESSION:
					int idx = this.TextureData.FindIndex(d => d.SequenceEqual(this.TextureData[i]));
					uint offset = (uint)data.Count;
					if (idx != i)
						offset = this.TextureInfo.Data[idx].TextureOffset;

					this.TextureInfo.Data[i].TextureOffset = offset;

					// If it is not present add it
					if (offset == data.Count) {
						data.AddRange(this.TextureData[i]);
						while (data.Count % 8 != 0)
							data.Add(0);
					}
				}

				return data.ToArray();
			}

			protected override void WriteData(Stream strOut)
			{
				// TODO: Texel support
				BinaryWriter bw = new BinaryWriter(strOut);

				byte[] textureData   = this.CompressTextureData();
				uint textureDataSize = (uint)textureData.Length;
				uint textureDataPadd = 8 - textureDataSize % 8;
				if (textureDataPadd != 8)
					textureDataSize += textureDataPadd;

				uint paletteDataSize = (uint)this.PaletteData.Count;
				uint paletteDataPadd = paletteDataSize % 8;
				if (paletteDataPadd != 8)
					paletteDataSize += paletteDataPadd;

				uint textureInfoOffset = 0x3C;
				uint paletteInfoOffset = textureInfoOffset + (uint)this.TextureInfo.GetSize();
				uint textureDataOffset = paletteInfoOffset + (uint)this.PaletteInfo.GetSize();
				uint paletteDataOffset = textureDataOffset + textureDataSize;
				uint texelInfoOffset     = textureInfoOffset;
				uint texelDataOffset     = paletteDataOffset;
				uint texelInfoDataOffset = paletteDataOffset;

				// Write header
				bw.Write(this.Unknown1);
				bw.Write((ushort)(textureDataSize >> 3));
				bw.Write((ushort)0x3C);	// Just after header
				bw.Write(this.Unknown2);
				bw.Write(textureDataOffset);

				bw.Write(this.Unknown3);
				bw.Write((ushort)0x00);
				bw.Write((ushort)texelInfoOffset);
				bw.Write(this.Unknown4);
				bw.Write((uint)texelDataOffset);
				bw.Write((uint)texelInfoDataOffset);

				bw.Write(this.Unknown5);
				bw.Write(paletteDataSize >> 3);
				bw.Write(paletteInfoOffset);
				bw.Write(paletteDataOffset);

				// Write texture info
				this.TextureInfo.WriteData(strOut);

				// Write palette info
				this.PaletteInfo.WriteData(strOut);

				// Write texture data
				bw.Write(textureData);
				for (int i = 0; i < textureDataPadd && textureDataPadd != 8; i++)
					bw.Write((byte)0x00);

				// Write palette data
				bw.Write(this.PaletteData.ToArray());
				for (int i = 0; i < paletteDataPadd && paletteDataPadd != 8; i++)
					bw.Write((byte)0x00);
			}

			protected override void UpdateSize()
			{
				this.Size = 8 + 0x34;
				this.Size += this.TextureInfo.GetSize();
				this.Size += this.PaletteInfo.GetSize();
				this.Size += this.CompressTextureData().Length;	// I know...
				this.Size += this.PaletteData.Count;
			}

			public class InfoCollection<T> where T : DataInfo, new()
			{
				public byte    NumObjects { get; set; }
				public List<T> Data       { get; set; }

				public InfoCollection()
				{
					this.Data = new List<T>();
				}

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
					strIn.Position += this.NumObjects * 4;
					br.ReadUInt16();	// Info Block HeaderSize
					br.ReadUInt16();	// Info Block SectionSize
					#endif

					this.Data = new List<T>(this.NumObjects);
					for (int i = 0; i < this.NumObjects; i++) {
						strIn.Position = objStart;
						T entry = new T();
						entry.ReadData(strIn, i, this.NumObjects);
						this.Data.Add(entry);
					}
				}

				public void WriteData(Stream strOut)
				{
					BinaryWriter bw = new BinaryWriter(strOut);

					// Header
					bw.Write((byte)0x00);
					bw.Write(this.NumObjects);
					bw.Write((ushort)this.GetSize());

					// Write unknown values
					bw.Write((ushort)0x08);
					bw.Write((ushort)(0xC + this.NumObjects * 4));
					bw.Write(0x017F);
					foreach (T data in this.Data) {
						bw.Write(data.UnknownData1);
						bw.Write(data.UnknownData2);
					}

					// Write info values
					int infoSize = (this.Data.Count > 0) ? this.Data[0].GetInfoSize() : 0;
					bw.Write((ushort)infoSize);
					bw.Write((ushort)(4 + this.NumObjects * infoSize));
					foreach (T data in this.Data)
						data.WriteInfo(strOut);

					// Write names
					foreach (T data in this.Data) {
						byte[] name = System.Text.Encoding.ASCII.GetBytes(data.Name);
						bw.Write(name);
						bw.Write(new byte[0x10 - name.Length]);
					}
				}

				public void AddElement(T element)
				{
					this.Data.Add(element);
					this.NumObjects++;
				}

				public void Clear()
				{
					this.Data.Clear();
					this.NumObjects = 0;
				}

				public int GetSize()
				{
					int size = 4 + 0xC;
					if (this.Data.Count > 0)
						size += this.NumObjects * this.Data[0].Size;
					return size;
				}
			}

            public abstract class DataInfo
            {
				public ushort UnknownData1 { get; set; }  // Unkonwn data 1 from Unknown
				public ushort UnknownData2 { get; set; }  // Unknown data 2 from Unknown

				public string Name { get; set; }

				public int Size {
					get {
						return 4 + this.GetInfoSize() + 0x10;
					}
				}

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

				public abstract void WriteInfo(Stream strOut);

				public abstract int GetInfoSize();
            }

			public class TextureDataInfo
				: DataInfo
			{
				public uint TextureOffset { get; set; }
				public int Width    { get; set; }
				public byte Unknown1 { get; set; }
				public byte Unknown2 { get; set; }
				public byte Unknown3 { get; set; }

				public byte CoordinateTransformation { get; set; }
				public bool Color0 { get; set; }
				public ColorFormat Format { get; set; }
				public int Height { get; set; }
				//public int Width2 { get; set; }
				public bool FlipY { get; set; }
				public bool FlipX { get; set; }
				public bool RepeatY { get; set; }
				public bool RepeatX { get; set; }

				public int Length {
					get {
						return (int)((this.Format.Bpp() * this.Height * this.Width) / 8);
					}
				}

				protected override void ReadInfo(Stream strIn) 
				{
					BinaryReader br = new BinaryReader(strIn);

					this.TextureOffset = (uint)(br.ReadUInt16() << 3);
					ushort parameters  = br.ReadUInt16();
					br.ReadByte();	// Width
					this.Unknown1 = br.ReadByte();
					this.Unknown2 = br.ReadByte();
					this.Unknown3 = br.ReadByte();

					// Now let's get the information inside Parameters
					this.CoordinateTransformation = (byte)(parameters >> 14);
					this.Color0  = ((parameters >> 13) & 1) == 1;
					this.Format  = (ColorFormat)((parameters >> 10) & 7);
					this.Height  = (byte)(1 << (((parameters >> 7) & 7) + 3));
					this.Width   = (byte)(1 << (((parameters >> 4) & 7) + 3));
					this.FlipY   = (byte)((parameters >> 3) & 1) == 1;
					this.FlipX   = (byte)((parameters >> 2) & 1) == 1;
					this.RepeatY = (byte)((parameters >> 1) & 1) == 1;
					this.RepeatX = (byte)(parameters & 1) == 1;

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
				}

				public override void WriteInfo(Stream strOut)
				{
					BinaryWriter bw = new BinaryWriter(strOut);
					
					// Now let's set the information inside Parameters
					ushort parameters = 0;
					parameters |= (ushort)((this.CoordinateTransformation & 0x03) << 14);
					parameters |= (ushort)((this.Color0 ? 1 : 0) << 13);
					parameters |= (ushort)(((int)this.Format & 0x07) << 10);
					parameters |= (ushort)(((int)(Math.Log(this.Height, 2) - 3) & 0x07) << 7);
					parameters |= (ushort)(((int)(Math.Log(this.Width , 2) - 3) & 0x07) << 4);
					parameters |= (ushort)((this.FlipY ? 1 : 0) << 3);
					parameters |= (ushort)((this.FlipX ? 1 : 0) << 2);
					parameters |= (ushort)((this.RepeatY ? 1 : 0) << 1);
					parameters |= (ushort)((this.RepeatX ? 1 : 0) << 0);

					bw.Write((ushort)(this.TextureOffset >> 3));
					bw.Write(parameters);
					bw.Write((byte)this.Width);
					bw.Write(this.Unknown1);
					bw.Write(this.Unknown2);
					bw.Write(this.Unknown3);
				}

				public override int GetInfoSize()
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

				public override void WriteInfo(Stream strOut)
				{
					BinaryWriter bw = new BinaryWriter(strOut);
					bw.Write((ushort)(this.Offset >> 3));
					bw.Write(this.Unknown);
				}

				public override int GetInfoSize()
				{
					return 4;
				}
			}
		}
	}
}
