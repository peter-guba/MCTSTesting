using System.Collections.Generic;
using ChessTesting;

namespace Benchmarking
{
    /// <summary>
    /// Settings under which should given battle run. Represents a battle from "Resources/Battles".
    /// </summary>
    internal class BattleSettings
    {
        /// <summary>
        /// How many times should be the battle repeated.
        /// </summary>
        public int Repeats { get; }

        /// <summary>
        /// Name of the battle.
        /// </summary>
        public string Name { get; }

        public BattleSettings(int repeats, string name)
        {
            Repeats = repeats;
            Name = name;
        }
    }
}
