//#define USE_PFATHFINDING_CACHE
//#define USE_A_STAR_NODE_POOLING

using System;
using System.Collections.Generic;
using CMS.Collections;
using CMS.Units;
using CMS.Utility;
using Priority_Queue;

namespace CMS.Pathfinding
{
    /// <summary>
    /// A class taking care of pathfinding in the environment.
    /// </summary>
    public class Pathfinder
    {
        private static readonly int MAX_NODE_COUNT = 256;

        public delegate bool AStarStopCondition(Hex hex, Hex target, Unit unit, GameEnvironment env);

#if USE_PFATHFINDING_CACHE
        private static readonly Dictionary<EnvironmentHexMap, Dictionary<long, List<PathNode>>> _pathfindingCache
            = new Dictionary<EnvironmentHexMap, Dictionary<long, List<PathNode>>>();
#endif

        public static int CacheHitCount { get; set; }
        public static int TotalQueries { get; set; }

#if USE_A_STAR_NODE_POOLING
        private static readonly ObjectPool<AStarNode> _nodePool = new ObjectPool<AStarNode>(MAX_NODE_COUNT);
#endif

#if USE_PFATHFINDING_CACHE
        private Dictionary<long, List<PathNode>> _pathsCache;
#endif
        private readonly GameEnvironment _environment;

        /// <param name="environment"><see cref="GameEnvironment"/> where to perform the pathfinding.</param>
        public Pathfinder(GameEnvironment environment)
        {
#if USE_PFATHFINDING_CACHE
            _pathsCache = new Dictionary<long, List<PathNode>>();
#endif
            _environment = environment;
        }

#if USE_PFATHFINDING_CACHE
        /// <summary>
        /// Creates a unique hash code for a pair of hexes.
        /// </summary>
        private static long MakeKey(Hex start, Hex end)
        {
            return (long)start.GetHashCode() << 32 | (long)end.GetHashCode() & int.MaxValue;
        }
#endif

        /// <summary>
        /// Tries to find a path from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <param name="start">A <see cref="Hex"/> where the path should start.</param>
        /// <param name="end">A <see cref="Hex"/> where the path should end.</param>
        /// <param name="condition">Optional stop condition. If not specified, a default condition for full pathfinding is used.</param>
        /// <returns>A list of <see cref="PathNode"/> represening a path. An empty list is returned if a path does not exist.</returns>
        public IReadOnlyList<PathNode> FindPath(Hex start, Hex end, AStarStopCondition condition = null)
        {
            TotalQueries++;

#if USE_PFATHFINDING_CACHE
            Dictionary<long, List<PathNode>> cache;
            if (_pathfindingCache.TryGetValue(_environment.GameState.DynamicEnvMap, out cache))
            {
                _pathsCache = cache;
            }
            else
            {
                _pathfindingCache[_environment.GameState.DynamicEnvMap] = _pathsCache;
            }

            long key = MakeKey(start, end);
            List<PathNode> cachedPath;
            if (_pathsCache.TryGetValue(key, out cachedPath))
            {
                CacheHitCount++;
                return cachedPath;
            }
#endif

            var distance = start.GetDistance(end);
            if (distance == 0)
            {
                return new List<PathNode> {new PathNode(end, 0)};
            }

            var path = AStar(start, end, condition);

#if USE_PFATHFINDING_CACHE
            _pathsCache[key] = path;

            // Add sub paths to cache
            for (int i = 1; i < path.Count - 1; i++)
            {
                PathNode e = path[i];
                var k = MakeKey(start, e.Hex);
                _pathsCache[k] = path.GetRange(i, path.Count - i);
            }
#endif
            return path;
        }

        private bool IsPathValid(IReadOnlyList<PathNode> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                PathNode node = path[i];
                if (!_environment.IsPassable(node.Hex))
                {
                    return false;
                }
            }
            return true;
        }

        private List<PathNode> AStar(Hex start, Hex end, AStarStopCondition condition)
        {
            if (condition == null)
                condition = (hex, t, a, b) => hex == t;

            Unit unit = _environment.GameState.GetUnitAt(start);

            var frontier = new FastPriorityQueue<AStarNode>(MAX_NODE_COUNT);
            
#if USE_A_STAR_NODE_POOLING
            AStarNode startNode = _nodePool.Get();
            startNode.Hex = start;
#else
            var startNode = new AStarNode(start);
#endif
            frontier.Enqueue(startNode, 0.0f);

            var cameFrom = new Dictionary<Hex, Hex>(23) {[start] = start};
            var costSoFar = new Dictionary<Hex, int>(23) {[start] = 0};
            var hexToNode = new Dictionary<Hex, AStarNode>(23) {[start] = startNode};

            bool pathFound = false;
            Hex final = end;
            while (frontier.Count != 0)
            {
                AStarNode current = frontier.Dequeue();
                if (condition(current.Hex, end, unit, _environment))
                {
                    pathFound = true;
                    final = current.Hex;
#if USE_A_STAR_NODE_POOLING
                    _nodePool.Put(current);
#endif
                    break;
                }

                if (hexToNode.Count >= 128)
                {
                    // Path not found
#if USE_A_STAR_NODE_POOLING
                    _nodePool.Put(current);
#endif
                    break;
                }

                for (var i = 0; i < 6; i++)
                {
                    Hex dir = Constants.HexDirections[i];
                    Hex next = current.Hex + dir;
                    if (!_environment.IsValid(next))
                    {
                        continue;
                    }

                    HexType hexType = _environment.GetHexTypeAt(next);
                    if (hexType != HexType.Impassable)
                    {
                        var newCost = costSoFar[current.Hex] + _environment.GetCost(hexType);
                        int nextCostSoFar;
                        bool visited = costSoFar.TryGetValue(next, out nextCostSoFar);
                        if (!visited || newCost < nextCostSoFar)
                        {
                            var prio = newCost + next.GetDistance(end);
                            if (visited)
                            {
                                frontier.UpdatePriority(hexToNode[next], prio);
                            }
                            else
                            {
#if DEBUG
                                if (frontier.Count == frontier.MaxSize)
                                {
                                    frontier.Resize(frontier.MaxSize * 2);
                                    Logger.Log($"Resizing frontier to: {frontier.Count}");
                                }
#endif
#if USE_A_STAR_NODE_POOLING
                                AStarNode nextNode = _nodePool.Get();
                                nextNode.Hex = next;
#else
                                var nextNode = new AStarNode(next);
#endif
                                frontier.Enqueue(nextNode, prio);
                                hexToNode[next] = nextNode;
                            }
                            costSoFar[next] = newCost;
                            cameFrom[next] = current.Hex;
                        }
                    }
                }
#if USE_A_STAR_NODE_POOLING
                _nodePool.Put(current);
#endif
            }

#if USE_A_STAR_NODE_POOLING
            while (frontier.Count > 0)
            {
                AStarNode node = frontier.Dequeue();
                _nodePool.Put(node);
            }
#endif

            if (!pathFound)
            {
                return new List<PathNode>();
            }

            var path = new List<PathNode>();
            Hex curPathNode = final;
            do
            {
                path.Add(new PathNode(curPathNode, _environment.GetStaticCost(curPathNode)));
                curPathNode = cameFrom[curPathNode];
            } while (curPathNode != start);

            // Always add start node to distinguish from empty (one node) paths
            path.Add(new PathNode(curPathNode, _environment.GetStaticCost(curPathNode)));

            return path;
        }

        private class AStarNode : FastPriorityQueueNode, IPoolable
        {
            public Hex Hex { get; set; }

            public AStarNode()
            {
                Hex = Hex.ORIGIN;
            }

            public AStarNode(Hex hex)
            {
                Hex = hex;
            }

            public void Reset()
            {
                Hex = Hex.ORIGIN;
            }
        }
    }
}
