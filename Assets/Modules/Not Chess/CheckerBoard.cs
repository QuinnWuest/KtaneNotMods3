using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CheckerBoard
{
    public CheckerPiece[] Pieces;
    public int Width;

    public CheckerBoard(CheckerPiece[] pieces, int width)
    {
        Pieces = pieces;
        Width = width;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int Y = Width - 1; Y >= 0; Y--)
        {
            sb.Append(string.Format("{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}\n", Y % 2 == 0 ? "▓" : "░", Y % 2 == 0 ? "░" : "▓"));
            for (int X = 0; X <= (Width - 1); X++)
            {
                int pos = Y * Width + X;
                var shade = Y % 2 != X % 2 ? "░" : "▓";
                var p = Pieces[pos];
                CheckerColor? color;
                if (p == null)
                    color = null;
                else
                    color = p.Color;
                string str = color == CheckerColor.White ? "W" : color == CheckerColor.Black ? "B" : shade;
                sb.Append(string.Format("{1}{0}{1}", str, shade));
                if (X != (Width - 1))
                    sb.Append("|");
                else
                    sb.Append("\n");
            }
            sb.Append(string.Format("{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}\n", Y % 2 == 0 ? "▓" : "░", Y % 2 == 0 ? "░" : "▓"));
            if (Y != 0)
                sb.Append("---+---+---+---+---+---\n");
        }
        return sb.ToString();
    }


}
