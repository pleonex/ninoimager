// -----------------------------------------------------------------------
// <copyright file="IPaletteReducer.cs" company="none">
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
// <date>04/05/2014</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Color = Emgu.CV.Structure.Rgba;

namespace Ninoimager
{
	public abstract class PaletteReducer
	{
		private List<Color[]> palettes = new List<Color[]>();
		private Color[][] reducedPalettes;
		private int[] paletteApproximation;

		public void AddPalette(Color[] palette)
		{
			this.palettes.Add(palette);
		}

		public void AddPaletteRange(Color[][] palettes)
		{
			this.palettes.AddRange(palettes);
		}

		public void Clear()
		{
			this.palettes.Clear();
			this.reducedPalettes      = null;
			this.paletteApproximation = null;
		}

		public int[] PaletteApproximation
		{
			get { return (int[])this.paletteApproximation.Clone(); }
			protected set { this.paletteApproximation = value; }
		}

		public Color[][] GetPalettes
		{
			get { return (Color[][])this.reducedPalettes; }
			protected set { this.reducedPalettes = value; }
		}

		public abstract void Reduce(int number);
	}
}

