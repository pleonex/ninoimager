// -----------------------------------------------------------------------
// <copyright file="Runner.cs" company="none">
// Copyright (C) 2019
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
// <date>28/05/2019</date>
// -----------------------------------------------------------------------
namespace Ninoimager
{
    using System;
    using System.IO;
    using Ninoimager.Cli;
    using Ninoimager.Format;
    using Ninoimager.ImageProcessing;
    using EmguImage = Emgu.CV.Image<Emgu.CV.Structure.Bgra, System.Byte>;

    public class Runner
    {
        readonly Options options;

        public Runner(Options options)
        {
            this.options = options;
        }

        public void Run()
        {
            Environment.CurrentDirectory = options.WorkingDirectory;

            if (options.ExportMultiNscr != null) {
                foreach (var param in options.ExportMultiNscr)
                    ExportMultiNscr(param);
            }

            if (options.ImportMultiNscr != null) {
                foreach (var param in options.ImportMultiNscr)
                    ImportMultiNscr(param);
            }
        }

        void ExportMultiNscr(ExportMultiNscr param)
        {
            Nclr nclr = new Nclr(param.InputPalette);
            Ncgr ncgr = new Ncgr(param.InputTiles);

            foreach (var inputMap in param.InputMaps) {
                string name = Path.GetFileNameWithoutExtension(inputMap);
                string output = Path.Combine(param.Output, name + ".png");
                Directory.CreateDirectory(param.Output);

                Console.WriteLine($"Exporting {name} to {output}");
                Nscr nscr = new Nscr(inputMap);
                nscr.CreateBitmap(ncgr, nclr).Save(output);
            }
        }

        void ImportMultiNscr(ImportMultiNscr param)
        {
            Nclr nclr = new Nclr(param.InputPalette);
            EmguImage[] emguImgs = new EmguImage[param.Inputs.Length];
            for (int i = 0; i < param.Inputs.Length; i++)
                emguImgs[i] = new EmguImage(param.Inputs[i]);

            BackgroundImporter importer = new BackgroundImporter();
            using (var palStr = new FileStream(param.InputPalette, FileMode.Open))
            using (var tilesStr = new FileStream(param.ReferenceTiles, FileMode.Open))
            using (var mapStr = new FileStream(param.ReferenceMap, FileMode.Open))
                importer.SetOriginalSettings(mapStr, tilesStr, palStr);

            EmguImage combinedImg = emguImgs[0].Clone();
            // Concatenate images
            for (int i = 1; i < emguImgs.Length; i++)
                combinedImg = combinedImg.ConcateHorizontal(emguImgs[i]);

            // if (!(importer.Quantization is FixedPaletteQuantization)) {
            //     // Get quantization to share palette
            //     NdsQuantization quantization = new NdsQuantization();
            //     quantization.Quantizate(combinedImg);
            //     importer.Quantization = new FixedPaletteQuantization(quantization.Palette);
            // }

            // Get the palette and image file that it's shared
            MemoryStream nclrStr = new MemoryStream();
            MemoryStream ncgrStr = new MemoryStream();
            importer.ImportBackground(combinedImg, null, ncgrStr, nclrStr);
            nclrStr.Position = ncgrStr.Position = 0;
            using (var file = new FileStream(param.OutputTiles, FileMode.Create))
                ncgrStr.CopyTo(file);

            // Get the array of pixel from the image file
            nclrStr.Position = ncgrStr.Position = 0;
            Ncgr ncgr = new Ncgr(ncgrStr);
            Pixel[] fullImage = ncgr.GetPixels();
            ncgrStr.Position = 0;

            for (int i = 0; i < emguImgs.Length; i++) {
                MemoryStream nscrStr = new MemoryStream();
                importer.ImportBackgroundShareImage(emguImgs[i], fullImage, nscrStr);
                nscrStr.Position = 0;

                // Write NSCR
                using (var file = new FileStream(param.OutputMaps[i], FileMode.Create))
                    nscrStr.CopyTo(file);
            }
        }
    }
}