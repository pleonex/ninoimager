// -----------------------------------------------------------------------
// <copyright file="Nscr.cs" company="none">
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
// <date>16/08/2013</date>
// -----------------------------------------------------------------------

//#define VERBOSE
using System;
using System.IO;

namespace Ninoimager.Format
{
	// For more info see GbaTek
	public enum PaletteMode {
		Palette16_16 = 0,	// 16 colors / 16 palettes
		Palette256_1 = 1,	// 256 colors / 1 palette
		Extended = 2,		// 256 colors / 16 palettes
	}

	public class Nscr : Map
	{
		private static Type[] BlockTypes = { typeof(Nscr.Scrn) };
		private NitroFile nitro;
		private Scrn scrn;

		public Nscr()
		{
			this.nitro = new NitroFile(BlockTypes);
		}

		public Nscr(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.GetInfo();
		}

		public Nscr(Stream str)
		{
			this.nitro = new NitroFile(str, BlockTypes);
			this.GetInfo();
		}

		public NitroFile NitroData {
			get { return this.nitro; }
		}

		public PaletteMode PaletteMode {
			get { return this.scrn.PaletteMode; }
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
			this.scrn = this.nitro.GetBlock<Scrn>(0);

			this.TileSize = new System.Drawing.Size(8, 8);
			this.Width    = this.scrn.Width;
			this.Height   = this.scrn.Height;
			this.BgMode   = this.scrn.BgMode;
			this.SetMapInfo(this.scrn.Info);
		}

		private class Scrn : NitroBlock
		{
			public Scrn(NitroFile nitro)
				: base(nitro)
			{
			}

			public override string Name {
				get { return "SCRN"; }
			}

			public ushort Width {
				get;
				private set;
			}

			public ushort Height {
				get;
				private set;
			}

			public PaletteMode PaletteMode {
				get;
				private set;
			}

			public BgMode BgMode {
				get;
				private set;
			}

			public MapInfo[] Info {
				get;
				private set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);

				this.Width       = br.ReadUInt16();
				this.Height      = br.ReadUInt16();
				this.PaletteMode = (PaletteMode)br.ReadUInt16();
				this.BgMode      = (BgMode)br.ReadUInt16();
				uint dataLength  = br.ReadUInt32();
				uint numInfos = (this.BgMode == BgMode.Affine) ? dataLength : dataLength / 2;

#if VERBOSE
				if (this.Width == 0 || this.Width == 0xFFFF)
					Console.WriteLine("\t* Invalid width.");
				if (this.Height == 0 || this.Height == 0xFFFF)
					Console.WriteLine("\t* Invalid height.");
				if (dataLength == 0)
					Console.WriteLine("\t* Length is 0.");
				if (this.BgMode == BgMode.Extended)
					Console.WriteLine("\t* Extended mode.");
				if (this.PaletteMode == PaletteMode.Extended)
					Console.WriteLine("\t* Extended palette.");
				if (this.PaletteMode == PaletteMode.Palette256_1)
					Console.WriteLine("\t* 256/1 Palette.");
				if (this.BgMode == BgMode.Text && (this.Width > 256 || this.Height > 256))
					Console.WriteLine("\t* Multi pixel areas.");
				if (this.BgMode == BgMode.Affine)
					Console.WriteLine("\t* Affine mode.");
#endif

				this.Info = new MapInfo[numInfos];
				for (int i = 0; i < this.Info.Length; i++) {
					if (this.BgMode == BgMode.Affine)
						this.Info[i] = new MapInfo(br.ReadByte());
					else
						this.Info[i] = new MapInfo(br.ReadUInt16());
				}
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);
				int numInfos = (this.BgMode == BgMode.Affine) ? this.Info.Length : this.Info.Length * 2;

				bw.Write(this.Width);
				bw.Write(this.Height);
				bw.Write((ushort)this.PaletteMode);
				bw.Write((ushort)this.BgMode);
				bw.Write(numInfos);

				foreach (MapInfo info in this.Info) {
					if (this.BgMode == BgMode.Affine)
						bw.Write(info.ToByte());
					else
						bw.Write(info.ToUInt16());
				}
			}
		}
	}
}

