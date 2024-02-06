using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS
{
    public interface IDeepCloneable<out T>
    {
        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        T DeepClone();
    }
}
