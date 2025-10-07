public class CheckerPiece
{
    public CheckerColor Color;
    public CheckerCoordinate Coordinate;
    public bool IsKing;

    public CheckerPiece(CheckerColor color, CheckerCoordinate coordinate, bool isKing = false)
    {
        Color = color;
        Coordinate = coordinate;
        IsKing = isKing;
    }
}
