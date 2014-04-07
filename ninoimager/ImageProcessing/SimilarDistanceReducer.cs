// -----------------------------------------------------------------------
// <copyright file="BasicPaletteReducer.cs" company="none">
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
using LabColor = Emgu.CV.Structure.Lab;
using Color    = Emgu.CV.Structure.Rgba;

namespace Ninoimager.ImageProcessing
{
	public class SimilarDistanceReducer : PaletteReducer
	{
		public override void Reduce(int number)
		{
			// Easy case :D
			if (this.Palettes.Count <= number) {
				this.ReducedPalettes = this.Palettes.ToArray();
				int[] rangeArray = new int[this.Palettes.Count];
				for (int i = 0; i < rangeArray.Length; i++)
					rangeArray[i] = -1;
				this.PaletteApproximation = rangeArray;
				return;
			}

			// Get difference between palettes and sort to get similar palettes first
			List<Difference> distances = new List<Difference>(this.CalculateDistances());
			distances.Sort((a, b) => b.Distance.CompareTo(a.Distance));

			int[] paletteApprox = new int[this.Palettes.Count];
			for (int i = 0; i < paletteApprox.Length; i++)
				paletteApprox[i] = -2;

			// Start removing similar palettes.
			List<Color[]> palettes = new List<Color[]>();
			while (palettes.Count < number && distances.Count > 0) {
				Difference diff = distances[0];

				if (paletteApprox[diff.Palette1] == -2 && paletteApprox[diff.Palette2] == -2) {
					palettes.Add(this.Palettes[diff.Palette1]);
					paletteApprox[diff.Palette1] = -1;
					paletteApprox[diff.Palette2] = palettes.Count - 1;
				} else {
					this.UpdateApproximation(palettes, paletteApprox, diff.Palette1, diff.Palette2);
				}

				distances.RemoveAt(0);
			}

			// Since we are going to the same but starting from the end -> reverse the list
			distances.Reverse();

			// Continue approximating
			while (distances.Count > 0) {
				for (int i = distances.Count - 1; i > 0; i--) {
					Difference diff = distances[i];
					if (this.UpdateApproximation(palettes, paletteApprox, diff.Palette1, diff.Palette2))
						distances.RemoveAt(i);
				}
			}

			this.PaletteApproximation = paletteApprox;
			this.ReducedPalettes      = palettes.ToArray();
		}

		private bool UpdateApproximation(List<Color[]> palettes, int[] paletteApprox, int pal1, int pal2)
		{
			if (paletteApprox[pal1] == -2 && paletteApprox[pal2] == -2)
				return false;

			// If Pal1 has not been used and Pal2 is approximate... Copy approximation
			if (paletteApprox[pal1] == -2 && paletteApprox[pal2] != -1)
				paletteApprox[pal1] = paletteApprox[pal2];
			// If Pal1 has not been used and Pal2 is copied... Set its index
			else if (paletteApprox[pal1] == -2 && paletteApprox[pal2] == -1)
				paletteApprox[pal1] = palettes.IndexOf(this.Palettes[pal2]);
			// If Pal2 has not been used and Pal1 is approximate... Copy approximation
			else if (paletteApprox[pal2] == -2 && paletteApprox[pal1] != -1)
				paletteApprox[pal2] = paletteApprox[pal1];
			// If Pal2 has not been used and Pal1 is copied... Set its index
			else if (paletteApprox[pal2] == -2 && paletteApprox[pal1] == -1)
				paletteApprox[pal2] = palettes.IndexOf(this.Palettes[pal1]);

			return true;
		}

		private Difference[] CalculateDistances()
		{
			int numDiff = 0;
			for (int i = 1; i < this.Palettes.Count; i++)
				numDiff += i;

			Difference[] distances = new Difference[numDiff];

			// Convert palettes to labcolor space to compute difference
			LabColor[][] palettes = new LabColor[this.Palettes.Count][];
			for (int i = 0; i < this.Palettes.Count; i++)
				palettes[i] = ColorConversion.ToLabPalette<Color>(this.Palettes[i]);

			int idx = 0;
			for (int i = 0; i < this.Palettes.Count; i++) {
				for (int j = i + 1; j < this.Palettes.Count; j++, idx++) {
					distances[idx] = new Difference();
					distances[idx].Palette1 = i;
					distances[idx].Palette2 = j;
					distances[idx].Distance = this.CalculateDistance(palettes[i], palettes[j]);
				}
			}

			return distances;
		}

		private double CalculateDistance(LabColor[] palette1, LabColor[] palette2)
		{
			double distance = 0;

			for (int i = 0; i < palette1.Length; i++)
				for (int j = 0; j < palette2.Length; j++)
					distance += palette1[i].GetDistanceSquared(palette2[j]);

			return distance;
		}

		private class Difference
		{
			public int Palette1 {
				get;
				set;
			}

			public int Palette2 {
				get;
				set;
			}

			public double Distance {
				get;
				set;
			}
		}
	}
}

