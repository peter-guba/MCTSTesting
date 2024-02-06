using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CMS.ActionGenerators;

namespace CMS.Micro.Search.MCTS
{
    public class MctsNode
    {
        /// <summary>
        /// A random number generator used when picking moves during expansion.
        /// </summary>
        protected static System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// The sum of hulls of the units of the two teams in the state that
        /// corresponds to this node.
        /// </summary>
        public float[] UnitHulls { get; }

        /// <summary>
        /// The depth of this node.
        /// </summary>
        public readonly int depth;

        public MctsNode(MctsNode parent, ActionStatePair thisActionState, IEnumerable<ActionStatePair> childActions, int depth)
        {
            Parent = parent;
            CurrentActionState = thisActionState;
            ChildActions = childActions;
            Children = new List<MctsNode>();
            _currentActionEnum = ChildActions.GetEnumerator();
            UnitHulls = new[]
            {
                thisActionState.Environment.GameState.Units[0].Values.Sum(x => x.Hull),
                thisActionState.Environment.GameState.Units[1].Values.Sum(x => x.Hull)
            };
            this.depth = depth;
        }

        public MctsNode Parent { get; }

        public List<MctsNode> Children { get; }

        /// <summary>
        /// An object that stores the game state that corresponds to the node and
        /// the actions that were performed to get to that state.
        /// </summary>
        public ActionStatePair CurrentActionState { get; }

        /// <summary>
        /// The number of times the node has been visited by the algorithm.
        /// </summary>
        public int VisitedCount { get; set; }

        /// <summary>
        /// Sum of scores of playouts that happened after this node was traversed.
        /// </summary>
        public virtual double Value { get; set; }

        public IEnumerable<ActionStatePair> ChildActions { get; }

        /// <summary>
        /// An enumerator of the ChildActions enumerable.
        /// </summary>
        protected readonly IEnumerator<ActionStatePair> _currentActionEnum;

        /// <summary>
        /// Indicates whether the enumerator has already been shifted by at least one position.
        /// Used to synchronise the TryGetNextChild and TryGetNextAction methods.
        /// </summary>
        protected bool _actionSwitched = false;

        public bool IsTerminal 
            => CurrentActionState.Environment.GameState.GetResult() != GameState.WINNER_NONE;

        public bool TryGetNextAction(out ActionStatePair action)
        {
            if (!_currentActionEnum.MoveNext())
            {
                action = null;
                return false;
            }
            else
            {
                action = _currentActionEnum.Current;
                _actionSwitched = true;
                return true;
            }
        }

        public virtual bool TryGetNextChild(IActionGenerator generator, out MctsNode child)
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

                    child = new MctsNode(this, playerAction, generator.EnumerateActions(playerAction.Environment), depth + 1);
                    playerAction.Environment.GameState.NextTurn();
                    Children.Add(child);
                    return true;
                }
            }
            else
            {
                _actionSwitched = false;
                ActionStatePair playerAction = _currentActionEnum.Current;

                child = new MctsNode(this, playerAction, generator.EnumerateActions(playerAction.Environment), depth + 1);
                playerAction.Environment.GameState.NextTurn();
                Children.Add(child);
                return true;
            }
        }
    }
}
