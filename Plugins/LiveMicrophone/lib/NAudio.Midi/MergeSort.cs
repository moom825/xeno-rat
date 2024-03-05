using System;
using System.Collections.Generic;

namespace NAudio.Utils
{
    class MergeSort
    {

        /// <summary>
        /// Sorts the elements of the input list using the specified comparer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to be sorted.</param>
        /// <param name="comparer">The comparer to use for sorting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input list or comparer is null.</exception>
        /// <remarks>
        /// This method sorts the elements of the input list in place using the specified comparer.
        /// The sorting algorithm used is not specified and may vary based on the implementation of the comparer.
        /// </remarks>
        static void Sort<T>(IList<T> list, int lowIndex, int highIndex, IComparer<T> comparer)
        {
            if (lowIndex >= highIndex)
            {
                return;
            }


            int midIndex = (lowIndex + highIndex) / 2;


            // Partition the list into two lists and Sort them recursively
            Sort(list, lowIndex, midIndex, comparer);
            Sort(list, midIndex + 1, highIndex, comparer);

            // Merge the two sorted lists
            int endLow = midIndex;
            int startHigh = midIndex + 1;


            while ((lowIndex <= endLow) && (startHigh <= highIndex))
            {
                // MRH, if use < 0 sort is not stable
                if (comparer.Compare(list[lowIndex], list[startHigh]) <= 0)
                {
                    lowIndex++;
                }
                else
                {
                    // list[lowIndex] > list[startHigh]
                    // The next element comes from the second list, 
                    // move the list[start_hi] element into the next 
                    //  position and shuffle all the other elements up.
                    T t = list[startHigh];

                    for (int k = startHigh - 1; k >= lowIndex; k--)
                    {
                        list[k + 1] = list[k];
                    }

                    list[lowIndex] = t;
                    lowIndex++;
                    endLow++;
                    startHigh++;
                }
            }
        }

        /// <summary>
        /// MergeSort a list of comparable items
        /// </summary>
        public static void Sort<T>(IList<T> list) where T : IComparable<T>
        {
            Sort(list, 0, list.Count - 1, Comparer<T>.Default);
        }

        /// <summary>
        /// MergeSort a list 
        /// </summary>
        public static void Sort<T>(IList<T> list, IComparer<T> comparer)
        {
            Sort(list, 0, list.Count - 1, comparer);
        }
    }
}
