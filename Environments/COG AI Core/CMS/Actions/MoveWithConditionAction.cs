using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Pathfinding;
using CMS.Units;

namespace CMS.Actions
{
    [Obsolete("Needs to be implemented first.")]
    public class MoveWithConditionAction : ActionCms
    {
        private readonly Pathfinder.AStarStopCondition _condition;

        public MoveWithConditionAction(Unit u, Pathfinder.AStarStopCondition condition) 
            : base(u)
        {
            _condition = condition;
        }

        public override void Execute(GameEnvironment environment)
        {
            throw new NotImplementedException();
        }
    }
}
