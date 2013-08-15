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
using System;
using System.IO;

namespace Ninoimager.Format
{
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

			public uint Unknown {
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

				this.Width      = br.ReadUInt16();
				this.Height     = br.ReadUInt16();
				this.Unknown    = br.ReadUInt32();
				uint dataLength = br.ReadUInt32();

				this.Info = new MapInfo[dataLength / 2];
				for (int i = 0; i < this.Info.Length; i++) {
					ushort value = br.ReadUInt16();
					this.Info[i] = new MapInfo(
						(value >> 00) & 0x3FF,
						(value >> 12) & 0x0F,
						((value >> 11) & 0x01) == 1,
						((value >> 10) & 0x01) == 1);
				}
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);

				bw.Write(this.Width);
				bw.Write(this.Height);
				bw.Write(this.Unknown);
				bw.Write((uint)(this.Info.Length * 2));

				foreach (MapInfo info in this.Info) {
					ushort value = 
						(ushort)((info.TileIndex << 00) |
						(info.PaletteIndex << 12) |
						((info.FlipX ? 1 : 0) << 11) |
						((info.FlipY ? 1 : 0) << 10));
					bw.Write(value);
				}
			}
		}
	}
}

