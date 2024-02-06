using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.ActionGenerators;
using CMS.Actions;

namespace CMS.Players
{
    /// <summary>
    /// Represents an actor in a combat simulation.
    /// </summary>
    public abstract class Player
    {
        /// <summary>
        /// For given <paramref name="environment"/> generates actions for the player.
        /// </summary>
        /// <param name="environment">Context in which the actions should be generated.</param>
        /// <returns>Collection of actions the player makes in this <see cref="environment"/>.</returns>
        public abstract ICollection<ActionCms> MakeActions(GameEnvironment environment);

        /// <summary>
        /// Sets the name of the current battle.
        /// </summary>
        public abstract void SetBattleName(string name);

        /// <summary>
        /// Sets the random string that is added to the ends of files that contain time
        /// and depth logs.
        /// </summary>
        public abstract void SetRndBattleString(string str);
    }
}