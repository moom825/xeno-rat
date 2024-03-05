using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NAudio.Midi
{
    /// <summary>
    /// Utility class for comparing MidiEvent objects
    /// </summary>
    public class MidiEventComparer : IComparer<MidiEvent>
    {

        /// <summary>
        /// Compares two MidiEvent objects based on their absolute time and returns a value indicating their relative order.
        /// </summary>
        /// <param name="x">The first MidiEvent to compare.</param>
        /// <param name="y">The second MidiEvent to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative order of the objects being compared.
        /// Less than 0: <paramref name="x"/> is less than <paramref name="y"/>.
        /// 0: <paramref name="x"/> is equal to <paramref name="y"/>.
        /// Greater than 0: <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        /// <remarks>
        /// This method compares the absolute time of the input MidiEvent objects. If the absolute times are equal, it further sorts the MetaEvent before note events, except for the EndTrack event.
        /// The method returns a negative value if <paramref name="x"/> is to be placed before <paramref name="y"/>, zero if they are equal, and a positive value if <paramref name="x"/> is to be placed after <paramref name="y"/>.
        /// </remarks>
        public int Compare(MidiEvent x, MidiEvent y)
        {
            long xTime = x.AbsoluteTime;
            long yTime = y.AbsoluteTime;

            if (xTime == yTime)
            {
                // sort meta events before note events, except end track
                MetaEvent xMeta = x as MetaEvent;
                MetaEvent yMeta = y as MetaEvent;

                if (xMeta != null)
                {
                    if (xMeta.MetaEventType == MetaEventType.EndTrack)
                        xTime = Int64.MaxValue;
                    else
                        xTime = Int64.MinValue;
                }
                if (yMeta != null)
                {
                    if (yMeta.MetaEventType == MetaEventType.EndTrack)
                        yTime = Int64.MaxValue;
                    else
                        yTime = Int64.MinValue;
                }
            }
            return xTime.CompareTo(yTime);
        }

        #endregion
    }
}