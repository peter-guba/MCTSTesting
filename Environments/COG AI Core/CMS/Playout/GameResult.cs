using System.Collections.Generic;

namespace CMS.Playout
{
    /// <summary>
    /// Represents a result and statistics of a combat simulation.
    /// </summary>
    public class GameResult
    {
        public GameResult(int roundCount, int winner, bool isDraw, List<double> intermediateValues
#if BENCHMARK
            , List<double>[] roundTimes
#endif
            )
        {
            RoundCount = roundCount;
            Winner = winner;
            IsDraw = isDraw;
            IntermediateValues = intermediateValues;
#if BENCHMARK
            RoundTimes = roundTimes;
#endif
        }

        public bool IsDraw { get; }
        public int RoundCount { get; }
        public int Winner { get; }

        /// <summary>
        /// Contains intermediate results of the entire playout.
        /// </summary>
        public List<double> IntermediateValues { get; }

#if BENCHMARK
        public List<double>[] RoundTimes { get; }
#endif
    }
}
