using System.Collections.Generic;
using CMS.Actions;
using CMS.Pathfinding;
using CMS.Units;

namespace CMS.Micro.Scripts
{
    /// <summary>
    /// Interface representing a script.
    /// </summary>
    public interface IScript
    {
        /// <summary>
        /// Considering <paramref name="environment"/> and <paramref name="enemyUnits"/> generates action for given <paramref name="unit"/>
        /// </summary>
        /// <param name="environment">Context in which the action should be generated.</param>
        /// <param name="enemyUnits">Collection of all units hostile to <paramref name="unit"/>.</param>
        /// <param name="unit"><see cref="Unit"/> for which the action is generated.</param>
        /// <returns>Action for <paramref name="unit"/> in given context.</returns>
        ActionCms MakeAction(GameEnvironment environment, IEnumerable<Unit> enemyUnits, Unit unit);
        string ToShortName();
    }
}
