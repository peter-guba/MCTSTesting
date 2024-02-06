using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Xml.Schema;
using CMS.Benchmark.Exceptions;

namespace CMS.Benchmark.Config
{
    /// <summary>
    /// Factory class which fills and creates an environment.
    /// </summary>
    internal static class EnvironmentFactory
    {
        private static readonly string _envDbDir = "../../../../../../Resources/Environments";
        private static readonly string _envSchemaFile = Path.Combine(_envDbDir, "Environment.xsd");
        private static readonly XmlSchemaSet _envSchema = new XmlSchemaSet();

        private static bool _isInitialized;

        public static void Initialize()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _envSchema.Add("", XmlReader.Create(new StreamReader(_envSchemaFile)));
            }
        }

        /// <summary>
        /// Returns a fully created <see cref="GameEnvironment"/> specified in file with name <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Name of the file with the environment config.</param>
        /// <param name="gameState">Initial game state for this environment.</param>
        /// <returns></returns>
        public static GameEnvironment MakeEnvironment(string id, GameState gameState)
        {
            Initialize();
            string docFile = Path.Combine(_envDbDir, id + ".xml");
            ResourceValidation.CheckResource(docFile);
            XDocument doc = XDocument.Load(docFile, LoadOptions.SetLineInfo);
            XmlValidation.ValidateDocument(doc, _envSchema, docFile);

            int radius = int.Parse(doc.Root.Elements("Radius").First().Value);
            
            var envMap = new EnvironmentHexMap();

            // Handle sun
            var sunHexes = GetSunHexes(doc);
            foreach (Hex sunHex in sunHexes)
            {
                envMap.Add(sunHex, HexType.Impassable);
            }

            // Handle asteroid belts
            XElement beltsNode = doc.Root.Elements("AsteroidBelts").FirstOrDefault();
            if (beltsNode != null)
            {
                foreach (var belt in beltsNode.Elements())
                {
                    var beltHexes = GetAsteroidBeltHexes(belt);
                    foreach (Hex beltHex in beltHexes)
                    {
                        envMap.Add(beltHex, HexType.DoubleCost);
                    }
                }
            }

            // Handle other hexes
            var others = doc.Root.Elements("Hexes");
            foreach (XElement otherHex in others)
            {
                var q = short.Parse(otherHex.Attributes("Q").First().Value);
                var r = short.Parse(otherHex.Attributes("R").First().Value);
                HexType type;
                if (!Enum.TryParse(otherHex.Attributes("Type").First().Value, out type))
                {
                    throw new InvalidHexTypeException(otherHex.Attributes("Type").First().Value);
                }
                envMap.Add(new Hex(q, r), type);
            }

            return new GameEnvironment(radius, gameState, envMap);
        }

        private static List<Hex> GetSunHexes(XDocument doc)
        {
            XElement sun = doc.Root.Elements("Sun").FirstOrDefault();
            if (sun == null)
            {
                return new List<Hex>();
            }

            short q = short.Parse(sun.Attributes("Q").First().Value);
            short r = short.Parse(sun.Attributes("R").First().Value);

            var sunHex = new Hex(q, r);

            var sunHexes = new List<Hex> {sunHex};

            foreach (Hex dir in Constants.HexDirections)
            {
                sunHexes.Add(sunHex + dir);
            }

            return sunHexes;
        }

        private static List<Hex> GetAsteroidBeltHexes(XElement beltNode)
        {
            var q = short.Parse(beltNode.Attributes("CenterQ").First().Value);
            var r = short.Parse(beltNode.Attributes("CenterR").First().Value);
            var center = new Hex(q, r);
            var radius = short.Parse(beltNode.Attributes("Radius").First().Value);
            var width = short.Parse(beltNode.Attributes("Width").First().Value);
            var density = double.Parse(beltNode.Attributes("Density").First().Value);
            var seed = int.Parse(beltNode.Attributes("Seed").First().Value);
            var rnd = new Random(seed);

            var asteroids = new List<Hex>();

            for (short i = radius; i < radius + width; i++)
            {
                var ring = center.GetRing(i);
                foreach (Hex hex in ring)
                {
                    if (rnd.NextDouble() < density)
                    {
                        asteroids.Add(hex);
                    }
                }
            }

            return asteroids;
        }
    }
}
