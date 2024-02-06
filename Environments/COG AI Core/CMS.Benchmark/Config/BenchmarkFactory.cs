using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CMS.ActionGenerators;
using CMS.Benchmark.Exceptions;
using CMS.Micro.Scripts;
using CMS.Micro.Search;
using CMS.Micro.Search.MCTS;
using CMS.Players;
using CMS.Players.Script;
using CMS.Players.Search;

namespace CMS.Benchmark.Config
{
    /// <summary>
    /// Factory class which fills and creates a benchmark.
    /// </summary>
    internal static class BenchmarkFactory
    {
        private static readonly string baseFolder = "../../../../../../";

        private static readonly string _benchmarkDbDir = "Resources/Benchmarks";
        private static readonly string _benchmarkSetDir = "Resources/BenchmarkSets";
        private static readonly string _battleSetDbDir = "Resources/BattleSets";
        private static readonly string _aiDbDir = "Resources/AIs";

        private static readonly string _benchmarkSchemaFile = Path.Combine(baseFolder, _benchmarkDbDir, "Benchmark.xsd");
        private static readonly string _benchmarkSetSchemaFile = Path.Combine(baseFolder, _benchmarkSetDir, "BenchmarkSet.xsd");
        private static readonly string _battleSetSchemaFile = Path.Combine(baseFolder, _battleSetDbDir, "BattleSet.xsd");
        private static readonly string _aiDbSchemaFile = Path.Combine(baseFolder, _aiDbDir, "AI.xsd");

        private static readonly XmlSchemaSet _benchmarkSchema = new XmlSchemaSet();
        private static readonly XmlSchemaSet _benchmarkSetSchema = new XmlSchemaSet();
        private static readonly XmlSchemaSet _battleSetSchema = new XmlSchemaSet();
        private static readonly XmlSchemaSet _aiSchemaSet = new XmlSchemaSet();

        private static bool _isInitialized;

        private static void Initialize()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _benchmarkSchema.Add("", XmlReader.Create(new StreamReader(_benchmarkSchemaFile)));

                _benchmarkSchema.Add(XmlValidation.TypeSchema);
                _benchmarkSetSchema.Add("", XmlReader.Create(new StreamReader(_benchmarkSetSchemaFile)));

                _battleSetSchema.Add("", XmlReader.Create(new StreamReader(_battleSetSchemaFile)));

                _aiSchemaSet.Add("", XmlReader.Create(new StreamReader(_aiDbSchemaFile)));
                _aiSchemaSet.Add(XmlValidation.TypeSchema);
            }
        }

        /// <summary>
        /// Creates and returns fully initialized benchmark specified in file with name <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Name of the file with the battle config.</param>
        public static List<Benchmark> MakeBenchmarkSet(string id)
        {
            Initialize();
            string docFile = Path.Combine(baseFolder, _benchmarkSetDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile, LoadOptions.SetLineInfo);
            XmlValidation.ValidateDocument(doc, _benchmarkSetSchema, docFile);

            IEnumerable<XElement> benchmarksXml = doc.Root.Elements("Benchmark");
            var benchmarks = new List<Benchmark>();
            foreach (XElement benchmarkXml in benchmarksXml)
            {
                try
                {
                    benchmarks.Add(MakeBenchmark(benchmarkXml.Attributes("Id").First().Value));
                }
                catch (ResourceMissingException rme)
                {
                    throw new InvalidResourceReferenceException(rme.Resource, docFile);
                }
            }

            return benchmarks;
        }

        private static Benchmark MakeBenchmark(string id)
        {
            string docFile = Path.Combine(baseFolder, _benchmarkDbDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile, LoadOptions.SetLineInfo);
            XmlValidation.ValidateDocument(doc, _benchmarkSchema, docFile);

            // Repeats
            int repeats = int.Parse(doc.Root.Elements("Repeats").First().Value);

            // Handle battles
            var battleSetsXml = doc.Root.Elements("BattleSet");
            var battles = new List<BattleSettings>();
            foreach (XElement battleSet in battleSetsXml)
            {
                try
                {
                    battles.AddRange(MakeBattles(battleSet.Attributes("Id").First().Value, repeats));
                }
                catch (ResourceMissingException rme)
                {
                    throw new InvalidResourceReferenceException(rme.Resource, docFile);
                }
            }

            // Handle players
            var playersXml = doc.Root.Elements("Player");
            var players = new List<Player>(2) {null, null};
            foreach (XElement playerXml in playersXml)
            {
                string date = "" + DateTime.Now.Day + '_' + DateTime.Now.Month + '_' + DateTime.Now.Year + '_' + DateTime.Now.Hour + '_' + DateTime.Now.Minute + '_' + DateTime.Now.Second + '-';
                Player player = MakePlayer(playerXml, date + id, out _);
                var playerIdx = int.Parse(playerXml.Attributes("Index").Single().Value);
                players[playerIdx] = player;
            }

            // Handle max rounds
            int maxRounds = int.Parse(doc.Root.Elements("MaxRounds").First().Value);

            // Handle is symmetric
            bool isSymmetric = bool.Parse(doc.Root.Elements("IsSymmetric").First().Value);

            var benchmark = new Benchmark(battles, players, maxRounds, isSymmetric, id);
            return benchmark;
        }

        private static List<BattleSettings> MakeBattles(string id, int repeats)
        {
            string docFile = Path.Combine(baseFolder, _battleSetDbDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile, LoadOptions.SetLineInfo);
            XmlValidation.ValidateDocument(doc, _battleSetSchema, docFile);

            IEnumerable<XElement> battlesXml = doc.Root.Elements("Battle");
            var battles = new List<BattleSettings>();
            foreach (XElement battle in battlesXml)
            {
                try
                {
                    battles.Add(BattleFactory.MakeBattle(battle.Attributes("Id").First().Value, repeats));
                }
                catch (ResourceMissingException rme)
                {
                    throw new InvalidResourceReferenceException(rme.Resource, docFile);
                }
            }

            return battles;
        }

        private static Player MakePlayer(XElement playerXml, string bmrkID, out string playerName)
        {
            XElement ai;
            XElement aiElement = playerXml.Elements("AI").SingleOrDefault();
            if (aiElement != null)
            {
                ai = aiElement.Elements().Single();
                playerName = ai.Name.LocalName;
            }
            else // We have reference to AI
            {
                ai = ResolveAIRef(playerXml.Elements("AIRef").Single());
                playerName = playerXml.Elements("AIRef").Single().Attributes("Id").Single().Value;
            }
            string name = ai.Name.LocalName;
            switch (name)
            {
                case "NOKAV":
                    return new ScriptedPlayer<NOKAV>();
                case "Kiter":
                    return new ScriptedPlayer<Kiter>();
                case "KiterSimple":
                    //return new ScriptedPlayer<KiterSimple>();
                    throw new InvalidEnumArgumentException($"{name} is not supported yet.");
                case "NOKAVSimple":
                    //return new ScriptedPlayer<NOKAVSimple>();
                    throw new InvalidEnumArgumentException($"{name} is not supported yet.");
                case "PortfolioGreedySearch":
                    return MakePGSPlayer(ai);
                case "FapMcts":
                    return MakeFapMctsPlayer(ai, bmrkID);
                case "Mcts":
                    return MakeBasicMctsPlayer(ai, bmrkID);
                case "MctsHP":
                    return MakeMctsHpPlayer(ai, bmrkID);
                case "RQBonusMcts":
                    return MakeRQBonusMcts(ai, bmrkID);
                case "SigmoidMcts":
                    return MakeSigmoidMctsPlayer(ai, bmrkID);
                case "SimpleRegretMcts":
                    return MakeSimpleRegretMctsPlayer(ai, bmrkID);
                case "UcbTunedMcts":
                    return MakeUcbTunedMcts(ai, bmrkID);
                case "VoiMcts":
                    return MakeVoiMcts(ai, bmrkID);
                case "WPMcts":
                    return MakeWPMcts(ai, bmrkID);
                case "RandomScriptSelector":
                    return MakeRandomScriptSelectorPlayer(ai);
                default:
                    throw new ArgumentOutOfRangeException(nameof(playerXml));
            }
        }

        private static Player MakeRandomScriptSelectorPlayer(XElement playerXml)
        {
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            return new RandomScriptSelectPlayer(portfolio);
        }

        private static XElement ResolveAIRef(XElement aiRefXml)
        {
            string aiId = aiRefXml.Attributes("Id").Single().Value;
            return MakeAi(aiId);
        }

        private static XElement MakeAi(string id)
        {
            string docFile = Path.Combine(baseFolder, _aiDbDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile);
            XmlValidation.ValidateDocument(doc, _aiSchemaSet, docFile);

            XElement ai = doc.Root.Elements().Single();
            return ai;
        }

        private static Player MakePGSPlayer(XElement playerXml)
        {
            int imprCount = int.Parse(playerXml.Attributes("ImproveCount").First().Value);
            int responseCount = int.Parse(playerXml.Attributes("ResponseCount").First().Value);
            IScript defaultScript = MakeScript(playerXml.Attributes("DefaultScript").First().Value);
            int timeLimit = int.Parse(playerXml.Attributes("TimeLimit").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var search = new PortfolioGreedySearch(portfolio, imprCount, responseCount, defaultScript, timeLimit, maxTurns);
            return new PortfolioGreedyPlayer<PortfolioGreedySearch>(search);
        }

        private static Player MakeWPMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            double voiBase = double.Parse(playerXml.Attributes("VB").First().Value);
            double poeBase = double.Parse(playerXml.Attributes("PB").First().Value);
            bool normalize = bool.Parse(playerXml.Attributes("Normalize").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new WPMcts(actionGenerator, maxPlayouts, players, voiBase, poeBase, normalize, bmrkID, maxTurns);
            return new MctsPlayer<WPMcts>(search);
        }

        private static Player MakeFapMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int numOfSegments = int.Parse(playerXml.Attributes("NumOfSegments").First().Value);
            bool expSeg = bool.Parse(playerXml.Attributes("ExponentialSegmentation").First().Value);
            bool expMul = bool.Parse(playerXml.Attributes("ExponentialMultiplication").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new FapMcts(actionGenerator, maxPlayouts, numOfSegments, players, expSeg, expMul, bmrkID, maxTurns);
            return new MctsPlayer<FapMcts>(search);
        }

        private static Player MakeBasicMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new Mcts(actionGenerator, maxPlayouts, players, bmrkID, maxTurns);
            return new MctsPlayer<Mcts>(search);
        }

        private static Player MakeMctsHpPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new MctsHP(actionGenerator, maxPlayouts, players, bmrkID, maxTurns);
            return new MctsPlayer<MctsHP>(search);
        }

        private static Player MakeRQBonusMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            double k = double.Parse(playerXml.Attributes("K").First().Value);
            bool relEnabled = bool.Parse(playerXml.Attributes("RelativeBonusEnabled").First().Value);
            bool qualEnabled = bool.Parse(playerXml.Attributes("QualitativeBonusEnabled").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new RQBonusMcts(actionGenerator, maxPlayouts, players, k, bmrkID, maxTurns, relEnabled, qualEnabled);
            return new MctsPlayer<RQBonusMcts>(search);
        }

        private static Player MakeSigmoidMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            double constantK = double.Parse(playerXml.Attributes("K").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new SigmoidMcts(actionGenerator, maxPlayouts, players, constantK, bmrkID, maxTurns);
            return new MctsPlayer<SigmoidMcts>(search);
        }

        private static Player MakeSimpleRegretMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            bool useEpsilonGreedy = bool.Parse(playerXml.Attributes("UseEpsilonGreedy").First().Value);
            double epsilon = double.Parse(playerXml.Attributes("Epsilon").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new SimpleRegretMcts(actionGenerator, maxPlayouts, players, useEpsilonGreedy, bmrkID, epsilon, maxTurns);
            return new MctsPlayer<SimpleRegretMcts>(search);
        }

        private static Player MakeUcbTunedMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new UcbTunedMcts(actionGenerator, maxPlayouts, players, bmrkID, maxTurns);
            return new MctsPlayer<UcbTunedMcts>(search);
        }

        private static Player MakeVoiMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            IEnumerable<XElement> portfolioXml = playerXml.Elements("Script");
            var portfolio = new List<IScript>();
            foreach (XElement script in portfolioXml)
            {
                portfolio.Add(MakeScript(script.Value));
            }

            var players = new List<Player>
            {
                new RandomScriptSelectPlayer(portfolio),
                new RandomScriptSelectPlayer(portfolio)
            };
            var actionGenerator = new ScriptActionGenerator(portfolio);
            var search = new VoiMcts(actionGenerator, maxPlayouts, players, bmrkID, maxTurns);
            return new MctsPlayer<VoiMcts>(search);
        }

        private static IScript MakeScript(string scriptName)
        {
            switch (scriptName)
            {
                case "NOKAV":
                    return new NOKAV();
                case "Kiter":
                    return new Kiter();
                case "KiterSimple":
                    //return new KiterSimple();
                    throw new InvalidEnumArgumentException($"{scriptName} is not supported yet.");
                case "NOKAVSimple":
                    //return new NOKAVSimple();
                    throw new InvalidEnumArgumentException($"{scriptName} is not supported yet.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scriptName));
            }
        }
    }
}
