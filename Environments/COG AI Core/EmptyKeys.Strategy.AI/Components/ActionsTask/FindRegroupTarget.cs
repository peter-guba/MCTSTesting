using System.Collections.Generic;
using System.Linq;
using EmptyKeys.Strategy.AI.CoordinateSystem;
using EmptyKeys.Strategy.AI.TaskGeneration;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ActionsTask
{
    public class FindRegroupTarget : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var taskContext = context as TaskBehaviorContext;
            ICollection<MoveableUnit> units = taskContext?.Units.OfType<MoveableUnit>().ToList();
            HexElement attackTarget = taskContext?.Task.Target;
            if (units == null)
                return returnCode = BehaviorReturnCode.Failure;

            float qTotal = 0.0f, rTotal = 0.0f;
            foreach (MoveableUnit unit in units)
            {
                if (unit.IsInWarp)
                {
                    qTotal += unit.Q;
                    rTotal += unit.R;
                }
                else
                {
                    qTotal += unit.Environment.Q;
                    rTotal += unit.Environment.R;
                }
            }
            var avgPosition = new HexElement()
            {
                Q = (short)(qTotal / units.Count),
                R = (short)(rTotal / units.Count)
            };

            var system = (StarSystem)attackTarget;
            Player targetOwner = system.PlayersInfluence.Max().Key;

            // Check if average position is not in enenmy territory
            HexElement avgPosInfluence;
            if (taskContext.GalaxyInfluence.TryGetValue(avgPosition.HexMapKey, out avgPosInfluence))
            {
                var infElem = avgPosInfluence as InfluenceElement;
                if (infElem != null)
                {
                    if (infElem.Owner == targetOwner)
                    {
                        taskContext.EnvironmentTarget = avgPosition;
                        return returnCode = BehaviorReturnCode.Success;
                    }
                }
                else
                {
                    var layer = avgPosInfluence as MultiLayerElement;
                    infElem = layer?.Values.FirstOrDefault(inf => ((InfluenceElement)inf).Owner == targetOwner) as InfluenceElement;
                    if (infElem != null)
                    {
                        taskContext.EnvironmentTarget = avgPosition;
                        return returnCode = BehaviorReturnCode.Success;
                    }
                }
            }

            // Get regroup location
            IList<HexElement> line = HexHelpers.DrawLine(avgPosition.Q, avgPosition.R, attackTarget.Q, attackTarget.R);
            for (int i = 0; i < line.Count; i++)
            {
                Player owner = line[i].GetOwner(taskContext.GalaxyInfluence);
                if (owner == targetOwner)
                {
                    taskContext.EnvironmentTarget = line[i - 1];
                    break;
                }
            }

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}