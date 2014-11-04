// -----------------------------------------------------------------------
// <copyright file="NitroBlock.cs" company="none">
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
// <date>30/05/2013</date>
// -----------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;

namespace Ninoimager.Format
{
	public abstract class NitroBlock
	{
		private NitroFile file;

		protected NitroBlock(NitroFile file)
		{
			this.file = file;
		}

		public string Name {
			get { return this.GetType().Name.ToUpper(); }
		}

		public int Size {
			get;
			protected set;
		}

		protected NitroFile File {
			get { return this.file; }
		}

		public void Read(Stream strIn)
		{
			BinaryReader br = new BinaryReader(strIn);

			char[] blockName = br.ReadChars(4);
			if (new string(blockName.Reverse().ToArray()) != this.Name && new string(blockName) != this.Name)
				throw new FormatException("Block name does not match");

			this.Size = br.ReadInt32();
			this.ReadData(strIn);

			br = null;
		}

		protected abstract void ReadData(Stream strIn);

		public void Write(Stream strOut)
		{
			this.UpdateSize();
			//if (this.Size % 4 != 0)
			//	this.Size += 4 - (this.Size % 4);

			BinaryWriter bw = new BinaryWriter(strOut);
			long startPos = strOut.Position;

			if (this.Name[3] != '0')
				bw.Write(this.Name.Reverse().ToArray());
			else
				bw.Write(this.Name.ToCharArray());
			bw.Write(this.Size);

			this.WriteData(strOut);

			strOut.Position = startPos + this.Size;
			if (strOut.Position > strOut.Length)
				strOut.Position = strOut.Length;

			//while (strOut.Position % 0x04 != 0)
			//	strOut.WriteByte(0x00);
		}

		protected abstract void WriteData(Stream strOut);

		protected abstract void UpdateSize();
	}
}

