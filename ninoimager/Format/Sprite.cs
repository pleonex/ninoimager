// -----------------------------------------------------------------------
// <copyright file="Sprite.cs" company="none">
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
using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Rgba, System.Byte>;

namespace Ninoimager.Format
{
	public class Sprite
	{
		private Frame[] frames;

		public Sprite()
		{
			this.frames = null;
		}

		public int NumFrames {
			get { return this.frames.Length; }
		}

		public void SetFrames(Frame[] frames)
		{
			this.frames = (Frame[])frames.Clone();
		}

		public Frame[] GetFrames()
		{
			return (Frame[])this.frames.Clone();
		}

		public EmguImage CreateBitmap(int numFrame, Image image, Palette palette)
		{
			return this.frames[numFrame].CreateBitmap(image, palette);
		}
	}
}

