using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS
{
    /// <summary>
    /// Represents the full game environment where the game takes place.
    /// </summary>
    public class GameEnvironment
    {
        /// <summary>
        /// Dynamic game state in this environment.
        /// </summary>
        public GameState GameState { get; }
        /// <summary>
        /// Static environment map of this environment.
        /// </summary>
        public EnvironmentHexMap EnvironmentMap { get; }
        /// <summary>
        /// Radius of this environment.
        /// </summary>
        public int Radius { get; }

        public GameEnvironment(int radius, GameState state, EnvironmentHexMap envMap)
        {
            Radius = radius;
            GameState = state;
            EnvironmentMap = envMap;
            foreach (KeyValuePair<Hex, HexType> keyValuePair in EnvironmentMap)
            {
                GameState.DynamicEnvMap[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        /// <summary>
        /// Returns whether a given hex has valid coordinates in this <see cref="GameEnvironment"/>.
        /// </summary>
        public bool IsValid(Hex hex)
        {
            return
                hex.Q >= -Radius &&
                hex.Q <= Radius &&
                hex.R >= -Radius &&
                hex.R <= Radius &&
                -hex.Q - hex.R >= -Radius &&
                -hex.Q - hex.R <= Radius;
        }

        /// <summary>
        /// Returns a <see cref="HexType"/> at <paramref name="position"/> in this <see cref="GameEnvironment"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HexType GetHexTypeAt(Hex position)
        {
            HexType value;
            if (GameState.DynamicEnvMap.TryGetValue(position, out value))
                return value;
            return HexType.Empty;
        }

        /// <summary>
        /// Returns the environment at position <paramref name="hex"/> is passable or not.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public bool IsPassable(Hex hex)
        {
            return GetHexTypeAt(hex) != HexType.Impassable;
        }

        /// <summary>
        /// Returns the cost of moving through <paramref name="hex"/> considering only static environment (no units).
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public int GetStaticCost(Hex hex)
        {
            HexType value;
            if (!EnvironmentMap.TryGetValue(hex, out value))
                return 1;

            return GetCost(value);
        }

        /// <summary>
        /// Returns the cost of moving through <see cref="HexType"/> of given <paramref name="value"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetCost(HexType value)
        {
            switch (value)
            {
                case HexType.Empty:
                    return 1;
                case HexType.Impassable:
                    return int.MaxValue;
                case HexType.DoubleCost:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns new <see cref="GameEnvironment"/> instance where the <see cref="GameState"/> is a deep copy of this one.
        /// </summary>
        public GameEnvironment DeepCloneState()
        {
            return new GameEnvironment(Radius, GameState.DeepClone(), EnvironmentMap);
        }
    }
}
