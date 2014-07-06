// -----------------------------------------------------------------------
// <copyright file="N2d.cs" company="none">
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
// <date>20/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.IO;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.Format
{
	public class Npck : Pack
	{
		private const string Header = "NPCK";

		public Npck()
		{
		}

		public Npck(string fileIn)
			: base(fileIn)
		{
		}

        public static Npck FromSpriteStreams(Stream ncerStr, Stream ncgrLinealStr,
            Stream ncgrTiledStr, Stream nclrStr, Stream nanrStr)
		{
			Npck npck = new Npck();
			npck.AddSubfile(nclrStr);
            npck.AddSubfile(ncgrTiledStr);
            npck.AddSubfile(ncgrLinealStr);
			npck.AddSubfile(ncerStr);
			npck.AddSubfile(null);		// Unknown
            npck.AddSubfile(nanrStr);
			npck.AddSubfile(null);		// NSCR
			npck.AddSubfile(null);		// Unknown
			npck.AddSubfile(null);		// Unknown
			return npck;
		}

		public static Npck FromBackgroundStreams(Stream nscrStr, Stream ncgrStr, Stream nclrStr)
		{
			Npck npck = new Npck();
			npck.AddSubfile(nclrStr);
			npck.AddSubfile(ncgrStr);
			npck.AddSubfile(null);		// NCBR (NCGR lineal)
			npck.AddSubfile(null);		// NCER
			npck.AddSubfile(null);		// Unknown
			npck.AddSubfile(null);		// NANR
			npck.AddSubfile(nscrStr);
			npck.AddSubfile(null);		// Unknown
			npck.AddSubfile(null);		// Unknown
			return npck;
		}

		protected override void Read(Stream strIn)
		{
			BinaryReader br = new BinaryReader(strIn);
			long fileOffset = strIn.Position;

			if (!Header.Equals(new string(br.ReadChars(4))))
				throw new FormatException("Invalid header");

			br.ReadUInt32();	// data offset
			uint numSubfiles = br.ReadUInt32();

			for (int i = 0; i < numSubfiles; i++) {
				strIn.Position = fileOffset + 0x0C + i * 0x08;

				uint subfileOffset = br.ReadUInt32();
				uint subfileSize   = br.ReadUInt32();

				if (subfileOffset == 0x00) {
					this.AddSubfile(null);
				} else {
					MemoryStream subfile = new MemoryStream();

					strIn.Position = fileOffset + subfileOffset;
					for (int p = 0; p < subfileSize; p++)
						subfile.WriteByte(br.ReadByte());
					subfile.Position = 0;

                    // Check compression
                    int compr = subfile.ReadByte();
                    subfile.Position = 0;
                    if (compr == 0x11) {
                        MemoryStream decodedSubFile = new MemoryStream();
                        Lzx.Decode(subfile, (int)subfileSize, decodedSubFile);
                        subfile.Close();
                        subfile = decodedSubFile;
                    }

                    this.AddSubfile(subfile);
				}
			}
		}

		public override void Write(Stream strOut)
		{
			BinaryWriter bw = new BinaryWriter(strOut);
			long fileOffset = strOut.Position;
			uint dataOffset = 0x0C + (uint)this.NumSubfiles * 0x08;
			                        
			bw.Write(Header.ToCharArray());
			bw.Write(dataOffset);
			bw.Write(this.NumSubfiles);

			uint subfileOffset = dataOffset;
			for (int i = 0; i < this.NumSubfiles; i++) {
				Stream subfile = this[i];

				// Write file allocation data
				strOut.Position = fileOffset + 0x0C + i * 0x08;
				if (subfile == null) {
					bw.Write((uint)0x00);	// Null offset
					bw.Write((uint)0x00);	// Null size
					continue;
				}

				bw.Write(subfileOffset);
				bw.Write((uint)subfile.Length);

				// Write subfile
				strOut.Position = fileOffset + subfileOffset;
				subfile.CopyTo(strOut);

				// Update offset
				subfileOffset += (uint)subfile.Length;

				bw.Flush();
			}
		}

		public EmguImage GetBackgroundImage()
		{
			if (this.NumSubfiles != 9 || this[0] == null || this[1] == null || this[6] == null)
				throw new FormatException("The pack does not contain a background image.");

			Nclr nclr = new Nclr(this[0]);
			Ncgr ncgr = new Ncgr(this[1]);
			Nscr nscr = new Nscr(this[6]);

			return nscr.CreateBitmap(ncgr, nclr);
		}

		public EmguImage[] GetSpriteImage()
		{
			if (this.NumSubfiles != 9 || this[0] == null || this[1] == null || this[6] == null)
				throw new FormatException("The pack does not contain a background image.");

			Nclr nclr = new Nclr(this[0]);
			Ncgr ncgr = new Ncgr(this[1]);
			Ncer ncer = new Ncer(this[3]);

			EmguImage[] images = new EmguImage[ncer.NumFrames];
			for (int i = 0; i < ncer.NumFrames; i++)
				images[i] = ncer.CreateBitmap(i, ncgr, nclr);
			return images;
		}
	}
}

