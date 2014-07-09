// -----------------------------------------------------------------------
// <copyright file="PaletteDistance.cs" company="none">
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
// <date>28/06/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Linq;
using LabColor = Emgu.CV.Structure.Lab;
using Color    = Emgu.CV.Structure.Bgra;

namespace Ninoimager
{
	public static class PaletteDistance
	{
		public static double CalculateDistance(LabColor[] palette1, LabColor[] palette2)
		{
			double totalDistance = 0;

			for (int i = 0; i < palette1.Length; i++) {
				double minColorDistance = -1;
				for (int j = 0; j < palette2.Length; j++) {
					double distance = palette1[i].GetDistanceSquared(palette2[j]);
					if (minColorDistance == -1 || distance < minColorDistance)
						minColorDistance = distance;
				}

				totalDistance += minColorDistance;
			}

			return totalDistance;
		}

		public static double CalculateDistance(Color[] palette1, Color[] palette2)
		{
			double totalDistance = 0;

			for (int i = 0; i < palette1.Length; i++) {
				double minColorDistance = -1;
				for (int j = 0; j < palette2.Length; j++) {
					double distance = palette1[i].GetDistanceSquared(palette2[j]);
					if (minColorDistance == -1 || distance < minColorDistance)
						minColorDistance = distance;
				}

				totalDistance += minColorDistance;
			}

			return totalDistance;
		}

		public static int CalculateDifferentsColors(Color[] palette1, Color[] palette2)
		{
			int count = 0;
			foreach (Color c in palette1)
				if (!palette2.Contains(c))
					count++;

			return count;
		}
	}
}

