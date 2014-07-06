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
		private static Type[] BlockTypes = { typeof(Btx0.Tex0) };
		private NitroFile nitro;
		private Tex0 tex0;

		private Image[] images;

		public Btx0()
		{
			this.nitro = new NitroFile("BTX0", "1.0", BlockTypes) { HasOffsets = true };
			this.tex0 = new Tex0(this.nitro);
			this.nitro.Blocks.Add(this.tex0);
			this.images = new Image[0];
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

		public int NumObjects {
			get { return this.tex0.texInfo.NumObjects; }
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
			this.tex0 = this.nitro.GetBlock<Tex0>(0);
			this.images = new Image[this.tex0.texInfo.NumObjects];
			for (int i = 0; i < tex0.texInfo.NumObjects; i++) {
				this.images[i] = new Image();
				this.images[i].Width = tex0.texInfo.parameters[i].Width;
				this.images[i].Height = tex0.texInfo.parameters[i].Height;
				this.images[i].SetData(
					tex0.TextureData[i],
					Ninoimager.Format.PixelEncoding.Lineal,
					tex0.texInfo.colorFormat[i]
				);
			}
		}

		private void SetInfo()
		{
			throw new NotImplementedException();
		}

		public EmguImage CreateBitmap(int texIdx)
        {
			string texName = this.tex0.TexInfo.BlockName.Names[texIdx];

			int palIdx = -1;
			for (int i = 0; i < this.tex0.palInfo.NumObjects && palIdx == -1; i++) {
				string palName = this.tex0.PalInfo.BlockName.Names[i].Replace("_pl", "");
				if (palName == texName || palName.Replace("_pl", "") == texName)
					palIdx = i;
			}

			if (palIdx == -1)
				palIdx = 0;

			return this.CreateBitmap(texIdx, palIdx);
        }

		public EmguImage CreateBitmap(int texIdx, int palIdx)
		{
			return this.images[texIdx].CreateBitmap(new Palette(this.tex0.Palette), palIdx);
		}

		private class Tex0 : NitroBlock
		{
			public Tex0(NitroFile nitro)
				: base(nitro)
			{
			}

			public uint Unknown1 {
				get;
				set;
			}

			public uint Unknown2 {
				get;
				set;
			}

			public uint Unknown3 {
				get;
				set;
			}

			public uint Unknown4 {
				get;
				set;
			}

			public uint Unknown5 {
				get;
				set;
			}

			public Info3D TexInfo {
				get;
				set;
			}

			public Info3D PalInfo {
				get;
				set;
			}

			public byte[][] TextureData {
				get;
				set;
			}

			public byte[] TextureCompressedData {
				get;
				set;
			}

			public byte[] TextureCompressedInfoData {
				get;
				set;
			}

			public Color[][] Palette {
				get;
				set;
			}

            public Info3D.InfoBlock.InfoBlockPalette palInfo { get; set; }

            public Info3D.InfoBlock.InfoBlockTexture texInfo { get; set; }



			protected override void ReadData(Stream strIn)
			{
				long blockOffset = strIn.Position - 8;
				BinaryReader br  = new BinaryReader(strIn);

				// Offset and size section
				this.Unknown1       = br.ReadUInt32();
				uint texDataSize    = br.ReadUInt16();
				uint texInfoOffset  = br.ReadUInt16();
				this.Unknown2       = br.ReadUInt32();
				uint texDataOffset  = br.ReadUInt32();

				this.Unknown3               = br.ReadUInt32();
				uint texTexelDataSize       = (uint)(br.ReadUInt16() << 3);
				uint texTexelInfoOffset     = br.ReadUInt16();
				this.Unknown4               = br.ReadUInt32();
				uint texTexelDataOffset     = br.ReadUInt32();
				uint texTexelInfoDataOffset = br.ReadUInt32();

				this.Unknown5      = br.ReadUInt32();
				uint palDataSize   = (br.ReadUInt32() << 3);
				uint palInfoOffset = br.ReadUInt32();
				uint palDataOffset = br.ReadUInt32();

                //Info3D sections

                //texture
                strIn.Position = blockOffset + texInfoOffset;
                this.TexInfo   = new Info3D(Info3D.Info3dType.texture);
                this.TexInfo.ReadData(strIn);
                this.TextureData = new byte[this.TexInfo.NumObjects][];

                //palette
                strIn.Position = blockOffset + palInfoOffset;
                this.PalInfo = new Info3D(Info3D.Info3dType.palette);
                this.PalInfo.ReadData(strIn);
                this.Palette = new Color[this.PalInfo.NumObjects][];

                //TODO read palette and image data                           
                palInfo = PalInfo.BlockInfo.ReturnInfoPalette();
                texInfo = TexInfo.BlockInfo.ReturnInfoTexture();

                for (int i = 0; i < PalInfo.NumObjects; i++)
                {
                    //palette
                    strIn.Position = blockOffset + palDataOffset + palInfo.PaletteOffset[i];
                    this.Palette[i] = br.ReadBytes(PalInfo.BlockInfo.DataSize).ToBgr555Colors();

                    //pixel
					strIn.Position = blockOffset + texDataOffset + texInfo.TextureOffset[i];
                    this.TextureData[i] = br.ReadBytes(texInfo.Length[i]);
                }
                
			}

			protected override void WriteData(Stream strOut)
			{
				throw new NotImplementedException ();
			}

			protected override void UpdateSize()
			{
				throw new NotImplementedException ();
			}

            public class Info3D
            {
                public enum Info3dType { palette, texture }

                public Info3dType type { get; set; }

                public byte NumObjects { get; set; }

                public UnknownBlock BlockUnknown { get; set; }

                public InfoBlock BlockInfo { get; set; }

                public NameBlock BlockName { get; set; }

                public Info3D(Info3dType type)
                {
                    this.type = type;
                }

                public void ReadData(Stream strIn)
                {
                    BinaryReader br = new BinaryReader(strIn);

                    byte dummy         = br.ReadByte();
                    this.NumObjects    = br.ReadByte();
                    ushort sectionSize = br.ReadUInt16();
                    
                    //reading unknown block
                    this.BlockUnknown = new UnknownBlock(this.NumObjects);
                    this.BlockUnknown.ReadData(strIn);
                    
                    //reading info block
                    this.BlockInfo = new InfoBlock(this.type, this.NumObjects);
                    this.BlockInfo.ReadData(strIn);

                    //reading name block
                    this.BlockName = new NameBlock(this.NumObjects);
                    this.BlockName.ReadData(strIn);
                }

                public class UnknownBlock
                {
                    public UnknownBlock(byte numObjects)
                    {
						this.NumObjects = numObjects;
                    }

                    private byte NumObjects { get; set; }

                    private ushort HeaderSize { get; set; }

                    private ushort SectionSize { get; set; }

                    private uint Constant { get; set; }

                    private UnknownRepeatedData[] RepeatedData { get; set; }

                    private struct UnknownRepeatedData
                    {
                        public ushort Unknow1 { get; set; }
                        public ushort Unknow2 { get; set; }
                    }

                    public void ReadData(Stream strIn)
                    {
                        BinaryReader br = new BinaryReader(strIn);

                        this.HeaderSize  = br.ReadUInt16();
                        this.SectionSize = br.ReadUInt16();
                        this.Constant    = br.ReadUInt32();

                        this.RepeatedData = new UnknownRepeatedData[NumObjects];
                        for (int i = 0; i < NumObjects; i++)
                        {
                            RepeatedData[i].Unknow1 = br.ReadUInt16();
                            RepeatedData[i].Unknow2 = br.ReadUInt16();
                        }
                    }
                    
                }

                public class InfoBlock
                {
                    public InfoBlock(Info3dType type, byte numObjects)
                    {
                        this.Type = type;
                        this.NumObjects = numObjects;
                    }

                    public Info3dType Type { get; set; }

                    public ushort HeaderSize { get; set; }

                    public ushort DataSize { get; set; }

                    public byte NumObjects { get; set; }

                    IInfoBase infoBlock;

                    public void ReadData(Stream strIn)
                    {
                        BinaryReader br = new BinaryReader(strIn);

                        this.HeaderSize = br.ReadUInt16();
                        this.DataSize   = br.ReadUInt16();
                        
                        //read info block about texture and palette
                        
                        if (Type == Info3dType.texture)
                            infoBlock = new InfoBlockTexture(this.NumObjects);
                        else
                            infoBlock = new InfoBlockPalette(this.NumObjects);
							
                        infoBlock.ReadData(strIn);
                    }

                    private interface IInfoBase
                    {
                        void ReadData(Stream strIn);
                    }

                    public class InfoBlockTexture : IInfoBase
                    {
                        public byte NumObjects { get; set; }

                        public int[] Length { get; set; }

                        public ColorFormat[] colorFormat { get; set; }

                        public InfoBlockTexture(byte numObjects)
                        {
                            this.NumObjects = numObjects;
                        }
                            

                        void IInfoBase.ReadData(Stream strIn)
                        {
                            //initializing arrays
                            this.TextureOffset = new ushort[this.NumObjects];
							this.Parameters = new ushort[this.NumObjects];
                            this.parameters = new Params[this.NumObjects];
                            this.Width = new byte[this.NumObjects];
                            this.Unknown1 = new byte[this.NumObjects];
                            this.Unknown2 = new byte[this.NumObjects];
                            this.Unknown3 = new byte[this.NumObjects];
							this.colorFormat = new ColorFormat[this.NumObjects];
							this.Length = new int[this.NumObjects];


                            //reading texture informations...
                            BinaryReader br = new BinaryReader(strIn);

                            for (int i = 0; i < this.NumObjects; i++)
                            {
								this.TextureOffset[i] = (ushort)(br.ReadUInt16() << 3);
                                this.Parameters[i] = br.ReadUInt16();
                                this.Width[i] = br.ReadByte();
                                this.Unknown1[i] = br.ReadByte();
                                this.Unknown2[i] = br.ReadByte();
                                this.Unknown3[i] = br.ReadByte();

                                //now let's get the information inside Parameters
                                this.parameters[i] = new Params();
                                this.parameters[i].CoordTransf = (byte)(this.Parameters[i] & 14);
                                this.parameters[i].Color0 = (byte)((this.Parameters[i] >> 13) & 1);
                                this.parameters[i].Format = (byte)((this.Parameters[i] >> 10) & 7);
                                this.parameters[i].Height = (byte)(8 << ((this.Parameters[i] >> 7) & 7));
                                this.parameters[i].Width = (byte)(8 << ((this.Parameters[i] >> 4) & 7));
                                this.parameters[i].Flip_Y = (byte)((this.Parameters[i] >> 3) & 1);
                                this.parameters[i].Flip_X = (byte)((this.Parameters[i] >> 2) & 1);
                                this.parameters[i].Repeat_Y = (byte)((this.Parameters[i] >> 1) & 1);
                                this.parameters[i].Repeat_X = (byte)(this.Parameters[i] & 1);

                                //copied from Tinke source code:
                                if (parameters[i].Width == 0x00)
                                    switch (this.Unknown1[i] & 0x3)
                                    {
                                        case 2:
                                            parameters[i].Width = 0x200;
                                            break;
                                        default:
                                            parameters[i].Width = 0x100;
                                            break;
                                    }
                                if (parameters[i].Height == 0x00)
                                    switch ((this.Unknown1[i] >> 4) & 0x3)
                                    {
                                        case 2:
                                            parameters[i].Height = 0x200;
                                            break;
                                        default:
                                            parameters[i].Height = 0x100;
                                            break;
                                    }

                                //getting the depth and texture length
                                this.colorFormat[i] = (ColorFormat)this.parameters[i].Format;
								this.Length[i] = (colorFormat[i].Bpp() * this.parameters[i].Height *
                                    this.parameters[i].Width) / 8;                                    
                            }

                        }

                        public ushort[] TextureOffset { get; set; }

                        public ushort[] Parameters { get; set; }

                        public Params[] parameters;

                        public byte[] Width { get; set; }

                        public byte[] Unknown1 { get; set; }

                        public byte[] Unknown2 { get; set; }

                        public byte[] Unknown3 { get; set; }

                        public struct Params
                        {
                            public byte CoordTransf { get; set; }
                            public byte Color0 { get; set; }
                            public byte Format { get; set; }
                            public ushort Height { get; set; }
                            public ushort Width { get; set; }
                            public byte Flip_Y { get; set; }
                            public byte Flip_X { get; set; }
                            public byte Repeat_Y { get; set; }
                            public byte Repeat_X { get; set; }
                        }

                    }

                    public class InfoBlockPalette : IInfoBase
                    {
                        public byte NumObjects { get; set; }

                        public InfoBlockPalette(byte numObjects)
                        {
                            this.NumObjects = numObjects;
                        }
                        void IInfoBase.ReadData(Stream strIn)
                        {
                            //initializing arrays
                            this.PaletteOffset = new ushort[this.NumObjects];
                            this.Unknown = new ushort[this.NumObjects];

                            //reading palette informations...
                            BinaryReader br = new BinaryReader(strIn);

                            for (int i = 0; i < this.NumObjects; i++)
                            {
								this.PaletteOffset[i] = (ushort)(br.ReadUInt16() << 3);
                                this.Unknown[i] = br.ReadUInt16();
                            }
                           
                        }

                        public ushort[] PaletteOffset { get; set; }

                        public ushort[] Unknown { get; set; }

                    }

                    public InfoBlockPalette ReturnInfoPalette()
                    {
                        if (this.Type == Info3dType.palette)
                            return (InfoBlockPalette)infoBlock;
                        return null;
                    }

                    public InfoBlockTexture ReturnInfoTexture()
                    {
						if (this.Type == Info3dType.texture)
                            return (InfoBlockTexture)infoBlock;
                        return null;
                    }
                }

                public class NameBlock
                {
                    public NameBlock(byte numObjects)
                    {
                        this.NumObjects = numObjects;
                    }

                    public byte NumObjects { get; set; }

                    public string[] Names { get; set; }

                    public void ReadData(Stream strIn)
                    {
                        BinaryReader br = new BinaryReader(strIn);

						this.Names = new string[this.NumObjects];
                        for (int i = 0; i < this.NumObjects; i++)
							this.Names[i] = new string(br.ReadChars(16)).Replace("\0", "");
                    }


                }

            }
		}

		

	}
}
