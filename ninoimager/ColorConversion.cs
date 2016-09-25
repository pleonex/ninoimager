// -----------------------------------------------------------------------
// <copyright file="ColorConversion.cs" company="none">
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
// <date>01/08/2014</date>
// -----------------------------------------------------------------------
using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Ninoimager
{
	public static class ColorConversion
	{
		public static Lab[] ToLabPalette<TColor>(TColor[] palette)
			where TColor : struct, IColor
		{
			Lab[] labPalette = null;

			try {
				// Try direct conversion
				CvToolbox.GetColorCvtCode(typeof(TColor), typeof(Lab));
				labPalette = ColorConversion.ConvertColors<TColor, Lab>(palette);
			} catch {
				// Indirect conversion (converting first to Rgb)
				Rgb[] tempPalette = ColorConversion.ConvertColors<TColor, Rgb>(palette);
				labPalette = ColorConversion.ConvertColors<Rgb, Lab>(tempPalette);
			}

			return labPalette;
		}

		public static ColorDst[] ConvertColors<ColorSrc, ColorDst>(ColorSrc[] colors)
			where ColorSrc : struct, Emgu.CV.IColor
			where ColorDst : struct, Emgu.CV.IColor 
		{
			if (colors == null || colors.Length == 0)
                throw new ArgumentNullException(nameof(colors), "The array is null or empty.");

			int dimensionSrc = colors[0].Dimension;
			int dimensionDst = new ColorDst().Dimension;
			Emgu.CV.Matrix<byte> matSrc = new Emgu.CV.Matrix<byte>(1, colors.Length, dimensionSrc);
			Emgu.CV.Matrix<byte> matDst = new Emgu.CV.Matrix<byte>(1, colors.Length, dimensionDst);
            Emgu.CV.CvEnum.ColorConversion code = Emgu.CV.Util.CvToolbox.GetColorCvtCode(
				typeof(ColorSrc),
				typeof(ColorDst)
			);

			// Copy colors into matSrc
			for (int i = 0; i < colors.Length; i++) {
				Emgu.CV.Structure.MCvScalar colorComp = colors[i].MCvScalar;
				if (typeof(Rgba).IsAssignableFrom(typeof(ColorSrc))) {
                    double swap  = colorComp.V0;
					colorComp.V0 = colorComp.V2;
					colorComp.V2 = swap;
				}

				if (dimensionSrc > 0) matSrc.Data[0, i * dimensionSrc + 0] = (byte)colorComp.V0;
				if (dimensionSrc > 1) matSrc.Data[0, i * dimensionSrc + 1] = (byte)colorComp.V1;
				if (dimensionSrc > 2) matSrc.Data[0, i * dimensionSrc + 2] = (byte)colorComp.V2;
				if (dimensionSrc > 3) matSrc.Data[0, i * dimensionSrc + 3] = (byte)colorComp.V3;
			}

			// Convert colors
            Emgu.CV.CvInvoke.CvtColor(matSrc, matDst, code);

			// Copy matDst into new color array
			ColorDst[] newColors = new ColorDst[colors.Length];
			for (int i = 0; i < colors.Length; i++) {
				newColors[i] = new ColorDst();
				Emgu.CV.Structure.MCvScalar colorComp = new Emgu.CV.Structure.MCvScalar();
				if (dimensionDst > 0) colorComp.V0 = matDst.Data[0, i * dimensionDst + 0];
				if (dimensionDst > 1) colorComp.V1 = matDst.Data[0, i * dimensionDst + 1];
				if (dimensionDst > 2) colorComp.V2 = matDst.Data[0, i * dimensionDst + 2];
				if (dimensionDst > 3) colorComp.V3 = matDst.Data[0, i * dimensionDst + 3];
				newColors[i].MCvScalar = colorComp;
			}

			return newColors;
		}
	}
}

