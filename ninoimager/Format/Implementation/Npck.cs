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
using System.Linq;
using Ninoimager.ImageProcessing;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.Format
{
	public class Npck : Pack
	{
		private const string Header = "NPCK";

		public Npck(string fileIn)
			: base(fileIn)
		{
		}

		public Npck(Stream nscrStr, Stream ncgrStr, Stream nclrStr)
		{
			this.AddSubfile(nclrStr);
			this.AddSubfile(ncgrStr);
			this.AddSubfile(null);		// NCBR (NCGR lineal)
			this.AddSubfile(null);		// NCER
			this.AddSubfile(null);		// Unknown
			this.AddSubfile(null);		// NANR
			this.AddSubfile(nscrStr);
			this.AddSubfile(null);		// Unknown
			this.AddSubfile(null);		// Unknown
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
					this.AddSubfile(subfile);

					strIn.Position = fileOffset + subfileOffset;
					for (int p = 0; p < subfileSize; p++)
						subfile.WriteByte(br.ReadByte());
					subfile.Position = 0;
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

		public static Npck ImportBackgroundImage(string image)
		{
			return ImportBackgroundImage(new EmguImage(image));
		}

		public static Npck ImportBackgroundImage(EmguImage image)
		{
			MemoryStream nclrStr = new MemoryStream();
			MemoryStream ncgrStr = new MemoryStream();
			MemoryStream nscrStr = new MemoryStream();

			Importer importer = new Importer();
			importer.ImportBackground(image, nscrStr, ncgrStr, nclrStr);

			nclrStr.Position = ncgrStr.Position = nscrStr.Position = 0;
			return new Npck(nscrStr, ncgrStr, nclrStr);
		}

		public static Npck ImportBackgroundImage(string image, Npck original)
		{
			return ImportBackgroundImage(new EmguImage(image), original);
		}

		public static Npck ImportBackgroundImage(EmguImage image, Npck original)
		{
			if (original[0] == null || original[1] == null)
				throw new FormatException(
					"Can not import image.\n" + 
					"There is not palette or image in the original pack."
				);

			MemoryStream nclrStr = new MemoryStream();
			MemoryStream ncgrStr = new MemoryStream();
			MemoryStream nscrStr = new MemoryStream();

			// Import image
			Importer importer = new Importer();
			importer.SetOriginalSettings(original[2], original[1], original[0]);
			importer.ImportBackground(image, nscrStr, ncgrStr, nclrStr);

			nclrStr.Position = ncgrStr.Position = nscrStr.Position = 0;
			return new Npck(nscrStr, ncgrStr, nclrStr);
		}

		public static Npck[] ImportBackgroundImageSharePalette(string[] images)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			return ImportBackgroundImageSharePalette(emguImgs);
		}

		public static Npck[] ImportBackgroundImageSharePalette(EmguImage[] images)
		{
			throw new NotImplementedException();
		}

		public static Npck[] ImportBackgroundImageShareImage(string[] images)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			return ImportBackgroundImageShareImage(emguImgs);
		}

		public static Npck[] ImportBackgroundImageShareImage(EmguImage[] images)
		{
			throw new NotImplementedException();
		}
	}
}

