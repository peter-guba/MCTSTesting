using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CMS.Benchmark
{
    /// <summary>
    /// Class wrapping all results from a single benchmark run.
    /// </summary>
    internal class Results
    {
        /// <summary>
        /// Number of wins for player 1, player 2, and number of draws.
        /// </summary>
        public IList<int> WinCounts { get; set; } = new[] { 0, 0, 0 };

        /// <summary>
        /// Number of sym-wins for player 1, player 2, and number of sym draws.
        /// </summary>
        public IList<int> SymWinCounts { get; set; } = new[] { 0, 0, 0 };

        /// <summary>
        /// Sum of hull remaining for all units of player 1 and player 2.
        /// </summary>
        public IList<float> HullRemaining { get; set; } = new[] { 0.0f, 0.0f };

        /// <summary>
        /// Number of rounds for respective battles in order.
        /// </summary>
        public List<int> RoundCounts { get; set; } = new List<int>();

        /// <summary>
        /// How long did rounds take for player 1 and player 2 in fractional milliseconds.
        /// </summary>
        public List<double>[] RoundTimes { get; set; } = {new List<double>(), new List<double>()};

        /// <summary>
        /// The number of unfinished games.
        /// </summary>
        public int Unfinished { get; set; } = 0;

        /// <summary>
        /// Add <paramref name="other"/> result to this result maintaining all necessary statistics.
        /// </summary>
        public void Add(Results other)
        {
            Debug.Assert(WinCounts.Count == other.WinCounts.Count);
            Debug.Assert(SymWinCounts.Count == other.SymWinCounts.Count);
            Debug.Assert(WinCounts.Count == SymWinCounts.Count);
            for (int i = 0; i < WinCounts.Count; i++)
            {
                WinCounts[i] += other.WinCounts[i];
                SymWinCounts[i] += other.SymWinCounts[i];
            }

            Debug.Assert(HullRemaining.Count == other.HullRemaining.Count);
            Debug.Assert(RoundTimes.Length == other.RoundTimes.Length);
            for (int i = 0; i < HullRemaining.Count; i++)
            {
                HullRemaining[i] += other.HullRemaining[i];
                RoundTimes[i] = RoundTimes[i].Concat(other.RoundTimes[i]).ToList();
            }

            RoundCounts = RoundCounts.Concat(other.RoundCounts).ToList();
            Unfinished += other.Unfinished;
        }
    }
}
