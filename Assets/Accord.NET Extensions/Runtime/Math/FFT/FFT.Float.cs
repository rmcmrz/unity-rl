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
using AForge.Math;

namespace Accord.Extensions.Math
{
    public static partial class FourierTransform
    {
		/*
		/// <summary>
		/// One dimensional Fast Fourier Transform.
		/// </summary>
		/// <param name="data">Data to transform.</param>
		/// <param name="direction">Transformation direction.</param>
		public static void FFT(ComplexF[] data, Direction direction){
			int length = data.Length;
			int n = length;
			int m = AForge.Math.Tools.Log2(n);
			
			// reorder data first
			ReorderData(data, length);
			
			// compute FFT
			int tn = 1, tm;
			
			for (int k = 1; k <= m; k++)
			{
				ComplexF[] rotation = FourierTransform.GetComplexRotation(k,  direction);
				
				tm = tn;
				tn <<= 1;
				
				for (int i = 0; i < tm; i++)
				{
					ComplexF t = rotation[i];
					
					for (int even = i; even < n; even += tn)
					{
						int odd = even + tm;
						ComplexF* ce = &data[even];
						ComplexF* co = &data[odd];
						
						float tr = co->Re * t.Re - co->Im * t.Im;
						float ti = co->Re * t.Im + co->Im * t.Re;
						
						co->Re = ce->Re - tr;
						co->Im = ce->Im - ti;
						
						ce->Re += tr;
						ce->Im += ti;
					}
				}
			}
			
			if (direction == Direction.Backward)
			{
				for (int i = 0; i < n; i++)
				{
					data[i].Re /= n;
					data[i].Im /= n;
				}
			}
		}
		
		
		/// <summary>
		/// Two dimensional Fast Fourier Transform.
		/// </summary>
		/// <param name="data">Data to transform.</param>
		/// <param name="direction">Transformation direction.</param>
		public unsafe static void FFT2(ComplexF[,] data, Direction direction)
		{
			int width = data.GetLength(1);
			int height = data.GetLength(0);
			
			fixed (ComplexF* dataPtr = data)
            {
                FFT2(dataPtr, width, height, width, direction);
            }
        }

        /// <summary>
        /// Two dimensional Fast Fourier Transform.
        /// </summary>
        /// <param name="width">Image width.</param> 
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image stride.</param>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// 
        /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
        /// only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
        /// is valid, but 15x15 is not.</note></para></remarks>
        /// 
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        /// 
        public unsafe static void FFT2(ComplexF* data, int width, int height, int stride, Direction direction)
        {
            const int MIN_PATCH_SIZE = 32; //how much rows/columns should one thread process

            int k = height;
            int n = width;

            // check data size
            if (
                    (!AForge.Math.Tools.IsPowerOf2(k)) ||
                    (!AForge.Math.Tools.IsPowerOf2(n)) ||
                    (k < minLength) || (k > maxLength) ||
                    (n < minLength) || (n > maxLength)
                    )
            {
                throw new ArgumentException("Incorrect data length.");
            }

            // process rows
            var procRow = new ParallelProcessor<bool, bool>(
            	new Size(
            		1 // does not matter
					, height
				)
				,
	            () => true,
	            (_, __, area) =>
	            {
	                ComplexF* dataPatchPtr = data + area.Y * stride / sizeof(ComplexF); //get row

	                for (int i = 0; i < area.Height; i++)
	                {
	                    // transform it
	                    FourierTransform.FFT(dataPatchPtr, n, direction);

	                    dataPatchPtr += stride / sizeof(ComplexF);
	                }
	            },
	            new ParallelOptions2D { 
					ParallelTrigger = (size) => size.Height >= MIN_PATCH_SIZE
					//,ForceSequential = true
				}
			);

            // process columns
            //(y and x are swaped => proc thinks it is diving horizontal pacthes but instead we are using them as vertical ones)
            var procCol = new ParallelProcessor<bool, bool>(
            	new Size(
            		1 // does not matter
            		, width
            	)
            	,
	            () => true,
	            (_, __, area) =>
	            {
	                ComplexF* dataPatchPtr = &data[area.Y]; //get column 

	                fixed (ComplexF* _col = new ComplexF[k])
	                {
	                    ComplexF* col = _col;

	                    for (int j = 0; j < area.Height; j++)
	                    {
	                        // copy column
	                        ComplexF* dataColPtr = &dataPatchPtr[j];
	                        for (int i = 0; i < k; i++)
	                        {
	                            col[i] = *dataColPtr;
	                            dataColPtr += stride / sizeof(ComplexF);
	                        }

	                        // transform it
	                        FourierTransform.FFT(col, k, direction);

	                        // copy back
	                        dataColPtr = &dataPatchPtr[j];
	                        for (int i = 0; i < k; i++)
	                        {
	                            *dataColPtr = col[i];
	                            dataColPtr += stride / sizeof(ComplexF);
	                        }
	                    }
	                }
	            },
	            new ParallelOptions2D { 
					ParallelTrigger = (size) => size.Height >= MIN_PATCH_SIZE
	                //,ForceSequential = true 
				}
			);

           procRow.Process(true);
           procCol.Process(true);         
        }

        #region Private Region

        private static ComplexF[,][] complexRotation = new ComplexF[maxBits, 2][];

        // Get rotation of complex number
        private static ComplexF[] GetComplexRotation(int numberOfBits, Direction direction)
        {
            int directionIndex = (direction == Direction.Forward) ? 1 : 0;

            // check if the array is already calculated
            if (complexRotation[numberOfBits - 1, directionIndex] == null)
            {
                int n = 1 << (numberOfBits - 1);
                float uR = 1.0f;
                float uI = 0.0f;
                double angle = System.Math.PI / n * (-(int)(direction));
                float wR = (float)System.Math.Cos(angle);
                float wI = (float)System.Math.Sin(angle);
                float t;
                ComplexF[] rotation = new ComplexF[n];

                for (int i = 0; i < n; i++)
                {
                    rotation[i] = new ComplexF { Re = uR, Im = uI};
                    t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                complexRotation[numberOfBits - 1, directionIndex] = rotation;
            }
            return complexRotation[numberOfBits - 1, directionIndex];
        }

        // Reorder data for FFT using
        private static void ReorderData(ComplexF[] data){
			int length = data.Length;

            // check data length
			if ((length < minLength) || (length > maxLength) || (!AForge.Math.Tools.IsPowerOf2(length)))
                throw new ArgumentException("Incorrect data length.");

            int[] rBits = GetReversedBits(AForge.Math.Tools.Log2(len));

            for (int i = 0; i < len; i++)
            {
                int s = rBits[i];

                if (s > i)
                {
                    ComplexF t = data[i];
                    data[i] = data[s];
                    data[s] = t;
                }
            }
        }
        #endregion
        */

		/// <summary>
		/// One dimensional Fast Fourier Transform.
		/// </summary>
		/// 
		/// <param name="data">Data to transform.</param>
		/// <param name="direction">Transformation direction.</param>
		/// 
		/// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
		/// only, where <b>n</b> may vary in the [1, 14] range.</note></para></remarks>
		/// 
		/// <exception cref="ArgumentException">Incorrect data length.</exception>
		/// 
		public static void FFT( ComplexF[] data, Direction direction )
		{
			int		n = data.Length;
			int		m = Tools.Log2( n );
			
			// reorder data first
			ReorderData( data );
			
			// compute FFT
			int tn = 1, tm;
			
			for ( int k = 1; k <= m; k++ )
			{
				ComplexF[] rotation = FourierTransform.GetComplexRotation( k, direction );
				
				tm = tn;
				tn <<= 1;
				
				for ( int i = 0; i < tm; i++ )
				{
					ComplexF t = rotation[i];
					
					for ( int even = i; even < n; even += tn )
					{
						int		odd = even + tm;
						ComplexF	ce = data[even];
						ComplexF	co = data[odd];
						
						float	tr = co.Re * t.Re - co.Im * t.Im;
						float	ti = co.Re * t.Im + co.Im * t.Re;
						
						data[even].Re += tr;
						data[even].Im += ti;
						
						data[odd].Re = ce.Re - tr;
						data[odd].Im = ce.Im - ti;
					}
				}
			}
			
			if ( direction == Direction.Forward ) 
			{
				for (int i = 0; i < n; i++) 
				{
					data[i].Re /= (float) n;
					data[i].Im /= (float) n;
				}
			}
		}
		
		/// <summary>
		/// Two dimensional Fast Fourier Transform.
		/// </summary>
		/// 
		/// <param name="data">Data to transform.</param>
		/// <param name="direction">Transformation direction.</param>
		/// 
		/// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
		/// only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
		/// is valid, but 15x15 is not.</note></para></remarks>
		/// 
		/// <exception cref="ArgumentException">Incorrect data length.</exception>
		/// 
		public static void FFT2( ComplexF[,] data, Direction direction )
		{
			int k = data.GetLength( 0 );
			int n = data.GetLength( 1 );
			
			// check data size
			if (
				( !Tools.IsPowerOf2( k ) ) ||
				( !Tools.IsPowerOf2( n ) ) ||
				( k < minLength ) || ( k > maxLength ) ||
				( n < minLength ) || ( n > maxLength )
				)
			{
				throw new ArgumentException( "Incorrect data length." );
			}
			
			// process rows
			ComplexF[]	row = new ComplexF[n];
			
			for ( int i = 0; i < k; i++ )
			{
				// copy row
				for ( int j = 0; j < n; j++ )
					row[j] = data[i, j];
				// transform it
				FourierTransform.FFT( row, direction );
				// copy back
				for ( int j = 0; j < n; j++ )
					data[i, j] = row[j];
			}
			
			// process columns
			ComplexF[]	col = new ComplexF[k];
			
			for ( int j = 0; j < n; j++ )
			{
				// copy column
				for ( int i = 0; i < k; i++ )
					col[i] = data[i, j];
				// transform it
				FourierTransform.FFT( col, direction );
				// copy back
				for ( int i = 0; i < k; i++ )
					data[i, j] = col[i];
			}
		}

		#region Private Region
		private static ComplexF[,][]	complexRotationF = new ComplexF[maxBits, 2][];

		/*
		private const int		minLength	= 2;
		private const int		maxLength	= 16384;
		private const int		minBits		= 1;
		private const int		maxBits		= 14;
		private static int[][]	reversedBits = new int[maxBits][];

		// Get array, indicating which data members should be swapped before FFT
		private static int[] GetReversedBits( int numberOfBits )
		{
			if ( ( numberOfBits < minBits ) || ( numberOfBits > maxBits ) )
				throw new ArgumentOutOfRangeException( );
			
			// check if the array is already calculated
			if ( reversedBits[numberOfBits - 1] == null )
			{
				int		n = Tools.Pow2( numberOfBits );
				int[]	rBits = new int[n];
				
				// calculate the array
				for ( int i = 0; i < n; i++ )
				{
					int oldBits = i;
					int newBits = 0;
					
					for ( int j = 0; j < numberOfBits; j++ )
					{
						newBits = ( newBits << 1 ) | ( oldBits & 1 );
						oldBits = ( oldBits >> 1 );
					}
					rBits[i] = newBits;
				}
				reversedBits[numberOfBits - 1] = rBits;
			}
			return reversedBits[numberOfBits - 1];
		}
		*/
		// Get rotation of complex number
		private static ComplexF[] GetComplexRotation( int numberOfBits, Direction direction )
		{
			int directionIndex = ( direction == Direction.Forward ) ? 0 : 1;
			
			// check if the array is already calculated
			if ( complexRotationF[numberOfBits - 1, directionIndex] == null )
			{
				int			n = 1 << ( numberOfBits - 1 );
				float		uR = 1.0f;
				float		uI = 0.0f;
				float		angle = UnityEngine.Mathf.PI / (float)n * (float) direction;
				float		wR = UnityEngine.Mathf.Cos( angle );
				float		wI = UnityEngine.Mathf.Sin( angle );
				float		t;
				ComplexF[]	rotation = new ComplexF[n];
				
				for ( int i = 0; i < n; i++ )
				{
					rotation[i] = new ComplexF( uR, uI );
					t = uR * wI + uI * wR;
					uR = uR * wR - uI * wI;
					uI = t;
				}
				
				complexRotationF[numberOfBits - 1, directionIndex] = rotation;
			}
			return complexRotationF[numberOfBits - 1, directionIndex];
		}
		
		// Reorder data for FFT using
		private static void ReorderData( ComplexF[] data )
		{
			int len = data.Length;
			
			// check data length
			if ( ( len < minLength ) || ( len > maxLength ) || ( !Tools.IsPowerOf2( len ) ) )
				throw new ArgumentException( "Incorrect data length." );
			
			int[] rBits = GetReversedBits( Tools.Log2( len ) );
			
			for ( int i = 0; i < len; i++ )
			{
				int s = rBits[i];
				
				if ( s > i )
				{
					ComplexF t = data[i];
					data[i] = data[s];
					data[s] = t;
				}
			}
		}
		
		#endregion
	}
}
