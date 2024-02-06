using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Utility
{
    internal static class ListExt
    {
        /// <summary>
        /// Sets all elements in given <paramref name="list"/> to given <paramref name="value"/>.
        /// </summary>
        public static void SetAll<T>(this IList<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = value;
            }
        }
    }
}
