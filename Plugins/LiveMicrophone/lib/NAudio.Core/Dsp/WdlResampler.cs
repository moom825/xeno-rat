// This class based on the Resampler that is part of Cockos WDL
// originally written in C++ and ported to C# for NAudio by Mark Heath
// Used in NAudio with permission from Justin Frankel
// Original WDL License:
//     Copyright (C) 2005 and later Cockos Incorporated
//     
//     Portions copyright other contributors, see each source file for more information
// 
//     This software is provided 'as-is', without any express or implied
//     warranty.  In no event will the authors be held liable for any damages
//     arising from the use of this software.
// 
//     Permission is granted to anyone to use this software for any purpose,
//     including commercial applications, and to alter it and redistribute it
//     freely, subject to the following restrictions:
// 
//     1. The origin of this software must not be misrepresented; you must not
//        claim that you wrote the original software. If you use this software
//        in a product, an acknowledgment in the product documentation would be
//        appreciated but is not required.
//     2. Altered source versions must be plainly marked as such, and must not be
//        misrepresented as being the original software.
//     3. This notice may not be removed or altered from any source distribution.


using System;

// default to floats for audio samples
using WDL_ResampleSample = System.Single; // n.b. default in WDL is double

// default to floats for sinc filter ceofficients
using WDL_SincFilterSample = System.Single; // can also be set to double

namespace NAudio.Dsp
{
    /// <summary>
    /// Fully managed resampler, based on Cockos WDL Resampler
    /// </summary>
    class WdlResampler
    {
        private const int WDL_RESAMPLE_MAX_FILTERS = 4;
        private const int WDL_RESAMPLE_MAX_NCH = 64;
        private const double PI = 3.1415926535897932384626433832795;

        /// <summary>
        /// Creates a new Resampler
        /// </summary>
        public WdlResampler()
        {
            m_filterq = 0.707f;
            m_filterpos = 0.693f; // .792 ?

            m_sincoversize = 0;
            m_lp_oversize = 1;
            m_sincsize = 0;
            m_filtercnt = 1;
            m_interp = true;
            m_feedmode = false;

            m_filter_coeffs_size = 0;
            m_sratein = 44100.0;
            m_srateout = 44100.0;
            m_ratio = 1.0;
            m_filter_ratio = -1.0;

            Reset();
        }

        /// <summary>
        /// Sets the mode for resampling with optional sinc interpolation and filter count.
        /// </summary>
        /// <param name="interp">Specifies whether to use interpolation.</param>
        /// <param name="filtercnt">The number of filters to be used.</param>
        /// <param name="sinc">Specifies whether to use sinc interpolation.</param>
        /// <param name="sinc_size">The size of the sinc interpolation. Default is 64.</param>
        /// <param name="sinc_interpsize">The interpolation size for sinc. Default is 32.</param>
        /// <remarks>
        /// This method sets the mode for resampling with optional sinc interpolation and filter count.
        /// It modifies the internal state of the resampler based on the provided parameters.
        /// </remarks>
        public void SetMode(bool interp, int filtercnt, bool sinc, int sinc_size = 64, int sinc_interpsize = 32)
        {
            m_sincsize = sinc && sinc_size >= 4 ? sinc_size > 8192 ? 8192 : sinc_size : 0;
            m_sincoversize = (m_sincsize != 0) ? (sinc_interpsize <= 1 ? 1 : sinc_interpsize >= 4096 ? 4096 : sinc_interpsize) : 1;

            m_filtercnt = (m_sincsize != 0) ? 0 : (filtercnt <= 0 ? 0 : filtercnt >= WDL_RESAMPLE_MAX_FILTERS ? WDL_RESAMPLE_MAX_FILTERS : filtercnt);
            m_interp = interp && (m_sincsize == 0);

            //Debug.WriteLine(String.Format("setting interp={0}, filtercnt={1}, sinc={2},{3}\n", m_interp, m_filtercnt, m_sincsize, m_sincoversize));

            if (m_sincsize == 0)
            {
                m_filter_coeffs = new WDL_SincFilterSample[0]; //.Resize(0);
                m_filter_coeffs_size = 0;
            }
            if (m_filtercnt == 0)
            {
                m_iirfilter = null;
            }
        }

        /// <summary>
        /// Sets the filter parameters with default values for position and quality factor.
        /// </summary>
        /// <param name="filterpos">The position parameter for the filter. Default value is 0.693.</param>
        /// <param name="filterq">The quality factor parameter for the filter. Default value is 0.707.</param>
        public void SetFilterParms(float filterpos = 0.693f, float filterq = 0.707f)
        {
            m_filterpos = filterpos;
            m_filterq = filterq;
        }

        /// <summary>
        /// Sets the feed mode for the system.
        /// </summary>
        /// <param name="wantInputDriven">A boolean value indicating whether the system should be in input-driven feed mode.</param>
        /// <remarks>
        /// This method sets the feed mode for the system. If <paramref name="wantInputDriven"/> is true, the system will operate in input-driven feed mode; otherwise, it will operate in a different mode.
        /// </remarks>
        public void SetFeedMode(bool wantInputDriven)
        {
            m_feedmode = wantInputDriven;
        }

        /// <summary>
        /// Resets the history of the resampler by creating a new history array.
        /// </summary>
        public void Reset(double fracpos = 0.0)
        {
            m_last_requested = 0;
            m_filtlatency = 0;
            m_fracpos = fracpos;
            m_samples_in_rsinbuf = 0;
            if (m_iirfilter != null) m_iirfilter.Reset();
        }

        /// <summary>
        /// Sets the input and output rates, ensuring they are at least 1.0, and updates the internal ratio if the rates have changed.
        /// </summary>
        /// <param name="rate_in">The input rate to be set.</param>
        /// <param name="rate_out">The output rate to be set.</param>
        /// <remarks>
        /// If the input rate <paramref name="rate_in"/> is less than 1.0, it is set to 1.0.
        /// If the output rate <paramref name="rate_out"/> is less than 1.0, it is set to 1.0.
        /// If either <paramref name="rate_in"/> or <paramref name="rate_out"/> is different from the current input or output rates, the internal rates are updated and the ratio is recalculated as the input rate divided by the output rate.
        /// </remarks>
        public void SetRates(double rate_in, double rate_out)
        {
            if (rate_in < 1.0) rate_in = 1.0;
            if (rate_out < 1.0) rate_out = 1.0;
            if (rate_in != m_sratein || rate_out != m_srateout)
            {
                m_sratein = rate_in;
                m_srateout = rate_out;
                m_ratio = m_sratein / m_srateout;
            }
        }

        /// <summary>
        /// Gets the current latency in seconds.
        /// </summary>
        /// <returns>The current latency in seconds.</returns>
        public double GetCurrentLatency()
        {
            double v = ((double)m_samples_in_rsinbuf - m_filtlatency) / m_sratein;

            if (v < 0.0) v = 0.0;
            return v;
        }

        /// <summary>
        /// Prepares the resampling process and returns the number of samples required for the resampled output.
        /// </summary>
        /// <param name="out_samples">The number of output samples.</param>
        /// <param name="nch">The number of channels.</param>
        /// <param name="inbuffer">The input buffer for resampling.</param>
        /// <param name="inbufferOffset">The offset for the input buffer.</param>
        /// <returns>The number of samples required for the resampled output.</returns>
        /// <remarks>
        /// This method prepares the resampling process by resizing the input buffer and calculating the number of samples required for the resampled output.
        /// It also handles the filter latency and adjusts the size of the input buffer accordingly.
        /// </remarks>
        public int ResamplePrepare(int out_samples, int nch, out WDL_ResampleSample[] inbuffer, out int inbufferOffset)
        {
            if (nch > WDL_RESAMPLE_MAX_NCH || nch < 1)
            {
                inbuffer = null;
                inbufferOffset = 0;
                return 0;
            }

            int fsize = 0;
            if (m_sincsize > 1)
            {
                fsize = m_sincsize;
            }

            int hfs = fsize / 2;
            if (hfs > 1 && m_samples_in_rsinbuf < hfs - 1)
            {
                m_filtlatency += hfs - 1 - m_samples_in_rsinbuf;

                m_samples_in_rsinbuf = hfs - 1;

                if (m_samples_in_rsinbuf > 0)
                {
                    m_rsinbuf = new WDL_SincFilterSample[m_samples_in_rsinbuf * nch];
                }
            }

            int sreq = 0;

            if (!m_feedmode) sreq = (int)(m_ratio * out_samples) + 4 + fsize - m_samples_in_rsinbuf;
            else sreq = out_samples;

            if (sreq < 0) sreq = 0;

        again:
            Array.Resize(ref m_rsinbuf, (m_samples_in_rsinbuf + sreq) * nch);

            int sz = m_rsinbuf.Length / ((nch != 0) ? nch : 1) - m_samples_in_rsinbuf;
            if (sz != sreq)
            {
                if (sreq > 4 && (sz == 0))
                {
                    sreq /= 2;
                    goto again; // try again with half the size
                }
                // todo: notify of error?
                sreq = sz;
            }

            inbuffer = m_rsinbuf;
            inbufferOffset = m_samples_in_rsinbuf * nch;

            m_last_requested = sreq;
            return sreq;
        }

        /// <summary>
        /// Resamples the input buffer to the output buffer based on the given parameters and returns the number of samples written to the output buffer.
        /// </summary>
        /// <param name="outBuffer">The output buffer to write the resampled samples to.</param>
        /// <param name="outBufferIndex">The index in the output buffer to start writing the resampled samples.</param>
        /// <param name="nsamples_in">The number of input samples to be resampled.</param>
        /// <param name="nsamples_out">The number of output samples to be generated.</param>
        /// <param name="nch">The number of channels in the input and output buffers.</param>
        /// <returns>The number of samples written to the output buffer.</returns>
        /// <remarks>
        /// This method resamples the input buffer to the output buffer based on the given parameters. It handles filtering, interpolation, and padding as necessary to ensure accurate resampling.
        /// The resampling process modifies the output buffer in place.
        /// </remarks>
        public int ResampleOut(WDL_ResampleSample[] outBuffer, int outBufferIndex, int nsamples_in, int nsamples_out, int nch)
        {
            if (nch > WDL_RESAMPLE_MAX_NCH || nch < 1)
            {
                return 0;
            }

            if (m_filtercnt > 0)
            {
                if (m_ratio > 1.0 && nsamples_in > 0) // filter input
                {
                    if (m_iirfilter == null) m_iirfilter = new WDL_Resampler_IIRFilter();

                    int n = m_filtercnt;
                    m_iirfilter.setParms((1.0 / m_ratio) * m_filterpos, m_filterq);

                    int bufIndex = m_samples_in_rsinbuf * nch;
                    int a, x;
                    int offs = 0;
                    for (x = 0; x < nch; x++)
                        for (a = 0; a < n; a++)
                            m_iirfilter.Apply(m_rsinbuf, bufIndex + x, m_rsinbuf, bufIndex + x, nsamples_in, nch, offs++);
                }
            }

            m_samples_in_rsinbuf += Math.Min(nsamples_in, m_last_requested); // prevent the user from corrupting the internal state


            int rsinbuf_availtemp = m_samples_in_rsinbuf;

            if (nsamples_in < m_last_requested) // flush out to ensure we can deliver
            {
                int fsize = (m_last_requested - nsamples_in) * 2 + m_sincsize * 2;

                int alloc_size = (m_samples_in_rsinbuf + fsize) * nch;
                Array.Resize(ref m_rsinbuf, alloc_size);
                if (m_rsinbuf.Length == alloc_size)
                {
                    Array.Clear(m_rsinbuf, m_samples_in_rsinbuf * nch, fsize * nch);
                    rsinbuf_availtemp = m_samples_in_rsinbuf + fsize;
                }
            }

            int ret = 0;
            double srcpos = m_fracpos;
            double drspos = m_ratio;
            int localin = 0; // localin is an index into m_rsinbuf

            int outptr = outBufferIndex;  // outptr is an index into  outBuffer;

            int ns = nsamples_out;

            int outlatadj = 0;

            if (m_sincsize != 0) // sinc interpolating
            {
                if (m_ratio > 1.0) BuildLowPass(1.0 / (m_ratio * 1.03));
                else BuildLowPass(1.0);

                int filtsz = m_filter_coeffs_size;
                int filtlen = rsinbuf_availtemp - filtsz;
                outlatadj = filtsz / 2 - 1;
                int filter = 0; // filter is an index into m_filter_coeffs m_filter_coeffs.Get();

                if (nch == 1)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;

                        if (ipos >= filtlen - 1) break; // quit decoding, not enough input samples

                        SincSample1(outBuffer, outptr, m_rsinbuf, localin + ipos, srcpos - ipos, m_filter_coeffs, filter, filtsz);
                        outptr++;
                        srcpos += drspos;
                        ret++;
                    }
                }
                else if (nch == 2)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;

                        if (ipos >= filtlen - 1) break; // quit decoding, not enough input samples

                        SincSample2(outBuffer, outptr, m_rsinbuf, localin + ipos * 2, srcpos - ipos, m_filter_coeffs, filter, filtsz);
                        outptr += 2;
                        srcpos += drspos;
                        ret++;
                    }
                }
                else
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;

                        if (ipos >= filtlen - 1) break; // quit decoding, not enough input samples

                        SincSample(outBuffer, outptr, m_rsinbuf, localin + ipos * nch, srcpos - ipos, nch, m_filter_coeffs, filter, filtsz);
                        outptr += nch;
                        srcpos += drspos;
                        ret++;
                    }
                }
            }
            else if (!m_interp) // point sampling
            {
                if (nch == 1)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        if (ipos >= rsinbuf_availtemp) break; // quit decoding, not enough input samples

                        outBuffer[outptr++] = m_rsinbuf[localin + ipos];
                        srcpos += drspos;
                        ret++;
                    }
                }
                else if (nch == 2)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        if (ipos >= rsinbuf_availtemp) break; // quit decoding, not enough input samples

                        ipos += ipos;

                        outBuffer[outptr + 0] = m_rsinbuf[localin + ipos];
                        outBuffer[outptr + 1] = m_rsinbuf[localin + ipos + 1];
                        outptr += 2;
                        srcpos += drspos;
                        ret++;
                    }
                }
                else
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        if (ipos >= rsinbuf_availtemp) break; // quit decoding, not enough input samples

                        Array.Copy(m_rsinbuf, localin + ipos * nch, outBuffer, outptr, nch);
                        outptr += nch;
                        srcpos += drspos;
                        ret++;
                    }
            }
            else // linear interpolation
            {
                if (nch == 1)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        double fracpos = srcpos - ipos;

                        if (ipos >= rsinbuf_availtemp - 1)
                        {
                            break; // quit decoding, not enough input samples
                        }

                        double ifracpos = 1.0 - fracpos;
                        int inptr = localin + ipos;
                        outBuffer[outptr++] = (WDL_ResampleSample)(m_rsinbuf[inptr] * (ifracpos) + m_rsinbuf[inptr + 1] * (fracpos));
                        srcpos += drspos;
                        ret++;
                    }
                }
                else if (nch == 2)
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        double fracpos = srcpos - ipos;

                        if (ipos >= rsinbuf_availtemp - 1)
                        {
                            break; // quit decoding, not enough input samples
                        }

                        double ifracpos = 1.0 - fracpos;
                        int inptr = localin + ipos * 2;
                        outBuffer[outptr + 0] = (WDL_ResampleSample)(m_rsinbuf[inptr] * (ifracpos) + m_rsinbuf[inptr + 2] * (fracpos));
                        outBuffer[outptr + 1] = (WDL_ResampleSample)(m_rsinbuf[inptr + 1] * (ifracpos) + m_rsinbuf[inptr + 3] * (fracpos));
                        outptr += 2;
                        srcpos += drspos;
                        ret++;
                    }
                }
                else
                {
                    while (ns-- != 0)
                    {
                        int ipos = (int)srcpos;
                        double fracpos = srcpos - ipos;

                        if (ipos >= rsinbuf_availtemp - 1)
                        {
                            break; // quit decoding, not enough input samples
                        }

                        double ifracpos = 1.0 - fracpos;
                        int ch = nch;
                        int inptr = localin + ipos * nch;
                        while (ch-- != 0)
                        {
                            outBuffer[outptr++] = (WDL_ResampleSample)(m_rsinbuf[inptr] * (ifracpos) + m_rsinbuf[inptr + nch] * (fracpos));
                            inptr++;
                        }
                        srcpos += drspos;
                        ret++;
                    }
                }
            }

            if (m_filtercnt > 0)
            {
                if (m_ratio < 1.0 && ret > 0) // filter output
                {
                    if (m_iirfilter == null) m_iirfilter = new WDL_Resampler_IIRFilter();
                    int n = m_filtercnt;
                    m_iirfilter.setParms(m_ratio * m_filterpos, m_filterq);

                    int x, a;
                    int offs = 0;
                    for (x = 0; x < nch; x++)
                        for (a = 0; a < n; a++)
                            m_iirfilter.Apply(outBuffer, x, outBuffer, x, ret, nch, offs++);
                }
            }

            if (ret > 0 && rsinbuf_availtemp > m_samples_in_rsinbuf) // we had to pad!!
            {
                // check for the case where rsinbuf_availtemp>m_samples_in_rsinbuf, decrease ret down to actual valid samples
                double adj = (srcpos - m_samples_in_rsinbuf + outlatadj) / drspos;
                if (adj > 0)
                {
                    ret -= (int)(adj + 0.5);
                    if (ret < 0) ret = 0;
                }
            }

            int isrcpos = (int)srcpos;
            m_fracpos = srcpos - isrcpos;
            m_samples_in_rsinbuf -= isrcpos;
            if (m_samples_in_rsinbuf <= 0)
            {
                m_samples_in_rsinbuf = 0;
            }
            else
            {
                // TODO: bug here
                Array.Copy(m_rsinbuf, localin + isrcpos * nch, m_rsinbuf, localin, m_samples_in_rsinbuf * nch);
            }



            return ret;
        }

        /// <summary>
        /// Builds a lowpass filter based on the given filter position.
        /// </summary>
        /// <param name="filtpos">The position of the filter.</param>
        /// <remarks>
        /// This method builds a lowpass filter based on the given filter position, filter size, and interpolation size.
        /// It modifies the original filter coefficients array in place.
        /// The algorithm uses Blackman-Harris window and sinc function to construct the filter coefficients.
        /// </remarks>
        private void BuildLowPass(double filtpos)
        {
            int wantsize = m_sincsize;
            int wantinterp = m_sincoversize;

            if (m_filter_ratio != filtpos ||
                m_filter_coeffs_size != wantsize ||
                m_lp_oversize != wantinterp)
            {
                m_lp_oversize = wantinterp;
                m_filter_ratio = filtpos;

                // build lowpass filter
                int allocsize = (wantsize + 1) * m_lp_oversize;
                Array.Resize(ref m_filter_coeffs, allocsize);
                //int cfout = 0; // this is an index into m_filter_coeffs
                if (m_filter_coeffs.Length == allocsize)
                {
                    m_filter_coeffs_size = wantsize;

                    int sz = wantsize * m_lp_oversize;
                    int hsz = sz / 2;
                    double filtpower = 0.0;
                    double windowpos = 0.0;
                    double dwindowpos = 2.0 * PI / (double)(sz);
                    double dsincpos = PI / m_lp_oversize * filtpos; // filtpos is outrate/inrate, i.e. 0.5 is going to half rate
                    double sincpos = dsincpos * (double)(-hsz);

                    int x;
                    for (x = -hsz; x < hsz + m_lp_oversize; x++)
                    {
                        double val = 0.35875 - 0.48829 * Math.Cos(windowpos) + 0.14128 * Math.Cos(2 * windowpos) - 0.01168 * Math.Cos(6 * windowpos); // blackman-harris
                        if (x != 0) val *= Math.Sin(sincpos) / sincpos;

                        windowpos += dwindowpos;
                        sincpos += dsincpos;

                        m_filter_coeffs[hsz + x] = (WDL_SincFilterSample)val;
                        if (x < hsz) filtpower += val;
                    }
                    filtpower = m_lp_oversize / filtpower;
                    for (x = 0; x < sz + m_lp_oversize; x++)
                    {
                        m_filter_coeffs[x] = (WDL_SincFilterSample)(m_filter_coeffs[x] * filtpower);
                    }
                }
                else m_filter_coeffs_size = 0;

            }
        }

        /// <summary>
        /// Performs sample rate conversion using sinc interpolation.
        /// </summary>
        /// <param name="outBuffer">The output buffer to store the resampled samples.</param>
        /// <param name="outBufferIndex">The index in the output buffer to start writing the resampled samples.</param>
        /// <param name="inBuffer">The input buffer containing the original samples.</param>
        /// <param name="inBufferIndex">The index in the input buffer to start reading the original samples.</param>
        /// <param name="fracpos">The fractional position within the input buffer for interpolation.</param>
        /// <param name="nch">The number of channels in the audio data.</param>
        /// <param name="filter">The sinc filter coefficients for interpolation.</param>
        /// <param name="filterIndex">The index in the filter array to start applying the filter.</param>
        /// <param name="filtsz">The size of the filter.</param>
        /// <remarks>
        /// This method performs sample rate conversion using sinc interpolation. It applies a sinc filter to the input samples to generate the resampled output. The fractional position within the input buffer is used for interpolation, and the number of channels and filter size are taken into account during the process. The resampled samples are written to the output buffer starting from the specified index.
        /// </remarks>
        private void SincSample(WDL_ResampleSample[] outBuffer, int outBufferIndex, WDL_ResampleSample[] inBuffer, int inBufferIndex, double fracpos, int nch, WDL_SincFilterSample[] filter, int filterIndex, int filtsz)
        {
            int oversize = m_lp_oversize;
            fracpos *= oversize;
            int ifpos = (int)fracpos;
            filterIndex += oversize - 1 - ifpos;
            fracpos -= ifpos;

            for (int x = 0; x < nch; x++)
            {
                double sum = 0.0, sum2 = 0.0;
                int fptr = filterIndex;
                int iptr = inBufferIndex + x;
                int i = filtsz;
                while (i-- != 0)
                {
                    sum += filter[fptr] * inBuffer[iptr];
                    sum2 += filter[fptr + 1] * inBuffer[iptr];
                    iptr += nch;
                    fptr += oversize;
                }
                outBuffer[outBufferIndex + x] = (WDL_ResampleSample)(sum * fracpos + sum2 * (1.0 - fracpos));
            }
        }

        /// <summary>
        /// Performs sinc resampling on the input buffer and writes the result to the output buffer at the specified indices.
        /// </summary>
        /// <param name="outBuffer">The output buffer where the resampled data will be written.</param>
        /// <param name="outBufferIndex">The index in the output buffer where the resampled data will be written.</param>
        /// <param name="inBuffer">The input buffer containing the original data to be resampled.</param>
        /// <param name="inBufferIndex">The index in the input buffer from where the original data will be read for resampling.</param>
        /// <param name="fracpos">The fractional position within the input buffer for resampling.</param>
        /// <param name="filter">The sinc filter used for resampling.</param>
        /// <param name="filterIndex">The index in the sinc filter array.</param>
        /// <param name="filtsz">The size of the sinc filter.</param>
        /// <remarks>
        /// This method performs sinc resampling on the input buffer using the provided sinc filter and fractional position.
        /// It calculates the resampled value using the filter coefficients and writes the result to the output buffer at the specified index.
        /// </remarks>
        private void SincSample1(WDL_ResampleSample[] outBuffer, int outBufferIndex, WDL_ResampleSample[] inBuffer, int inBufferIndex, double fracpos, WDL_SincFilterSample[] filter, int filterIndex, int filtsz)
        {
            int oversize = m_lp_oversize;
            fracpos *= oversize;
            int ifpos = (int)fracpos;
            filterIndex += oversize - 1 - ifpos;
            fracpos -= ifpos;

            double sum = 0.0, sum2 = 0.0;
            int fptr = filterIndex;
            int iptr = inBufferIndex;
            int i = filtsz;
            while (i-- != 0)
            {
                sum += filter[fptr] * inBuffer[iptr];
                sum2 += filter[fptr + 1] * inBuffer[iptr];
                iptr++;
                fptr += oversize;
            }
            outBuffer[outBufferIndex] = (WDL_ResampleSample)(sum * fracpos + sum2 * (1.0 - fracpos));
        }

        /// <summary>
        /// Performs a sinc resampling of the input buffer and stores the result in the output buffer.
        /// </summary>
        /// <param name="outptr">The output buffer where the resampled data will be stored.</param>
        /// <param name="outBufferIndex">The index in the output buffer where the resampled data will start.</param>
        /// <param name="inBuffer">The input buffer containing the original data to be resampled.</param>
        /// <param name="inBufferIndex">The index in the input buffer from where the original data will be read for resampling.</param>
        /// <param name="fracpos">The fractional position within the input buffer for resampling.</param>
        /// <param name="filter">The sinc filter used for resampling.</param>
        /// <param name="filterIndex">The index in the filter array from where filtering will start.</param>
        /// <param name="filtsz">The size of the filter array.</param>
        /// <remarks>
        /// This method performs a sinc resampling of the input buffer using the provided sinc filter. It calculates the resampled values based on the fractional position within the input buffer and stores the result in the output buffer starting from the specified index.
        /// The resampling process involves applying the sinc filter to the input buffer data to obtain the resampled values. The filter is applied based on the fractional position, and the resulting values are stored in the output buffer.
        /// </remarks>
        private void SincSample2(WDL_ResampleSample[] outptr, int outBufferIndex, WDL_ResampleSample[] inBuffer, int inBufferIndex, double fracpos, WDL_SincFilterSample[] filter, int filterIndex, int filtsz)
        {
            int oversize = m_lp_oversize;
            fracpos *= oversize;
            int ifpos = (int)fracpos;
            filterIndex += oversize - 1 - ifpos;
            fracpos -= ifpos;

            double sum = 0.0;
            double sum2 = 0.0;
            double sumb = 0.0;
            double sum2b = 0.0;
            int fptr = filterIndex;
            int iptr = inBufferIndex;
            int i = filtsz / 2;
            while (i-- != 0)
            {
                sum += filter[fptr] * inBuffer[iptr];
                sum2 += filter[fptr] * inBuffer[iptr + 1];
                sumb += filter[fptr + 1] * inBuffer[iptr];
                sum2b += filter[fptr + 1] * inBuffer[iptr + 1];
                sum += filter[fptr + oversize] * inBuffer[iptr + 2];
                sum2 += filter[fptr + oversize] * inBuffer[iptr + 3];
                sumb += filter[fptr + oversize + 1] * inBuffer[iptr + 2];
                sum2b += filter[fptr + oversize + 1] * inBuffer[iptr + 3];
                iptr += 4;
                fptr += oversize * 2;
            }
            outptr[outBufferIndex + 0] = (WDL_ResampleSample)(sum * fracpos + sumb * (1.0 - fracpos));
            outptr[outBufferIndex + 1] = (WDL_ResampleSample)(sum2 * fracpos + sum2b * (1.0 - fracpos));
        }

        private double m_sratein; // WDL_FIXALIGN
        private double m_srateout;
        private double m_fracpos;
        private double m_ratio;
        private double m_filter_ratio;
        private float m_filterq, m_filterpos;
        private WDL_ResampleSample[] m_rsinbuf; // WDL_TypedBuf<WDL_ResampleSample>
        private WDL_SincFilterSample[] m_filter_coeffs; // WDL_TypedBuf<WDL_SincFilterSample>

        private WDL_Resampler_IIRFilter m_iirfilter; // WDL_Resampler_IIRFilter *

        private int m_filter_coeffs_size;
        private int m_last_requested;
        private int m_filtlatency;
        private int m_samples_in_rsinbuf;
        private int m_lp_oversize;

        private int m_sincsize;
        private int m_filtercnt;
        private int m_sincoversize;
        private bool m_interp;
        private bool m_feedmode;



        class WDL_Resampler_IIRFilter
        {
            public WDL_Resampler_IIRFilter()
            {
                m_fpos = -1;
                Reset();
            }

            public void Reset()
            {
                m_hist = new double[WDL_RESAMPLE_MAX_FILTERS * WDL_RESAMPLE_MAX_NCH, 4];
            }

            /// <summary>
            /// Sets the parameters for the filter.
            /// </summary>
            /// <param name="fpos">The position parameter.</param>
            /// <param name="Q">The Q factor.</param>
            /// <remarks>
            /// This method sets the parameters for the filter based on the given position <paramref name="fpos"/> and Q factor <paramref name="Q"/>.
            /// It calculates various coefficients based on the input parameters and updates the internal state of the filter.
            /// </remarks>
            public void setParms(double fpos, double Q)
            {
                if (Math.Abs(fpos - m_fpos) < 0.000001) return;
                m_fpos = fpos;

                double pos = fpos * PI;
                double cpos = Math.Cos(pos);
                double spos = Math.Sin(pos);

                double alpha = spos / (2.0 * Q);

                double sc = 1.0 / (1 + alpha);
                m_b1 = (1 - cpos) * sc;
                m_b2 = m_b0 = m_b1 * 0.5;
                m_a1 = -2 * cpos * sc;
                m_a2 = (1 - alpha) * sc;

            }

            /// <summary>
            /// Applies a filter to the input buffer and stores the result in the output buffer.
            /// </summary>
            /// <param name="inBuffer">The input buffer containing samples to be filtered.</param>
            /// <param name="inIndex">The index in the input buffer to start filtering from.</param>
            /// <param name="outBuffer">The output buffer to store the filtered samples.</param>
            /// <param name="outIndex">The index in the output buffer to start storing the filtered samples.</param>
            /// <param name="ns">The number of samples to filter.</param>
            /// <param name="span">The span between samples in the input and output buffers.</param>
            /// <param name="w">The value used for indexing in the history buffer.</param>
            /// <remarks>
            /// This method applies a filter to the input buffer using the coefficients b0, b1, b2, a1, and a2.
            /// The filtered samples are stored in the output buffer starting from the specified index.
            /// The history buffer m_hist is used to store previous input and output samples for filtering.
            /// </remarks>
            public void Apply(WDL_ResampleSample[] inBuffer, int inIndex, WDL_ResampleSample[] outBuffer, int outIndex, int ns, int span, int w)
            {
                double b0 = m_b0, b1 = m_b1, b2 = m_b2, a1 = m_a1, a2 = m_a2;

                while (ns-- != 0)
                {
                    double inx = inBuffer[inIndex];
                    inIndex += span;
                    double outx = (double)(inx * b0 + m_hist[w, 0] * b1 + m_hist[w, 1] * b2 - m_hist[w, 2] * a1 - m_hist[w, 3] * a2);
                    m_hist[w, 1] = m_hist[w, 0];
                    m_hist[w, 0] = inx;
                    m_hist[w, 3] = m_hist[w, 2];
                    m_hist[w, 2] = denormal_filter(outx);
                    outBuffer[outIndex] = (WDL_ResampleSample)m_hist[w, 2];

                    outIndex += span;
                }
            }

            /// <summary>
            /// Filters the input value to prevent denormalization and returns the filtered value.
            /// </summary>
            /// <param name="x">The input value to be filtered.</param>
            /// <returns>The filtered value of <paramref name="x"/>.</returns>
            /// <remarks>
            /// This method is intended to prevent denormalization of the input value <paramref name="x"/> and returns the filtered value.
            /// Denormalization occurs when a floating-point number is too close to zero, causing a loss of precision and performance degradation.
            /// The implementation of denormalization filtering is pending and needs to be completed.
            /// </remarks>
            double denormal_filter(float x)
            {
                // TODO: implement denormalisation
                return x;
            }
            double denormal_filter(double x)
            {
                // TODO: implement denormalisation
                return x;
            }

            private double m_fpos;
            private double m_a1, m_a2;
            private double m_b0, m_b1, m_b2;
            private double[,] m_hist;
        }

    }
}
