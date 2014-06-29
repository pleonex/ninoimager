// -----------------------------------------------------------------------
// <copyright file="Labl.cs" company="none">
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ninoimager.Format
{
	public class Labl : NitroBlock
	{
		public string[] Names {
			set;
			get;
		}

		public Labl(NitroFile file)
			: base(file)
		{
		}

		protected override void ReadData(Stream strIn)
		{
			BinaryReader br = new BinaryReader(strIn);

			// Get name offsets
			List<uint> offsets = new List<uint>();
			bool endOffsets = false;
			do {
				uint offset = br.ReadUInt32();

				if (offset >= this.Size) {
					br.BaseStream.Position -= 4;
					endOffsets = true;
				} else {
					offsets.Add(offset);
				}
			} while (!endOffsets);

			// Read names
			long namesPos = br.BaseStream.Position;
			this.Names = new string[offsets.Count];
			for (int i = 0; i < offsets.Count; i++) {
				br.BaseStream.Position = namesPos + offsets[i];

				// Read chars until reach null char \0
				StringBuilder name = new StringBuilder();
				bool endName = false;
				do {
					byte ch = br.ReadByte();
					if (ch == 0)
						endName = true;
					else
						name.Append(ch);
				} while (!endName);

				this.Names[i] = name.ToString();
			}

		}
	
		protected override void WriteData(Stream strOut)
		{
			BinaryWriter bw = new BinaryWriter(strOut);

			byte[] data = new byte[this.Size - 4 * this.Names.Length];
			uint offset = 0;
			for (int i = 0; i < this.Names.Length; i++) {
				bw.Write(offset);

				byte[] dataName = Encoding.GetEncoding("shift_jis").GetBytes(this.Names[i] + '\0');
				Array.Copy(dataName, 0, data, offset, dataName.Length);
				offset += (uint)dataName.Length;
			}

			bw.Write(data);
		}

		protected override void UpdateSize()
		{
			this.Size = 8 + 4 * this.Name.Length;
			foreach (String n in this.Names)
				this.Size += System.Text.Encoding.GetEncoding("shift_jis").GetByteCount(n) + 1;
		}
	}
}

