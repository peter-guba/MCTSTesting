using System.Collections.Generic;
using System.Collections;

namespace ChessTesting
{
    public class AlphaBetaSettings : AISettings
    {
        public int depth;
        public bool useIterativeDeepening;
        public bool useTranspositionTable;

        public bool useFixedDepthSearch;
        public bool clearTTEachMove;
    }
}
