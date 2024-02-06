using System.Collections.Generic;

namespace CMS.Benchmark
{
    /// <summary>
    /// Settings under which should given battle run. Represents a battle from "Resources/Battles".
    /// </summary>
    internal class BattleSettings
    {
        /// <summary>
        /// <see cref="GameEnvironment"/> in which the battle takes place.
        /// </summary>
        public GameEnvironment GameEnvironment { get; }
        /// <summary>
        /// How many times should be the battle repeated.
        /// </summary>
        public int Repeats { get; }
        /// <summary>
        /// Name of the battle.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Numbers of units of given type for player 1 and player 2.
        /// </summary>
        public Dictionary<string, int>[] UnitCounts { get; }

        /// <summary>
        /// Creates a battle with given settings.
        /// </summary>
        /// <param name="gameEnvironment"><inheritdoc cref="GameEnvironment"/></param>
        /// <param name="repeats"><inheritdoc cref="Repeats"/></param>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="unitCounts"><inheritdoc cref="UnitCounts"/></param>
        public BattleSettings(GameEnvironment gameEnvironment, int repeats, string name, Dictionary<string, int>[] unitCounts)
        {
            GameEnvironment = gameEnvironment;
            Repeats = repeats;
            Name = name;
            UnitCounts = unitCounts;
        }
    }

    /// <summary>
    /// Class with helper methods for battles.
    /// </summary>
    internal static class BattleUtil
    {
        /// <summary>
        /// Extracts number of battleships from given <paramref name="unitCounts"/>.
        /// </summary>
        public static int GetBattleshipCount(Dictionary<string, int> unitCounts)
        {
            return GetUnitCount(unitCounts, "battleship");
        }

        /// <summary>
        /// Extracts number of destroyers from given <paramref name="unitCounts"/>.
        /// </summary>
        public static int GetDestroyerCount(Dictionary<string, int> unitCounts)
        {
            return GetUnitCount(unitCounts, "destroyer");
        }

        /// <summary>
        /// Extracts number of units called <paramref name="unitName"/> from given <paramref name="unitCounts"/>.
        /// </summary>
        /// <param name="unitCounts"></param>
        /// <param name="unitName"></param>
        /// <returns></returns>
        private static int GetUnitCount(Dictionary<string, int> unitCounts, string unitName)
        {
            foreach (string unitId in unitCounts.Keys)
            {
                if (unitId.ToLower().Contains(unitName))
                {
                    return unitCounts[unitId];
                }
            }

            return 0;
        }
    }
}
