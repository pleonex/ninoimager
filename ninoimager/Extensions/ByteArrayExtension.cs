// -----------------------------------------------------------------------
// <copyright file="ByteArrayExtension.cs" company="none">
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

namespace Ninoimager
{
	internal static class ByteArrayExtension
	{
		public static uint GetBits(this byte[] data, ref int bitPos, int size)
		{
			if (size < 0 || size > 32)
				throw new ArgumentOutOfRangeException("Size is too big");

			if (bitPos + size > data.Length * 8)
				throw new IndexOutOfRangeException();

			uint value = 0;
			for (int s = 0; s < size; s++, bitPos++) {
				uint bit = data[bitPos / 8];
				bit >>= (bitPos % 8);
				bit &= 1;

				value |= bit << s;
			}

			return value;
		}

		public static void SetBits(this byte[] data, ref int bitPos, int size, uint value)
		{
			if (size < 0 || size > 32)
				throw new ArgumentOutOfRangeException("Size is too big");

			if (bitPos + size > data.Length * 8)
				throw new IndexOutOfRangeException();

			for (int s = 0; s < size; s++, bitPos++) {
				uint bit = (value >> s) & 1;

				uint dByte = data[bitPos / 8];
				dByte |= bit << (bitPos % 8);
				data[bitPos / 8] = (byte)dByte;
			}
		}
	}
}

