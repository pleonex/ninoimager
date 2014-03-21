// -----------------------------------------------------------------------
// <copyright file="Obj.cs" company="none">
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
using Rectangle = System.Drawing.Rectangle;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

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
	/// Object (moveable sprite).
	/// <see cref="http://nocash.emubase.de/gbatek.htm#lcdobjoverview"/>
	/// </summary>
	public class Obj
	{
		private ushort id;
		private sbyte coordY;
		private short coordX;
		private PaletteMode paletteMode;
		private ushort tileNumber;
		private byte rotScaGroup;
		private byte objSize;
		private byte objPriority;
		private byte paletteIdx;
		private byte alpha;

		public static Obj FromUshort(ushort attr0, ushort attr1, ushort attr2)
		{
			Obj obj = new Obj();

			// Attribute 0
			obj.CoordY      = (sbyte)      ((attr0 >> 00) & 0x00FF);
			obj.RotSca      =              ((attr0 >> 08) & 0x0001) == 1;
			obj.Mode        = (ObjMode)    ((attr0 >> 10) & 0x0003);
			obj.IsMosaic    =              ((attr0 >> 12) & 0x0001) == 1;
			obj.PaletteMode = (PaletteMode)((attr0 >> 13) & 0x0001);
			obj.Shape       = (ObjShape)   ((attr0 >> 14) & 0x0003);

			// Attribute 1
			obj.CoordX   = (short)(((attr1 >> 00) & 0x001FF) - 256);
			obj.SizeMode = (byte)  ((attr1 >> 14) & 0x0003);

			// Attribute 2
			obj.tileNumber   = (ushort)((attr2 >> 00) & 0x003FF);	// I don't use the property since is the raw value
			obj.ObjPriority  = (byte)  ((attr2 >> 10) & 0x0003);
			obj.PaletteIndex = (byte)  ((attr2 >> 12) & 0x000F);

			// Rotation / Scaling mode
			if (obj.RotSca) {
				obj.DoubleSize  =       ((attr0 >> 09) & 0x0001) == 1;
				obj.RotScaGroup = (byte)((attr1 >> 09) & 0x001F);        
			} else {
				obj.IsDisabled     = ((attr0 >> 09) & 0x0001) == 1;
				obj.HorizontalFlip = ((attr1 >> 12) & 0x0001) == 1;
				obj.VerticalFlip   = ((attr1 >> 13) & 0x0001) == 1;
			}

			return obj;
		}

		/// <summary>
		/// Gets or sets the identifier (OAM Entry Number).
		/// It is used for priority.
		/// </summary>
		/// <value>The identifier.</value>
		public ushort Id {
			get { return this.id; }
			set {
				if (value >= 128)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 128");

				this.id = value;
			}
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
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/>
		/// supports rotation or scaling.
		/// </summary>
		/// <value><c>true</c> if rotation or scaling is used; otherwise, <c>false</c>.</value>
		public bool RotSca {
			get;
			set;
		}

		#region Rotation / Scaling enabled
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/> support double size.
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
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/> is displayed.
		/// </summary>
		/// <remarks>Only if Rotation/Scaling is disabled.</remarks>
		/// <value><c>true</c> if object is disabled; otherwise, <c>false</c>.</value>
		public bool IsDisabled {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/> must perfom a
		/// horizontal flip.
		/// </summary>
		/// <value><c>true</c> if horizontal flip; otherwise, <c>false</c>.</value>
		public bool HorizontalFlip {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/> must perfom a
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
		public ObjMode Mode {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Ninoimager.Format.Obj"/>
		/// mosaic mode is enabled. <see cref="http://nocash.emubase.de/gbatek.htm#lcdiomosaicfunction"/>
		/// </summary>
		/// <value><c>true</c> if object mosaic; otherwise, <c>false</c>.</value>
		public bool IsMosaic {
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
		/// Gets or sets the tile number.
		/// </summary>
		/// <remarks>
		/// It returns the final value multiplied by the factor that depends on the PaletteMode.
		/// It will also convert at the setter the final value to the internal.
		/// The exception will be thrown when the final value is higher than 1024. To see how this value is
		/// calculated check <see cref="http://nocash.emubase.de/gbatek.htm#lcdobjoverview"/>.
		/// </remarks>
		/// <value>The tile number.</value>
		public ushort TileNumber {
			get {
				ushort tileNumber = (ushort)(this.tileNumber * 4);
				if (this.PaletteMode == PaletteMode.Palette256_1)
					tileNumber *= 2;

				return tileNumber;
			}
			set {
				ushort tileNumber = (ushort)(value / 4);
				if (this.PaletteMode == PaletteMode.Palette256_1)
					tileNumber /= 2;

				if (value >= 1024)
					throw new ArgumentOutOfRangeException("Property value", value, "Out of range tile number.");

				this.tileNumber = tileNumber;
			}
		}

		/// <summary>
		/// Gets or sets the Object priority.
		/// </summary>
		/// <remarks>
		/// This is not the final priority. To get that value see <see cref="Ninoimager.Format.Obj.GetPriority"/>.
		/// </remarks>
		/// <value>The obj priority.</value>
		public byte ObjPriority {
			get { return this.objPriority; }
			set {
				if (value >= 4)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 4");

				this.objPriority = value;
			}
		}

		/// <summary>
		/// Gets or sets the index of the palette.
		/// </summary>
		/// <remarks>
		/// Only if Object Mode is not set to Bitmap.
		/// </remarks>
		/// <value>The index of the palette.</value>
		public byte PaletteIndex {
			get { return this.paletteIdx; }
			set {
				if (value >= 16)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 16");

				this.paletteIdx = value;
			}
		}

		/// <summary>
		/// Gets or sets the alpha component of the Object.
		/// </summary>
		/// <remarks>
		/// Only if Object Mode is set to Bitmap.
		/// </remarks>
		/// <value>The alpha component.</value>
		public byte Alpha {
			get { return this.alpha; }
			set {
				if (value >= 16)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 16");

				this.alpha = value;
			}
		}

		/// <summary>
		/// Gets or sets the object shape (first parameter of the Object size).
		/// </summary>
		/// <value>The object shape.</value>
		public ObjShape Shape {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the second parameter of the Object size.
		/// </summary>
		/// <value>The size of the object.</value>
		public byte SizeMode {
			get { return this.objSize; }
			set {
				if (value >= 4)
					throw new ArgumentOutOfRangeException("Property value", value, "Must be less than 4");

				this.objSize = value;
			}
		}

		public Size GetSize()
		{
			int[,] SizeMatrix = new int[3, 4] {
				{  8, 16, 32, 64},	// Square
				{ 16, 32, 32, 64},	// Horizontal
				{  8,  8, 16, 32}	// Vertical
			};

			Size size = new Size();
			if (this.Shape == ObjShape.Square)
				size = new Size(SizeMatrix[0, this.SizeMode], SizeMatrix[0, this.SizeMode]);
			else if (this.Shape == ObjShape.Horizontal)
				size = new Size(SizeMatrix[1, this.SizeMode], SizeMatrix[2, this.SizeMode]);
			else if (this.Shape == ObjShape.Vertical)
				size = new Size(SizeMatrix[2, this.SizeMode], SizeMatrix[1, this.SizeMode]);
			return size;
		}

		public void SetSize(Size size)
		{
			if (size.Width % 8 != 0 || size.Height % 8 != 0)
				throw new ArgumentException("Invalid size.", "size");

			if (size.Width == size.Height) {
				this.Shape = ObjShape.Square;

				if (size.Width == 8)
					this.SizeMode = 0;
				else if (size.Width == 16)
					this.SizeMode = 1;
				else if (size.Width == 32)
					this.SizeMode = 2;
				else if (size.Width == 64)
					this.SizeMode = 3;
				else
					throw new ArgumentException("Invalid size.", "size");

			} else if (size.Width > size.Height) {
				this.Shape = ObjShape.Horizontal;

				if (size.Width == 16 && size.Height == 8)
					this.SizeMode = 0;
				else if (size.Width == 32 && size.Height == 8)
					this.SizeMode = 1;
				else if (size.Width == 32 && size.Height == 16)
					this.SizeMode = 2;
				else if (size.Width == 64 && size.Height == 32)
					this.SizeMode = 3;
				else
					throw new ArgumentException("Invalid size.", "size");

			} else { 
				this.Shape = ObjShape.Vertical;

				if (size.Width == 8 && size.Height == 16)
					this.SizeMode = 0;
				else if (size.Width == 8 && size.Height == 32)
					this.SizeMode = 1;
				else if (size.Width == 16 && size.Height == 32)
					this.SizeMode = 2;
				else if (size.Width == 32 && size.Height == 64)
					this.SizeMode = 3;
				else
					throw new ArgumentException("Invalid size.", "size");

			}
		}

		public Point GetReferencePoint() {
			return new Point(this.CoordX, this.CoordY);
		}

		public Point GetRotationCenter() {
			Size size = this.GetSize();
			return new Point(this.CoordX + size.Width / 2, this.CoordY + size.Height / 2);
		}

		/// <summary>
		/// Gets the priority.
		/// </summary>
		/// <remarks>
		/// UNTESTED. There is no info about how to calculate it.
		/// <see cref="http://nocash.emubase.de/gbatek.htm#dsvideoobjs"/>
		/// </remarks>
		/// <returns>The priority.</returns>
		public ushort GetPriority() {
			return (ushort)((this.objPriority << 7) | this.Id);
		}

		public Rectangle GetArea()
		{
			return new Rectangle(this.GetReferencePoint(), this.GetSize());
		}

		public EmguImage CreateBitmap(Image image, Palette palette)
		{
			Size size = this.GetSize();
			Image objImage = image.CreateSubImage(this.TileNumber, size.Width * size.Height);

			EmguImage bitmap = null;
			if (this.Mode == ObjMode.Bitmap)
				bitmap = objImage.CreateBitmap();	// TODO: Add alpha support
			else
				bitmap = objImage.CreateBitmap(palette, this.PaletteIndex);

			if (!this.RotSca && this.HorizontalFlip)
				bitmap = bitmap.Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL);
			else if (!this.RotSca && this.VerticalFlip)
				bitmap = bitmap.Flip(Emgu.CV.CvEnum.FLIP.VERTICAL);

			return bitmap;
		}
	}
}
