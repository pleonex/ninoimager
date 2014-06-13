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

//#define VERBOSE
using System;
using System.IO;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Ninoimager.Format
{
	public class Ncer : Sprite
	{
		private static Type[] BlockTypes = { typeof(Cebk), typeof(Labl), typeof(Uext) };
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

		public int TileSize {
			get;
			set;
		}

		public bool IsRectangularArea {
			get { return this.cebk.TypeFrame == 1; }
			set { this.cebk.TypeFrame = Convert.ToUInt16(value); }
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
			this.cebk     = this.nitro.GetBlock<Cebk>(0);
			this.TileSize = this.cebk.TileSize;
			this.SetFrames(this.cebk.Frames);
		}

		private void SetInfo()
		{
			this.cebk.Frames   = this.GetFrames();
			this.cebk.TileSize = this.TileSize;
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

			public int TileSize {
				get;
				set;
			}

			public uint UnknownOffset1 {
				get;
				set;
			}

			public uint Unknown {
				get;
				set;
			}

			public uint UnknownOffset2 {
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
				this.TileSize     = 1 << (5 + (int)(br.ReadUInt32() & 0xFF));
				int frameInfoSize = ((this.TypeFrame & 1) != 0) ? 0x10 : 0x08;

				this.UnknownOffset1 = br.ReadUInt32();	// Offset to unknown block
				this.Unknown        = br.ReadUInt32();	// Unknown
				this.UnknownOffset2 = br.ReadUInt32();	// Unknown offset

#if VERBOSE
				if (this.UnknownOffset1 != 0)
					Console.WriteLine("\t* UnknownOffset1 -> {0:x8}", this.UnknownOffset1);
				if (this.Unknown != 0)
					Console.WriteLine("\t* Unknown -> {0:x8}", this.Unknown);
				if (this.UnknownOffset2 != 0)
					Console.WriteLine("\t* UnknownOffset2 -> {0:X8}", this.UnknownOffset2);
#endif

				this.Frames = new Frame[numFrames];
				for (int i = 0; i < numFrames; i++) {
					strIn.Position = blockStart + frameOffset + i * frameInfoSize;
					Frame frame    = new Frame();
					frame.TileSize = this.TileSize;

					ushort numObjs  = br.ReadUInt16();
					ushort areaInfo = br.ReadUInt16();
					uint objOffset  = br.ReadUInt32();

					// Get area info
					bool squareSizeFlag = ((areaInfo >> 11) & 1) == 0;
					Rectangle frameArea = new Rectangle();
					if ((this.TypeFrame & 1) != 0 && !squareSizeFlag) {
						short xend = br.ReadInt16();
						short yend = br.ReadInt16();
						short xstart = br.ReadInt16();
						short ystart = br.ReadInt16();

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
					for (int j = 0; j < numObjs; j++) {
						objs[j] = Obj.FromUshort(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16());
						objs[j].Id = (ushort)j;
					}

					// TODO: Detect frame position for square size

					frame.SetObjects(objs);
					frame.VisibleArea = frameArea;
					this.Frames[i] = frame;
				}
			}

			protected override void WriteData(Stream strOut)
			{
				BinaryWriter bw = new BinaryWriter(strOut);

				// Write header
				bw.Write((ushort)this.Frames.Length);
				bw.Write((ushort)this.TypeFrame);
				bw.Write((uint)0x18);	// Frame offset, after header, it's constante
				bw.Write((uint)Math.Log(this.TileSize, 2) - 5);
				bw.Write(this.UnknownOffset1);
				bw.Write(this.Unknown);
				bw.Write(this.UnknownOffset2);

				// Write frame info
				uint objOffset = 0;
				foreach (Frame frame in this.Frames) {
					// Get square sides
					int squareSide = frame.VisibleArea.Width / 2;
					squareSide >>= 4;

					bw.Write((ushort)frame.NumObjects);
					bw.Write((ushort)(squareSide | (this.TypeFrame << 11)));
					bw.Write(objOffset);
					objOffset += (uint)(0x06 * frame.NumObjects);

					if (this.TypeFrame == 1) {
						bw.Write((short)(frame.VisibleArea.X + frame.VisibleArea.Width));	// XEnd
						bw.Write((short)(frame.VisibleArea.Y + frame.VisibleArea.Height));	// YEnd
						bw.Write((short)frame.VisibleArea.X);	// XStart
						bw.Write((short)frame.VisibleArea.Y);	// YStart
					}
				}

				// Write object info
				foreach (Frame frame in this.Frames) {
					foreach (Obj obj in frame.GetObjects()) {
						ushort[] values = obj.ToUshort();
						bw.Write(values[0]);
						bw.Write(values[1]);
						bw.Write(values[2]);
					}
				}
			}

			protected override void UpdateSize()
			{
				this.Size = 8;		// Nitro header
				this.Size += 0x18;	// Block header

				int frameInfoSize = ((this.TypeFrame & 1) != 0) ? 0x10 : 0x08;
				this.Size += frameInfoSize * this.Frames.Length;

				foreach (Frame f in this.Frames)
					this.Size += f.NumObjects * 0x06;
			}
		}
	}
}

