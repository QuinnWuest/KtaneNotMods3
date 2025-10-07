using System.Collections.Generic;

public class CheckerMove
{
    public CheckerCoordinate From;
    public CheckerCoordinate To;
    public List<CheckerCoordinate> CapturedPieces;

    public CheckerMove(CheckerCoordinate from, CheckerCoordinate to, List<CheckerCoordinate> capturedPieces = null)
    {
        From = from;
        To = to;
        CapturedPieces = capturedPieces ?? new List<CheckerCoordinate>();
    }

    public bool Equals(CheckerMove other)
    {
        return other != null && other.From.Equals(From) && other.To.Equals(To) && other.CapturedPieces.Equals(CapturedPieces);
    }

    public override string ToString()
    {
        return string.Format("({0} → {1})", From, To);
    }
}
