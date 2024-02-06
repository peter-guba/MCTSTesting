using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using CMS.Pathfinding;
using CMS.Players;
using System.Linq;
using System;

namespace CMS.Playout
{
    /// <summary>
    /// Static class which can simulate a combat scenario
    /// </summary>
    public static class Game
    {
        /// <summary>
        /// Simulates a combat scenario from given parameters.
        /// Ends either when one player does not have any units or when <paramref name="roundLimit"/> is reached.
        /// </summary>
        /// <param name="environment">Environment and game state of the combat scenario.
        /// Is changed during the simulation.</param>
        /// <param name="players">Players involved in this combat.</param>
        /// <param name="roundLimit">Maximum number of rounds for the simulation.</param>
        /// <param name="gatherValues">Determines if the values of the states throughout the playout are supposed to be gathered.</param>
        /// <param name="relative"> Determines whether the value of the initial position of the simulation
        /// is supposed to be subtracted from the collected intermediate results. </param>
#if BENCHMARK
        /// <param name="detailedStats">Whether detailed stats should be generated.</param>
#endif
        /// <returns>Result of the simulation.</returns>
        public static GameResult Playout(GameEnvironment environment, List<Player> players, int roundLimit = int.MaxValue, bool gatherValues = false, bool relative = false, bool sameLength = false
#if BENCHMARK
            , bool detailedStats = false
#endif 
            )
        {
#if BENCHMARK
            var roundTimes = new List<double>[players.Count];
            for (int i = 0; i < roundTimes.Length; i++)
            {
                roundTimes[i] = new List<double>();
            }
            var sw = new Stopwatch();
#endif
            List<double> values = new List<double>();
            double initialEval = environment.GameState.Units[0].Values.Sum(x => x.Hull) - environment.GameState.Units[1].Values.Sum(x => x.Hull);

            var round = 0;
            while (round < roundLimit &&
                    environment.GameState.ActivePlayerUnits.Count > 0 &&
                    environment.GameState.OtherPlayerUnits.Count > 0)
            {
                int lastActive = environment.GameState.ActivePlayer;

                ++round;

                sw.Restart();
                players[environment.GameState.ActivePlayer].MakeActions(environment);
                sw.Stop();

                environment.GameState.NextTurn();

                // If intermediate results are supposed to be gathered, evaluate the current
                // game state every other round and store the result.
                if (gatherValues && round % 2 == 1)
                {
                    double hull = environment.GameState.Units[0].Values.Sum(x => x.Hull) - environment.GameState.Units[1].Values.Sum(x => x.Hull);

                    if (relative)
                    {
                        values.Add(hull - initialEval);
                    }
                    else
                    {
                        values.Add(hull);
                    }
                }
                if (detailedStats)
                    roundTimes[lastActive].Add(sw.Elapsed.TotalMilliseconds);
            }

            if (gatherValues && sameLength && values.Count != roundLimit / 2)
            {
                double hull = environment.GameState.Units[0].Values.Sum(x => x.Hull) - environment.GameState.Units[1].Values.Sum(x => x.Hull);

                for (int repeatIndex = values.Count; repeatIndex < roundLimit / 2; ++repeatIndex)
                {
                    values.Add(hull);
                }
            }

            var result = environment.GameState.GetResult();

            return new GameResult(round, result, result == GameState.DRAW, values
#if BENCHMARK
                , roundTimes
#endif
                );
        }
    }
}
