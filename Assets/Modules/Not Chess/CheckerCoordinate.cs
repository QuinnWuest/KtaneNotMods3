public struct CheckerCoordinate
{
    public int X;
    public int Y;

    public CheckerCoordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(CheckerCoordinate other)
    {
        return other.X == X && other.Y == Y;
    }

    public override bool Equals(object obj)
    {
        return obj is CheckerCoordinate && Equals((CheckerCoordinate)obj);
    }

    public override int GetHashCode()
    {
        int hashCode = 1861411795;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        return string.Format("{0}{1}", "ABCDEF"[X], Y + 1);
    }
}