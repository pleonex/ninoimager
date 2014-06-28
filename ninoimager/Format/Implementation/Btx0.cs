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
using System.Drawing;
using System.IO;

namespace Ninoimager.Format
{
	public class Btx0 : Image
	{
		private static Type[] BlockTypes = { typeof(Btx0.Tex0) };
		private NitroFile nitro;
		private Tex0 tex0;

		public Btx0()
		{
			this.nitro = new NitroFile("BTX0", "1.0", BlockTypes);
			this.tex0 = new Tex0(this.nitro);
			this.nitro.Blocks.Add(this.tex0);
		}

		public Btx0(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.GetInfo();
		}

		public Btx0(Stream str)
		{
			this.nitro = new NitroFile(str, BlockTypes);
			this.GetInfo();
		}

		public NitroFile NitroData {
			get { return this.nitro; }
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
			throw new NotImplementedException();
		}

		private void SetInfo()
		{
			throw new NotImplementedException();
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

			protected override void ReadData(Stream strIn)
			{
				long blockOffset = strIn.Position;
				BinaryReader br  = new BinaryReader(strIn);

				// Offset and size section
				this.Unknown1       = br.ReadUInt32();
				uint texDataSize    = (uint)(br.ReadUInt16() << 3);
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
                        numObjects = this.NumObjects;
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
                            RepeatedData[NumObjects].Unknow1 = br.ReadUInt16();
                            RepeatedData[NumObjects].Unknow2 = br.ReadUInt16();
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

                    private Info3dType Type { get; set; }

                    private ushort HeaderSize { get; set; }

                    private ushort DataSize { get; set; }

                    private byte NumObjects { get; set; }

                    public void ReadData(Stream strIn)
                    {
                        BinaryReader br = new BinaryReader(strIn);

                        this.HeaderSize = br.ReadUInt16();
                        this.DataSize   = br.ReadUInt16();
                        
                        //TODO read info about texture and palette
                        IInfoBase infoBlock;
                        if (Type == Info3dType.texture)
                            infoBlock = new InfoBlockTexture();
                        else
                            infoBlock = new InfoBlockPalette();

                        for (int i = 0; i < this.NumObjects; i++)
                        {
                            infoBlock.ReadData(strIn);
                        }

                    }

                    private interface IInfoBase
                    {
                        void ReadData(Stream strIn);
                    }

                    public class InfoBlockTexture : IInfoBase
                    {
                        void IInfoBase.ReadData(Stream strIn)
                        {
                            //reading texture informations...
                            BinaryReader br = new BinaryReader(strIn);

                            this.TextureOffset = (ushort)(br.ReadUInt16() << 3);
                            this.Parameters    = br.ReadUInt16();
                            this.Width         = br.ReadByte();
                            this.Unknown1      = br.ReadByte();
                            this.Unknown2      = br.ReadByte();
                            this.Unknown3      = br.ReadByte();

                            //now let's get the information inside Parameters
                            this.parameters = new Params();
                            this.parameters.CoordTransf = (byte)(this.Parameters & 14);
                            this.parameters.Color0      = (byte)((this.Parameters >> 13) & 1);
                            this.parameters.Format      = (byte)((this.Parameters >> 10) & 7);
                            this.parameters.Height      = (byte)(8 << ((this.Parameters >> 7) & 7));
                            this.parameters.Width       = (byte)(8 << ((this.Parameters >> 4) & 7));
                            this.parameters.Flip_Y      = (byte)((this.Parameters >> 3) & 1);
                            this.parameters.Flip_X      = (byte)((this.Parameters >> 2) & 1);
                            this.parameters.Repeat_Y    = (byte)((this.Parameters >> 1) & 1);
                            this.parameters.Repeat_X    = (byte)(this.Parameters & 1);

                            //copied from Tinke source code:
                            if (parameters.Width == 0x00)
                                switch (this.Unknown1 & 0x3)
                                {
                                    case 2:
                                        parameters.Width = 0x200;
                                        break;
                                    default:
                                        parameters.Width = 0x100;
                                        break;
                                }
                            if (parameters.Height == 0x00)
                                switch ((this.Unknown1 >> 4) & 0x3)
                                {
                                    case 2:
                                        parameters.Height = 0x200;
                                        break;
                                    default:
                                        parameters.Height = 0x100;
                                        break;
                                }
                        }

                        public ushort TextureOffset { get; set; }

                        public ushort Parameters { get; set; }

                        public Params parameters;

                        public byte Width { get; set; }

                        public byte Unknown1 { get; set; }

                        public byte Unknown2 { get; set; }

                        public byte Unknown3 { get; set; }

                        private struct Params
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
                        void IInfoBase.ReadData(Stream strIn)
                        {
                            //reading palette informations...
                            BinaryReader br = new BinaryReader(strIn);

                            this.PaletteOffset = br.ReadUInt16();
                            this.Unknown       = br.ReadUInt16();
                        }

                        public ushort PaletteOffset { get; set; }

                        public ushort Unknown { get; set; }

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

                        for (int i = 0; i < this.NumObjects; i++)
                            this.Names[i] = br.ReadChars(16).ToString();
                    }


                }

            }
		}

		

	}
}
