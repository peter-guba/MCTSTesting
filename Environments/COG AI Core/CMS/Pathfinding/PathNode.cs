using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Pathfinding
{
    /// <summary>
    /// Represents a single node in a path.
    /// </summary>
    public class PathNode
    {
        /// <summary>
        /// Position of this <see cref="PathNode"/>.
        /// </summary>
        public Hex Hex { get; set; }
        
        /// <summary>
        /// Cost of traversing this <see cref="PathNode"/>.
        /// </summary>
        public int Cost { get; set; }

        public PathNode(Hex hex, int cost)
        {
            Hex = hex;
            Cost = cost;
        }
    }
}
