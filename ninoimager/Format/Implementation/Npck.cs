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

		public static Npck ImportBackgroundImage(string image)
		{
			return ImportBackgroundImage(new EmguImage(image));
		}

		public static Npck ImportBackgroundImage(EmguImage image)
		{
			MemoryStream nclrStr = new MemoryStream();
			MemoryStream ncgrStr = new MemoryStream();
			MemoryStream nscrStr = new MemoryStream();

			BackgroundImporter importer = new BackgroundImporter();
			importer.ImportBackground(image, nscrStr, ncgrStr, nclrStr);

			nclrStr.Position = ncgrStr.Position = nscrStr.Position = 0;
			return Npck.FromBackgroundStreams(nscrStr, ncgrStr, nclrStr);
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
			BackgroundImporter importer = new BackgroundImporter();
			importer.SetOriginalSettings(original[2], original[1], original[0]);
			importer.ImportBackground(image, nscrStr, ncgrStr, nclrStr);

			nclrStr.Position = ncgrStr.Position = nscrStr.Position = 0;
			return Npck.FromBackgroundStreams(nscrStr, ncgrStr, nclrStr);
		}

		public static Npck[] ImportBackgroundImageSharePalette(string[] images)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			return ImportBackgroundImageSharePalette(emguImgs, new BackgroundImporter());
		}

		public static Npck[] ImportBackgroundImageSharePalette(string[] images, Npck original)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			BackgroundImporter importer = new BackgroundImporter();
			importer.SetOriginalSettings(original[6], original[1], original[0]);
			return ImportBackgroundImageSharePalette(emguImgs, importer);
		}

		public static Npck[] ImportBackgroundImageSharePalette(EmguImage[] images, BackgroundImporter importer)
		{
			if (!(importer.Quantization is FixedPaletteQuantization)) {
				// Concatenate images
				EmguImage combinedImg = images[0].Clone();
				for (int i = 1; i < images.Length; i++)
					combinedImg = combinedImg.ConcateHorizontal(images[i]);

				NdsQuantization quantization = new NdsQuantization();
				quantization.Quantizate(combinedImg);
				importer.Quantization = new FixedPaletteQuantization(quantization.Palette);

				combinedImg.Dispose();
			}

			// Create packs
			Npck[] packs = new Npck[images.Length];
			for (int i = 0; i < images.Length; i++) {
				MemoryStream nclrStr = new MemoryStream();
				MemoryStream ncgrStr = new MemoryStream();
				MemoryStream nscrStr = new MemoryStream();

				importer.ImportBackground(images[i], nscrStr, ncgrStr, nclrStr);
				nclrStr.Position = ncgrStr.Position = nscrStr.Position = 0;

				// Only first pack file has palette file
				if (i == 0)
					packs[i] = Npck.FromBackgroundStreams(nscrStr, ncgrStr, nclrStr);
				else
					packs[i] = Npck.FromBackgroundStreams(nscrStr, ncgrStr, null);
			}

			return packs;
		}

		public static Npck[] ImportBackgroundImageShareImage(string[] images)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			return ImportBackgroundImageShareImage(emguImgs, new BackgroundImporter());
		}

		public static Npck[] ImportBackgroundImageShareImage(string[] images, Npck original)
		{
			EmguImage[] emguImgs = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++)
				emguImgs[i] = new EmguImage(images[i]);

			BackgroundImporter importer = new BackgroundImporter();
			importer.SetOriginalSettings(original[6], original[1], original[0]);
			return ImportBackgroundImageShareImage(emguImgs, importer);
		}

		public static Npck[] ImportBackgroundImageShareImage(EmguImage[] images, BackgroundImporter importer)
		{
			Npck[] packs = new Npck[images.Length];

			EmguImage combinedImg = images[0].Clone();
			// Concatenate images
			for (int i = 1; i < images.Length; i++)
				combinedImg = combinedImg.ConcateHorizontal(images[i]);

			if (!(importer.Quantization is FixedPaletteQuantization)) {
				// Get quantization to share palette
				NdsQuantization quantization = new NdsQuantization();
				quantization.Quantizate(combinedImg);
				importer.Quantization = new FixedPaletteQuantization(quantization.Palette);
			}

			// Get the palette and image file that it's shared
			MemoryStream nclrStr = new MemoryStream();
			MemoryStream ncgrStr = new MemoryStream();
			importer.ImportBackground(combinedImg, null, ncgrStr, nclrStr);
			nclrStr.Position = ncgrStr.Position = 0;

			// Get the array of pixel from the image file
			Ncgr ncgr = new Ncgr(ncgrStr);
			Pixel[] fullImage = ncgr.GetPixels();
			ncgrStr.Position = 0;

			// Create packs
			for (int i = 0; i < images.Length; i++) {
				MemoryStream nscrStr = new MemoryStream();
				importer.ImportBackgroundShareImage(images[i], fullImage, nscrStr);
				nscrStr.Position = 0;

				// Only first pack file has palette and image files
				if (i == 0)
					packs[i] = Npck.FromBackgroundStreams(nscrStr, ncgrStr, nclrStr);
				else
					packs[i] = Npck.FromBackgroundStreams(nscrStr, null, null);
			}

			combinedImg.Dispose();
			return packs;
		}

        public static Npck ImportSpriteImage(string[] images, int[] frames, Npck original)
		{
			EmguImage[] emguImages = new EmguImage[images.Length];
			for (int i = 0; i < images.Length; i++) {
				emguImages[i] = new EmguImage(images[i]);
			}

            Npck npck = ImportSpriteImage(emguImages, frames, original);

            foreach (EmguImage img in emguImages)
                img.Dispose();

            return npck;
		}

        public static Npck ImportSpriteImage(EmguImage[] images, int[] frames, Npck original)
        {
            SpriteImporter importer = new SpriteImporter();
            MemoryStream nclrStr = new MemoryStream();
            MemoryStream ncgrLinealStr = new MemoryStream();
            MemoryStream ncgrTiledStr = new MemoryStream();
            MemoryStream ncerStr = new MemoryStream();

            // Create sprites images to import
            // those sprite that have not been exported (they didn't have text)
            if (original[0] != null) {
                Nclr nclr = new Nclr(original[0]);
                Ncgr ncgr = new Ncgr(original[1] == null ? original[2] : original[1]);
                Ncer ncer = new Ncer(original[3]);

                // Set old settings
				importer.Quantization = new ManyFixedPaletteQuantization(nclr.GetPalettes());
				importer.OriginalPalettes = nclr.GetPalettes();
                importer.Format = nclr.Format;
                if (nclr.Format == ColorFormat.Indexed_8bpp)
                    importer.PaletteMode = PaletteMode.Palette256_1;
                else
                    importer.PaletteMode = PaletteMode.Palette16_16;

                int idx = 0;
                for (int i = 0; i < ncer.NumFrames; i++) {
                    if (frames.Contains(i))
                        importer.AddFrame(images[idx++]);
                    else if (ncer != null)
                        importer.AddFrame(ncer.CreateBitmap(i, ncgr, nclr), ncer.GetFrame(i));
                }
            } else {
                foreach (EmguImage img in images)
                    importer.AddFrame(img);
            }

            // TEMP: Check if the files were present
            if (original[0] == null)
                Console.Write("(Warning: No palette) ");
            if (original[1] == null) {
                //Console.Write("(Warning: No HImage) ");
                ncgrTiledStr = null;
            }
            if (original[2] == null) {
                //Console.Write("(Warning: No LImage) ");
                ncgrLinealStr = null;
            }
            if (original[3] == null)
                Console.Write("(Warning: No sprite) ");
            if (original[5] == null)
                Console.Write("(Warning: No animation) ");
                
            importer.Generate(nclrStr, ncgrLinealStr, ncgrTiledStr, ncerStr);

            nclrStr.Position = 0;
            ncerStr.Position = 0;
            if (ncgrTiledStr != null)
                ncgrTiledStr.Position = 0;
            if (ncgrLinealStr != null)
                ncgrLinealStr.Position = 0;

            return Npck.FromSpriteStreams(ncerStr, ncgrLinealStr, ncgrTiledStr, nclrStr, original[5]);
		}
	}
}

