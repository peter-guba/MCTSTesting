using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CMS
{
    /// <summary>
    /// Struct representing axial coordinates of a hex.
    /// </summary>
    [Serializable]
    [XmlRoot]
    public struct Hex
        : IEquatable<Hex> // To prevent boxing in dictionary equality check
    {
        public static readonly Hex ORIGIN = new Hex(0, 0);

        // Q and R are 16-bit integers to allow fast and uniqe hashing.
        public short Q { get; }
        public short R { get; }

        public Hex(short q, short r)
        {
            Q = q;
            R = r;
        }

        public int GetDistance(Hex to)
        {
            return (Math.Abs(Q - to.Q) + Math.Abs(R - to.R) + Math.Abs(Q + R - to.Q - to.R)) / 2;
        }

        public IEnumerable<Hex> GetRing(short radius)
        {
            int q = Q + radius;
            int r = R;
            foreach (Hex dir in Constants.HexDirections)
            {
                for (int j = 0; j < radius; ++j)
                {
                    q += dir.Q;
                    r += dir.R;
                    yield return new Hex((short) q, (short) r);
                }
            }
        }



        #region Directions

        /// <summary>
        /// Calculates index to directions array in <see cref="Constants"/> which corresponds to
        /// a direction vector from this <see cref="Hex"/> to <paramref name="to"/>.
        /// </summary>
        public int GetDirection(Hex to)
        {
            // Direction vector
            double dQ = to.Q - Q;
            double dR = to.R - R;
            // Normalize
            double len = Math.Sqrt(dQ * dQ + dR * dR);
            dQ /= len;
            dR /= len;
            // Find closest neighbour
            dQ = Math.Round(dQ, 0);
            dR = Math.Round(dR, 0);
            if (dQ == dR)
            {
                dR = 0.0;
            }

            return GetDirection((int) dQ, (int) dR);
        }

        private static int GetDirection(int dq, int dr)
        {
            int num = 0;
            for (int index = 0; index < Constants.HexDirections.Length; ++index)
            {
                Hex neighbor = Constants.HexDirections[index];
                if (neighbor.Q == dq && neighbor.R == dr)
                {
                    num = index;
                    break;
                }
            }

            return num;
        }

        /// <summary>
        /// Calculates index to directions array in <see cref="Constants"/> which is opposite to 
        /// direction at index <paramref name="dir"/>.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static int GetReverseDirection(int dir)
        {
            return (dir + 3) % 6;
        }

        #endregion

        #region Equality

        public bool Equals(Hex other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;
            return Equals((Hex) obj);
        }

        public override int GetHashCode()
        {
            return Q << 16 | R & ushort.MaxValue;
        }

        public static bool operator ==(Hex a, Hex b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Hex a, Hex b)
        {
            return !(a == b);
        }

        #endregion

        #region VectorOperators

        public static Hex operator +(Hex a, Hex b)
        {
            return new Hex((short) (a.Q + b.Q), (short) (a.R + b.R));
        }

        public static Hex operator *(Hex a, short k)
        {
            return new Hex((short) (a.Q * k), (short) (a.R * k));
        }

        public static Hex operator -(Hex a)
        {
            return new Hex((short) -a.Q, (short) -a.R);
        }

        #endregion

        public override string ToString()
        {
            return $"[{Q.ToString()}; {R.ToString()}]";
        }
    }
}
