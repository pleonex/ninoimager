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
				uint texDataSize    = br.ReadUInt16();
				uint texInfoOffset  = br.ReadUInt16();
				this.Unknown2       = br.ReadUInt32();
				uint texDataOffset  = br.ReadUInt32();

				this.Unknown3               = br.ReadUInt32();
				uint texTexelDataSize       = br.ReadUInt16();
				uint texTexelInfoOffset     = br.ReadUInt16();
				this.Unknown4               = br.ReadUInt32();
				uint texTexelDataOffset     = br.ReadUInt32();
				uint texTexelInfoDataOffset = br.ReadUInt32();

				this.Unknown5      = br.ReadUInt32();
				uint palDataSize   = br.ReadUInt32();
				uint palInfoOffset = br.ReadUInt32();
				uint palDataOffset = br.ReadUInt32();

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
			}

			protected override void WriteData(Stream strOut)
			{
				throw new NotImplementedException ();
			}

			protected override void UpdateSize()
			{
				throw new NotImplementedException ();
			}
		}

		private class Info3D
		{
			public Info3D(bool isTexture)
			{
				this.IsTexture = isTexture;
			}

			/// <summary>
			/// Gets a value indicating whether this instance contains texture or palette info.
			/// </summary>
			/// <value><c>true</c> if this instance contains texture info; otherwise, <c>false</c>.</value>
			public bool IsTexture {
				get;
				private set;
			}

			public byte Unknown1 {
				get;
				set;
			}

			public byte NumObjs {
				get;
				set;
			}

			public UnknownBlock Unknown2 {
				get;
				set;
			}

			public InfoBlock Info {
				get;
				set;
			}

			public string[] Names {
				get;
				set;
			}

			public class UnknownBlock
			{
				public UnknownBlock(int numObjs)
				{
					this.NumObjs = numObjs;
				}

				private int NumObjs {
					get;
					set;
				}

				public uint Constant {
					get;
					set;
				}

				public ushort[] Unknown1 {
					get;
					set;
				}

				public ushort[] Unknown2 {
					get;
					set;
				}

				public void Read(Stream str)
				{
					BinaryReader br = new BinaryReader(str);

					br.ReadUInt16();	// Header size
					br.ReadUInt16();	// Block size
					this.Constant = br.ReadUInt32();

					this.Unknown1 = new ushort[this.NumObjs];
					this.Unknown2 = new ushort[this.NumObjs];
					for (int i = 0; i < this.NumObjs; i++) {
						this.Unknown1[i] = br.ReadUInt16();
						this.Unknown2[i] = br.ReadUInt16();
					}
				}

				public void Write(Stream str)
				{
					throw new NotImplementedException();
				}
			}

			public class InfoBlock
			{
				public InfoBlock(int numObjs, bool isTexture)
				{
					this.NumObjs = numObjs;
					this.IsTexture = isTexture;
				}

				private int NumObjs {
					get;
					set;
				}

				private bool IsTexture {
					get;
					set;
				}

				public InfoBase[] InfoData {
					get;
					set;
				}

				public interface InfoBase
				{
				}

				public struct TexInfo : InfoBase
				{
					public TexInfo(Stream str)
						: this()
					{
						this.Read(str);
					}

					#region Parameter
					public byte RepeatX {
						get;
						set;
					}

					public byte RepeatY {
						get;
						set;
					}

					public byte FlipX {
						get;
						set;
					}

					public byte FlipY {
						get;
						set;
					}

					public ushort Width {
						get;
						set;
					}

					public ushort Height {
						get;
						set;
					}

					public ColorFormat Format {
						get;
						set;
					}

					public bool IsTransparent {
						get;
						set;
					}

					public byte CoordTransf {
						get;
						set;
					}

					public byte Depth {
						get;
						set;
					}
					#endregion

					public ushort Unknown1 {
						get;
						set;
					}

					public ushort Unknown2 {
						get;
						set;
					}

					private void Read(Stream str)
					{
						BinaryReader br = new BinaryReader(str);

						ushort texOffset = br.ReadUInt16();
						ushort parameter = br.ReadUInt16();
						this.Unknown1 = br.ReadUInt16();
						this.Unknown2 = br.ReadUInt16();
					}

					public void Write(Stream str)
					{
						throw new NotImplementedException();
					}
				}

				public struct PalInfo : InfoBase
				{
					public PalInfo(Stream str)
						: this()
					{
						this.Read(str);
					}

					public ushort Unknown1 {
						get;
						set;
					}

					private void Read(Stream str)
					{
						BinaryReader br = new BinaryReader(str);
						ushort palOffset = br.ReadUInt16();
						ushort unknown1 = br.ReadUInt16();
					}

					public void Write(Stream str)
					{
						throw new NotImplementedException();
					}
				}

				public void Read(Stream str)
				{
					BinaryReader br = new BinaryReader(str);

					br.ReadUInt16();	// Header size
					br.ReadUInt16();	// Data size

					this.InfoData = new InfoBase[this.NumObjs];
					for (int i = 0; i < this.NumObjs; i++) {
						if (this.IsTexture)
							this.InfoData[i] = new TexInfo(str);
						else
							this.InfoData[i] = new PalInfo(str);
					}
				}

				public void Write(Stream str)
				{
					throw new NotImplementedException();
				}
			}

			public void Read(Stream str)
			{
				BinaryReader br = new BinaryReader(str);

				this.Unknown1    = br.ReadByte();
				this.NumObjs     = br.ReadByte();
				br.ReadUInt16();	// blockSize

				this.Unknown2 = new UnknownBlock(this.NumObjs);
				this.Unknown2.Read(str);

				this.Info = new InfoBlock(this.NumObjs, this.IsTexture);
				this.Info.Read(str);

				this.Names = new string[this.NumObjs];
				for (int i = 0; i < this.NumObjs; i++)
					this.Names[i] = new string(br.ReadChars(0x10)).Replace("\0", "");
			}

			public void Write(Stream str)
			{
				throw new NotImplementedException();
			}
		}

	}
}
