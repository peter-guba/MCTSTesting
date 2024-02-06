using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Units;

namespace CMS
{
    /// <inheritdoc cref="HexMapPlain{T}"/>
    /// <summary>
    /// Represents a deep cloneable hashmap where the key is <see cref="Hex"/>.
    /// </summary>
    public class HexMap<T> : HexMapPlain<T>, IDeepCloneable<HexMap<T>>
        where T : IDeepCloneable<T>
    {
        public HexMap()
        {
        }

        public HexMap(int initCapacity)
            : base(initCapacity)
        {
        }

        public HexMap<T> DeepClone()
        {
            var clone = new HexMap<T>(Count);

            foreach (KeyValuePair<Hex, T> item in this)
            {
                clone[item.Key] = item.Value.DeepClone();
            }

            return clone;
        }
    }
}
