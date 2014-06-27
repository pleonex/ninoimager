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

                /*
				// Info section
				strIn.Position = blockOffset + texInfoOffset;
				this.TexInfo   = new Info3D(true);
				this.TexInfo.Read(strIn);
				this.TextureData = new byte[this.TexInfo.NumObjs][];

				strIn.Position = blockOffset + palInfoOffset;
				this.PalInfo   = new Info3D(false);
				this.PalInfo.Read(strIn);
				this.Palette = new Color[this.PalInfo.NumObjs][];

				// UNDONE: Read image data
                */
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
                public enum info3dType { palette, texture }

                public info3dType type { get; set; }

                public byte Nobjects { get; set; }

                private UnknownBlock BlockUnknown { get; set; }

                public Info3D(info3dType type)
                {
                    this.type = type;
                }

                public void ReadData(Stream strIn)
                {
                    BinaryReader br = new BinaryReader(strIn);

                    byte dummy         = br.ReadByte();
                    this.Nobjects      = br.ReadByte();
                    ushort sectionSize = br.ReadUInt16();

                    //reading unknown block
                    this.BlockUnknown = new UnknownBlock(Nobjects);
                    this.BlockUnknown.ReadData(strIn);

                    //TODO reading info block
                    throw new NotImplementedException();
                    //TODO reading name block


                }

                private class UnknownBlock
                {
                    public UnknownBlock(byte nObjects)
                    {
                        nObjects = this.Nobjects;
                    }

                    private byte Nobjects { get; set; }

                    private ushort HeaderSize { get; set; }

                    private ushort SectionSize { get; set; }

                    private uint Constant { get; set; }

                    private UnknownRepeatedData[] repeatedData { get; set; }

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

                        this.repeatedData = new UnknownRepeatedData[Nobjects];
                        for (int i = 0; i < Nobjects; i++)
                        {
                            repeatedData[Nobjects].Unknow1 = br.ReadUInt16();
                            repeatedData[Nobjects].Unknow2 = br.ReadUInt16();
                        }
                    }
                    
                }

                private class InfoBlock
                {
                    public InfoBlock(info3dType type)
                    {
                        this.type = type;
                    }

                    private info3dType type;
                    private ushort headerSize { get; set; }
                    private ushort dataSize { get; set; }

                    public void ReadData(Stream strIn)
                    {
                        BinaryReader br = new BinaryReader(strIn);

                        this.headerSize = br.ReadUInt16();
                        this.dataSize = br.ReadUInt16();
                        
                        //TODO read info about texture and palette
                    }

                    private interface InfoBase
                    {
                        void ReadData();
                    }

                    private class InfoBlockTexture : InfoBase
                    {
                        void InfoBase.ReadData()
                        {
                            //reading texture informations...
                            throw new NotImplementedException();
                        }
                    }

                    private class InfoBlockPalette : InfoBase
                    {

                        void InfoBase.ReadData()
                        {
                            //reading palette informations...
                            throw new NotImplementedException();
                        }
                    }

                }

                private class NameBlock
                {

                }

            }
		}

		

	}
}
