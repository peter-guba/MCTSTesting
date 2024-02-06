using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CMS;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.Micro.Scripts;
using CMS.Micro.Search;
using CMS.Micro.Search.MCTS;
using CMS.Pathfinding;
using CMS.Players;
using CMS.Players.Script;
using CMS.Players.Search;
using CMS.Units;
using CMS.Utility;
using EmptyKeys.Strategy.AI.Components;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;
using EmptyKeys.Strategy.Units;
using EmptyKeys.Strategy.Units.Tasks;
using EmptyKeys.UserInterface.Input;
using CMSPathNode = CMS.Pathfinding.PathNode;
using PlayerCMS = CMS.Players.Player;

namespace EmptyKeys.Strategy.AI.Micro
{
    public class ExecuteMicro : BehaviorComponentBase
    {
        public ExecuteMicro()
        {
            if (File.Exists(Mcts.path + "data.txt"))
            {
                File.Delete(Mcts.path + "data.txt");
            }
        }

        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var playerContext = context as PlayerBehaviorContext;
            if (playerContext == null)
                return returnCode = BehaviorReturnCode.Failure;

            // Setup
            // Units
            var moveableUnits = playerContext.Player.Units.OfType<MoveableUnit>().Where(u => u.Environment is StarSystem && u.Environment.Name == "Skirmish").ToList();
            if (moveableUnits.Count == 0)
                return returnCode = BehaviorReturnCode.Failure;

            BaseEnvironment unitEnvi = moveableUnits.First().Environment;
            GameState gameState = EnvironmentFactory.MakeGameState(unitEnvi, moveableUnits, playerContext);

            // Environment
            var envMap = EnvironmentFactory.MakeEnvironment(unitEnvi);
            var gameEnvi = new GameEnvironment(unitEnvi.MapRadius, gameState, envMap);

            //SerializeUnits(gameEnvi.GameState.ActivePlayerUnits.Values);
            //SerializeUnits(gameEnvi.GameState.OtherPlayerUnits.Values);

            // Simulate/get action
            //var player = new RandomPlayer(new SimpleActionGenerator());
            //var player = new ScriptedPlayer<NOKAV>(null);
            PlayerCMS player;
            if (playerContext.Player.Color.B > 0)
            {
                //player = new ScriptedPlayer<NOKAV>();
                var portfolio = new List<IScript> { new NOKAV(), new Kiter() };
                var search = new PortfolioGreedySearch(portfolio, 2, 2, new NOKAV(), 200, 20);
                player = new PortfolioGreedyPlayer<PortfolioGreedySearch>(search);
                //player = new ScriptedPlayer<Kiter>();

                // MCTS
                /*var portfolio = new List<IScript> {new Kiter(), new NOKAV()};
                var search = new Mcts(
                    new ScriptActionGenerator(portfolio),
                    100,
                    new List<PlayerCMS>
                    {
                        new RandomScriptSelectPlayer(portfolio),
                        new RandomScriptSelectPlayer(portfolio)
                    },
                    "" + DateTime.Now.Day + '_' + DateTime.Now.Month + '_' + DateTime.Now.Year + '_' + DateTime.Now.Hour + '_' + DateTime.Now.Minute + '_' + DateTime.Now.Second + '-' + "testing",
                    1.0,
                    9999
                );
                player = new MctsPlayer<Mcts>(search);*/
            }
            else
            {
                var portfolio = new List<IScript> {new Kiter(), new NOKAV()};
                //var search = new PortfolioGreedySearch(portfolio, imprCount: 3, responseCount: 3, defaultScript: new Kiter(), timeLimit: 200, maxTurns: 20);
                //player = new PortfolioGreedyPlayer<PortfolioGreedySearch>(search);

                //player = new ScriptedPlayer<Kiter>();

                var search = new Mcts(
                    new ScriptActionGenerator(portfolio),
                    1000,
                    new List<PlayerCMS>
                    {
                        new RandomScriptSelectPlayer(portfolio),
                        new RandomScriptSelectPlayer(portfolio)
                    },
                    "" + DateTime.Now.Day + '_' + DateTime.Now.Month + '_' + DateTime.Now.Year + '_' + DateTime.Now.Hour + '_' + DateTime.Now.Minute + '_' + DateTime.Now.Second + '-' + "testing",
                    9999
                );

                player = new MctsPlayer<Mcts>(search);
            }
            var actions = player.MakeActions(gameEnvi);

            // Execute action if valid
            foreach (ActionCms action in actions)
            {
                if (!ExecuteAction(action, unitEnvi, playerContext
#if DEBUG || TRACE
                    , gameEnvi
#endif
                    ))
                {
                    Logger.Log($"Action failed: {action}");
                }
            }

            var mu = playerContext.Player.Units.OfType<MoveableUnit>().Where(u => u.Environment is StarSystem && u.Environment.Name == "Skirmish").ToList();
            GameState actualState = EnvironmentFactory.MakeGameState(unitEnvi, mu, playerContext);

            if (!CompareChanges(gameEnvi.GameState, actualState))
            {
                Logger.Log("WARNING: Our state and actual state do not match!");
            }
            if (actualState.OtherPlayerUnits.Except(gameEnvi.GameState.OtherPlayerUnits).Any())
            {
                Logger.Log("WARNING: Other player units do not match to actual state!");
            }
            if (actualState.ActivePlayerUnits.Except(gameEnvi.GameState.ActivePlayerUnits).Any())
            {
                Logger.Log("WARNING: Active player units do not match to actual state!");
            }

            return returnCode = BehaviorReturnCode.Success;
        }

        private static bool ExecuteAction(ActionCms action, BaseEnvironment unitEnvi, PlayerBehaviorContext playerContext
#if DEBUG || TRACE
            , GameEnvironment gameEnvi
#endif
            )
        {
            var ca = action as CompositeAction;
            if (ca != null)
            {
                var result = true;
                foreach (ActionCms innerAction in ca.Actions)
                {
                    var currentResult = ExecuteAction(innerAction, unitEnvi, playerContext
#if DEBUG || TRACE
                        , gameEnvi
#endif
                        );
                    result &= currentResult;
                }
                return result;
            }

            var moveableUnits = GetMoveableUnits(playerContext);
            GameState gameState = EnvironmentFactory.MakeGameState(unitEnvi, moveableUnits, playerContext);
            var envMap = EnvironmentFactory.MakeEnvironment(unitEnvi);
            var currentGameEnvi = new GameEnvironment(unitEnvi.MapRadius, gameState, envMap);

            Logger.Log($"Performing {action}");
            // Move action
            var ma = action as MoveAction;
            if (ma != null)
            {
                HexElement realUnit;
                if (unitEnvi.UnitsMap.TryGetValue(HexMap.CalculateKey(ma.Source.Q, ma.Source.R), out realUnit))
                {
                    var mUnit = realUnit as MoveableUnit;
                    if (mUnit == null)
                    {
                        Logger.Log($"ERROR: Unit is not moveable!");
                        return false;
                    }

                    var path = ma.GetPath(currentGameEnvi);

                    // Go one node at a time to traverse the exact same path as in CMS
                    for (var i = path.Count - 1; i >= 0; i--)
                    {
                        CMSPathNode pathNode = path[i];
                        mUnit.CalculatePath(unitEnvi, pathNode.Hex.Q, pathNode.Hex.R);

                        var task = new MoveTask(mUnit);
                        while (!task.IsTurnProcessFinished && !task.IsTaskFinished)
                        {
                            task.Execute();
                        }
                    }

                    if (action.Source.Q != mUnit.Q && action.Source.R != mUnit.R)
                    {
                        Logger.Log("WARNING: Our unit positions do not match");
                        Logger.Log($"Our unit end:  [{action.Source.Q}; {action.Source.R}]");
                        Logger.Log($"Real unit end: [{mUnit.Q}; {mUnit.R}]");
                    }
                }
                else
                {
                    Logger.Log($"ERROR: Unit not found!");
                    return false;
                }
                return true;
            }

            // Attack action
            var aa = action as AttackAction;
            if (aa != null)
            {
                HexElement realUnit;
                HexElement targetUnit;
                if (unitEnvi.UnitsMap.TryGetValue(HexMap.CalculateKey(aa.Source.Q, aa.Source.R), out realUnit) &&
                    unitEnvi.UnitsMap.TryGetValue(HexMap.CalculateKey(aa.Target.Q, aa.Target.R), out targetUnit))
                {
                    var sUnit = realUnit as StaticUnit;
                    var tUnit = targetUnit as BaseUnit;
                    if (sUnit == null || tUnit == null)
                        return false;

                    sUnit.Target = tUnit;
                    var task = new AttackTask(sUnit);
                    while (!task.IsTaskFinished)
                    {
                        task.Execute();
                    }
#if DEBUG || TRACE
                    Logger.Log($"INFO: Actual hull for [{tUnit.Q};{tUnit.R}] decreased to: {tUnit.Hull}");
#endif
                }

                return true;
            }

            return false;
        }

        private static List<MoveableUnit> GetMoveableUnits(PlayerBehaviorContext playerContext)
        {
            return playerContext.Player.Units.OfType<MoveableUnit>().Where(u => u.Environment is StarSystem && u.Environment.Name == "Skirmish").ToList();
        }

        private static bool CompareChanges(GameState myState, GameState actualState)
        {
            return myState.Equals(actualState);
        }

        private static void SerializeUnits(IEnumerable<Unit> units)
        {
            Directory.CreateDirectory("AI_Log/SerializedUnits/");
            var serializer = new XmlSerializer(typeof(Unit));
            foreach (Unit unit in units)
            {
                using (var sw = new StreamWriter($"AI_Log/SerializedUnits/{unit.GlobalKey}.xml"))
                {
                    using (var writer = new XmlTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, unit);
                    }
                }
            }
        }
    }
}
