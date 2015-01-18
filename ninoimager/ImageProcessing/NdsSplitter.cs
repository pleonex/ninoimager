// -----------------------------------------------------------------------
// <copyright file="NdsSplitter.cs" company="none">
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
// <date>04/06/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Ninoimager.Format;
using Point     = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color     = Emgu.CV.Structure.Bgra;
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

namespace Ninoimager.ImageProcessing
{
	public class NdsSplitter : ISplitable
	{
		// First value is the limit and the second is the side.
		// From limit to side there must be non-transparent pixels to set it.
		private static int[][,] Modes = {
			new int[,] { {32, 64}, {16, 32}, {8, 16}, {0, 8} },	// 50%
			new int[,] { {48, 64}, {24, 32}, {8, 16}, {0, 8} }	// 75%
		};

		private int[,] splitMode;

		public NdsSplitter(int splitMode)
		{
			if (splitMode != 0 && splitMode != 1)
				throw new ArgumentOutOfRangeException("Only 0 and 1 available.");

			this.splitMode = NdsSplitter.Modes[splitMode];
		}

		public Frame Split(EmguImage frame)
		{
			// Trim image
			Point startPos = TrimImage(ref frame);

			// Create objects
			List<Obj> objs = new List<Obj>();
			this.CreateObjects(frame, objs, 0, 0, frame.Height);

			// Update coordinates. Sets to absolute values.
			foreach (Obj obj in objs) {
				obj.CoordX = (short)(obj.CoordX + startPos.X);
				obj.CoordY = (sbyte)(obj.CoordY + startPos.Y);
			}

			// Return new frame
			Frame f = new Frame();
			f.SetObjects(objs.ToArray());
			f.VisibleArea = new Rectangle(startPos.X - 256, startPos.Y - 128, frame.Width - 1, frame.Height - 1);
			return f;
		}

		private void CreateObjects(EmguImage frame, List<Obj> objList, int x, int y, int maxHeight)
		{
			// Go to first non-transparent pixel
			int newX = SearchNoTransparentPoint(frame, 1, x, y, yEnd: y + maxHeight);
			int newY = SearchNoTransparentPoint(frame, 0, x, y, yEnd: y + maxHeight);

			if (newY == -1 || newX == -1)
				return;

			int diffX = newX - x;
			diffX -= diffX % 8;
			x = diffX + x;

			int diffY = newY - y;
			diffY -= diffY % 8;
			y = diffY + y;

			int width  = 0;
			int height = 0;
			this.GetObjectSize(frame, x, y, frame.Width, maxHeight, out width, out height);

			if (width != 0 && height != 0) {
				// Create object
				Obj obj = new Obj();
				obj.Id     = (ushort)objList.Count;
				obj.CoordX = (short)(x - 256);
				obj.CoordY = (sbyte)(y - 128);
				obj.SetSize(width, height);
				objList.Add(obj);
			} else {
				// If everything is transparent
				width = this.splitMode[0, 1];  // Max width
				height = this.splitMode[0, 1]; // Max height
			}

			// Go to right
			if (frame.Width - (x + width) > 0)
				this.CreateObjects(frame, objList, x + width, y, height);

			// Go to down
			int newMaxHeight = maxHeight - (height + diffY);
			if (newMaxHeight > 0)
				this.CreateObjects(frame, objList, x, y + height, newMaxHeight);
		}

		private void GetObjectSize(EmguImage frame, int x, int y, int maxWidth, int maxHeight,
									out int width, out int height)
		{
			int minWidthConstraint = 0;
			width  = 0;
			height = 0;

			// Try to get a valid object size
			// The problem is the width can get fixed to 64 and in that case the height can not be 8 or 16.
			while (height == 0 && minWidthConstraint < this.splitMode.GetLength(0)) {
				// Get object width
				width = 0;
				for (int i = minWidthConstraint; i < this.splitMode.GetLength(0) && width == 0; i++) {
					if (this.splitMode[i, 1] > maxWidth - x)
						continue;

					int xRange = this.splitMode[i, 1] - this.splitMode[i, 0];
					if (!IsTransparent(frame, x + this.splitMode[i, 0], xRange, y, maxHeight))
						width = this.splitMode[i, 1];
				}

				// Everything is transparent, skip
				if (width == 0)
					return;

				// Get object height
				height = 0;
				for (int i = 0; i < this.splitMode.GetLength(0) && height == 0; i++) {
					if (this.splitMode[i, 1] > maxHeight)
						continue;

					if (!Obj.IsValidSize(width, this.splitMode[i, 1]))
						continue;

					int yRange = this.splitMode[i, 1] - this.splitMode[i, 0];
					if (!IsTransparent(frame, x, width, y + this.splitMode[i, 0], yRange))
						height = this.splitMode[i, 1];
				}

				minWidthConstraint++;
			}
		}

		private static Point TrimImage(ref EmguImage image)
		{
            // Get border points to get dimensions
			int xStart = SearchNoTransparentPoint(image, 1);
			int yStart = SearchNoTransparentPoint(image, 0);
            int width  = SearchNoTransparentPoint(image, 2) - xStart + 1;
            int height = SearchNoTransparentPoint(image, 3) - yStart + 1;

            // Size must be multiple of 8 due to Obj size
            if (width % 8 != 0)
			    width  += 8 - (width  % 8);
            if (height % 8 != 0)
			    height += 8 - (height % 8);

			if (xStart == -1)
				return new Point(0, 0);

			image = image.Copy(new Rectangle(xStart, yStart, width, height));
			return new Point(xStart, yStart);
		}

		private static int SearchNoTransparentPoint(EmguImage image, int direction,
			int xStart = 0, int yStart = 0, int xEnd = -1, int yEnd = -1)
		{
			if (xEnd == -1)
				xEnd = image.Width;

			if (yEnd == -1)
				yEnd = image.Height;

			int point = -1;
			byte[,,] imageData = image.Data;
			bool stop = false;

				// Get top most
			if (direction == 0) {
				for (int y = yStart; y < yEnd && !stop; y++) {
					for (int x = xStart; x < xEnd && !stop; x++) {
						if (imageData[y, x, 3] == 0)
							continue;

						point = y;
						stop  = true;
					}
				}

				// Get left most
			} else if (direction == 1) {
				for (int x = xStart; x < xEnd && !stop; x++) {
					for (int y = yStart; y < yEnd && !stop; y++) {
						if (imageData[y, x, 3] == 0)
							continue;

						point = x;
						stop  = true;
					}
				}

				// Get right most
			} else if (direction == 2) {
				for (int x = xEnd - 1; x > 0 && !stop; x--) {
					for (int y = yStart; y < yEnd && !stop; y++) {
						if (imageData[y, x, 3] == 0)
							continue;

						point = x;
						stop  = true;
					}
				}

				// Get bottom most
			} else if (direction == 3) {
				for (int y = yEnd - 1; y > 0 && !stop; y--) {
					for (int x = xStart; x < xEnd && !stop; x++) {
						if (imageData[y, x, 3] == 0)
							continue;

						point = y;
						stop  = true;
					}
				}

			} else {
				throw new ArgumentOutOfRangeException("Only 0 to 3 values");
			}

			return point;
		}

		private static bool IsTransparent(EmguImage image, int xStart, int xRange,
			int yStart, int yRange)
		{
			bool isTransparent = true;
			int xEnd = (xStart + xRange > image.Width)  ? image.Width  : xStart + xRange;
			int yEnd = (yStart + yRange > image.Height) ? image.Height : yStart + yRange;

			byte[,,] imageData = image.Data;
			bool stop = false;
			for (int x = xStart; x < xEnd && !stop; x++) {
				for (int y = yStart; y < yEnd && !stop; y++) {
					if (imageData[y, x, 3] != 0) {
						isTransparent = false;
						stop = true;
					}
				}
			}

			return isTransparent;
		}
	}
}

