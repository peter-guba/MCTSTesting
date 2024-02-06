using System;
using System.Collections.Generic;

namespace CMS.Benchmark
{
    /// <summary>
    /// Utility methods for collections.
    /// </summary>
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Returns median value from given list of values.
        /// </summary>
        /// <typeparam name="T">Type of items in the list.</typeparam>
        /// <param name="list">List of values where will the median be selected from.</param>
        /// <returns>Median value of given list.</returns>
        public static T Median<T>(this List<T> list) 
            where T : IComparable<T>
        {
            if (list.Count == 0)
                throw new Exception("Empty list"); // TODO better exception

            var sorted = new List<T>(list);
            sorted.Sort();
            return sorted[(int)Math.Ceiling(sorted.Count / 2.0) - 1];
        }
    }
}
