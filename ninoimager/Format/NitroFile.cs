// -----------------------------------------------------------------------
// <copyright file="NitroFile.cs" company="none">
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
// <date>07/08/2013</date>
// -----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ninoimager.Format
{
	public class NitroFile
	{
		private const ushort BomLittleEndiannes = 0xFEFF;	// Byte Order Mask

		private Type[] blockTypes;
		private bool hasOffsets;

		private string magicStamp;
		private ushort version;
		private BlockCollection blocks;

		private NitroFile(params Type[] blockTypes)
		{
			// Check that all types heredites from NitroBlock
			foreach (Type t in blockTypes) {
				if (!t.IsSubclassOf(typeof(NitroBlock)))
					throw new ArgumentException("Invalid type passed.");
			}

			this.hasOffsets = false;
			this.blockTypes = blockTypes;
			this.blocks = new BlockCollection();
		}

		public NitroFile(string magicStamp, string version, params Type[] blockTypes)
			: this(blockTypes)
		{
			this.hasOffsets = false;
			this.magicStamp = magicStamp;
			this.VersionS   = version;
		}

		public NitroFile(string fileIn, bool hasOffsets, params Type[] blockTypes)
			: this(blockTypes)
		{
			this.hasOffsets = hasOffsets;
			using (FileStream fs = new FileStream(fileIn, FileMode.Open, FileAccess.Read, FileShare.Read))
				this.Read(fs, (int)fs.Length);
		}

		public NitroFile(string fileIn, params Type[] blockTypes)
			: this(fileIn, false, blockTypes)
		{
		}

		public NitroFile(Stream fileIn, bool hasOffsets, params Type[] blockTypes)
			: this(blockTypes)
		{
			this.hasOffsets = hasOffsets;
			this.Read(fileIn, (int)fileIn.Length);
		}

		public NitroFile(Stream fileIn, params Type[] blockTypes)
			: this(fileIn, false, blockTypes)
		{
		}

		public static ushort BlocksStart {
			get { return 0x10; }
		}

		protected virtual void Read(Stream strIn, int size)
		{
			long basePosition = strIn.Position;
			BinaryReader br = new BinaryReader(strIn);

			// Nitro header
			this.magicStamp = this.ReadMagicStamp(br);

			ushort bom = br.ReadUInt16();
			if (bom != BomLittleEndiannes) {	// Byte Order Mark
				if (bom == 0)
					Console.WriteLine("##ERROR?## There is no BOM value.");
				else
					throw new InvalidDataException("The data is not little endiannes.");
			}

			this.version = br.ReadUInt16();

			uint fileSize = br.ReadUInt32();
			if (fileSize > size)
				throw new FormatException("File size doesn't match (smaller).");
			else if (fileSize + 4 < size)	// It could be padding bytes.
				Console.WriteLine("##ERROR?##  File size doesn't match (bigger).");
			else if (fileSize < size)
				Console.WriteLine("##WARNING## File field is smaller than specified. {0}", size - fileSize);

			ushort blocksStart = br.ReadUInt16();
			ushort numBlocks = br.ReadUInt16();

			strIn.Position = basePosition + blocksStart;
			uint[] offsets = null;
			if (this.hasOffsets) {
				offsets = new uint[numBlocks];
				for (int i = 0; i < numBlocks; i++)
					offsets[i] = br.ReadUInt32();
			}
				
			this.blocks = new BlockCollection(numBlocks);
			for (int i = 0; i < numBlocks; i++)
			{
				if (this.hasOffsets)
					strIn.Position = basePosition + offsets[i];

				if (strIn.Position == strIn.Length) {
					Console.WriteLine("##ERROR?## Missing {0} blocks", numBlocks - i);
					return;
				}


				long blockPosition = strIn.Position;

				// First get block parameters
				string blockName = this.ReadMagicStamp(br);
				int blockSize = br.ReadInt32();
				strIn.Position = blockPosition;

				Type blockType = Array.Find<Type>( 
					this.blockTypes, b => b.Name.ToLower() == blockName.ToLower());
				if (blockType == null)
					throw new FormatException("Unknown block --> " + blockName);

				NitroBlock block = (NitroBlock)Activator.CreateInstance(blockType, this);
				block.Read(strIn);
				this.blocks.Add(block);

				strIn.Position = blockPosition + blockSize;
			}
		}

		private string ReadMagicStamp(BinaryReader br)
		{
			if (this.hasOffsets)
				return new string(br.ReadChars(4));
			else
				return new string(br.ReadChars(4).Reverse().ToArray());
		}

		public void Write(string fileOut)
		{
			if (File.Exists(fileOut))
				File.Delete(fileOut);

			using (FileStream fs = new FileStream(fileOut, FileMode.CreateNew,
													FileAccess.Write, FileShare.Read))
				this.Write(fs);
		}

		public virtual void Write(Stream strOut)
		{
			if (this.Blocks.Count > ushort.MaxValue)
				throw new Exception("Too many blocks.");

			long startPos = strOut.Position;
			BinaryWriter bw = new BinaryWriter(strOut);

			// Write header (need to be updated later)
			if (this.magicStamp[3] != '0')
				bw.Write(this.magicStamp.Reverse().ToArray());
			else
				bw.Write(this.magicStamp.ToCharArray());
			bw.Write(BomLittleEndiannes);
			bw.Write(this.Version);
			bw.Write(0x00);					// File size, unknown at the moment
			bw.Write(BlocksStart);
			bw.Write((ushort)this.Blocks.Count);

			while (strOut.Position < startPos + BlocksStart)
				strOut.WriteByte(0x00);

			if (this.hasOffsets) {
				uint offset = (uint)(BlocksStart + this.blocks.Count * 4);
				foreach (NitroBlock block in this.Blocks) {
					bw.Write(offset);
					offset += (uint)block.Size;
				}
			}

			// Starts writing blocks
			foreach (NitroBlock block in this.blocks) {
				long blockPos = strOut.Position;
				block.Write(strOut);
				strOut.Flush();

				// Checks size
				if (strOut.Length < blockPos + block.Size)
					throw new InvalidDataException(block.Name + " block size does not match.");

				strOut.Position = blockPos + block.Size;
			}

			// Update file size
			uint fileSize = (uint)(strOut.Position - startPos);
			strOut.Position = startPos + 0x08;
			bw.Write(fileSize);
			bw.Flush();
		}

		public string MagicStamp {
			get { return this.magicStamp; }
			set { this.magicStamp = value; }
		}

		public ushort Version {
			get { return this.version; }
			set { this.version = value; }
		}

		public string VersionS {
			get { return (this.version >> 8).ToString() + "." + (this.version & 0xFF).ToString(); }
			set { this.version = (ushort)(((value[0] - '0') << 8) | (value[2] - '0')); }
		}

		public bool HasOffsets{
			get { return this.hasOffsets; }
			set { this.hasOffsets = value; }
		}

		public BlockCollection Blocks {
			get { return blocks; }
		}

		public T GetBlock<T>(int index) where T : NitroBlock
		{
			return this.Blocks.GetByType<T>(index);
		}
	}

	public class BlockCollection : List<NitroBlock>
	{
		public BlockCollection()
			: base()
		{
		}

		public BlockCollection(int capacity)
			: base(capacity)
		{
		}

		public NitroBlock this[string name, int index] {
			get {
				return this.FindAll(b => b.Name == name)[index];
			}
		}

		public IEnumerable this[string name] {
			get {
				foreach (NitroBlock b in this.FindAll(b => b.Name == name)) {
					yield return b;
				}

				yield break;
			}
		}

		public T GetByType<T>(int index) where T : NitroBlock
		{
			return (T)this.FindAll(b => b is T)[index];
		}

		public IEnumerable<T> GetByType<T>() where T : NitroBlock
		{
			foreach (NitroBlock b in this.FindAll(b => b is T)) {
				yield return (T)b;
			}

			yield break;

		}

		public bool ContainsType(string type)
		{
			return (this.FindIndex(b => b.Name == type) != -1) ? true : false;
		}
	}


}

