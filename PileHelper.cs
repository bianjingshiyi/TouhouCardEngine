using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    public static class PileHelper
    {
        public static bool ContainsPileOrAny(this IEnumerable<string> pileRange, string pileName)
        {
            return pileRange.Contains(pileName) || pileRange.Contains(Pile.PILE_RANGE_ANY);
        }
    }
}
