using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CMS.ActionGenerators;
using CMS.Actions;

namespace CMS.Micro.Search.MCTS
{
    /// <summary>
    /// An expansion of the MctsNode class which has an added property - SquaredValueSum - which is necessary for
    /// the UcbTunedMcts algorithm to function properly.
    /// </summary>
    public class UcbTunedMctsNode : MctsNode
    {
        public double SquaredValueSum { get; set; }

        public UcbTunedMctsNode(MctsNode parent, ActionStatePair thisActionState, IEnumerable<ActionStatePair> childActions, int depth)
            : base(parent, thisActionState, childActions, depth)
        { }

        // Re-implemented so that it returns UcbTunedMctsNode instead of MctsNode.
        public bool TryGetNextChild(IActionGenerator generator, out UcbTunedMctsNode child)
        {
            if (!_actionSwitched)
            {
                if (!_currentActionEnum.MoveNext())
                {
                    child = null;
                    return false;
                }
                else
                {
                    ActionStatePair playerAction = _currentActionEnum.Current;

                    child = new UcbTunedMctsNode(this, playerAction, generator.EnumerateActions(playerAction.Environment), depth + 1);
                    playerAction.Environment.GameState.NextTurn();
                    Children.Add(child);
                    return true;
                }
            }
            else
            {
                _actionSwitched = false;
                ActionStatePair playerAction = _currentActionEnum.Current;

                child = new UcbTunedMctsNode(this, playerAction, generator.EnumerateActions(playerAction.Environment), depth + 1);
                playerAction.Environment.GameState.NextTurn();
                Children.Add(child);
                return true;
            }
        }
    }
}
