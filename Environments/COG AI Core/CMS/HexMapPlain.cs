using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS
{
    /// <summary>
    /// Represent a generic mapping of hexes to any type <typeparamref name="T"/>
    /// </summary>
    // This could be done as a wrapper around Dictionary, inheritance is usually not a good idea
    // but in this case we avoid one pointer dereference and simplify the code using this class.
    public class HexMapPlain<T>
        : Dictionary<Hex, T>
    {
        public HexMapPlain()
        {
        }

        public HexMapPlain(int initCapacity)
            : base(initCapacity)
        {
        }

        protected bool Equals(HexMapPlain<T> other)
        {
            foreach (KeyValuePair<Hex, T> kv in this)
            {
                T val;
                if (!other.TryGetValue(kv.Key, out val) ||
                    !val.Equals(kv.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HexMapPlain<T>) obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var kv in this)
            {
                hash = hash * 23 + kv.Key.GetHashCode();
                hash = hash * 23 + kv.Value.GetHashCode();
            }

            return hash;
        }
    }
}
