using System;
using System.Collections;
using System.Collections.Generic;
using EmptyKeys.Strategy.Core;

namespace EmptyKeys.Strategy.AI.CoordinateSystem
{
    public static class HexHelpers
    {
        private struct Cube
        {
            public short X { get; }
            public short Y { get; }
            public short Z { get; }

            public Cube(short q, short r)
            {
                X = q;
                Z = r;
                Y = (short)(-q - r);
            }
            public Cube(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public HexElement ToHex()
            {
                return new HexElement() {Q = X, R = Z};
            }
        }

        private static Cube CubeRound(Tuple<float, float, float> h)
        {
            double rx = Math.Round(h.Item1);
            double ry = Math.Round(h.Item2);
            double rz = Math.Round(h.Item3);

            double xDiff = Math.Abs(rx - h.Item1);
            double yDiff = Math.Abs(ry - h.Item2);
            double zDiff = Math.Abs(rz - h.Item3);

            if (xDiff > yDiff && xDiff > zDiff)
                rx = -ry - rz;
            else if (yDiff > zDiff)
                ry = -rx - rz;
            else
                rz = -rx - ry;

            return new Cube((short)rx, (short)ry, (short)rz);
        }

        private static float Lerp(float a, float b, float t) 
            => a + (b - a) * t;

        private static Tuple<float, float, float> CubeLerp(Cube a, Cube b, float t)
        {
            return new Tuple<float, float, float>(
                Lerp(a.X, b.X, t),
                Lerp(a.Y, b.Y, t),
                Lerp(a.Z, b.Z, t));
        }

        private static IList<HexElement> DrawLine(Cube a, Cube b)
        {
            int n = HexMap.Distance(a.X, b.X, a.Z, b.Z);
            var results = new List<HexElement>();
            for (int i = 0; i < n; i++)
            {
                results.Add(CubeRound(CubeLerp(a, b, 1.0f / n * i)).ToHex());
            }
            return results;
        }

        public static IList<HexElement> DrawLine(short aQ, short aR, short bQ, short bR) 
            => DrawLine(new Cube(aQ, aR), new Cube(bQ, bR));
    }
}