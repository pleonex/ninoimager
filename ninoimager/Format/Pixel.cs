// -----------------------------------------------------------------------
// <copyright file="Pixel.cs" company="none">
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

namespace Ninoimager.Format
{
	public struct Pixel
	{
		public Pixel(uint info, uint alpha, bool isIndexed) : this()
		{
			this.IsIndexed = isIndexed;
			this.Info = info;
			this.Alpha = (byte)alpha;
		}

		public bool IsIndexed {
			get;
			private set;
		}

		/// <summary>
		/// Gets the pixel info.
		/// If it's indexed it returns the color index otherwise, it returns a 32bit BGR value.
		/// </summary>
		/// <value>The pixel info.</value>
		public uint Info {
			get;
			private set;
		}

		public byte Alpha {
			get;
			private set;
		}

		public Pixel ChangeInfo(uint info)
		{
			return new Pixel(info, this.Alpha, this.IsIndexed);
		}
	}
}

