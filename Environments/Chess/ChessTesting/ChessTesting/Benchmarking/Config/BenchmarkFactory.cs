using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using ChessTesting;
using Benchmarking.Exceptions;
using ChessTesting.Core.AI;

namespace Benchmarking.Config
{
    /// <summary>
    /// Factory class which fills and creates a benchmark.
    /// </summary>
    internal static class BenchmarkFactory
    {
        private static readonly string baseFolder = "../../../../../../../";

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
            var searches = new List<ISearch>(2) {null, null};
            foreach (XElement playerXml in playersXml)
            {
                string date = "" + DateTime.Now.Day + '_' + DateTime.Now.Month + '_' + DateTime.Now.Year + '_' + DateTime.Now.Hour + '_' + DateTime.Now.Minute + '_' + DateTime.Now.Second + '-';
                ISearch player = MakeSearch(playerXml, date + id, out _);
                var playerIdx = int.Parse(playerXml.Attributes("Index").Single().Value);
                searches[playerIdx] = player;
            }

            // Handle max rounds
            int maxRounds = int.Parse(doc.Root.Elements("MaxRounds").First().Value);

            // Handle is symmetric
            bool isSymmetric = bool.Parse(doc.Root.Elements("IsSymmetric").First().Value);

            var benchmark = new Benchmark(battles, searches, maxRounds, isSymmetric, id);
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
                    battles.Add(new BattleSettings(repeats, battle.Attributes("Id").First().Value));
                }
                catch (ResourceMissingException rme)
                {
                    throw new InvalidResourceReferenceException(rme.Resource, docFile);
                }
            }

            return battles;
        }

        private static ISearch MakeSearch(XElement playerXml, string bmrkID, out string playerName)
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
                case "MixMcts":
                    return MakeMix(ai, bmrkID);
                default:
                    throw new ArgumentOutOfRangeException(nameof(playerXml));
            }
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

        private static ISearch MakeFapMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int numOfSegments = int.Parse(playerXml.Attributes("NumOfSegments").First().Value);
            bool expSeg = bool.Parse(playerXml.Attributes("ExponentialSegmentation").First().Value);
            bool expMul = bool.Parse(playerXml.Attributes("ExponentialMultiplication").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new FAPMCTS(maxPlayouts, maxTurns, "fap_mcts", bmrkID, numOfSegments, expSeg, expMul);
        }

        private static ISearch MakeBasicMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new BasicMCTS(maxPlayouts, maxTurns, "basic_mcts", bmrkID);
        }

        private static ISearch MakeMctsHpPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new MCTSHP(maxPlayouts, maxTurns, "mcts_hp", bmrkID);
        }

        private static ISearch MakeRQBonusMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            float k = float.Parse(playerXml.Attributes("K").First().Value);
            bool relEnabled = bool.Parse(playerXml.Attributes("RelativeBonusEnabled").First().Value);
            bool qualEnabled = bool.Parse(playerXml.Attributes("QualitativeBonusEnabled").First().Value);

            string name = "";
            if (relEnabled && qualEnabled)
            {
                name = "rq_bonus_mcts";
            }
            else if (relEnabled)
            {
                name = "r_bonus_mcts";
            }
            else if (qualEnabled)
            {
                name = "q_bonus_mcts";
            }

            return new RQBonusMCTS(maxPlayouts, maxTurns, name, bmrkID, k, relEnabled, qualEnabled);
        }

        private static ISearch MakeSigmoidMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            float constantK = float.Parse(playerXml.Attributes("K").First().Value);

            return new SigmoidMCTS(maxPlayouts, maxTurns, "sigmoid_mcts", bmrkID, constantK);
        }

        private static ISearch MakeSimpleRegretMctsPlayer(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            bool useEpsilonGreedy = bool.Parse(playerXml.Attributes("UseEpsilonGreedy").First().Value);
            float epsilon = float.Parse(playerXml.Attributes("Epsilon").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new SimpleRegretMCTS(maxPlayouts, maxTurns, "sr_cr_mcts", bmrkID, useEpsilonGreedy, epsilon);
        }

        private static ISearch MakeUcbTunedMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new UCBTunedMCTS(maxPlayouts, maxTurns, "ucb_tuned_mcts", bmrkID);
        }

        private static ISearch MakeVoiMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            return new VOIMCTS(maxPlayouts, maxTurns, "voi_mcts", bmrkID);
        }

        private static ISearch MakeWPMcts(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);
            float voiBase = float.Parse(playerXml.Attributes("VB").First().Value);
            float poeBase = float.Parse(playerXml.Attributes("PB").First().Value);
            bool normalize = bool.Parse(playerXml.Attributes("Normalize").First().Value);

            return new WPMCTS(maxPlayouts, maxTurns, "wp_mcts", bmrkID, voiBase, poeBase, normalize);
        }

        private static ISearch MakeMix(XElement playerXml, string bmrkID)
        {
            int maxPlayouts = int.Parse(playerXml.Attributes("MaxPlayouts").First().Value);
            int maxTurns = int.Parse(playerXml.Attributes("PlayoutRoundLimit").First().Value);

            string mctsName = "ChessTesting.Core.AI.TwoCombos.";
            string folderName = "mix_";
            IEnumerable<XElement> constituents = playerXml.Elements("Constituent");

            for (int i = 0; i < constituents.Count(); ++i)
            {
                mctsName += GetAbbreviation(constituents.ElementAt(i).Elements().Single()) + '_';
                folderName += GetFolderName(constituents.ElementAt(i).Elements().Single()) + '_';
            }

            mctsName += "MCTS";
            folderName += "mcts";

            List<object> parameters = new List<object>
            {
                maxPlayouts,
                maxTurns,
                folderName,
                bmrkID
            };

            for (int i = 0; i < constituents.Count(); ++i)
            {
                parameters.AddRange(GetParameters(constituents.ElementAt(i).Elements().Single()));
            }

            return (ISearch) Activator.CreateInstance(Type.GetType(mctsName), parameters.ToArray());
        }

        private static string GetAbbreviation(XElement ai)
        {
            switch (ai.Name.LocalName)
            {
                case "FapMcts":
                    return "FAP";
                case "MctsHP":
                    return "HP";
                case "RQBonusMcts":
                    return "RQBonus";
                case "SigmoidMcts":
                    return "Sigmoid";
                case "SimpleRegretMcts":
                    return "SimpleRegret";
                case "UcbTunedMcts":
                    return "UCBTuned";
                case "VoiMcts":
                    return "VOI";
                case "WPMcts":
                    return "WP";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ai));
            }
        }

        private static string GetFolderName(XElement ai)
        {
            switch (ai.Name.LocalName)
            {
                case "FapMcts":
                    return "fap";
                case "MctsHP":
                    return "hp";
                case "RQBonusMcts":
                    {
                        bool relEnabled = bool.Parse(ai.Attributes("RelativeBonusEnabled").First().Value);
                        bool qualEnabled = bool.Parse(ai.Attributes("QualitativeBonusEnabled").First().Value);

                        if (relEnabled && qualEnabled)
                        {
                            return "rq_bonus";
                        }
                        else if (relEnabled)
                        {
                            return "r_bonus";
                        }
                        else
                        {
                            return "q_bonus";
                        }
                    }
                case "SigmoidMcts":
                    return "sigmoid";
                case "SimpleRegretMcts":
                    return "sr_cr";
                case "UcbTunedMcts":
                    return "ucb_tuned";
                case "VoiMcts":
                    return "voi";
                case "WPMcts":
                    return "wp";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ai));
            }
        }

        private static object[] GetParameters(XElement ai)
        {
            switch (ai.Name.LocalName)
            {
                case "FapMcts":
                    return GetFAPParameters(ai);
                case "MctsHP":
                    return GetHPParameters(ai);
                case "RQBonusMcts":
                    return GetRQBParameters(ai);
                case "SigmoidMcts":
                    return GetSigParameters(ai);
                case "SimpleRegretMcts":
                    return GetSRCRParameters(ai);
                case "UcbTunedMcts":
                    return GetUCBTParameters(ai);
                case "VoiMcts":
                    return GetVOIParameters(ai);
                case "WPMcts":
                    return GetWPParameters(ai);
                default:
                    throw new ArgumentOutOfRangeException(nameof(ai));
            }
        }

        private static object[] GetFAPParameters(XElement ai)
        {
            int numOfSegments = int.Parse(ai.Attributes("NumOfSegments").First().Value);
            bool expSeg = bool.Parse(ai.Attributes("ExponentialSegmentation").First().Value);
            bool expMul = bool.Parse(ai.Attributes("ExponentialMultiplication").First().Value);

            return new object[] { numOfSegments, expSeg, expMul};
        }

        private static object[] GetHPParameters(XElement ai)
        {
            return new object[] { };
        }

        private static object[] GetRQBParameters(XElement ai)
        {
            float k = float.Parse(ai.Attributes("K").First().Value);
            bool relEnabled = bool.Parse(ai.Attributes("RelativeBonusEnabled").First().Value);
            bool qualEnabled = bool.Parse(ai.Attributes("QualitativeBonusEnabled").First().Value);

            return new object[] { k, relEnabled, qualEnabled };
        }

        private static object[] GetSigParameters(XElement ai)
        {
            float constantK = float.Parse(ai.Attributes("K").First().Value);

            return new object[] { constantK };
        }

        private static object[] GetSRCRParameters(XElement ai)
        {
            bool useEpsilonGreedy = bool.Parse(ai.Attributes("UseEpsilonGreedy").First().Value);
            float epsilon = float.Parse(ai.Attributes("Epsilon").First().Value);

            return new object[] { useEpsilonGreedy, epsilon };
        }

        private static object[] GetUCBTParameters(XElement ai)
        {
            return new object[] { };
        }

        private static object[] GetVOIParameters(XElement ai)
        {
            return new object[] { };
        }

        private static object[] GetWPParameters(XElement ai)
        {
            float voiBase = float.Parse(ai.Attributes("VB").First().Value);
            float poeBase = float.Parse(ai.Attributes("PB").First().Value);
            bool normalize = bool.Parse(ai.Attributes("Normalize").First().Value);

            return new object[] { voiBase, poeBase, normalize };
        }
    }
}
