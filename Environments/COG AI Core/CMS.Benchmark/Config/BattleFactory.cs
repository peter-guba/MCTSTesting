using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using CMS.Benchmark.Exceptions;
using CMS.Units;

namespace CMS.Benchmark.Config
{
    /// <summary>
    /// Factory class which fills and creates a battle.
    /// </summary>
    internal static class BattleFactory
    {
        private static readonly string baseFolder = "../../../../../../";

        private static readonly string _battleDbDir = "Resources/Battles";
        private static readonly string _unitDbDir = "Resources/Units";
        private static readonly Dictionary<string, Unit> _unitCache = new Dictionary<string, Unit>();
        private static int _unitGlobalKey;

        private static readonly string _battleSchemaFile = Path.Combine(baseFolder, _battleDbDir, "Battle.xsd");
        private static readonly XmlSchemaSet _battleSchema = new XmlSchemaSet();

        private static bool _isInitialized;

        private static void Initialize()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                ResourceValidation.CheckResource(_battleSchemaFile);
                _battleSchema.Add("", XmlReader.Create(new StreamReader(_battleSchemaFile)));
                _battleSchema.Add(XmlValidation.TypeSchema);
            }
        }

        /// <summary>
        /// Returns a fully created battle specified in file with name <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Name of the file with the battle config.</param>
        /// <param name="repeats">How many times should be the battle repeated.</param>
        public static BattleSettings MakeBattle(string id, int repeats)
        {
            Initialize();
            _unitGlobalKey = 1;
            string docFile = Path.Combine(baseFolder, _battleDbDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile, LoadOptions.SetLineInfo);
            XmlValidation.ValidateDocument(doc, _battleSchema, docFile);

            Dictionary<string, int>[] unitCounts = {new Dictionary<string, int>(), new Dictionary<string, int>()};
            try
            {
                GameState gameState = MakeGameState(doc, unitCounts);
                Debug.Assert(gameState.Units[0].Count > 0 || gameState.Units[1].Count > 0);

                string environmentId = doc.Root.Elements("Environment").First().Attributes("Id").First().Value;
                GameEnvironment environment = EnvironmentFactory.MakeEnvironment(environmentId, gameState);
                var battle = new BattleSettings(environment, repeats, id, unitCounts);
                return battle;
            }
            catch (ResourceMissingException rme)
            {
                throw new InvalidResourceReferenceException(rme.Resource, docFile);
            }
        }

        private static GameState MakeGameState(XDocument doc, IReadOnlyList<Dictionary<string, int>> unitCounts)
        {
            bool isSymmetric = doc.Elements("SymmetricBattle").Any();
            IEnumerable<XElement> players = doc.Root.Elements("Player");

            var units = new[] {new HexMap<Unit>(), new HexMap<Unit>()};

            foreach (XElement player in players)
            {
                int i = int.Parse(player.Attributes("Index").First().Value);
                var pUnits = player.Elements("Units").Single().Elements("Unit");
                foreach (XElement unitXml in pUnits)
                {
                    Unit unit = MakeUnit(unitXml, unitCounts[i]);
                    if (units[0].ContainsKey(unit.Position) || units[1].ContainsKey(unit.Position))
                    {
                        throw new InvalidXmlDataException("Multiple units on the same hex detected!");
                    }
                    units[i].Add(unit.Position, unit);
                }
            }

            if (isSymmetric)
            {
                // There is at least one player with at least one unit
                int i = int.Parse(players.First().Attributes("Index").First().Value);
                int other = 1 - i;
                foreach (Unit unit in units[i].Values)
                {
                    Unit symUnit = unit.DeepClone();
                    symUnit.Position = -unit.Position;
                    symUnit.GlobalKey = _unitGlobalKey++;
                    units[other].Add(symUnit.Position, symUnit);
                }
            }

            var gameState = new GameState(units, 0);
            return gameState;
        }

        private static Unit MakeUnit(XElement unitXml, IDictionary<string, int> unitCounts)
        {
            short q = short.Parse(unitXml.Attributes("Q").First().Value);
            short r = short.Parse(unitXml.Attributes("R").First().Value);
            var position = new Hex(q, r);

            string unitId = unitXml.Attributes("Id").First().Value;
            Unit unitTemplate;
            Unit unit;
            if (_unitCache.TryGetValue(unitId, out unitTemplate))
            {
                unit = unitTemplate.DeepClone();
            }
            else
            {
                var serializer = new XmlSerializer(typeof(Unit));
                string unitFile = Path.Combine(baseFolder, _unitDbDir, unitId + ".xml");
                ResourceValidation.CheckResource(unitFile);
                using (var sr = new StreamReader(unitFile))
                {
                    unit = (Unit)serializer.Deserialize(sr);
                }

                _unitCache.Add(unitId, unit);
                unit = unit.DeepClone();
            }
            if (!unitCounts.ContainsKey(unitId))
            {
                unitCounts[unitId] = 1;
            }
            else
            {
                unitCounts[unitId]++;
            }

            unit.Position = position;
            unit.GlobalKey = _unitGlobalKey++;
            return unit;
        }
    }
}
