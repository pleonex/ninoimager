// -----------------------------------------------------------------------
// <copyright file="Ncer.cs" company="none">
// Copyright (C) 2014 
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
// <date>03/21/2014</date>
// -----------------------------------------------------------------------
using System;
using System.IO;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Ninoimager.Format
{
	public class Ncer : Sprite
	{
		private static Type[] BlockTypes = { typeof(Cebk) };
		private NitroFile nitro;
		private Cebk cebk;

		public Ncer()
		{
			this.nitro = new NitroFile("NCER", "1.0", BlockTypes);
			this.cebk = new Cebk(this.nitro);
			this.nitro.Blocks.Add(this.cebk);
		}

		public Ncer(string file)
		{
			this.nitro = new NitroFile(file, BlockTypes);
			this.GetInfo();
		}

		public Ncer(Stream str)
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
			this.cebk = this.nitro.GetBlock<Cebk>(0);
			this.SetFrames(this.cebk.Frames);
		}

		private void SetInfo()
		{
			this.cebk.Frames = this.GetFrames();
		}

		// CElls and BanKs
		private class Cebk : NitroBlock
		{
			public Cebk(NitroFile nitro)
				: base(nitro)
			{
			}

			public Frame[] Frames {
				get;
				set;
			}

			public ushort TypeFrame {
				get;
				set;
			}

			public uint TileSize {
				get;
				set;
			}

			protected override void ReadData(Stream strIn)
			{
				BinaryReader br = new BinaryReader(strIn);
				long blockStart = strIn.Position;

				ushort numFrames  = br.ReadUInt16();
				this.TypeFrame    = br.ReadUInt16();
				uint frameOffset  = br.ReadUInt32();
				this.TileSize     = br.ReadUInt32();
				int frameInfoSize = ((this.TypeFrame & 1) != 0) ? 0x10 : 0x08;

				br.ReadUInt32();	// Offset to unknown block
				br.ReadUInt32();	// Unknown
				br.ReadUInt32();	// Unknown offset

				this.Frames = new Frame[numFrames];
				for (int i = 0; i < numFrames; i++) {
					strIn.Position = blockStart + frameOffset + i * frameInfoSize;
					Frame frame = new Frame();

					ushort numObjs  = br.ReadUInt16();
					ushort areaInfo = br.ReadUInt16();
					uint objOffset  = br.ReadUInt32();

					// Get area info
					bool squareSizeFlag = ((areaInfo >> 11) & 1) == 0;
					Rectangle frameArea = new Rectangle();
					if ((this.TypeFrame & 1) != 0 && !squareSizeFlag) {
						ushort xend = br.ReadUInt16();
						ushort yend = br.ReadUInt16();
						ushort xstart = br.ReadUInt16();
						ushort ystart = br.ReadUInt16();

						frameArea.Location = new Point(xstart, ystart);
						frameArea.Width    = xend - xstart;
						frameArea.Height   = yend - ystart;
					} else if (!squareSizeFlag) {
						throw new FormatException("Obj area info is square but areaInfo is set");
					} else {
						int squareSize = (areaInfo & 0x3F) << 2;
						squareSize *= 2;

						frameArea.Width  = squareSize;
						frameArea.Height = squareSize;
					}

					// Read Objs
					strIn.Position = blockStart + frameOffset + numFrames * frameInfoSize + objOffset;
					Obj[] objs = new Obj[numObjs];
					for (int j = 0; j < numObjs; j++)
						objs[j] = Obj.FromUshort(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16());

					// TODO: Detect frame position for square size

					frame.SetObjects(objs);
					frame.VisibleArea = frameArea;
					this.Frames[i] = frame;
				}
			}

			protected override void WriteData(Stream strOut)
			{
				throw new NotImplementedException();
			}

			protected override void UpdateSize()
			{
				throw new NotImplementedException();
			}
		}
	}
}

