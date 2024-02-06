using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Units;

namespace CMS.Actions
{
    /// <summary>
    /// Base class for unit-actions for unit at given <see cref="Hex"/>.
    /// </summary>
    public abstract class ActionCms
    {
        public Hex Source { get; }

        protected ActionCms(Unit u)
        {
            Source = u.Position;
        }

        /// <summary>
        /// Executes this action in given <paramref name="environment"/>.
        /// </summary>
        public abstract void Execute(GameEnvironment environment);
    }
}
