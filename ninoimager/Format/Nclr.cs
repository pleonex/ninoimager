// -----------------------------------------------------------------------
// <copyright file="Nclr.cs" company="none">
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
// <date>07/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.IO;
using System.Drawing;

namespace Ninoimager.Format
{
	public enum ColorFormat {
		BGR555_4bpp = 3,
		BGR555_8bpp = 4
	}

	public class Nclr : Palette
	{
		private static Type[] BlockTypes = { typeof(Pltt), typeof(Pcmp) };
		private NitroFile nitro;

		public Nclr()
		{
			this.nitro = new NitroFile(BlockTypes);
		}

		public Nclr(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.SetInfo();
		}

		public Nclr(Stream stream)
		{
			this.nitro = new NitroFile(stream, BlockTypes);
			this.SetInfo();
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

		private void SetInfo()
		{
			this.SetPalette(this.nitro.GetBlock<Pltt>(0).PaletteColors);
		}

		private class Pltt : NitroBlock
		{
			public Pltt(NitroFile file) : base(file)
			{ 
			}

			public override string Name {
				get { return "PLTT"; }
			}

			public ColorFormat Depth {
				get;
				set;
			}

			public ushort Unknown1 {
				get;
				private set;
			}

			public uint Unknown2 {
				get;
				private set;
			}

			public uint Unknown3 {
				get;
				private set;
			}

			public Color[] PaletteColors {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br    = new BinaryReader(strIn);
				this.Depth         = (ColorFormat)br.ReadUInt16();
				this.Unknown1      = br.ReadUInt16();
				this.Unknown2      = br.ReadUInt32();
				int paletteSize    = br.ReadInt32();	// CHECK: Not very sure
				if (paletteSize == 0)
					paletteSize = 512;
				this.Unknown3      = br.ReadUInt32();
				this.PaletteColors = Palette.FromBGR555(br.ReadBytes(paletteSize));
			}

			protected override void WriteData(Stream strOut)
			{
				byte[] paletteBytes = Palette.ToBGR555(this.PaletteColors);

				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write((ushort)this.Depth);
				bw.Write(this.Unknown1);
				bw.Write(this.Unknown2);
				bw.Write((paletteBytes.Length != 512) ? paletteBytes.Length : 0); // CHECK: Not very sure
				bw.Write(this.Unknown3);
				bw.Write(paletteBytes);
			}

			public override bool Check()
			{
				// UNDONE
				throw new NotImplementedException();
			}
		}

		private class Pcmp : NitroBlock
		{
			public Pcmp(NitroFile file) : base(file)
			{
			}

			public override string Name {
				get { return "PCMP"; }
			}

			public ushort Unknown1 {
				get;
				set;
			}

			public ushort Unknown2 {
				get;
				set;
			}

			public byte[] Unknown3 {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);
				this.Unknown1 = br.ReadUInt16();
				this.Unknown2 = br.ReadUInt16();
				this.Unknown3 = new byte[this.Size - 12];
				this.Unknown3 = br.ReadBytes(this.Unknown3.Length);
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write(this.Unknown1);
				bw.Write(this.Unknown2);
				bw.Write(this.Unknown3);
			}

			public override bool Check()
			{
				// UNDONE
				throw new NotImplementedException();
			}
		}
	}
}

