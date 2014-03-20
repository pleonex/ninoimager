// -----------------------------------------------------------------------
// <copyright file="Oam.cs" company="none">
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
// <date>03/07/2014</date>
// -----------------------------------------------------------------------
using System;
using Size  = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace Ninoimager.Format
{
	public enum ObjMode : byte {
		Normal = 0,
		SemiTransparent = 1,
		ObjWindow = 2,
		Bitmap = 3
	}

	public enum ObjShape : byte {
		Square = 0,
		Horizontal = 1,
		Vertical = 2
	}

	/// <summary>
	/// Object Attribute Memory.
	/// <see cref="http://nocash.emubase.de/gbatek.htm#lcdobjoverview"/>
	/// </summary>
	public class Oam
	{
		private sbyte coordY;
		private short coordX;
		private PaletteMode paletteMode;
		private byte rotScaGroup;
		private byte objSize;

		public static Oam FromUshort(ushort attr0, ushort attr1, ushort attr2)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets or sets the coordinate y.
		/// </summary>
		/// <value>The coordinate y.</value>
		public sbyte CoordY {
			get { return this.coordY; }
			set { this.coordY = value; }
		}

		/// <summary>
		/// Gets or sets the coordinate x.
		/// </summary>
		/// <value>The coordinate x.</value>
		public short CoordX {
			get { return this.coordX; }
			set {
				if (value < -256 || value >= 256)
					throw new ArgumentOutOfRangeException(
						"property value",
						value,
						"X Coordinate must be between -256 and 255"
					);

				this.coordX = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/>
		/// supports rotation or scaling.
		/// </summary>
		/// <value><c>true</c> if rotation or scaling is used; otherwise, <c>false</c>.</value>
		public bool RotSca {
			get;
			set;
		}

		#region Rotation / Scaling enabled
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/> support double size.
		/// </summary>
		/// <description>
		/// The sprites are displayed inside a rectangular area. When the sprite is rotated or scaled this area
		/// could be smaller than needed and some parts could be not displayed.
		/// Enabling this feature, the rectangular area will be multiplied by 2.
		/// </description>
		/// <remarks>Only if Rotation/Scaling is enabled.</remarks>
		/// <value><c>true</c> to enable double-size; otherwise, <c>false</c>.</value>
		public bool DoubleSize {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the rotation or scaling group number.
		/// There are up to 32 differents groups.
		/// </summary>
		/// <description>
		/// The rotation / scaling groups are located after the OAM value in the RAM. 
		/// <see cref="http://nocash.emubase.de/gbatek.htm#lcdobjoamrotationscalingparameters"/>
		/// 
		/// The transformations are the same as for BG images:
		/// <see cref="http://nocash.emubase.de/gbatek.htm#lcdiobgrotationscaling"/>
		/// 
		/// In general, given a group of 4 parameter: A, B, C and D the transformated point is:
		///   x2 = A*(x1-x0) + B*(y1-y0) + x0
		///   y2 = C*(x1-x0) + D*(y1-y0) + y0
		/// where (x0, y0) is the rotation center, (x1, y1) is the old point and (x2, y2) the new point.
		/// </description>
		/// <value>The rotation or scaling group number.</value>
		public byte RotScaGroup {
			get { return this.rotScaGroup; }
			set {
				if (value >= 32)
					throw new ArgumentOutOfRangeException("property value", value, "Must be less than 32");
				this.rotScaGroup = value;
			}
		}
		#endregion

		#region Rotation / Scaling disabled
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/> is displayed.
		/// </summary>
		/// <remarks>Only if Rotation/Scaling is disabled.</remarks>
		/// <value><c>true</c> if object is disabled; otherwise, <c>false</c>.</value>
		public bool ObjDisable {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/> must perfom a
		/// horizontal flip.
		/// </summary>
		/// <value><c>true</c> if horizontal flip; otherwise, <c>false</c>.</value>
		public bool HorizontalFlip {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/> must perfom a
		/// vertical flip.
		/// </summary>
		/// <value><c>true</c> if vertical flip; otherwise, <c>false</c>.</value>
		public bool VerticalFlip {
			get;
			set;
		}
		#endregion

		/// <summary>
		/// Gets or sets the object mode. <see cref="Ninoimager.Format.ObjMode"/>
		/// </summary>
		/// <value>The object mode.</value>
		public ObjMode ObjMode {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Oam"/>
		/// mosaic mode is enabled. <see cref="http://nocash.emubase.de/gbatek.htm#lcdiomosaicfunction"/>
		/// </summary>
		/// <value><c>true</c> if object mosaic; otherwise, <c>false</c>.</value>
		public bool ObjMosaic {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the palette mode of the sprite.
		/// </summary>
		/// <remarks>Extended mode is not supported.</remarks>
		/// <value>The depth.</value>
		public PaletteMode PaletteMode {
			get { return this.paletteMode; }
			set {
				if (value == PaletteMode.Extended)
					throw new NotSupportedException("Extended mode is not supported.");

				this.paletteMode = value;
			}
		}

		/// <summary>
		/// Gets or sets the object shape (first parameter of the OAM size).
		/// </summary>
		/// <value>The object shape.</value>
		public ObjShape ObjShape {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the second parameter of the OAM size.
		/// </summary>
		/// <value>The size of the object.</value>
		public byte ObjSize {
			get { return this.objSize; }
			set {
				if (value >= 4)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 4");

				this.objSize = value;
			}
		}

		public Size GetSize()
		{
			int[,] sizeMatrix = new int[3, 4] {
				{  8, 16, 32, 64},
				{ 16, 32, 32, 64},
				{  8,  8, 16, 32}
			};

			Size size = new Size();
			if (this.ObjShape == ObjShape.Square)
				size = new Size(sizeMatrix[0, this.ObjSize], sizeMatrix[0, this.ObjSize]);
			else if (this.ObjShape == ObjShape.Horizontal)
				size = new Size(sizeMatrix[1, this.ObjSize], sizeMatrix[2, this.ObjSize]);
			else if (this.ObjShape == ObjShape.Vertical)
				size = new Size(sizeMatrix[2, this.ObjSize], sizeMatrix[1, this.ObjSize]);
			return size;
		}

		public void SetSize(Size size)
		{
			throw new NotImplementedException();
		}

		public Point GetReferencePoint() {
			return new Point(this.CoordX, this.CoordY);
		}

		public Point GetRotationCenter() {
			Size size = this.GetSize();
			return new Point(this.CoordX + size.Width / 2, this.CoordY + size.Height / 2);
		}
	}
}

