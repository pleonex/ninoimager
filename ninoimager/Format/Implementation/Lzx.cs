// -----------------------------------------------------------------------
// <copyright file="Lzx.cs" company="none">
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
// <author>CUE</author>
// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>20/06/2014</date>
// -----------------------------------------------------------------------
using System;
using System.IO;

namespace Ninoimager
{
    /// <summary>
    /// LZX encoding for GBA/NDS.
    /// <a href="http://romxhack.esforos.com/compresiones-para-las-consolas-gba-ds-de-nintendo-t117">
    /// Original code (in C) and notes by CUE.</a>
    /// </summary>
    public static class Lzx
    {
        private const int CmdCode11    = 0x11;  // LZX big endian magic number
        private const int LzxShift     = 1;     // bits to shift
        private const int LzxMask      = 0x80;  // first bit to check
        private const int LzxThreshold = 2;     // max number of bytes to not encode
        private const int LzxF         = 0x10;  // max coded (1 << 4)
        private const int LzxF1        = 0x110; // max coded ((1 << 4) + (1 << 8))

        public static void Decode(Stream strIn, int length, Stream strOut)
        {
            BinaryReader reader = new BinaryReader(strIn);

            uint header = reader.ReadUInt32();
            uint decLen = header >> 8;
            if ((header & 0xFF) != CmdCode11)
                throw new FormatException("Invalid header");

            uint len;
            uint pos;
            uint threshold;
            uint tmp;
            byte flags = 0;
            byte mask = 0;

            long endPosIn  = (strIn.Position - 4) + length;
            long endPosOut = strOut.Position + decLen;
            while (strOut.Position < endPosOut) {
                mask >>= LzxShift;
                if (mask == 0) {
                    flags = reader.ReadByte();
                    mask  = LzxMask;
                }

                if ((flags & mask) == 0) {
                    if (strIn.Position == endPosIn)
                        break;
                    byte b = reader.ReadByte();
                    strOut.WriteByte(b);
                } else {
                    if (strIn.Position + 1 == endPosIn)
                        break;
                    pos = reader.ReadByte();
                    pos = (pos << 8) | reader.ReadByte();

                    tmp = pos >> 12;
                    if (tmp < LzxThreshold) {
                        pos &= 0xFFF;
                        if (strIn.Position == endPosIn)
                            break;
                        pos = (pos << 8) | reader.ReadByte();
                        threshold = LzxF;
                        if (tmp != 0) {
                            if (strIn.Position == endPosIn)
                                break;
                            pos = (pos << 8) | reader.ReadByte();
                            threshold = LzxF1;
                        }
                    } else {
                        threshold = 0;
                    }

                    len = (pos >> 12) + threshold + 1;
                    pos = (pos & 0xFFF) + 1;

                    if (strOut.Position + len > endPosOut) {
                        Console.Write("(Warning: wrong decoded length) ");
                        len = (uint)(endPosOut - strOut.Position);
                    }

                    while (len-- != 0) {
                        long oldPos = strOut.Position;
                        strOut.Position -= pos;
                        byte b = (byte)strOut.ReadByte();
                        strOut.Position = oldPos;
                        strOut.WriteByte(b);
                    }
                }
            }

            strOut.Position = 0;
            strIn.Position  = 0;
        }
    }
}

