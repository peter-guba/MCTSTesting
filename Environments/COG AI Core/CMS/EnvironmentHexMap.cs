using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS
{
    /// <summary>
    /// Represents an environment in form of a hash table
    /// </summary>
    // This could be done as a wrapper around Dictionary, inheritance is usually not a good idea
    // but in this case we avoid one pointer dereference and simplify the code using this class.
    public class EnvironmentHexMap : Dictionary<Hex, HexType>
    {
        protected bool Equals(EnvironmentHexMap other)
        {
            foreach (KeyValuePair<Hex, HexType> kv in this)
            {
                HexType val;
                if (!other.TryGetValue(kv.Key, out val) ||
                    val != kv.Value)
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
            return Equals((EnvironmentHexMap)obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var kv in this)
            {
                hash = hash * 23 + kv.Key.GetHashCode();
                hash = hash * 23 + (int)kv.Value;
            }

            return hash;
        }
    }
}
