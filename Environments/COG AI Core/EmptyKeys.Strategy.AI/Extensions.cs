using System.Linq;
using EmptyKeys.Strategy.Core;

namespace EmptyKeys.Strategy.AI
{
    public static class Extensions
    {
        public static Player GetOwner(this HexElement hex, HexMap influenceMap)
        {
            HexElement influenceHex;
            if (!influenceMap.TryGetValue(hex.HexMapKey, out influenceHex))
                return null;

            var infElem = influenceHex as InfluenceElement;
            return infElem?.Owner;
        }
    }
}