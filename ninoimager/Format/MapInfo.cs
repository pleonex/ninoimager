// -----------------------------------------------------------------------
// <copyright file="MapInfo.cs" company="none">
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
	public struct MapInfo
	{
		public MapInfo(int tileIndex, int paletteIndex, bool flipX, bool flipY)
		: this()
		{
			this.TileIndex = tileIndex;
			this.PaletteIndex = paletteIndex;
			this.FlipX = flipX;
			this.FlipY = flipY;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ninoimager.Format.MapInfo"/> struct.
		/// Text and extended mode.
		/// </summary>
		/// <param name="value">Value.</param>
		public MapInfo(ushort value)
		: this()
		{
			this.TileIndex    = (value >> 00) & 0x3FF;
			this.PaletteIndex = (value >> 12) & 0x0F;
			this.FlipX        = ((value >> 10) & 0x01) == 1;
			this.FlipY        = ((value >> 11) & 0x01) == 1;

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ninoimager.Format.MapInfo"/> struct.
		/// Affine (rotation / scaling) mode.
		/// </summary>
		/// <param name="value">Value.</param>
		public MapInfo(byte value)
		: this()
		{
			this.TileIndex    = value;
			this.PaletteIndex = 0;
			this.FlipX        = false;
			this.FlipY        = false;
		}

		public int TileIndex {
			get;
			private set;
		}

		public int PaletteIndex {
			get;
			private set;
		}

		public bool FlipX {
			get;
			private set;
		}

		public bool FlipY {
			get;
			private set;
		}

		public byte ToByte()
		{
			return (byte)this.TileIndex;
		}

		public ushort ToUInt16()
		{
			return (ushort)(
				(this.TileIndex << 00) |
				(this.PaletteIndex << 12) |
				((this.FlipX ? 1 : 0) << 10) |
				((this.FlipY ? 1 : 0) << 11));
		}
	}}

