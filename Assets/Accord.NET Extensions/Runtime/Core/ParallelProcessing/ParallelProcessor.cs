#region Licence and Terms
// Accord.NET Extensions Framework
// https://github.com/dajuric/accord-net-extensions
//
// Copyright © Darko Jurić, 2014 
// darko.juric2@gmail.com
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU Lesser General Public License for more details.
// 
//   You should have received a copy of the GNU Lesser General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/lgpl.txt>.
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Point = AForge.IntPoint;

namespace Accord.Extensions
{
    /// <summary>
    /// Represents options for parallel patch computing.
    /// </summary>
    public class ParallelOptions2D
    {
        /// <summary>
        /// Creates default options.
        /// </summary>
        public ParallelOptions2D()
        {
#if DEBUG
            ForceSequential = true;
#else
            ForceSequential = false;
#endif
            ParallelTrigger = (size) => { return size.Width * size.Height > 100 * 100 * sizeof(byte); };
        }

        /// <summary>
        /// Force sequential execution even if parallel should be used.
        /// </summary>
        public bool ForceSequential { get; set; }

        /// <summary>
        /// Function that returns true if parallel processing should be used. Default one uses image size 100x100 as trigger.
        /// </summary>
        public Func<Size, bool> ParallelTrigger { get; set; }

        /// <summary>
        /// Returns whether parallel processor executes function in parallel or not.
        /// </summary>
        /// <param name="srcSize">Source image size.</param>
        /// <returns></returns>
        public bool ShouldProcessParallel(Size srcSize)
        {
            if (!ForceSequential && ParallelTrigger(srcSize) == true)
                return true;
            else
                return false;
        }
    }

    /// <summary>
    /// Represents a core class for parallel patch processing.
    /// </summary>
    /// <typeparam name="TSrc">Source data type.</typeparam>
    /// <typeparam name="TDest">Output data type.</typeparam>
    public class ParallelProcessor<TSrc, TDest>
    {
        /// <summary>
        /// Function that creates destination structure.
        /// </summary>
        /// <returns></returns>
        public delegate TDest FieldCreator();
        /// <summary>
        /// Function that performs patch processing.
        /// </summary>
        /// <param name="src">Source structure</param>
        /// <param name="dest">Destination structure</param>
        /// <param name="area">ROI for destination structure</param>
        public delegate void ProcessPatch(TSrc src, TDest dest, Rectangle area);

        private List<Rectangle> patches;

        private FieldCreator destImageCreator;
        private ProcessPatch processPatch;
        private Size imageSize;
        private bool runParallel;

        /// <summary>
        /// Creates parallel patch processor.
        /// </summary>
        /// <param name="imageSize">2D structure size.</param>
        /// <param name="destFieldCreator">Function that creates destination structure.</param>
        /// <param name="processPatch">Function that performs patch processing.</param>
        public ParallelProcessor(Size imageSize, FieldCreator destFieldCreator, ProcessPatch processPatch)
            : this(imageSize, destFieldCreator, processPatch, new ParallelOptions2D(), 0)
        { }

        /// <summary>
        /// Creates parallel patch processor.
        /// </summary>
        /// <param name="imageSize">2D structure size.</param>
        /// <param name="destFieldCreator">Function that creates destination structure.</param>
        /// <param name="processPatch">Function that performs patch processing.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="minPatchHeight">Minimal patch height. Patches that has lower size will not be created.</param>
        public ParallelProcessor(Size imageSize, FieldCreator destFieldCreator, ProcessPatch processPatch, ParallelOptions2D parallelOptions, int minPatchHeight = 0)
        {
            Initialize(imageSize, destFieldCreator, processPatch, parallelOptions, minPatchHeight);
        }

        /// <summary>
        /// Creates new parallel processor (not-initialized).
        /// </summary>
        protected ParallelProcessor()
        { }

        /// <summary>
        /// Initializes the parallel processor.
        /// </summary>
        /// <param name="imageSize">Image size.</param>
        /// <param name="destImageCreator">Destination image creator.</param>
        /// <param name="processPatch">Process patch func. The function will execute on every patch.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="minPatchHeight">Minimal patch height. Put 0 if there are no preferences.</param>
        protected void Initialize(Size imageSize, FieldCreator destImageCreator, ProcessPatch processPatch, ParallelOptions2D parallelOptions, int minPatchHeight)
        {
            this.imageSize = imageSize;
            this.destImageCreator = destImageCreator;
            this.processPatch = processPatch;
            this.runParallel = parallelOptions.ShouldProcessParallel(imageSize); //assume depth = sizeof(byte)

            if (runParallel) //do not build structures if they are not needed
            {
                makePatches(imageSize, minPatchHeight, out patches);
            }
        }

        /// <summary>
        /// Gets or sets image creator. Thus function is called only once.
        /// </summary>
        public FieldCreator DestImageCreator { get { return destImageCreator; } set { destImageCreator = value; } }
        /// <summary>
        /// Gets or sets patch process function. This function is called for every patch.
        /// </summary>
        public virtual ProcessPatch ProcessPatchFunc { get { return processPatch; } set { processPatch = value; } }

        /// <summary>
        /// Runs parallel processor.
        /// </summary>
        /// <param name="field2D">Source image</param>
        /// <returns>Processed destination image. The image which is created with <see cref="FieldCreator"/>.</returns>
        public virtual TDest Process(TSrc field2D)
        {
            TDest destImg = destImageCreator();

            if (runParallel) //process parallel
            {
                //do patches
                Parallel.For(0, patches.Count, (int i) =>
                {
                    processPatch(field2D, destImg, patches[i]);
                });
            }
            else //process sequential
            {
                processPatch(field2D, destImg, new Rectangle(new Point(), imageSize));
            }

            return destImg;
        }

        private void makePatches(Size fieldSize, int minPatchHeight, out List<Rectangle> patches)
        {
            int patchHeight, verticalPatches;
            getPatchInfo(fieldSize, out patchHeight, out verticalPatches);
            minPatchHeight = System.Math.Max(minPatchHeight, patchHeight);

            patches = new List<Rectangle>();

            for (int y = 0; y < fieldSize.Height; )
            {
                int h = System.Math.Min(patchHeight, fieldSize.Height - y);

                Rectangle patch = new Rectangle(0, y, fieldSize.Width, h);
                patches.Add(patch);

                y += h;
            }

            //ensure minPatchSize (merge last two patches if necessary)
            if (patches.Last().Height < minPatchHeight) 
            {
                var penultimate = patches[patches.Count - 1 - 1];
                var last = patches[patches.Count - 1];

                var mergedPatch = new Rectangle 
                {
                    X = penultimate.X,
                    Y = penultimate.Y,
                    Width = penultimate.Width,
                    Height = penultimate.Height + last.Height
                };

                patches.RemoveRange(patches.Count - 1 - 1, 2);
                patches.Add(mergedPatch);
            }
        }

        private void getPatchInfo(Size fieldSize, out int patchHeight, out int verticalPatches)
        {
            int numOfCores = System.Environment.ProcessorCount;
            int minNumOfPatches = numOfCores * 2;

            float avgNumPatchElements = (float)(fieldSize.Width * fieldSize.Height) / minNumOfPatches;

            //make patch look like a long stripe (it is probably more efficient to process than a square patch)
            patchHeight = (int)System.Math.Floor(avgNumPatchElements / fieldSize.Width);
            patchHeight = System.Math.Max(1, patchHeight); //if the image height is < num of CPUs

            //get number of patches
            verticalPatches = (int)System.Math.Ceiling((float)fieldSize.Height / patchHeight);
        }
    }
}
