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
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace Ninoimager.Format
{
	public enum ColorFormat {
		Unknown,
		BGR555_4bpp = 3,
		BGR555_8bpp = 4
	}

	public class Nclr : Palette
	{
		private static Type[] BlockTypes = { typeof(Pltt), typeof(Pcmp) };
		private NitroFile nitro;
		private Pltt pltt;
		private Pcmp pcmp;

		public Nclr()
		{
			this.nitro = new NitroFile(BlockTypes);
		}

		public Nclr(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.GetInfo();
		}

		public Nclr(Stream stream)
		{
			this.nitro = new NitroFile(stream, BlockTypes);
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
			this.pcmp = this.nitro.GetBlock<Pcmp>(0);
			this.pltt = this.nitro.GetBlock<Pltt>(0);

			if (this.nitro.Blocks.ContainsType("PCMP")) {
				int numColors   = (this.pltt.Depth == ColorFormat.BGR555_8bpp) ? 0x100 : 0x10;
				int numPalettes = this.pcmp.NumPalettes;
				this.SetPalette(DividePalette(numColors, numPalettes, this.pltt.PaletteColors));
			} else {
				this.SetPalette(this.pltt.PaletteColors);
			}
		}

		private void SetInfo()
		{
			List<Color> palette = new List<Color>();
			foreach (Color[] subpal in this.Palettes)
				palette.AddRange(subpal);

			this.pltt.Depth = (this.GetPalette(0).Length > 0x10) ? ColorFormat.BGR555_8bpp : ColorFormat.BGR555_4bpp;
			this.pltt.PaletteColors = palette.ToArray();
			this.pcmp.NumPalettes   = (ushort)this.NumPalettes;
		}

		private static Color[][] DividePalette(int numColors, int numPalettes, Color[] palette)
		{
			int totalColors = palette.Length;

			Color[][] newPalettes = new Color[numPalettes][];
			for (int i = 0; i < numPalettes; i++) {
				int copyColors = ((i + 1) * numColors < totalColors) ? numColors : totalColors - i * numColors;
				newPalettes[i] = new Color[copyColors];
				Array.Copy(palette, i * numColors, newPalettes, 0, copyColors);
			}

			return newPalettes;
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

			public uint Unknown2 {
				get;
				private set;
			}

			public Color[] PaletteColors {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);
				long blockPos   = strIn.Position;

				uint depth    = br.ReadUInt32();
				this.Depth    = (ColorFormat)depth;
				this.Unknown2 = br.ReadUInt32();

				int palSize        = br.ReadInt32();
				// Since if the file contains a PCMP block the palette size may be wrong and unused, I'll obtain
				// the size from the block size
				int actualSize     = this.Size - 0x8 - 0x10;
				int palOffset      = br.ReadInt32();
				strIn.Position     = blockPos + palOffset;
				this.PaletteColors = Palette.FromBGR555(br.ReadBytes(actualSize));

#if DEBUG
				if (palSize != actualSize)
					Console.WriteLine("\t* Palette size is different to actual size");
				if (palOffset != 0x10)
					Console.WriteLine("\t* Palette offset different to 0x10");
				if (depth != 3 && depth != 4)
					Console.WriteLine("\t* Unknown color format");
				if (this.Unknown2 != 0)
					Console.WriteLine("\t* Reserved 2 different to 0");
#endif
			}

			protected override void WriteData(Stream strOut)
			{
				byte[] paletteBytes = Palette.ToBGR555(this.PaletteColors);

				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write((uint)this.Depth);
				bw.Write(this.Unknown2);
				bw.Write(paletteBytes.Length);
				bw.Write((uint)0x10);
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

			public ushort NumPalettes {
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
				BinaryReader br  = new BinaryReader(strIn);
				long blockOffset = strIn.Length;

				this.NumPalettes = br.ReadUInt16();
				this.Unknown2    = br.ReadUInt16();

				uint dataOffset = br.ReadUInt32();
				strIn.Position  = blockOffset + dataOffset;
				this.Unknown3   = new byte[this.Size - 10];
				this.Unknown3   = br.ReadBytes(this.Unknown3.Length);
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write(this.NumPalettes);
				bw.Write(this.Unknown2);
				bw.Write((uint)0x08);
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

