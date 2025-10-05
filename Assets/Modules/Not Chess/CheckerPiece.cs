using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerPiece
{
    public CheckerColor Color;
    public CheckerCoordinate Coordinate;

    public CheckerPiece(CheckerColor color, CheckerCoordinate coordinate)
    {
        Color = color;
        Coordinate = coordinate;
    }
}
