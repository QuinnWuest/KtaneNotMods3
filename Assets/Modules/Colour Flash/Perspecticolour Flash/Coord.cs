using System;

public partial class PerspecticolourFlashScript
{
    public struct Coord : IEquatable<Coord>
    {
        public int X;
        public int Y;
        public int Z;

        public Coord(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public bool Equals(Coord other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
        public override bool Equals(object obj)
        {
            return obj is Coord && Equals((Coord)obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + X;
                h = h * 31 + Y;
                h = h * 31 + Z;
                return h;
            }
        }
        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }
    }
}