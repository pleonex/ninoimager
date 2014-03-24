// -----------------------------------------------------------------------
// <copyright file="Uext.cs" company="none">
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

namespace Ninoimager.Format
{
	public class Uext : NitroBlock
	{
		public uint Unknown {
			set;
			get;
		}

		public Uext(NitroFile file)
			: base(file)
		{
		}

		protected override void ReadData(Stream strIn)
		{
			BinaryReader br = new BinaryReader(strIn);
			this.Unknown = br.ReadUInt32();
		}

		protected override void WriteData(Stream strOut)
		{
			BinaryWriter bw = new BinaryWriter(strOut);
			bw.Write(this.Unknown);
		}

		protected override void UpdateSize()
		{
		}
	}
}

