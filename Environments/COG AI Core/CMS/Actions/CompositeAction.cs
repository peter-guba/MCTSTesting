using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Units;

namespace CMS.Actions
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a sequence of actions.
    /// </summary>
    public class CompositeAction : ActionCms
    {
        public List<ActionCms> Actions { get; }

        public CompositeAction(Unit u) : base(u)
        {
            Actions = new List<ActionCms>();
        }

        public CompositeAction(Unit u, List<ActionCms> actions) : base(u)
        {
            Actions = actions;
        }

        public override void Execute(GameEnvironment environment)
        {
            foreach (ActionCms action in Actions)
            {
                action.Execute(environment);
            }
        }
    }
}
