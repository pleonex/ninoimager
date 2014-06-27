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
        private Difference[,] distancesMatrix;
        private List<Difference> distances;
        private int[] paletteEquivalent;

        public override void Reduce(int number)
        {
            if (this.Palettes.Count == 0 || number <= 0)
                return;

            #if DEBUG
            System.Diagnostics.Stopwatch watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            #endif

            this.Preprocess();
            this.Process(number);
            this.Postprocess();

            #if DEBUG
            watcher.Stop();
            Console.Write("(Time: {0, 5}) ", watcher.ElapsedMilliseconds);
            #endif
        }
       
        private void Preprocess()
        {
            // Initialize the approximation matrix to state not process.
            this.paletteEquivalent = new int[this.Palettes.Count];
            for (int i = 0; i < this.paletteEquivalent.Length; i++)
                this.paletteEquivalent[i] = -2;

            // Get distance between each palette
            this.distancesMatrix = CalculateDistances(this.Palettes.ToArray());

            // Copy it to a list
            this.distances = new List<Difference>(this.Palettes.Count * this.Palettes.Count);
            for (int i = 0; i < this.Palettes.Count; i++) {
                for (int j = 0; j < this.Palettes.Count; j++) {
                    if (i != j)
                        this.distances.Add(this.distancesMatrix[i, j]);
                }
            }

            // Sort them to get similar palettes first
            this.distances.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            this.distances.Reverse();

            // Remove palettes that are equals
            this.RemoveRepeatedPalettes();

            // TODO: Create method AddPalette to try to add colors to
            //       another palette instead of adding full palette.
            //       Start with palette that contains most colors
        }

        private void Process(int number)
        {
            // Start removing similar palettes.
            int uniquePalettes      = this.CountUniquePalettes();
            int unprocessedPalettes = this.CountUnprocessedPalettes();
            while (uniquePalettes < number && distances.Count > 0) {
                // TODO: A queue look better
                Difference diff = distances[0];
                distances.RemoveAt(0);

                // If the source palette has not been approximated yet
                if (this.paletteEquivalent[diff.SrcPalette] == -2) {
                    // If the destination palette has not been processed set as unique
                    if (this.paletteEquivalent[diff.DstPalette] == -2) {
                        this.paletteEquivalent[diff.DstPalette] = -1;
                        uniquePalettes++;
                    }

                    // If the destionation palette has not been approximate, set it
                    int newIdx = this.paletteEquivalent[diff.DstPalette];
                    if (newIdx == -1)
                        newIdx = diff.DstPalette;

                    this.paletteEquivalent[diff.SrcPalette] = newIdx;
                    unprocessedPalettes++;
                }

                // Check if the non-added palettes still need to be approximated
                if (uniquePalettes + unprocessedPalettes <= number) {
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
        }

        private void Postprocess()
        {
            #if DEBUG
            Console.Write("(Cost: {0, 5}) ", this.CalculateCost());
            #endif

            // Add unique palettes and set approximations to them
            int[] paletteApprox = new int[this.paletteEquivalent.Length];
            List<Color[]> reducedPalettes = new List<Color[]>();

            for (int i = 0; i < this.paletteEquivalent.Length; i++) {
                if (this.paletteEquivalent[i] == -2) {
                    Console.Write("(Warning: Error 0 in SimilarDistanceReducer) ");
                    continue;
                }

                // Get the index of the new palette or '-1' if it's unique
                int palIdx = this.paletteEquivalent[i];
                Color[] pal = (palIdx == -1) ? this.Palettes[i] : this.Palettes[palIdx];

                // Get the index in of the palette in the final output list of palettes
                // or add it if it has not been added yet.
                int newIdx = reducedPalettes.IndexOf(pal);
                if (newIdx == -1) {
                    reducedPalettes.Add(pal);
                    newIdx = reducedPalettes.Count - 1;
                } 

                paletteApprox[i] = (palIdx == -1) ? -1 : newIdx;
            }

            this.PaletteApproximation = paletteApprox;
            this.ReducedPalettes      = reducedPalettes.ToArray();
        }

        private int CountUnprocessedPalettes()
        {
            return this.CountPaletteEquivalent(-2);
        }

        private int CountUniquePalettes()
        {
            return this.CountPaletteEquivalent(-1);
        }

        private int CountPaletteEquivalent(int equiva)
        {
            int unprocessed = 0;
            for (int i = 0; i < this.paletteEquivalent.Length; i++)
                if (this.paletteEquivalent[i] == equiva)
                    unprocessed++;

            return unprocessed;
        }

        private void RemoveRepeatedPalettes()
        {
            while (this.distances.Count > 0 &&  this.distances[0].Distance == 0) {
                // Remove all entries in the list with DstPalette the number of this
                // DstPalette, since it will be the same behaviour as DstPalette = this SrcPalette
                // This will remove the first entry too
                int samePalette   = this.distances[0].DstPalette;
                int removePalette = this.distances[0].SrcPalette;

                // The approximation will be the other palette (they are equals!)
                this.paletteEquivalent[samePalette]   = -1;
                this.paletteEquivalent[removePalette] = samePalette;
                this.distances.RemoveAt(0);

                for (int i = this.distances.Count - 1; i >= 0; i--) {
                    if (this.distances[i].DstPalette != removePalette &&
                        this.distances[i].SrcPalette != removePalette)
                        continue;
                        
                    this.distances.RemoveAt(i);
                }

                for (int i = 0; i < this.paletteEquivalent.Length; i++)
                    if (this.paletteEquivalent[i] == removePalette)
                        this.paletteEquivalent[i] = samePalette;
            }
        }

        private void AddRemainingPalettes()
		{
            for (int i = 0; i < this.paletteEquivalent.Length; i++)
                if (this.paletteEquivalent[i] == -2)
                    this.paletteEquivalent[i] = -1;
		}

        private bool UpdateApproximation(int srcPal, int dstPal)
		{
            // Since we can set it as unique, we can do nothing
            if (this.paletteEquivalent[dstPal] == -2)
				return false;

            // Already processed
            if (this.paletteEquivalent[srcPal] != -2)
                return true;

            // If dstPal is approximate... Copy approximation
            if (this.paletteEquivalent[dstPal] != -1)
                this.paletteEquivalent[srcPal] = this.paletteEquivalent[dstPal];
            // If dstPal is unique... Set to it
            else if (this.paletteEquivalent[dstPal] == -1)
                this.paletteEquivalent[srcPal] = dstPal;

			return true;
		}

        private double CalculateCost()
        {
            double cost = 0;
            for (int i = 0; i < this.paletteEquivalent.Length; i++)
                if (this.paletteEquivalent[i] != -1)
                    cost += this.distancesMatrix[i, this.paletteEquivalent[i]].Distance;

            return cost;
        }

        private static Difference[,] CalculateDistances(Color[][] palettes)
		{
            // Combination of each palette with each palette except with itself (diagonal)
            Difference[,] distances = new Difference[palettes.Length, palettes.Length];

			// Convert palettes to labcolor space to compute difference
            LabColor[][] labPalettes = new LabColor[palettes.Length][];
            for (int i = 0; i < palettes.Length; i++)
                labPalettes[i] = ColorConversion.ToLabPalette<Color>(palettes[i]);

            // Compute every possible difference
            for (int i = 0; i < palettes.Length; i++) {
                for (int j = 0; j < palettes.Length; j++) {
					if (i == j)
						continue;

                    distances[i, j] = new Difference();
                    distances[i, j].SrcPalette = i;
                    distances[i, j].DstPalette = j;
                    distances[i, j].Distance   = CalculateDistance(labPalettes[i], labPalettes[j]);
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
