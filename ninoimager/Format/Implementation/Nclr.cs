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

//#define VERBOSE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Color = Emgu.CV.Structure.Bgra;

namespace Ninoimager.Format
{
	public class Nclr : Palette
	{
		private static Type[] BlockTypes = { typeof(Pltt), typeof(Pcmp) };
		private NitroFile nitro;
		private Pltt pltt;
		private Pcmp pcmp;

		public Nclr()
		{
			this.nitro = new NitroFile("NCLR", "1.0", BlockTypes);
			this.pltt = new Pltt(this.nitro);

			this.nitro.Blocks.Add(this.pltt);
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

		public bool Extended {
			get { return this.pltt.ExtendedPalette == 0; }
			set { this.pltt.ExtendedPalette = (value ? 1u : 0u); }
		}

		public ColorFormat Format {
			get { return this.pltt.Depth; }
			set { this.pltt.Depth = value; }
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

		public void SetData(Color[] palette, ColorFormat depth)
		{
			this.pltt.Depth = depth;
			this.pltt.PaletteColors = palette;
			this.GetInfo();
		}

		private void GetInfo()
		{
			this.pltt = this.nitro.GetBlock<Pltt>(0);

			int numColors = (this.pltt.Depth == ColorFormat.Indexed_8bpp) ? 0x100 : 0x10;
			int numPalettes = 0;
			ushort[] index = null;

			if (this.nitro.Blocks.ContainsType("PCMP")) {
				this.pcmp = this.nitro.GetBlock<Pcmp>(0);
				index = this.pcmp.PaletteIndex;
				numPalettes = this.pcmp.NumPalettes;
			} else {
				numPalettes = this.pltt.PaletteColors.Length / numColors;
				if (this.pltt.PaletteColors.Length % numColors != 0) numPalettes++;
			}

			this.SetPalette(DividePalette(numColors, numPalettes, this.pltt.PaletteColors, index));
		}

		private void SetInfo()
		{
			List<Color> palette = new List<Color>();
			foreach (Color[] subpal in this.Palettes)
				palette.AddRange(subpal);

			this.pltt.Depth = (this.GetPalette(0).Length > 0x10) ? ColorFormat.Indexed_8bpp : ColorFormat.Indexed_4bpp;
			this.pltt.PaletteColors = palette.ToArray();

			// Disabled until know the meaning of PaletteInfo in that block
			//if (this.pcmp != null)
			//	this.pcmp.NumPalettes   = (ushort)this.NumPalettes;
		}

		private static Color[][] DividePalette(int numColors, int numPalettes, Color[] palette, ushort[] index = null)
		{
			if (index != null && index.Length != numPalettes)
				throw new ArgumentOutOfRangeException();

			Color[][] newPalettes;
			if (index != null && index.Length > 0)
			    newPalettes = new Color[index.Max() + 1][];
			else
			    newPalettes = new Color[numPalettes][];	
			for (int i = 0; i < newPalettes.Length; i++)
				newPalettes[i] = new Color[0];

			// Set the right palette
		      int totalColors = palette.Length;
			for (int i = 0; i < numPalettes; i++) {
				int idx = (index != null) ? index[i] : i;
				int copyColors = ((i + 1) * numColors < totalColors) ? numColors : totalColors - i * numColors;
				newPalettes[idx] = new Color[copyColors];
				Array.Copy(palette, i * numColors, newPalettes[idx], 0, copyColors);
			}

			return newPalettes;
		}

		private class Pltt : NitroBlock
		{
			public Pltt(NitroFile file) : base(file)
			{ 
			}

			public ColorFormat Depth {
				get;
				set;
			}

			/// <summary>
			/// Gets a value indicating whether this instance contains more than one palette of 256 colors.
			/// The value isn't actually read in game.
			/// More info at <seealso cref="http://nocash.emubase.de/gbatek.htm#dsvideoextendedpalettes"/>
			/// </summary>
			/// <value><c>true</c> if this instance is multipalette of 256 colors; otherwise, <c>false</c>.</value>
			public uint ExtendedPalette {
				get;
				set;
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
				this.ExtendedPalette = br.ReadUInt32();

				br.ReadInt32();		// palSize
				// Since if the file contains a PCMP block the palette size may be wrong and unused, I'll obtain
				// the size from the block size
				int actualSize     = this.Size - 0x8 - 0x10;
				int palOffset      = br.ReadInt32();
				strIn.Position     = blockPos + palOffset;
				this.PaletteColors = br.ReadBytes(actualSize).ToBgr555Colors();

#if VERBOSE
				if (palSize != actualSize)
					Console.WriteLine("\tPLTT: Palette size is different to actual size");
				if (palOffset != 0x10)
					Console.WriteLine("\tPLTT: Palette offset different to 0x10");
				if (depth != 3 && depth != 4)
					Console.WriteLine("\tPLTT: Unknown color format");
				if (this.ExtendedPalette == 1 && this.Depth != ColorFormat.Indexed_8bpp && 
				    this.PaletteColors.Length < 256)
					Console.WriteLine("\tPLTT: IsMultiPalette8bpp meaning is different!");
#endif
			}

			protected override void WriteData(Stream strOut)
			{
				byte[] paletteBytes = this.PaletteColors.ToBgr555();

				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write((uint)this.Depth);
				bw.Write(this.ExtendedPalette);
				bw.Write(paletteBytes.Length);
				bw.Write((uint)0x10);
				bw.Write(paletteBytes);
			}

			protected override void UpdateSize()
			{
				this.Size = 0x08 + 0x10 + (this.PaletteColors.Length * 2);
			}
		}

		private class Pcmp : NitroBlock
		{
			public Pcmp(NitroFile file) : base(file)
			{
			}

			public ushort NumPalettes {
				get;
				set;
			}

			public ushort Constant {
				get;
				set;
			}

			public ushort[] PaletteIndex {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br  = new BinaryReader(strIn);
				long blockOffset = strIn.Position;

				this.NumPalettes = br.ReadUInt16();
				this.Constant    = br.ReadUInt16();

				uint dataOffset  = br.ReadUInt32();
				strIn.Position   = blockOffset + dataOffset;
				this.PaletteIndex = new ushort[this.NumPalettes];
				for (int i = 0; i < this.NumPalettes; i++)
					this.PaletteIndex[i] = br.ReadUInt16();

#if DEBUG
				if (this.NumPalettes == 0)
					Console.WriteLine("\tPCMP: 0 palettes?");
				if (this.Constant != 0xBEEF)
					Console.WriteLine("\tPCMP: Constant is different");
#endif
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);
				bw.Write(this.NumPalettes);
				bw.Write(this.Constant);
				bw.Write((uint)0x08);
				for (int i = 0; i < this.NumPalettes; i++)
					bw.Write(this.PaletteIndex[i]);
			}

			protected override void UpdateSize()
			{
				this.Size = 0x08 + 0x08 + (this.PaletteIndex.Length * 2);
			}
		}
	}
}

