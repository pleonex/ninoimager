// -----------------------------------------------------------------------
// <copyright file="ExhaustiveSearch.cs" company="none">
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
using LabColor  = Emgu.CV.Structure.Lab;

namespace Ninoimager.ImageProcessing
{
	public class ExhaustivePaletteSearch : NearestNeighbour<LabColor>
	{
		public override void Initialize(LabColor[] vertex)
		{
			this.vertex = vertex;
		}

		/// <summary>
		/// Get the palette index of the nearest color by using exhaustive search.
		/// </summary>
		/// <returns>The nearest color palette index.</returns>
		/// <param name="color">Color to get its nearest palette color.</param>
		public override int Search(LabColor color)
		{
			// Set the largest distance and a null index
			double minDistance = (255 * 255) + (255 * 255) + (255 * 255) + 1;
			int nearestColor = -1;

			// FUTURE: Implement "Approximate Nearest Neighbors in Non-Euclidean Spaces" algorithm or
			// k-d tree if it's computing CIE76 color difference
			for (int i = 0; i < this.vertex.Length && minDistance > 0; i++) {
				// Since we only want the value to compare, it is faster to not computer the squared root
				double distance = color.GetDistanceSquared(this.vertex[i]);
				if (distance < minDistance) {
					minDistance = distance;
					nearestColor = i;
				}
			}

			return nearestColor;
		}
	}
}

