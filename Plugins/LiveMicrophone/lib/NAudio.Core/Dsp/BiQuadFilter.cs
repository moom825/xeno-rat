// based on Cookbook formulae for audio EQ biquad filter coefficients
// http://www.musicdsp.org/files/Audio-EQ-Cookbook.txt
// by Robert Bristow-Johnson  <rbj@audioimagination.com>

//    alpha = sin(w0)/(2*Q)                                       (case: Q)
//          = sin(w0)*sinh( ln(2)/2 * BW * w0/sin(w0) )           (case: BW)
//          = sin(w0)/2 * sqrt( (A + 1/A)*(1/S - 1) + 2 )         (case: S)
// Q: (the EE kind of definition, except for peakingEQ in which A*Q is
// the classic EE Q.  That adjustment in definition was made so that
// a boost of N dB followed by a cut of N dB for identical Q and
// f0/Fs results in a precisely flat unity gain filter or "wire".)
//
// BW: the bandwidth in octaves (between -3 dB frequencies for BPF
// and notch or between midpoint (dBgain/2) gain frequencies for
// peaking EQ)
//
// S: a "shelf slope" parameter (for shelving EQ only).  When S = 1,
// the shelf slope is as steep as it can be and remain monotonically
// increasing or decreasing gain with frequency.  The shelf slope, in
// dB/octave, remains proportional to S for all other values for a
// fixed f0/Fs and dBgain.

using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// BiQuad filter
    /// </summary>
    public class BiQuadFilter
    {
        // coefficients
        private double a0;
        private double a1;
        private double a2;
        private double a3;
        private double a4;

        // state
        private float x1;
        private float x2;
        private float y1;
        private float y2;

        /// <summary>
        /// Transforms the input sample using the given coefficients and returns the result.
        /// </summary>
        /// <param name="inSample">The input sample to be transformed.</param>
        /// <returns>The transformed result based on the input sample and coefficients.</returns>
        /// <remarks>
        /// This method computes the result by applying the given coefficients (a0, a1, a2, a3, a4) to the input sample and previous input/output values (x1, x2, y1, y2).
        /// It then updates the internal state variables (x1, x2, y1, y2) for the next transformation.
        /// </remarks>
        public float Transform(float inSample)
        {
            // compute result
            var result = a0 * inSample + a1 * x1 + a2 * x2 - a3 * y1 - a4 * y2;

            // shift x1 to x2, sample to x1 
            x2 = x1;
            x1 = inSample;

            // shift y1 to y2, result to y1 
            y2 = y1;
            y1 = (float)result;

            return y1;
        }

        /// <summary>
        /// Precomputes the coefficients for the given parameters.
        /// </summary>
        /// <param name="aa0">The value of aa0.</param>
        /// <param name="aa1">The value of aa1.</param>
        /// <param name="aa2">The value of aa2.</param>
        /// <param name="b0">The value of b0.</param>
        /// <param name="b1">The value of b1.</param>
        /// <param name="b2">The value of b2.</param>
        /// <remarks>
        /// This method precomputes the coefficients a0, a1, a2, a3, and a4 based on the input parameters aa0, aa1, aa2, b0, b1, and b2.
        /// The coefficients are computed using the formulas:
        /// a0 = b0/aa0
        /// a1 = b1/aa0
        /// a2 = b2/aa0
        /// a3 = aa1/aa0
        /// a4 = aa2/aa0
        /// </remarks>
        private void SetCoefficients(double aa0, double aa1, double aa2, double b0, double b1, double b2)
        {
            // precompute the coefficients
            a0 = b0/aa0;
            a1 = b1/aa0;
            a2 = b2/aa0;
            a3 = aa1/aa0;
            a4 = aa2/aa0;
        }

        /// <summary>
        /// Sets the coefficients for a low-pass filter based on the given sample rate, cutoff frequency, and Q value.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the input signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the filter.</param>
        /// <param name="q">The Q value of the filter.</param>
        /// <remarks>
        /// This method calculates the coefficients for a low-pass filter based on the given sample rate, cutoff frequency, and Q value using the following transfer function:
        /// H(s) = 1 / (s^2 + s/Q + 1)
        /// where s = jw (j is the imaginary unit and w is the frequency in radians per second).
        /// The coefficients are then set using the SetCoefficients method to be used in the filter implementation.
        /// </remarks>
        public void SetLowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            // H(s) = 1 / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 - cosw0) / 2;
            var b1 = 1 - cosw0;
            var b2 = (1 - cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0,aa1,aa2,b0,b1,b2);
        }

        /// <summary>
        /// Sets the coefficients for a peaking equalizer filter based on the given parameters.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="centreFrequency">The center frequency of the peaking equalizer filter.</param>
        /// <param name="q">The Q factor of the peaking equalizer filter.</param>
        /// <param name="dbGain">The gain in decibels of the peaking equalizer filter.</param>
        /// <remarks>
        /// This method calculates the coefficients for a peaking equalizer filter based on the given parameters using the following formulas:
        /// H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)
        /// where s = e^(j*w), w = 2 * π * centreFrequency / sampleRate, A = 10^(dbGain / 40), and alpha = sin(w0) / (2 * q).
        /// The coefficients are then set using the SetCoefficients method.
        /// </remarks>
        public void SetPeakingEq(float sampleRate, float centreFrequency, float q, float dbGain)
        {
            // H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);
            var a = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?

            var b0 = 1 + alpha * a;
            var b1 = -2 * cosw0;
            var b2 = 1 - alpha * a;
            var aa0 = 1 + alpha / a;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha / a;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Sets the coefficients for a high-pass filter based on the provided sample rate, cutoff frequency, and Q value.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the input signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the high-pass filter.</param>
        /// <param name="q">The Q value of the high-pass filter.</param>
        /// <remarks>
        /// This method calculates the coefficients for a high-pass filter based on the provided sample rate, cutoff frequency, and Q value using the following transfer function:
        /// H(s) = s^2 / (s^2 + s/Q + 1)
        /// The coefficients are then set using the SetCoefficients method.
        /// </remarks>
        public void SetHighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            // H(s) = s^2 / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 + cosw0) / 2;
            var b1 = -(1 + cosw0);
            var b2 = (1 + cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Creates a low-pass filter with the specified sample rate, cutoff frequency, and Q value.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the filter.</param>
        /// <param name="q">The Q value of the filter.</param>
        /// <returns>A low-pass filter with the specified parameters.</returns>
        public static BiQuadFilter LowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var filter = new BiQuadFilter();
            filter.SetLowPassFilter(sampleRate,cutoffFrequency,q);
            return filter;
        }

        /// <summary>
        /// Creates a high-pass filter with the specified sample rate, cutoff frequency, and Q value.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the high-pass filter.</param>
        /// <param name="q">The Q value of the high-pass filter.</param>
        /// <returns>A high-pass filter with the specified parameters.</returns>
        public static BiQuadFilter HighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var filter = new BiQuadFilter();
            filter.SetHighPassFilter(sampleRate, cutoffFrequency, q);
            return filter;
        }

        /// <summary>
        /// Creates a BiQuad filter for a band-pass filter with constant skirt gain.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="centreFrequency">The center frequency of the band-pass filter.</param>
        /// <param name="q">The quality factor of the filter.</param>
        /// <returns>A BiQuadFilter object representing the band-pass filter with constant skirt gain.</returns>
        /// <remarks>
        /// This method calculates the coefficients for a band-pass filter with constant skirt gain using the given sample rate, center frequency, and quality factor.
        /// The coefficients are used to create a BiQuadFilter object, which can then be used to process audio signals.
        /// </remarks>
        public static BiQuadFilter BandPassFilterConstantSkirtGain(float sampleRate, float centreFrequency, float q)
        {
            // H(s) = s / (s^2 + s/Q + 1)  (constant skirt gain, peak gain = Q)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = sinw0 / 2; // =   Q*alpha
            var b1 = 0;
            var b2 = -sinw0 / 2; // =  -Q*alpha
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Returns a BiQuadFilter for a band-pass filter with constant 0 dB peak gain.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="centreFrequency">The center frequency of the band-pass filter.</param>
        /// <param name="q">The Q factor of the band-pass filter.</param>
        /// <returns>A BiQuadFilter for the specified band-pass filter.</returns>
        /// <remarks>
        /// This method calculates the coefficients for a band-pass filter with constant 0 dB peak gain using the input sample rate, center frequency, and Q factor.
        /// The coefficients are then used to create a BiQuadFilter instance, which represents the band-pass filter.
        /// </remarks>
        public static BiQuadFilter BandPassFilterConstantPeakGain(float sampleRate, float centreFrequency, float q)
        {
            // H(s) = (s/Q) / (s^2 + s/Q + 1)      (constant 0 dB peak gain)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = alpha;
            var b1 = 0;
            var b2 = -alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates a notch filter using the given sample rate, center frequency, and quality factor.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the input signal.</param>
        /// <param name="centreFrequency">The center frequency of the notch filter.</param>
        /// <param name="q">The quality factor of the notch filter.</param>
        /// <returns>A BiQuadFilter representing the notch filter.</returns>
        public static BiQuadFilter NotchFilter(float sampleRate, float centreFrequency, float q)
        {
            // H(s) = (s^2 + 1) / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1;
            var b1 = -2 * cosw0;
            var b2 = 1;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates an all-pass filter using the specified sample rate, center frequency, and Q value.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="centreFrequency">The center frequency of the filter.</param>
        /// <param name="q">The Q value of the filter.</param>
        /// <returns>A <see cref="BiQuadFilter"/> representing the all-pass filter.</returns>
        /// <remarks>
        /// This method calculates the coefficients for an all-pass filter based on the provided sample rate, center frequency, and Q value.
        /// The all-pass filter is defined by the transfer function H(s) = (s^2 - s/Q + 1) / (s^2 + s/Q + 1), where s is the Laplace transform variable.
        /// The method computes the coefficients b0, b1, b2, a0, a1, and a2 using the provided parameters and returns a new <see cref="BiQuadFilter"/> instance initialized with these coefficients.
        /// </remarks>
        public static BiQuadFilter AllPassFilter(float sampleRate, float centreFrequency, float q)
        {
            //H(s) = (s^2 - s/Q + 1) / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1 - alpha;
            var b1 = -2 * cosw0;
            var b2 = 1 + alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates a peaking equalization filter with the specified parameters.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="centreFrequency">The center frequency of the peaking filter.</param>
        /// <param name="q">The Q factor of the peaking filter.</param>
        /// <param name="dbGain">The gain of the peaking filter in decibels.</param>
        /// <returns>A BiQuadFilter configured as a peaking equalization filter.</returns>
        public static BiQuadFilter PeakingEQ(float sampleRate, float centreFrequency, float q, float dbGain)
        {
            var filter = new BiQuadFilter();
            filter.SetPeakingEq(sampleRate, centreFrequency, q, dbGain);
            return filter;
        }

        /// <summary>
        /// Calculates the coefficients for a low-shelf filter based on the given parameters and returns a BiQuadFilter object.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the filter.</param>
        /// <param name="shelfSlope">The slope of the shelf.</param>
        /// <param name="dbGain">The gain in decibels for the shelf.</param>
        /// <returns>A BiQuadFilter object representing the calculated filter coefficients.</returns>
        /// <remarks>
        /// This method calculates the coefficients for a low-shelf filter based on the given parameters using the formulas derived from the analog prototype.
        /// The coefficients are used to create a BiQuadFilter object that can be applied to an audio signal for low-shelf filtering.
        /// </remarks>
        public static BiQuadFilter LowShelf(float sampleRate, float cutoffFrequency, float shelfSlope, float dbGain)
        {
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;
            
            var b0 = a * ((a + 1) - (a - 1) * cosw0 + temp);
            var b1 = 2 * a * ((a - 1) - (a + 1) * cosw0);
            var b2 = a * ((a + 1) - (a - 1) * cosw0 - temp);
            var a0 = (a + 1) + (a - 1) * cosw0 + temp;
            var a1 = -2 * ((a - 1) + (a + 1) * cosw0);
            var a2 = (a + 1) + (a - 1) * cosw0 - temp;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Calculates the coefficients for a high-shelf filter using the given parameters and returns a BiQuadFilter object.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio signal.</param>
        /// <param name="cutoffFrequency">The cutoff frequency of the filter.</param>
        /// <param name="shelfSlope">The slope of the shelf.</param>
        /// <param name="dbGain">The gain in decibels for the shelf filter.</param>
        /// <returns>A BiQuadFilter object with the calculated coefficients for the high-shelf filter.</returns>
        /// <remarks>
        /// This method calculates the coefficients for a high-shelf filter using the given parameters based on the following formulas:
        /// - w0 = 2 * π * cutoffFrequency / sampleRate
        /// - cosw0 = Cos(w0)
        /// - sinw0 = Sin(w0)
        /// - a = 10^(dbGain / 40)
        /// - alpha = sinw0 / 2 * Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2)
        /// - temp = 2 * Sqrt(a) * alpha
        /// - b0 = a * ((a + 1) + (a - 1) * cosw0 + temp)
        /// - b1 = -2 * a * ((a - 1) + (a + 1) * cosw0)
        /// - b2 = a * ((a + 1) + (a - 1) * cosw0 - temp)
        /// - a0 = (a + 1) - (a - 1) * cosw0 + temp
        /// - a1 = 2 * ((a - 1) - (a + 1) * cosw0)
        /// - a2 = (a + 1) - (a - 1) * cosw0 - temp
        /// The calculated coefficients are used to create a BiQuadFilter object, which is then returned.
        /// </remarks>
        public static BiQuadFilter HighShelf(float sampleRate, float cutoffFrequency, float shelfSlope, float dbGain)
        {
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * ((a + 1) + (a - 1) * cosw0 + temp);
            var b1 = -2 * a * ((a - 1) + (a + 1) * cosw0);
            var b2 = a * ((a + 1) + (a - 1) * cosw0 - temp);
            var a0 = (a + 1) - (a - 1) * cosw0 + temp;
            var a1 = 2 * ((a - 1) - (a + 1) * cosw0);
            var a2 = (a + 1) - (a - 1) * cosw0 - temp;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        private BiQuadFilter()
        {
            // zero initial samples
            x1 = x2 = 0;
            y1 = y2 = 0;
        }

        private BiQuadFilter(double a0, double a1, double a2, double b0, double b1, double b2)
        {
            SetCoefficients(a0,a1,a2,b0,b1,b2);

            // zero initial samples
            x1 = x2 = 0;
            y1 = y2 = 0;
        }
    }
}
