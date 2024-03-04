using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Summary description for FastFourierTransform.
    /// </summary>
    public static class FastFourierTransform
    {

        /// <summary>
        /// Computes the Fast Fourier Transform (FFT) of the input data array.
        /// </summary>
        /// <param name="forward">Specifies whether to compute the forward or inverse FFT.</param>
        /// <param name="m">The number of bits representing the size of the input data array.</param>
        /// <param name="data">The input data array of complex numbers.</param>
        /// <remarks>
        /// This method computes the FFT of the input data array using the Cooley-Tukey algorithm, which is a divide-and-conquer algorithm for efficiently computing the FFT.
        /// The method performs bit reversal on the input data array, then computes the FFT using iterative butterfly operations.
        /// The scaling for the forward transform is applied if the <paramref name="forward"/> parameter is set to true.
        /// </remarks>
        public static void FFT(bool forward, int m, Complex[] data)
        {
            int n, i, i1, j, k, i2, l, l1, l2;
            float c1, c2, tx, ty, t1, t2, u1, u2, z;

            // Calculate the number of points
            n = 1;
            for (i = 0; i < m; i++)
                n *= 2;

            // Do the bit reversal
            i2 = n >> 1;
            j = 0;
            for (i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    tx = data[i].X;
                    ty = data[i].Y;
                    data[i].X = data[j].X;
                    data[i].Y = data[j].Y;
                    data[j].X = tx;
                    data[j].Y = ty;
                }
                k = i2;

                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }

            // Compute the FFT 
            c1 = -1.0f;
            c2 = 0.0f;
            l2 = 1;
            for (l = 0; l < m; l++)
            {
                l1 = l2;
                l2 <<= 1;
                u1 = 1.0f;
                u2 = 0.0f;
                for (j = 0; j < l1; j++)
                {
                    for (i = j; i < n; i += l2)
                    {
                        i1 = i + l1;
                        t1 = u1 * data[i1].X - u2 * data[i1].Y;
                        t2 = u1 * data[i1].Y + u2 * data[i1].X;
                        data[i1].X = data[i].X - t1;
                        data[i1].Y = data[i].Y - t2;
                        data[i].X += t1;
                        data[i].Y += t2;
                    }
                    z = u1 * c1 - u2 * c2;
                    u2 = u1 * c2 + u2 * c1;
                    u1 = z;
                }
                c2 = (float)Math.Sqrt((1.0f - c1) / 2.0f);
                if (forward)
                    c2 = -c2;
                c1 = (float)Math.Sqrt((1.0f + c1) / 2.0f);
            }

            // Scaling for forward transform 
            if (forward)
            {
                for (i = 0; i < n; i++)
                {
                    data[i].X /= n;
                    data[i].Y /= n;
                }
            }
        }

        /// <summary>
        /// Calculates the value of the Hamming window function at the specified index for a given frame size.
        /// </summary>
        /// <param name="n">The index at which to calculate the Hamming window function.</param>
        /// <param name="frameSize">The size of the frame for which the Hamming window function is being calculated.</param>
        /// <returns>The value of the Hamming window function at the specified index for the given frame size.</returns>
        /// <remarks>
        /// The Hamming window function is defined as 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1)).
        /// It is commonly used in signal processing to reduce spectral leakage when performing a Fourier Transform on a windowed signal.
        /// </remarks>
        public static double HammingWindow(int n, int frameSize)
        {
            return 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1));
        }

        /// <summary>
        /// Calculates the Hann window value for the given index and frame size.
        /// </summary>
        /// <param name="n">The index for which the Hann window value is to be calculated.</param>
        /// <param name="frameSize">The size of the frame for which the Hann window value is to be calculated.</param>
        /// <returns>The Hann window value for the given index and frame size.</returns>
        public static double HannWindow(int n, int frameSize)
        {
            return 0.5 * (1 - Math.Cos((2 * Math.PI * n) / (frameSize - 1)));
        }

        /// <summary>
        /// Calculates the value of the Blackman-Harris window function at a given index.
        /// </summary>
        /// <param name="n">The index at which to calculate the window function value.</param>
        /// <param name="frameSize">The size of the frame for which the window function is being calculated.</param>
        /// <returns>The value of the Blackman-Harris window function at the specified index <paramref name="n"/>.</returns>
        public static double BlackmannHarrisWindow(int n, int frameSize)
        {
            return 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * n) / (frameSize - 1))) + (0.14128 * Math.Cos((4 * Math.PI * n) / (frameSize - 1))) - (0.01168 * Math.Cos((6 * Math.PI * n) / (frameSize - 1)));
        }
    }
}
