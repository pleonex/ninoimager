// -----------------------------------------------------------------------
// <copyright file="Pack.cs" company="none">
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
using System.Collections.Generic;
using System.IO;

namespace Ninoimager.Format
{
	public abstract class Pack
	{
		private List<Stream> subfiles = new List<Stream>();

		public Pack()
		{
		}

		public Pack(Stream str)
		{
			this.Read(str);
		}

		public Pack(string file)
		{
			FileStream fs = new FileStream(file, FileMode.Open);
			this.Read(fs);
			fs.Close();
		}

		public Pack(params Stream[] subfiles)
		{
			this.subfiles.AddRange(subfiles);
		}

		public int NumSubfiles {
			get { return this.subfiles.Count; }
		}

		public IEnumerable<Stream> Subfiles {
			get {
				foreach (Stream sf in this.subfiles)
					yield return sf;
			}
		}
	
		public Stream this[int index] {
			get {
				if (index < 0 || index > this.subfiles.Count)
					throw new ArgumentOutOfRangeException();

				return this.subfiles[index];
			}

			set {
				if (index < 0 || index > this.subfiles.Count)
					throw new ArgumentOutOfRangeException();

				this.subfiles[index] = value;
			}
		}

		public void AddSubfile(Stream str)
		{
			this.subfiles.Add(str);
		}

		protected abstract void Read(Stream strIn);

		public abstract void Write(Stream strOut);

		public void Write(string fileOut)
		{
			FileStream fs = new FileStream(fileOut, FileMode.Create);
			this.Write(fs);
			fs.Flush();
			fs.Close();
		}
	
		public void CloseAll()
		{
			foreach (Stream subfile in this.subfiles) {
				if (subfile != null)
					subfile.Close();
			}
		}
	}
}

