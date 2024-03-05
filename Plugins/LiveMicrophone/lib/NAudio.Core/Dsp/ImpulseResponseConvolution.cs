using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Summary description for ImpulseResponseConvolution.
    /// </summary>
    public class ImpulseResponseConvolution
    {

        /// <summary>
        /// Convolves the input array with the given impulse response and returns the resulting array.
        /// </summary>
        /// <param name="input">The input array to be convolved.</param>
        /// <param name="impulseResponse">The impulse response array used for convolution.</param>
        /// <returns>The convolved array obtained by convolving <paramref name="input"/> with <paramref name="impulseResponse"/>.</returns>
        /// <remarks>
        /// This method performs convolution by sliding the impulse response over the input array and accumulating the weighted sum at each position.
        /// The resulting array is normalized using the Normalize method before being returned.
        /// </remarks>
        public float[] Convolve(float[] input, float[] impulseResponse)
        {
            var output = new float[input.Length + impulseResponse.Length];
            for(int t = 0; t < output.Length; t++)
            {
                for(int n = 0; n < impulseResponse.Length; n++)
                {
                    if((t >= n) && (t-n < input.Length))
                    {
                        output[t] += impulseResponse[n] * input[t-n];
                    }
                }
            }
            Normalize(output);
            return output;
        }

        /// <summary>
        /// Normalizes the input array of floating-point numbers by dividing each element by the maximum absolute value in the array, if it exceeds 1.0.
        /// </summary>
        /// <param name="data">The array of floating-point numbers to be normalized.</param>
        /// <remarks>
        /// This method iterates through the input array to find the maximum absolute value.
        /// If the maximum absolute value exceeds 1.0, each element in the array is divided by the maximum absolute value to normalize the data.
        /// </remarks>
        public void Normalize(float[] data)
        {
            float max = 0;
            for(int n = 0; n < data.Length; n++)
                max = Math.Max(max,Math.Abs(data[n]));
            if(max > 1.0)
                for(int n = 0; n < data.Length; n++)
                    data[n] /= max;
        }
    }
}
