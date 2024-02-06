using System.Collections.Generic;
using System.Linq;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;

namespace EmptyKeys.Strategy.AI
{
    static class Helpers
    {
        public static IEnumerable<BaseEnvironment> GetOccupiedSystems(Player player)
        {
            var systems = new List<BaseEnvironment>();
            foreach (var envi in player.GameSession.Galaxy.EnvironmentMap.Values.OfType<BaseEnvironment>())
            {
                var maxInfluence = new KeyValuePair<Player, float>(null, float.MinValue);
                foreach (KeyValuePair<Player, float> item in envi.PlayersInfluence)
                {
                    if (maxInfluence.Value < item.Value)
                        maxInfluence = item;
                }
                if (maxInfluence.Key != null)
                    systems.Add(envi);
            }

            return systems;
        }

        public static IEnumerable<BaseEnvironment> GetEnemySystems(Player player)
        {
            var systems = new List<BaseEnvironment>();
            foreach (var envi in player.GameSession.Galaxy.EnvironmentMap.Values.OfType<BaseEnvironment>())
            {
                var maxInfluence = new KeyValuePair<Player, float>(null, float.MinValue);
                foreach (KeyValuePair<Player, float> item in envi.PlayersInfluence)
                {
                    if (maxInfluence.Value < item.Value)
                        maxInfluence = item;
                }
                if (maxInfluence.Key != null && maxInfluence.Key != player)
                    systems.Add(envi);
            }

            return systems;
        }

        public static IEnumerable<BaseEnvironment> GetOwnedSystems(Player player)
        {
            var systems = new List<BaseEnvironment>();
            foreach (var envi in player.GameSession.Galaxy.EnvironmentMap.Values.OfType<BaseEnvironment>())
            {
                var maxInfluence = new KeyValuePair<Player, float>(null, float.MinValue);
                foreach (KeyValuePair<Player, float> item in envi.PlayersInfluence)
                {
                    if (maxInfluence.Value < item.Value)
                        maxInfluence = item;
                }
                if (maxInfluence.Key == player)
                    systems.Add(envi);
            }

            return systems;
        }

        public static BaseEnvironment FindClosestEnemySystem(Player player, float distanceSystemUtilityCoefficient)
        {
            BaseEnvironment enemySystem = null;
            float maxUtility = float.MinValue;
            foreach (var elem in player.GameSession.Galaxy.EnvironmentMap.Values)
            {
                BaseEnvironment envi = elem as BaseEnvironment;
                if (envi == null || !player.ExploredEnvironments.Contains(envi.HexMapKey))
                {
                    continue;
                }

                int distance = HexMap.Distance(player.HomeStarSystem, envi);
                foreach (var item in envi.PlayersInfluence)
                {
                    if (item.Key == player)
                    {
                        continue;
                    }

                    if (!(player.IsAtWar(item.Key) || item.Key.IsAtWar(player)))
                    {
                        continue;
                    }

                    float utility = item.Value - distance * distanceSystemUtilityCoefficient;
                    if (maxUtility > utility)
                    {
                        continue;
                    }

                    maxUtility = utility;
                    enemySystem = envi;
                }
            }

            return enemySystem;
        }
    }
}
