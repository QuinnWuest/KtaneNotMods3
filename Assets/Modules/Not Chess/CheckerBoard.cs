using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public class CheckerBoard
{
    public List<CheckerPiece> Pieces;
    public int Width;

    private static readonly CheckerCoordinate[] WhiteDirections = new CheckerCoordinate[]
    {
        new CheckerCoordinate(-1, 1),
        new CheckerCoordinate(1, 1)
    };
    private static readonly CheckerCoordinate[] BlackDirections = new CheckerCoordinate[]
    {
        new CheckerCoordinate(-1, -1),
        new CheckerCoordinate(1, -1)
    };

    public CheckerBoard(List<CheckerPiece> pieces, int width)
    {
        Pieces = pieces;
        Width = width;
    }

    public bool IsWithinBounds(CheckerCoordinate coord)
    {
        return coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Width;
    }

    public CheckerPiece GetPieceAt(CheckerCoordinate coord)
    {
        return Pieces.FirstOrDefault(p => p != null && p.Coordinate.Equals(coord));
    }

    public CheckerBoard ApplyMove(CheckerMove move)
    {

        var newPieces = new List<CheckerPiece>(
            Pieces.Select(p => p == null ? null : new CheckerPiece(p.Color, new CheckerCoordinate(p.Coordinate.X, p.Coordinate.Y), p.IsKing)
            ));

        var movingPiece = newPieces.FirstOrDefault(p => p != null && p.Coordinate.Equals(move.From));
        if (movingPiece == null)
            throw new InvalidOperationException("No piece found at " + move.From.X + "," + move.From.Y);

        if (move.CapturedPieces != null)
        {
            newPieces = newPieces
                .Where(p => p == null || !move.CapturedPieces.Any(c => c.Equals(p.Coordinate)))
                .ToList();
        }

        bool isKing = movingPiece.IsKing;
        var newCoord = new CheckerCoordinate(move.To.X, move.To.Y);

        if (!isKing)
        {
            if (movingPiece.Color == CheckerColor.White && newCoord.Y == Width - 1)
                isKing = true;
            else if (movingPiece.Color == CheckerColor.Black && newCoord.Y == 0)
                isKing = true;
        }

        newPieces.Remove(movingPiece);
        newPieces.Add(new CheckerPiece(movingPiece.Color, newCoord, isKing));

        return new CheckerBoard(newPieces, Width);
    }

    public List<List<CheckerMove>> GetAllValidMoveSequences(CheckerColor color)
    {
        var allSequences = new List<List<CheckerMove>>();
        bool anyJumpsExist = false;

        foreach(var piece in Pieces)
        {
            if (piece == null || piece.Color != color)
                continue;

            var jumps = GetJumpSequences(piece.Coordinate, piece.Color, piece.IsKing);
            if (jumps != null && jumps.Count > 0)
            {
                anyJumpsExist = true;
                allSequences.AddRange(jumps);
            }
        }

        if (anyJumpsExist)
            return allSequences;

        foreach (var piece in Pieces)
        {
            if (piece == null || piece.Color != color)
                continue;

            var directions =
                piece.IsKing ? WhiteDirections.Concat(BlackDirections) :
                piece.Color == CheckerColor.White ? WhiteDirections : BlackDirections;


            foreach (var dir in directions)
            {
                int newX = piece.Coordinate.X + dir.X;
                int newY = piece.Coordinate.Y + dir.Y;

                if (newX < 0 || newX >= Width || newY < 0 || newY >= Width)
                    continue;

                var targetCoord = new CheckerCoordinate(newX, newY);

                if (GetPieceAt(targetCoord) == null)
                {
                    var move = new CheckerMove(piece.Coordinate, targetCoord, new List<CheckerCoordinate>());
                    allSequences.Add(new List<CheckerMove> { move });
                }
            }
        }

        return allSequences;
    }

    private List<List<CheckerMove>> GetMoveSequencesForPiece(CheckerPiece piece)
    {
        var sequences = new List<List<CheckerMove>>();

        var moveDirections = piece.IsKing
            ? WhiteDirections.Concat(BlackDirections)
            : (piece.Color == CheckerColor.White ? WhiteDirections : BlackDirections);

        foreach (var dir in moveDirections)
        {
            var next = new CheckerCoordinate(piece.Coordinate.X + dir.X, piece.Coordinate.Y + dir.Y);
            if (!IsWithinBounds(next))
                continue;
            if (GetPieceAt(next) == null)
                sequences.Add(new List<CheckerMove> { new CheckerMove(piece.Coordinate, next) });
        }

        var jumpSequences = GetJumpSequences(piece.Coordinate, piece.Color, piece.IsKing);
        sequences.AddRange(jumpSequences);

        return sequences;
    }

    private List<List<CheckerMove>> GetJumpSequences(CheckerCoordinate start, CheckerColor color, bool isKing)
    {
        var sequences = new List<List<CheckerMove>>();

        var jumpDirections = isKing
            ? WhiteDirections.Concat(BlackDirections)
            : (color == CheckerColor.White ? WhiteDirections : BlackDirections);

        foreach (var dir in jumpDirections)
        {
            var mid = new CheckerCoordinate(start.X + dir.X, start.Y + dir.Y);
            var end = new CheckerCoordinate(start.X + dir.X * 2, start.Y + dir.Y * 2);

            if (!IsWithinBounds(end))
                continue;

            var midPiece = GetPieceAt(mid);
            if (midPiece != null && midPiece.Color != color && GetPieceAt(end) == null)
            {
                var captured = new List<CheckerCoordinate> { mid };
                var move = new CheckerMove(start, end, captured);

                var nextBoard = ApplyMove(move);

                bool nextIsKing = isKing;
                if (!nextIsKing)
                {
                    if (color == CheckerColor.White && end.Y == 0)
                        nextIsKing = true;
                    else if (color == CheckerColor.Black && end.Y == Width - 1)
                        nextIsKing = true;
                }

                var further = nextBoard.GetJumpSequences(end, color, nextIsKing);

                if (further.Any())
                {
                    foreach (var seq in further)
                    {
                        var fullSeq = new List<CheckerMove> { move };
                        fullSeq.AddRange(seq);
                        sequences.Add(fullSeq);
                    }
                }
                else
                {
                    sequences.Add(new List<CheckerMove> { move });
                }
            }
        }

        return sequences;
    }

    public List<List<CheckerMove>> GetMoveSequencesForPieceAt(CheckerCoordinate coord)
    {
        var piece = GetPieceAt(coord);
        if (piece == null) return new List<List<CheckerMove>>();

        return GetAllValidMoveSequences(piece.Color)
               .Where(seq => seq.First().From.Equals(coord))
               .ToList();
    }

    public CheckerBoard ApplyMoveSequence(List<CheckerCoordinate> coordinates, bool skipValidation)
    {
        var newBoard = this;

        List<CheckerMove> matchingSeq = null;
        if (!skipValidation)
        {
            var validSequences = newBoard.GetMoveSequencesForPieceAt(coordinates[0]);

            foreach (var seq in validSequences)
            {
                if (seq.Count + 1 < coordinates.Count)
                    continue;

                bool matches = true;
                for (int x = 0; x < coordinates.Count - 1; x++)
                {
                    if (!seq[x].From.Equals(coordinates[x]) || !seq[x].To.Equals(coordinates[x + 1]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    matchingSeq = seq;
                    break;
                }
            }

            if (matchingSeq == null)
                throw new InvalidOperationException("Input sequence does not match any valid move sequence.");
        }

        if (skipValidation)
        {
            for (int i = 1; i < coordinates.Count; i++)
            {
                var from = coordinates[i - 1];
                var to = coordinates[i];

                int dx = to.X - from.X;
                int dy = to.Y - from.Y;

                if (Math.Abs(dx) != Math.Abs(dy) || (Math.Abs(dx) != 1 && Math.Abs(dx) != 2))
                    throw new InvalidOperationException("Invalid step in coordinate sequence (not a diagonal step of length 1 or 2).");

                List<CheckerCoordinate> captured = new List<CheckerCoordinate>();

                if (Math.Abs(dx) == 2)
                {
                    var mid = new CheckerCoordinate(from.X + dx / 2, from.Y + dy / 2);

                    var midPiece = newBoard.GetPieceAt(mid);
                    if (midPiece != null)
                        captured.Add(mid);
                    else
                        throw new InvalidOperationException(string.Format("Expected captured piece at {0},{1} but none found.", mid.X, mid.Y));
                }
                var stepMove = new CheckerMove(from, to, captured);
                newBoard = newBoard.ApplyMove(stepMove);
            }
        }
        else
        {
            foreach (var move in matchingSeq)
                newBoard = newBoard.ApplyMove(move);
        }

        return newBoard;
    }

    public IEnumerable<CheckerCoordinate> GetPositionsOfPiecesOfColor(CheckerColor color)
    {
        for (int X = 0; X < Width; X++)
        {
            for (int Y = 0; Y < Width; Y++)
            {
                var coord = new CheckerCoordinate(X, Y);
                var piece = GetPieceAt(coord);
                if (piece != null && piece.Color == color)
                    yield return coord;
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int Y = Width - 1; Y >= 0; Y--)
        {
            sb.Append(string.Format("{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}\n", Y % 2 == 0 ? "▓" : "░", Y % 2 == 0 ? "░" : "▓"));
            for (int X = 0; X <= (Width - 1); X++)
            {
                var p = GetPieceAt(new CheckerCoordinate(X, Y));
                string shade = Y % 2 != X % 2 ? "░" : "▓";
                string str = p == null ? shade : p.Color == CheckerColor.White ? (p.IsKing ? "W" : "w") : (p.IsKing ? "B" : "b");
                sb.Append(shade + str + shade);
                sb.Append(X != Width - 1 ? "|" : "\n");
            }
            sb.Append(string.Format("{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}|{0}{0}{0}|{1}{1}{1}\n", Y % 2 == 0 ? "▓" : "░", Y % 2 == 0 ? "░" : "▓"));
            if (Y != 0)
                sb.Append("---+---+---+---+---+---\n");
        }
        return sb.ToString();
    }
}
