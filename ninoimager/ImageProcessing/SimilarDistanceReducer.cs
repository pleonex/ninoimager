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
using System.Linq;
using LabColor = Emgu.CV.Structure.Lab;
using Color    = Emgu.CV.Structure.Bgra;

namespace Ninoimager.ImageProcessing
{
	public class SimilarDistanceReducer : PaletteReducer
	{
        private List<Color[]> reducedPalettes;
        private List<Difference> distances;
        private int[] paletteApprox;

        public override void Reduce(int number)
        {
            if (this.Palettes.Count == 0 || number <= 0)
                return;

            this.Preprocess();

            // Start removing similar palettes.
            while (this.reducedPalettes.Count < number && distances.Count > 0) {
                // A queue look better
                Difference diff = distances[0];
                distances.RemoveAt(0);

                // If the source palette has not been approximated yet
                if (paletteApprox[diff.SrcPalette] == -2) {
                    // Get the index of the approximated palette
                    // it won't be always the DstPalette because that palette
                    // can be already approximated
                    int newPaletteIdx = this.paletteApprox[diff.DstPalette];    // Approximated
                    if (newPaletteIdx == -2) {
                        this.reducedPalettes.Add(this.Palettes[diff.DstPalette]);   // Added
                        newPaletteIdx = this.reducedPalettes.Count - 1;
                    } else if (newPaletteIdx == -1) {   // Unique
                        newPaletteIdx = this.reducedPalettes.IndexOf(this.Palettes[diff.DstPalette]);
                    }

                    // Update paletteApprox array
                    paletteApprox[diff.DstPalette] = -1;
                    paletteApprox[diff.SrcPalette] = newPaletteIdx;
                }

                // Check if the non-added palettes still need to be approximated
                if (this.reducedPalettes.Count + this.CountUnprocessedPalettes() <= number) {
                    this.AddRemainingPalettes();
                    distances.Clear();
                    break;
                }
            }

            // Since we are going to the same but starting from the end -> reverse the list
            distances.Reverse();

            // Continue approximating
            while (distances.Count > 0) {
                for (int i = distances.Count - 1; i >= 0; i--) {
                    Difference diff = distances[i];
                    if (this.UpdateApproximation(diff.SrcPalette, diff.DstPalette))
                        distances.RemoveAt(i);
                }
            }

            this.Postprocess();
        }

        private int CountUnprocessedPalettes()
        {
            int unprocessed = 0;
            for (int i = 0; i < this.paletteApprox.Length; i++)
                if (this.paletteApprox[i] == -2)
                    unprocessed++;

            return unprocessed;
        }

        private void Preprocess()
        {
            // TODO: Create method AddPalette to try to add colors to
            //       another palette instead of adding full palette.
            //       Start with palette that contains most colors


            // Get distance between each palette
            this.distances = new List<Difference>(CalculateDistances(this.Palettes.ToArray()));

            // Sort them to get similar palettes first
            this.distances.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            this.distances.Reverse();

            // Initialize the approximation matrix to state not process.
            this.paletteApprox = new int[this.Palettes.Count];
            for (int i = 0; i < this.paletteApprox.Length; i++)
                this.paletteApprox[i] = -2;

            // Initilize output palettes
            this.reducedPalettes = new List<Color[]>();

            // Remove palettes that are equals
            this.RemoveRepeatedPalettes();
        }

        private void RemoveRepeatedPalettes()
        {
            int[] approx = new int[this.Palettes.Count];
            for (int i = 0; i < approx.Length; i++)
                approx[i] = -2;

            while (this.distances.Count > 0 &&  this.distances[0].Distance == 0) {
                // Remove all entries in the list with DstPalette the number of this
                // DstPalette, since it will be the same behaviour as DstPalette = this SrcPalette
                // This will remove the first entry too
                int samePalette   = this.distances[0].DstPalette;
                int removePalette = this.distances[0].SrcPalette;

                // The approximation will be the other palette (they are equals!)
                approx[samePalette]   = -1;
                approx[removePalette] = samePalette;
                this.distances.RemoveAt(0);

                for (int i = this.distances.Count - 1; i >= 0; i--) {
                    if (this.distances[i].DstPalette != removePalette &&
                        this.distances[i].SrcPalette != removePalette)
                        continue;
                        
                    this.distances.RemoveAt(i);
                }

                for (int i = 0; i < approx.Length; i++)
                    if (approx[i] == removePalette)
                        approx[i] = samePalette;
            }

            for (int i = 0; i < approx.Length; i++) {
                if (approx[i] == -2)
                    continue;

                int palIdx = approx[i];
                Color[] pal = (palIdx == -1) ? this.Palettes[i] : this.Palettes[palIdx];
                int newIdx = this.reducedPalettes.IndexOf(pal);
                if (newIdx == -1) {
                    this.reducedPalettes.Add(pal);
                    newIdx = this.reducedPalettes.Count - 1;
                } 

                this.paletteApprox[i] = (palIdx == -1) ? -1 : newIdx;
            }
        }

        private void Postprocess()
        {
            this.PaletteApproximation = paletteApprox;
            this.ReducedPalettes      = this.reducedPalettes.ToArray();
        }

        private void AddRemainingPalettes()
		{
            for (int i = 0; i < this.paletteApprox.Length; i++) {
                if (this.paletteApprox[i] != -2)
					continue;

                this.reducedPalettes.Add(this.Palettes[i]);
                this.paletteApprox[i] = -1;
			}
		}

        private bool UpdateApproximation(int srcPal, int dstPal)
		{
            if (paletteApprox[dstPal] == -2)
				return false;

            // If srcPal has not been used and dstPal is approximate... Copy approximation
            if (paletteApprox[srcPal] == -2 && paletteApprox[dstPal] != -1)
                paletteApprox[srcPal] = paletteApprox[dstPal];
            // If srcPal has not been used and dstPal is copied... Set its index
            else if (paletteApprox[srcPal] == -2 && paletteApprox[dstPal] == -1)
                paletteApprox[srcPal] = this.reducedPalettes.IndexOf(this.Palettes[dstPal]);

			return true;
		}

        private static Difference[] CalculateDistances(Color[][] palettes)
		{
            // Combination of each palette with each palette except with itself (diagonal)
            int numDiff = palettes.Length * palettes.Length - palettes.Length;
			Difference[] distances = new Difference[numDiff];

			// Convert palettes to labcolor space to compute difference
            LabColor[][] labPalettes = new LabColor[palettes.Length][];
            for (int i = 0; i < palettes.Length; i++)
                labPalettes[i] = ColorConversion.ToLabPalette<Color>(palettes[i]);

            // Compute every possible difference
			int idx = 0;
            for (int i = 0; i < palettes.Length; i++) {
                for (int j = 0; j < palettes.Length; j++) {
					if (i == j)
						continue;

					distances[idx] = new Difference();
					distances[idx].SrcPalette = i;
					distances[idx].DstPalette = j;
                    distances[idx].Distance   = CalculateDistance(labPalettes[i], labPalettes[j]);
					idx++;
				}
			}

			return distances;
		}

        private static double CalculateDistance(LabColor[] palette1, LabColor[] palette2)
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
            
        /// <summary>>
        /// Store the distance from palette 1 to palette 2.
        /// </summary>
		private class Difference
		{
            /// <summary>
            /// Gets or sets the source palette
            /// </summary>
            /// <value>The palette 1.</value>
			public int SrcPalette {
				get;
				set;
			}

            /// <summary>
            /// Gets or sets the destination palette.
            /// </summary>
            /// <value>The palette2 .</value>
            public int DstPalette {
				get;
				set;
			}

            /// <summary>
            /// Gets or sets the distance.
            /// </summary>
            /// <value>The distance.</value>
			public double Distance {
				get;
				set;
			}
		}
	}
}
