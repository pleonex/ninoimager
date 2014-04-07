// -----------------------------------------------------------------------
// <copyright file="Frame.cs" company="none">
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
using Point     = System.Drawing.Point;
using Size      = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;
using Color     = Emgu.CV.Structure.Rgba;

namespace Ninoimager.Format
{
	public class Frame
	{
		private Obj[] objects;
		private Rectangle visibleArea;
		private int tileSize;

		public Frame()
		{
			this.tileSize    = 128;
			this.objects     = null;
			this.visibleArea = new Rectangle();
		}

		public int TileSize {
			get { return this.tileSize; }
			set { this.tileSize = value; }
		}

		public Rectangle VisibleArea {
			get { return this.visibleArea; }
			set { this.visibleArea = value; }
		}

		public int NumObjects {
			get { return this.objects.Length; }
		}

		public void SetObjects(Obj[] objs)
		{
			this.objects = (Obj[])objs.Clone();
		}

		public Obj[] GetObjects()
		{
			return (Obj[])this.objects.Clone();
		}

		public EmguImage CreateBitmap(Image image, Palette palette)
		{
			EmguImage bitmap = new EmguImage(512, 256);
			List<Obj> sortedObjs = new List<Obj>(this.objects);
			sortedObjs.Sort((a, b) => b.GetPriority().CompareTo(a.GetPriority()));

			foreach (Obj obj in sortedObjs) {
				EmguImage objBitmap = obj.CreateBitmap(image, palette, this.tileSize);

				// Get first palette color
				// !! DUE TO BUG IN EMGU.CV RED AND BLUE COMPONENTS ARE SWAPED IN RGBA FORMAT
				Color transparent = palette.GetColor(obj.PaletteIndex, 0);
				double temp = transparent.Red;
				transparent.Red  = transparent.Blue;
				transparent.Blue = temp;

				// Set first palette color as transparent
				var mask = objBitmap.InRange(transparent, transparent);
				objBitmap.SetValue(0, mask);

				// Copy the object image to the frame
				Point position = obj.GetReferencePoint();
				position.Offset(256, 128);	// Only positive coordinate values
				bitmap.Overlay(position.X, position.Y, objBitmap);

				objBitmap.Dispose();
			}

			//Rectangle absArea = this.VisibleArea;
			//absArea.Offset(512, 128); // Only positive coordinate values
			//EmguImage roiBitmap = bitmap.Copy(absArea);
			//bitmap.Dispose();
			return bitmap;
		}
	}
}

