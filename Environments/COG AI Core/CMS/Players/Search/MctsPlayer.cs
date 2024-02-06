using System;
using System.Collections.Generic;
using CMS.Actions;
using CMS.Micro.Search.MCTS;

namespace CMS.Players.Search
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="Player" /> using the <see cref="Mcts" /> algorithm.
    /// </summary>
    /// <typeparam name="TSearch">Type of <see cref="Mcts"/> search to use.</typeparam>
    public class MctsPlayer<TSearch> : Player
        where TSearch : Mcts
    {
        private readonly TSearch _mcts;

        /// <param name="mcts"><see cref="TSearch"/> algorithm to use for action generation.</param>
        public MctsPlayer(TSearch mcts)
        {
            _mcts = mcts;
        }

        public override ICollection<ActionCms> MakeActions(GameEnvironment environment)
        {
            var actions = _mcts.GetActions(environment);
            foreach (ActionCms action in actions)
            {
                action.Execute(environment);
            }
            return actions;
        }

        public override string ToString()
        {
            return $"MCTS player, {_mcts}";
        }

        public override void SetBattleName(string name)
        {
            _mcts.SetBattleName(name);
        }

        public override void SetRndBattleString(string str)
        {
            _mcts.SetRndBattleString(str);
        }
    }
}