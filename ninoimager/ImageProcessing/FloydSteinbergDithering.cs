// -----------------------------------------------------------------------
// <copyright file="FloydSteinbergDithering.cs" company="none">
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
// <date>02/22/2014</date>
// -----------------------------------------------------------------------
using System;

namespace Ninoimager.ImageProcessing
{
	public class FloydSteinbergDithering : Dithering<byte>
	{
		public override void ApplyDithering(byte[,,] data, int x, int y, int channel, double error)
		{
			// These values are not exactly width and height but for this task are ok
			int width  = data.GetLength(1);
			int height = data.GetLength(0);

			if (x + 1 < width)
				data[y    , x + 1, channel] = (byte)(data[y    , x + 1, channel] + 7.0 / 16.0 * error);
			if (x - 1 > 0 && y + 1 < height)
				data[y + 1, x - 1, channel] = (byte)(data[y + 1, x - 1, channel] + 3.0 / 16.0 * error);
			if (y + 1 < height)
				data[y + 1, x    , channel] = (byte)(data[y + 1, x    , channel] + 5.0 / 16.0 * error);
			if (x + 1 < width && y + 1 < height)
				data[y + 1, x + 1, channel] = (byte)(data[y + 1, x + 1, channel] + 1.0 / 16.0 * error);
		}
	}
}

