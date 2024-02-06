using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS
{
    public static class Constants
    {
        public static readonly Hex[] HexDirections =
        {
            new Hex( 0, -1),
            new Hex(-1,  0),
            new Hex(-1,  1),
            new Hex( 0,  1),
            new Hex( 1,  0),
            new Hex( 1, -1)
        };
    }
}
