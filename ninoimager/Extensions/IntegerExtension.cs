// -----------------------------------------------------------------------
// <copyright file="IntegerExtension.cs" company="none">
// Copyright (C) 2015 
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
// <date>01/04/2015</date>
// -----------------------------------------------------------------------
using System;

namespace Ninoimager
{
	public static class IntegerExtension
	{
		public static int CountBits(this uint value)
		{
			int count;
			for (count = 0; value != 0; count++)
				value >>= 1;
			return count;
		}

		public static uint SetCountBits(this int value)
		{
			uint bits;
			for (bits = 0; value != 0; value--)
				bits = (bits << 1) | 1;
			return bits;
		}
	}
}

