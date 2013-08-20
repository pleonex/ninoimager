// -----------------------------------------------------------------------
// <copyright file="ColorFormat.cs" company="none">
// Copyright (C) 2013
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
// <date>20/08/2013</date>
// -----------------------------------------------------------------------
using System;

namespace Ninoimager.Format
{
	public enum ColorFormat {
		Unknown,
		// Texture NDS formats from http://nocash.emubase.de/gbatek.htm#ds3dtextureformats
		Indexed_A3I5  = 1,	// 8  bits-> 0-4: index; 5-7: alpha
		Indexed_2bpp  = 2,	// 2  bits for 4   colors
		Indexed_4bpp  = 3,	// 4  bits for 16  colors
		Indexed_8bpp  = 4,	// 8  bits for 256 colors          
		Texeled_4x4   = 5,	// 32 bits-> 2 bits per texel (only in textures)
		Indexed_A5I3  = 6,	// 8  bits-> 0-2: index; 3-7: alpha
		ABGR555_16bpp = 7,	// 16 bits BGR555 color with alpha component
		// Also common formats
		Indexed_1bpp,		// 1  bit  for 2   colors
		Indexed_A4I4,  		// 8  bits-> 0-3: index; 4-7: alpha
		BGRA_32bpp, 		// 32 bits BGRA color
		ABGR_32bpp, 		// 32 bits ABGR color
	}

	public static class ColorFormatExtension
	{
		public static int Bpp(this ColorFormat format)
		{
			switch (format) {
				case ColorFormat.Indexed_1bpp:  return 1;
				case ColorFormat.Indexed_2bpp:  return 2;
				case ColorFormat.Indexed_4bpp:  return 4;
				case ColorFormat.Indexed_8bpp:  return 8;
				case ColorFormat.Indexed_A3I5:  return 8;
				case ColorFormat.Indexed_A4I4:  return 8;
				case ColorFormat.Indexed_A5I3:  return 8;
				case ColorFormat.ABGR555_16bpp: return 16;
				case ColorFormat.BGRA_32bpp:    return 32;
				case ColorFormat.ABGR_32bpp:    return 32;
				case ColorFormat.Texeled_4x4:   return 2;

				default:
				throw new FormatException();
			}
		}

		public static bool IsIndexed(this ColorFormat format)
		{
			switch (format) {
				case ColorFormat.Indexed_1bpp:
				case ColorFormat.Indexed_2bpp:
				case ColorFormat.Indexed_4bpp:
				case ColorFormat.Indexed_8bpp:
				case ColorFormat.Indexed_A3I5:
				case ColorFormat.Indexed_A4I4:
				case ColorFormat.Indexed_A5I3:
				case ColorFormat.Texeled_4x4:
				return true;

				case ColorFormat.ABGR555_16bpp:
				case ColorFormat.BGRA_32bpp:
				case ColorFormat.ABGR_32bpp:
				return false;

				default:
				throw new FormatException();
			}
		}

		public static uint UnpackColor(this ColorFormat format, uint info)
		{
			switch (format) {
				// 100% alpha, no transparency
				case ColorFormat.Indexed_1bpp:
				case ColorFormat.Indexed_2bpp:
				case ColorFormat.Indexed_4bpp:
				case ColorFormat.Indexed_8bpp:
				return (0xFFu << 24) | info;

				case ColorFormat.Indexed_A3I5:
				return (((info >> 5) * 0xFF / 0x07) << 24) | (info & 0x1F);
				case ColorFormat.Indexed_A4I4:
				return (((info >> 4) * 0xFF / 0x0F) << 24) | (info & 0x0F);
				case ColorFormat.Indexed_A5I3:
				return (((info >> 3) * 0xFF / 0x1F) << 24) | (info & 0x07);

				case ColorFormat.ABGR555_16bpp:
				return 
					((((info >> 15) & 0x01) * 0xFF / 0x01) << 24) |	// alpha, 1 bit
						((((info >> 10) & 0x1F) * 0xFF / 0x1F) << 16) | // blue,  5 bits
						((((info >> 05) & 0x1F) * 0xFF / 0x1F) << 08) | // green, 5 bits
						((((info >> 00) & 0x1F) * 0xFF / 0x1F) << 00);	// red,   5 bits
				case ColorFormat.ABGR_32bpp:
				return info;
				case ColorFormat.BGRA_32bpp:
				return ((info & 0x0F) << 24) | (info >> 8);

				default:
				throw new NotSupportedException();
			}
		}

		public static uint PackColor(this ColorFormat format, uint pxInfo)
		{
			switch (format) {
				// No transparency
				case ColorFormat.Indexed_1bpp:
				case ColorFormat.Indexed_2bpp:
				case ColorFormat.Indexed_4bpp:
				case ColorFormat.Indexed_8bpp:
				return pxInfo & 0x00FFFFFF;

				case ColorFormat.Indexed_A3I5:
				return (((pxInfo >> 24) * 0x07 / 0xFF) << 5) | (pxInfo & 0x1F);
				case ColorFormat.Indexed_A4I4:
				return (((pxInfo >> 24) * 0x0F / 0xFF) << 4) | (pxInfo & 0x0F);
				case ColorFormat.Indexed_A5I3:
				return (((pxInfo >> 24) * 0x1F / 0xFF) << 3) | (pxInfo & 0x07);

				case ColorFormat.ABGR555_16bpp:
				return
					((((pxInfo >> 24) & 0xFF) * 0x01 / 0xFF) << 15) |	// alpha, 1 bit
						((((pxInfo >> 16) & 0xFF) * 0x1F / 0xFF) << 10) |	// blue,  5 bits
						((((pxInfo >> 08) & 0xFF) * 0x1F / 0xFF) << 05) |	// green, 5 bits
						((((pxInfo >> 00) & 0xFF) * 0x1F / 0xFF) << 00);	// red,   5 bits
				case ColorFormat.ABGR_32bpp:
				return pxInfo;
				case ColorFormat.BGRA_32bpp:
				return ((pxInfo >> 24) & 0xFF) | (pxInfo << 8);

				default:
				throw new NotSupportedException();
			}
		}
	}
}

