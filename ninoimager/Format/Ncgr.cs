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
using System;
using System.IO;

namespace Ninoimager.Format
{
	public class Ncgr : Image
	{
		private static Type[] BlockTypes = { typeof(Ncgr.CharBlock), typeof(Ncgr.Sopc) };
		private NitroFile nitro;
		private CharBlock charBlock;
		private Sopc sopc;

		public Ncgr()
		{
			this.nitro = new NitroFile(BlockTypes);
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
			this.charBlock = this.nitro.GetBlock<CharBlock>(0);
			this.sopc      = this.nitro.GetBlock<Sopc>(0);

			this.Width  = this.charBlock.Width;
			this.Height = this.charBlock.Height;
			this.SetData(this.charBlock.ImageData, PixelEncoding.Lineal, this.charBlock.Format);
		}

		private void SetInfo()
		{
			throw new NotImplementedException();
		}

		private class CharBlock : NitroBlock
		{
			public CharBlock(NitroFile nitro)
				: base(nitro)
			{
			}

			public override string Name {
				get { return "CHAR"; }
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

			public uint Unknown1 {
				get;
				set;
			}

			public uint Unknown2 {
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

				this.Height   = br.ReadUInt16();
				this.Width    = br.ReadUInt16();
				uint format   = br.ReadUInt32();
				this.Format   = (ColorFormat)format;
				this.Unknown1 = br.ReadUInt32();
				this.Unknown2 = br.ReadUInt32();

				uint dataLength = br.ReadUInt32();
				uint dataOffset = br.ReadUInt32();
				strIn.Position  = blockPos + dataOffset;
				this.ImageData  = br.ReadBytes((int)dataLength);

#if DEBUG
				if (this.Height == 0 || this.Height == 0xFFFF)
					Console.WriteLine("\t* Invalid height value.");
				if (this.Width == 0 || this.Width == 0xFFFF)
					Console.WriteLine("\t* Invalid width value.");
				if (format != 3 && format != 4)
					Console.WriteLine("\t* Uncommon format.");
				if (this.Unknown1 != 0)
					Console.WriteLine("\t* Unknown1 different to 0.");
				if (this.Unknown2 != 0)
					Console.WriteLine("\t* Unknown2 different to 0.");
				if (this.ImageData.Length == 0)
					Console.WriteLine("\t* Image data null.");
#endif
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);

				bw.Write(this.Height);
				bw.Write(this.Width);
				bw.Write((uint)this.Format);
				bw.Write(this.Unknown1);
				bw.Write(this.Unknown2);
				bw.Write((uint)this.ImageData.Length);
				bw.Write(0x18);
				bw.Write(this.ImageData);
			}
		}

		private class Sopc : NitroBlock
		{
			public Sopc(NitroFile nitro)
				: base(nitro)
			{
			}

			public override string Name {
				get { return "SOPC"; }
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
		}
	}
}

