using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Units;

namespace CMS
{
    /// <summary>
    /// Represents a dynamic game state.
    /// </summary>
    public class GameState : IDeepCloneable<GameState>
    {
        private const int PLAYER_COUNT = 2;
        public static readonly int DRAW = -1;
        public static readonly int WINNER_NONE = -2;

        public int ActivePlayer;
        public int OtherPlayer => 1 - ActivePlayer;

        public EnvironmentHexMap DynamicEnvMap;

        public HexMap<Unit>[] Units;

        /// <summary>
        /// Units of the currently active players.
        /// </summary>
        public HexMap<Unit> ActivePlayerUnits => Units[ActivePlayer];
        /// <summary>
        /// Units of the currently inactive player.
        /// </summary>
        public HexMap<Unit> OtherPlayerUnits => Units[Opo(ActivePlayer)];

        /// <summary>
        /// Returns a unit at given <paramref name="position"/>.
        /// </summary>
        /// <returns>Null if there is no unit at given <paramref name="position"/>.</returns>
        public Unit GetUnitAt(Hex position)
        {
            Unit unit;
            if (ActivePlayerUnits.TryGetValue(position, out unit))
                return unit;

            if (OtherPlayerUnits.TryGetValue(position, out unit))
                return unit;

            return null;
        }

        /// <summary>
        /// Returns a unit of the active player at given <paramref name="position"/>.
        /// </summary>
        /// <returns>Null if no units of the active player are at given <paramref name="position"/>.</returns>
        public Unit GetActivePUnitAt(Hex position)
        {
            Unit unit;
            if (ActivePlayerUnits.TryGetValue(position, out unit))
                return unit;

            return null;
        }

        /// <summary>
        /// Destroys unit at given <paramref name="position"/>.
        /// </summary>
        public void KillUnitAt(Hex position)
        {
            foreach (HexMap<Unit> map in Units)
            {
                if (map.Remove(position))
                    break;
            }
            DynamicEnvMap.Remove(position);
        }

        /// <summary>
        /// Instantly moves <paramref name="unit"/> from <see cref="Hex"/> <paramref name="from"/> to <see cref="Hex"/> <paramref name="to"/>.
        /// </summary>
        public void MoveUnit(Hex from, Hex to, Unit unit)
        {
            foreach (HexMap<Unit> map in Units)
            {
                if (map.Remove(from))
                {
                    map.Add(to, unit);
                    unit.Position = to;
                }
            }

            DynamicEnvMap.Remove(from);
            DynamicEnvMap[to] = HexType.Impassable;
        }

        public GameState(HexMap<Unit>[] units, int activePlayer)
        {
            Debug.Assert(units.Length == PLAYER_COUNT, "Different unit count expected");
            Units = units;
            ActivePlayer = activePlayer;

            DynamicEnvMap = new EnvironmentHexMap();
            foreach (var unitCol in units)
            {
                foreach (KeyValuePair<Hex, Unit> keyValuePair in unitCol)
                {
                    DynamicEnvMap[keyValuePair.Key] = HexType.Impassable;
                }
            }
        }

        /// <summary>
        /// Performs all actions necessary to advance the game one turn forward.
        /// </summary>
        public void NextTurn()
        {
            ActivePlayer = Opo(ActivePlayer);
            foreach (Unit unit in ActivePlayerUnits.Values)
            {
                unit.PrepareForNextTurn();
            }
        }

        /// <summary>
        /// Returns opponent's number of player number <paramref name="player"/>.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int Opo(int player)
        {
            //return (player + 1) % PLAYER_COUNT; // For PC != 2
            Debug.Assert(PLAYER_COUNT == 2);
            return 1 - player;
        }

        public GameState DeepClone()
        {
            var units = new HexMap<Unit>[Units.Length];

            for (int i = 0; i < units.Length; i++)
            {
                units[i] = Units[i].DeepClone();
            }

            return new GameState(units, ActivePlayer);
        }

        protected bool Equals(GameState other)
        {
            bool equal = Units.Length == other.Units.Length;
            for (int i = 0; i < PLAYER_COUNT; ++i)
            {
                equal &= Units[i].Count == other.Units[i].Count && !Units[i].Except(other.Units[i]).Any();
            }
            return equal && ActivePlayer == other.ActivePlayer;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameState) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ActivePlayer * 397) ^ (Units != null ? Units.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Returns a result of current game state.
        /// <para><see cref="WINNER_NONE"/> for non terminal states.</para>
        /// <para><see cref="DRAW"/> for draw states.</para>
        /// <para>Number of the winning player otherwise.</para>
        /// </summary>
        public int GetResult()
        {
            if (ActivePlayerUnits.Count == 0 &&
                OtherPlayerUnits.Count == 0)
            {
                return DRAW;
            }
            else
            {
                if (ActivePlayerUnits.Count == 0)
                    return Opo(ActivePlayer);
                else if (OtherPlayerUnits.Count == 0)
                    return ActivePlayer;
                else
                    return WINNER_NONE;
            }
        }
    }
}
