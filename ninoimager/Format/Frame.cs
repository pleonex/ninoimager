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
using Point     = System.Drawing.Point;
using Size      = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.Format
{
	public class Frame
	{
		private Obj[] objects;
		private Rectangle visibleArea;

		public Frame()
		{
			this.objects = null;
			this.visibleArea = new Rectangle();
		}

		public Rectangle VisibleArea {
			get { return this.visibleArea; }
			set { this.visibleArea = value; }
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

			foreach (Obj obj in this.objects) {
				EmguImage objBitmap = obj.CreateBitmap(image, palette);

				// Copy the object image to the frame
				Rectangle roi = obj.GetArea();
				roi.Offset(256, 128);	// Only positive coordinate values
				bitmap.ROI = roi;
				objBitmap.CopyTo(bitmap);

				objBitmap.Dispose();
			}

			Rectangle absArea = this.VisibleArea;
			absArea.Offset(512, 128); // Only positive coordinate values
			EmguImage roiBitmap = bitmap.Copy(absArea);
			bitmap.Dispose();
			return roiBitmap;
		}
	}
}

