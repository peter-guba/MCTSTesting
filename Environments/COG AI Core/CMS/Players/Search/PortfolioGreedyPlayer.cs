using System;
using System.Collections.Generic;
using CMS.Actions;
using CMS.Micro.Search;

namespace CMS.Players.Search
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="Player" /> using the <see cref="PortfolioGreedySearch" /> algorithm.
    /// </summary>
    /// <typeparam name="TSearch">Type of <see cref="PortfolioGreedySearch"/></typeparam>
    public class PortfolioGreedyPlayer<TSearch>
        : Player
        where TSearch : PortfolioGreedySearch
    {
        private readonly TSearch _search;

        /// <param name="search"><see cref="TSearch"/> algorithm to use for action generation.</param>
        public PortfolioGreedyPlayer(TSearch search)
        {
            _search = search;
        }

        public override ICollection<ActionCms> MakeActions(GameEnvironment environment)
        {
            return _search.GetActions(environment, environment.GameState.ActivePlayer);
        }

        public override string ToString()
        {
            return $"PGS, {_search}";
        }

        public override void SetBattleName(string name)
        {
            throw new NotImplementedException();
        }

        public override void SetRndBattleString(string str)
        {
            throw new NotImplementedException();
        }
    }
}

