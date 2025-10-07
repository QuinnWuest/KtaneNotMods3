using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotChessScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMBossModule Boss;

    public KMSelectable[] LetterSels;
    public KMSelectable[] NumberSels;
    public TextMesh DisplayText;
    public GameObject[] LedObjs;
    public Material[] LedMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _activated;

    private decimal _moduleCount;
    private string[] _ignoredModules;

    private const int _timerStart = 108;
    private Coroutine _countdownTimer;

    private static T[] NewArray<T>(params T[] array) { return array; }

    private static readonly CheckerBoard _initialCheckerBoard = new CheckerBoard(NewArray
    (
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(0, 0)), null,
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(2, 0)), null,
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(4, 0)), null,
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(1, 1)),
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(3, 1)),
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(5, 1)),
        null, null, null, null, null, null,
        null, null, null, null, null, null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(0, 4)), null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(2, 4)), null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(4, 4)), null,
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(1, 5)),
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(3, 5)),
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(5, 5))
    ), 6);
    private CheckerBoard _checkerBoard;

    private bool _expectingInput;
    private bool _expectingFinalInput;
    private bool _setReadyFlag;
    private bool _lockModule;

    private int? _inputtedLetter;
    private int? _inputtedNumber;

    private readonly List<CheckerCoordinate> _inputtedCoordinates = new List<CheckerCoordinate>();
    private List<CheckerCoordinate> _blacksLastMoves = new List<CheckerCoordinate>();
    private List<List<CheckerMove>> _movesForInputtedPiece = new List<List<CheckerMove>>();
    private readonly List<CheckerCoordinate> _finalInput = new List<CheckerCoordinate>();
    private CheckerCoordinate[] _finalAnswer = new CheckerCoordinate[2];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < LetterSels.Length; i++)
            LetterSels[i].OnInteract += LetterPress(i);

        for (int i = 0; i < NumberSels.Length; i++)
            NumberSels[i].OnInteract += NumberPress(i);

        _checkerBoard = _initialCheckerBoard;
        LogAllPossibleMovesForWhite(_checkerBoard);
        Module.OnActivate += Activate;
    }

    private void Activate()
    {
        if (_ignoredModules == null)
            _ignoredModules = Boss.GetIgnoredModules("Not Chess", new string[] { "Not Chess" });
        _moduleCount = BombInfo.GetSolvableModuleNames().Count(x => !_ignoredModules.Contains(x));
        _activated = true;
        StartCoroutine(ResetCountdown());
    }

    private KMSelectable.OnInteractHandler LetterPress(int btn)
    {
        return delegate ()
        {
            if (_moduleSolved || !_activated || _lockModule)
                return false;

            LetterSels[btn].AddInteractionPunch(0.5f);
            Audio.PlaySoundAtTransform("CHkey", LetterSels[btn].transform);

            if (!_expectingInput)
            {
                Debug.LogFormat("[Not Chess #{0}] Pressed {1} when input was not expected. Strike.", _moduleId, "ABCDEF"[btn]);
                Module.HandleStrike();
                return false;
            }

            if (_expectingFinalInput)
            {
                if (_inputtedLetter != null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, even though a letter has already been pressed. Strike.", _moduleId, "ABCDEF"[btn]);
                    _finalInput.Clear();
                    _inputtedLetter = null;
                    Module.HandleStrike();
                    return false;
                }

                if (btn != _finalAnswer[_finalInput.Count].X)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but {2} was expected. Strike.", _moduleId, "ABCDEF"[btn], "ABCDEF"[_finalAnswer[_finalInput.Count].X]);
                    _finalInput.Clear();
                    Module.HandleStrike();
                    return false;
                }

                _inputtedLetter = btn;

                return false;
            }

            if (_inputtedCoordinates.Count == 0)
            {
                if (_inputtedLetter != null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, even though a letter has already been pressed. Strike.", _moduleId, "ABCDEF"[btn]);
                    Module.HandleStrike();
                    return false;
                }

                if (!_checkerBoard.Pieces.Any(piece => piece != null && piece.Coordinate.X == btn && piece.Color == CheckerColor.White))
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but there are no white pieces in this column. Strike.", _moduleId, "ABCDEF"[btn]);
                    Module.HandleStrike();
                    return false;
                }

                var piecesWithinThisColumn = _checkerBoard.Pieces.Where(piece => piece != null && piece.Coordinate.X == btn && piece.Color == CheckerColor.White)
                    .Select(piece => piece.Coordinate).ToArray();

                if (!piecesWithinThisColumn.Any(piece => _checkerBoard.GetAllValidMoveSequences(CheckerColor.White).Any(seq => seq[0].From.Equals(piece))
                ))
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but no valid moves can be made from any white piece within this column. Strike.", _moduleId, "ABCDEF"[btn]);
                    Module.HandleStrike();
                    return false;
                }

                _inputtedLetter = btn;
            }
            else
            {
                if (_inputtedNumber != null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, even though a letter has already been pressed. Strike.", _moduleId, "ABCDEF"[btn]);
                    _movesForInputtedPiece.Clear();
                    _inputtedCoordinates.Clear();
                    Module.HandleStrike();
                    return false;
                }

                bool isNextStepValid = _movesForInputtedPiece.Any(seq =>
                {
                    if (_inputtedCoordinates.Count >= seq.Count + 1)
                        return false;

                    for (int i = 1; i < _inputtedCoordinates.Count; i++)
                    {
                        if (!seq[i - 1].To.Equals(_inputtedCoordinates[i]))
                            return false;
                    }

                    var nextMove = seq[_inputtedCoordinates.Count - 1];
                    return nextMove.To.X == btn;
                });

                if (!isNextStepValid)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but no moves from {2} can be moved to this column. Strike.", _moduleId, "ABCDEF"[btn], _inputtedCoordinates.Last());
                    _movesForInputtedPiece.Clear();
                    _inputtedCoordinates.Clear();
                    Module.HandleStrike();
                    return false;
                }
                _inputtedLetter = btn;
            }
            return false;
        };
    }

    private KMSelectable.OnInteractHandler NumberPress(int btn)
    {
        return delegate ()
        {
            if (_moduleSolved || !_activated || _lockModule)
                return false;

            NumberSels[btn].AddInteractionPunch(0.5f);
            Audio.PlaySoundAtTransform("CHkey", NumberSels[btn].transform);

            if (!_expectingInput)
            {
                Debug.LogFormat("[Not Chess #{0}] Pressed {1} when input was not expected. Strike.", _moduleId, btn + 1);
                Module.HandleStrike();
                return false;
            }

            if (_expectingFinalInput)
            {
                if (_inputtedLetter == null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1} without having pressed a letter button first. Strike.", _moduleId, btn + 1);
                    _finalInput.Clear();
                    Module.HandleStrike();
                    return false;
                }

                if (btn != _finalAnswer[_finalInput.Count].Y)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but {2} was expected. Strike.", _moduleId, btn + 1, _finalAnswer[_finalInput.Count].Y + 1);
                    _inputtedLetter = null;
                    _finalInput.Clear();
                    Module.HandleStrike();
                    return false;
                }

                _inputtedNumber = btn;
                var newCoord = new CheckerCoordinate(_inputtedLetter.Value, _inputtedNumber.Value);
                _inputtedLetter = null;
                _inputtedNumber = null;
                _finalInput.Add(newCoord);

                if (_finalInput.SequenceEqual(_finalAnswer))
                {
                    _lockModule = true;
                    if (_countdownTimer != null)
                        StopCoroutine(_countdownTimer);
                    StartCoroutine(SolveAnimation());
                    return false;
                }

                return false;
            }

            if (_inputtedCoordinates.Count == 0)
            {
                if (_inputtedLetter == null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1} without having pressed a letter button first. Strike.", _moduleId, btn + 1);
                    Module.HandleStrike();
                    return false;
                }

                _inputtedNumber = btn;
                var newCoord = new CheckerCoordinate(_inputtedLetter.Value, _inputtedNumber.Value);
                _inputtedLetter = null;
                _inputtedNumber = null;

                if (_checkerBoard.GetPieceAt(newCoord) == null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Attempted to start a move from {1}, but there is no piece there. Strike.", _moduleId, newCoord);
                    Module.HandleStrike();
                    return false;
                }

                if (_checkerBoard.GetPieceAt(newCoord).Color != CheckerColor.White)
                {
                    Debug.LogFormat("[Not Chess #{0}] Attempted to start a move from {1}, but there is a black piece there. Strike.", _moduleId, newCoord);
                    Module.HandleStrike();
                    return false;
                }

                var piece = _checkerBoard.GetPieceAt(newCoord);
                if (!_checkerBoard.GetAllValidMoveSequences(piece.Color).Any(seq => seq.First().From.Equals(piece.Coordinate)))
                {
                    Debug.LogFormat("[Not Chess #{0}] Attempted to start a move from {1}, but no valid moves can be made with this piece. Strike.", _moduleId, newCoord);
                    Module.HandleStrike();
                    return false;
                }

                _inputtedCoordinates.Add(newCoord);
                Debug.LogFormat("[Not Chess #{0}] Inputted starting coordinate: {1}", _moduleId, newCoord);

                _movesForInputtedPiece = _checkerBoard.GetMoveSequencesForPieceAt(piece.Coordinate);
                Debug.LogFormat("[Not Chess #{0}] Possible moves: {1}", _moduleId, _movesForInputtedPiece.Select(i => i.Join(" ")).Join("; "));
            }
            else
            {
                if (_inputtedLetter == null)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1} without having pressed a letter button first. Strike.", _moduleId, btn + 1);
                    _movesForInputtedPiece.Clear();
                    _inputtedCoordinates.Clear();
                    Module.HandleStrike();
                    return false;
                }

                _inputtedNumber = btn;
                var newCoord = new CheckerCoordinate(_inputtedLetter.Value, _inputtedNumber.Value);
                _inputtedLetter = null;
                _inputtedNumber = null;

                _inputtedCoordinates.Add(newCoord);
                var moveMade = new CheckerMove(_inputtedCoordinates[_inputtedCoordinates.Count - 2], _inputtedCoordinates[_inputtedCoordinates.Count - 1], null);

                bool isComplete = false;
                var validSequences = _checkerBoard.GetMoveSequencesForPieceAt(_inputtedCoordinates.First());
                bool isPartialValid = false;
                foreach (var seq in validSequences)
                {
                    if (_inputtedCoordinates.Count - 1 > seq.Count)
                        continue;

                    bool matches = true;
                    for (int i = 1; i < _inputtedCoordinates.Count; i++)
                    {
                        if (!seq[i - 1].To.Equals(_inputtedCoordinates[i]))
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        isPartialValid = true;
                        if (_inputtedCoordinates.Count - 1 == seq.Count)
                            isComplete = true;
                        moveMade.CapturedPieces = seq[_inputtedCoordinates.Count - 2].CapturedPieces;
                        break;
                    }
                }

                if (!isPartialValid)
                {
                    Debug.LogFormat("[Not Chess #{0}] Pressed {1}, but the move {2} to {3} is invalid. Strike.",
                        _moduleId, btn,
                        _inputtedCoordinates[_inputtedCoordinates.Count - 2],
                        _inputtedCoordinates[_inputtedCoordinates.Count - 1]
                    );
                    _inputtedCoordinates.Clear();
                    _movesForInputtedPiece.Clear();
                    Module.HandleStrike();
                    return false;
                }

                if (isComplete)
                {
                    var newBoard = _checkerBoard;
                    for (int i = 1; i < _inputtedCoordinates.Count; i++)
                    {
                        var stepMove = new CheckerMove(
                            _inputtedCoordinates[i - 1],
                            _inputtedCoordinates[i],
                            null
                        );

                        foreach (var seq in validSequences)
                        {
                            if (seq.Count >= i)
                            {
                                bool matches = true;
                                for (int j = 1; j <= i; j++)
                                {
                                    if (!seq[j - 1].To.Equals(_inputtedCoordinates[j]))
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                                if (matches)
                                {
                                    stepMove.CapturedPieces = seq[i - 1].CapturedPieces;
                                    break;
                                }
                            }
                        }
                        newBoard = newBoard.ApplyMove(stepMove);
                    }
                    _checkerBoard = newBoard;

                    Debug.LogFormat("[Not Chess #{0}] Moves made: ({1})", _moduleId, _inputtedCoordinates.Join(" → "));
                    if (_countdownTimer != null)
                        StopCoroutine(_countdownTimer);
                    Debug.LogFormat("[Not Chess #{0}] Board:\n{1}", _moduleId, _checkerBoard);

                    _inputtedCoordinates.Clear();

                    if (_setReadyFlag)
                    {
                        Debug.LogFormat("[Not Chess #{0}] The checkers game has ended early!", _moduleId);
                        _finalAnswer = CalculateFinalInput(_checkerBoard, null).ToArray();
                        _expectingInput = false;
                        StartCoroutine(ResetCountdown());
                        return false;
                    }

                    var blackSequences = _checkerBoard.GetAllValidMoveSequences(CheckerColor.Black);
                    if (blackSequences.Count == 0)
                    {
                        Debug.LogFormat("[Not Chess #{0}] Black has no more moves!", _moduleId);
                        _finalAnswer = CalculateFinalInput(_checkerBoard, null).ToArray();
                        _expectingInput = false;
                        StartCoroutine(ResetCountdown());
                        return false;
                    }

                    var randSequence = blackSequences.PickRandom();
                    _blacksLastMoves = new List<CheckerCoordinate> { randSequence[0].From };
                    foreach (var move in randSequence)
                        _blacksLastMoves.Add(move.To);

                    _checkerBoard = _checkerBoard.ApplyMoveSequence(_blacksLastMoves, true);

                    Debug.LogFormat("[Not Chess #{0}] Black has made the move: ({1})", _moduleId, _blacksLastMoves.Join(" → "));
                    Debug.LogFormat("[Not Chess #{0}] Board:\n{1}", _moduleId, _checkerBoard);

                    if (_checkerBoard.GetAllValidMoveSequences(CheckerColor.White).Count == 0)
                    {
                        Debug.LogFormat("[Not Chess #{0}] White has no more moves!", _moduleId);
                        _finalAnswer = CalculateFinalInput(_checkerBoard, _blacksLastMoves.Last()).ToArray();
                        return false;
                    }
                    _expectingInput = false;
                    LogAllPossibleMovesForWhite(_checkerBoard);
                    StartCoroutine(ResetCountdown());
                }
            }
            return false;
        };
    }

    private void LogAllPossibleMovesForWhite(CheckerBoard board)
    {
        var moves = board.GetAllValidMoveSequences(CheckerColor.White);
        Debug.LogFormat("[Not Chess #{0}] All possible moves for white: {1}", _moduleId, moves.Select(i => i.Join(" → ")).Join("; "));
    }

    private bool TryBuildMoveFromInput(List<CheckerCoordinate> input, out CheckerMove moveMade, out bool isComplete)
    {
        moveMade = null;
        isComplete = false;

        if (input.Count < 2)
            return false;

        moveMade = new CheckerMove(input[input.Count - 2], input[input.Count - 1], null);
        var validSequences = _checkerBoard.GetMoveSequencesForPieceAt(input[0]);
        bool isPartialValid = validSequences.Any(seq =>
        {
            if (input.Count - 1 > seq.Count) return false;
            for (int i = 1; i < input.Count; i++)
                if (!seq[i - 1].To.Equals(input[i]))
                    return false;
            return true;
        });

        if (!isPartialValid) return false;

        isComplete = validSequences.Any(seq =>
        {
            if (seq.Count != input.Count - 1) return false;
            for (int i = 1; i < input.Count; i++)
                if (!seq[i - 1].To.Equals(input[i]))
                    return false;
            return true;
        });

        var matchingSeq = validSequences.First(seq =>
            seq.Take(input.Count - 1)
               .Select(m => m.To)
               .SequenceEqual(input.Skip(1))
        );
        moveMade.CapturedPieces = matchingSeq[input.Count - 2].CapturedPieces;

        return true;
    }

    private CheckerCoordinate[] CalculateFinalInput(CheckerBoard board, CheckerCoordinate? lastBlackMove)
    {
        _expectingFinalInput = true;

        var whitePositions = board.Pieces.Where(p => p != null && p.Color == CheckerColor.White).Select(i => i.Coordinate).ToArray();
        var blackPositions = board.Pieces.Where(p => p != null && p.Color == CheckerColor.Black).Select(i => i.Coordinate).ToArray();

        var coords = new CheckerCoordinate[2];

        switch (whitePositions.Length)
        {
            case 0:
                {
                    // The location of the black checker which captured the last white piece.
                    coords[0] = lastBlackMove.Value;
                    break;
                }
            case 1:
                {
                    // If it is in rank 4 or below and there is a black checker, file d in the rank corresponding to the number of black checkers. Otherwise, d4.
                    coords[0] = new CheckerCoordinate(3, whitePositions[0].Y <= 4 && blackPositions.Length > 0 ? (blackPositions.Length - 1) : 3);
                    break;
                }
            case 2:
                {
                    // If there is one black checker diagonally touching both, its position. Otherwise, c5.
                    var blacksAdjToBothWhites = blackPositions.Where(p => IsDiagonalFrom(p, whitePositions[0]) && IsDiagonalFrom(p, whitePositions[1])).ToArray();
                    coords[0] = blacksAdjToBothWhites.Length == 1 ? blacksAdjToBothWhites[0] : new CheckerCoordinate(2, 4);
                    break;
                }
            case 3:
                {
                    // If there is exactly one kinged checker, its position. Otherwise, f2
                    var kingedPieces = board.Pieces.Where(p => p != null && p.IsKing).Select(p => p.Coordinate).ToArray();
                    if (kingedPieces.Length == 1)
                        coords[0] = kingedPieces[0];
                    else
                        coords[0] = new CheckerCoordinate(5, 1);
                    break;
                }
            case 4:
                {
                    // The square with the same rank and file which has no checkers on it. If none or multiple, e5.
                    var files = Enumerable.Range(0, 6).Select(a => Enumerable.Range(0, 6).Select(b => board.GetPieceAt(new CheckerCoordinate(a, b))).ToArray()).ToArray();
                    var ranks = Enumerable.Range(0, 6).Select(a => Enumerable.Range(0, 6).Select(b => board.GetPieceAt(new CheckerCoordinate(b, a))).ToArray()).ToArray();
                    var blankFiles = Enumerable.Range(0, 6).Where(f => files[f].All(p => p == null)).ToArray();
                    var blankRanks = Enumerable.Range(0, 6).Where(r => ranks[r].All(p => p == null)).ToArray();
                    if (blankFiles.Length == 1 && blankRanks.Length == 1)
                        coords[0] = new CheckerCoordinate(blankFiles[0], blankRanks[0]);
                    else
                        coords[0] = new CheckerCoordinate(4, 4);
                    break;
                }
            case 5:
                {
                    // The square on which a white checker began on which has no checker on it now. If none or multiple, a1.
                    var startWCoords = _initialCheckerBoard.Pieces.Where(p => p != null && p.Color == CheckerColor.White).Select(i => i.Coordinate).ToArray();
                    var piecesAtStartWCoords = startWCoords.Select(p => board.GetPieceAt(p)).Where(x => x == null || x.Color == CheckerColor.Black).Select(p => p.Coordinate).ToArray();
                    if (piecesAtStartWCoords.Length == 1)
                        coords[0] = piecesAtStartWCoords.First();
                    else
                        coords[0] = new CheckerCoordinate(0, 0);
                    break;
                }
            case 6:
                {
                    // a5
                    coords[0] = new CheckerCoordinate(0, 4);
                    break;
                }
            default:
                throw new InvalidCastException(string.Format("There are {0} white pieces on the board.", whitePositions.Length));
        }

        switch (blackPositions.Length)
        {
            case 0:
                {
                    // d4
                    coords[1] = new CheckerCoordinate(3, 3);
                    break;
                }
            case 1:
                {
                    // Its position.
                    coords[1] = blackPositions.First();
                    break;
                }
            case 2:
                {
                    // Their midpoint if it lies in the center of a square on the board. Otherwise, f2
                    coords[1] = MidPointCalc(blackPositions);
                    break;
                }
            case 3:
                {
                    // If all three are diagonally touching, the square which lies at the midpoint of their arrangement. Otherwise, a1.
                    coords[1] = TripleDiagonalCalc(blackPositions);
                    break;
                }
            case 4:
                {
                    // File a in whichever rank has the most black checkers. If tied, a5.
                    var ranks = new List<int>();
                    var mostNumOfPieces = 0;
                    for (int rank = 0; rank < 6; rank++)
                    {
                        var numPieces = board.Pieces.Count(p => p != null && p.Coordinate.Y == rank);
                        if (numPieces > mostNumOfPieces)
                        {
                            mostNumOfPieces = numPieces;
                            ranks.Clear();
                            ranks.Add(rank);
                        }
                        else if (numPieces == mostNumOfPieces)
                        {
                            ranks.Add(rank);
                        }
                    }
                    coords[1] = ranks.Count == 1 ? new CheckerCoordinate(0, ranks[0]) : new CheckerCoordinate(0, 4);
                    break;
                }
            case 5:
                {
                    // Rank 5 in whichever file has a unique number of black checkers. If there are multiple or none, c5.
                    var blackCounts = new Dictionary<int, List<int>>();
                    for (int file = 0; file < 6; file++)
                    {
                        var numPieces = board.Pieces.Count(p => p != null && p.Coordinate.X == file);
                        if (blackCounts.ContainsKey(numPieces))
                            blackCounts[numPieces] = new List<int>();
                        blackCounts[numPieces].Add(file);
                    }
                    var uniques = blackCounts.Keys.Where(b => blackCounts[b].Count == 1).ToArray();
                    coords[1] = uniques.Length == 1 ? new CheckerCoordinate(blackCounts[uniques[0]][0], 4) : new CheckerCoordinate(2, 4);
                    break;
                }
            case 6:
                {
                    // If there are 3 or fewer white checkers, the coordinate obtained by using the number of white checkers in this table. Otherwise, e5.
                    var whiteCount = whitePositions.Length;
                    coords[1] =
                        whiteCount == 0 ? new CheckerCoordinate(3, 3) :
                        whiteCount == 1 ? whitePositions.First() :
                        whiteCount == 2 ? MidPointCalc(whitePositions) :
                        whiteCount == 3 ? TripleDiagonalCalc(whitePositions) :
                        new CheckerCoordinate(4, 4);
                    break;
                }
            default:
                throw new InvalidCastException(string.Format("There are {0} black pieces on the board.", blackPositions.Length));
        }

        Debug.LogFormat("[Not Chess #{0}] Coordinates to input: {1}.", _moduleId, coords.Join("; "));

        return coords;
    }

    private CheckerCoordinate TripleDiagonalCalc(CheckerCoordinate[] blackPositions)
    {
        var defaultCoord = new CheckerCoordinate(0, 0);
        var a = blackPositions[0];
        var b = blackPositions[1];
        var c = blackPositions[2];
        if ((IsDiagonalFrom(a, b) ? 1 : 0) + (IsDiagonalFrom(a, c) ? 1 : 0) + (IsDiagonalFrom(b, c) ? 1 : 0) >= 2)
        {
            var xsUnique = blackPositions.Select(i => i.X).Distinct().Count() == 3;
            var ysUnique = blackPositions.Select(i => i.Y).Distinct().Count() == 3;
            return !xsUnique || !ysUnique ? defaultCoord : new CheckerCoordinate((a.X + b.X + c.X) / 3, (a.Y + b.Y + c.Y) / 3);
        }
        return defaultCoord;
    }

    private CheckerCoordinate MidPointCalc(CheckerCoordinate[] positions)
    {
        var a = positions.First();
        var b = positions.Last();
        return a.X % 2 == b.X % 2 && a.Y % 2 == b.Y % 2 ? new CheckerCoordinate((a.X + b.X) / 2, (a.Y + b.Y) / 2) : new CheckerCoordinate(5, 1);
    }

    private bool IsDiagonalFrom(CheckerCoordinate a, CheckerCoordinate b)
    {
        return
            (a.X == b.X - 1 && a.Y == b.Y - 1) ||
            (a.X == b.X - 1 && a.Y == b.Y + 1) ||
            (a.X == b.X + 1 && a.Y == b.Y - 1) ||
            (a.X == b.X + 1 && a.Y == b.Y + 1);
    }

    private void Update()
    {
        if (_moduleSolved || _expectingFinalInput || _setReadyFlag || _lockModule)
            return;

        decimal allSolvedModules = BombInfo.GetSolvedModuleNames().Count(x => !_ignoredModules.Contains(x));
        if (_moduleCount - allSolvedModules < 4)
        {
            Debug.LogFormat("[Not Chess #{0}] The bomb has too few solves to continue the game. The next move will end the game, and final input will be required.", _moduleId);
            _setReadyFlag = true;
        }

        decimal modulePercentage = (allSolvedModules / _moduleCount) * 100;
        if (modulePercentage >= 40)
        {
            Debug.LogFormat("[Not Chess #{0}] 40% of the bomb’s modules have been solved. The next move will end the game, and final input will be required.", _moduleId);
            _setReadyFlag = true;
        }

        if (BombInfo.GetTime() < 180)
        {
            Debug.LogFormat("[Not Chess #{0}] The bomb's timer has reached below three minutes. The next move will end the game, and final input will be required.", _moduleId);
            _setReadyFlag = true;
        }
    }

    private IEnumerator CountdownTimer()
    {
        for (int time = _timerStart; time >= 0; time--)
        {
            DisplayText.text = string.Format("{0}-{1}", "0abcdefghij"[time / 10], time % 10);
            if (!_expectingFinalInput)
            {
                var cols = _blacksLastMoves.Select(i => (int?)i.X).ToList();
                var rows = _blacksLastMoves.Select(i => (int?)i.Y).ToList();
                cols.Add(null);
                rows.Add(null);
                int blacksMoveIx = (108 - time) % cols.Count;
                var blacksMoveCol = cols[blacksMoveIx];
                var blacksMoveRow = rows[blacksMoveIx];

                var ledColors = new int[6];
                if (blacksMoveRow != null)
                {
                    ledColors[blacksMoveCol.Value] += 1;
                    ledColors[blacksMoveRow.Value] += 2;
                }
                for (int led = 0; led < 6; led++)
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[ledColors[led]];
            }
            else
                for (int led = 0; led < 6; led++)
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[3];
            if (time == 40)
                _expectingInput = true;
            if (time % 2 == 0 && time <= 40 && time > 20)
                Audio.PlaySoundAtTransform("CHbeep", transform);
            if (time <= 10 || (time <= 20 && time % 2 == 0))
                Audio.PlaySoundAtTransform("CHalarm", transform);
            yield return new WaitForSeconds(0.9f);
            Audio.PlaySoundAtTransform("CHtick", transform);
            yield return new WaitForSeconds(0.1f);
        }
        _lockModule = true;
        Debug.LogFormat("[Not Chess #{0}] Ran out of time. Strike.", _moduleId);
        _checkerBoard = _initialCheckerBoard;
        _expectingInput = false;
        _expectingFinalInput = false;
        _setReadyFlag = false;
        _inputtedLetter = null;
        _inputtedNumber = null;
        _inputtedCoordinates.Clear();
        _blacksLastMoves.Clear();
        _movesForInputtedPiece.Clear();
        _finalInput.Clear();
        _finalAnswer = new CheckerCoordinate[2];
        StartCoroutine(SystemFailure());
    }

    private IEnumerator ResetCountdown()
    {
        Audio.PlaySoundAtTransform("CHreset", transform);
        DisplayText.text = "-";
        for (int i = 0; i < 24; i++)
        {
            string randStr = "abcdefghij0123456789-";
            string strA = randStr[Rnd.Range(0, randStr.Length)].ToString();
            string strB = randStr[Rnd.Range(0, randStr.Length)].ToString();
            string strC = randStr[Rnd.Range(0, randStr.Length)].ToString();
            DisplayText.text = strA + strB + strC;
            for (int led = 0; led < 6; led++)
            {
                if (i % 6 == led)
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[3];
                else
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[0];
            }
            yield return new WaitForSeconds(0.065f);
        }
        for (int i = 0; i < 6; i++)
            LedObjs[i].GetComponent<MeshRenderer>().material = LedMats[0];
        _countdownTimer = StartCoroutine(CountdownTimer());
    }

    private IEnumerator SystemFailure()
    {
        if (_countdownTimer != null)
            StopCoroutine(_countdownTimer);
        Audio.PlaySoundAtTransform("CHsystemfailure", transform);
        for (int i = 0; i < 168; i++)
        {
            string randStr = "abcdefghij0123456789-";
            string strA = randStr[Rnd.Range(0, randStr.Length)].ToString();
            string strB = randStr[Rnd.Range(0, randStr.Length)].ToString();
            string strC = randStr[Rnd.Range(0, randStr.Length)].ToString();
            DisplayText.text = strA + strB + strC;
            for (int led = 0; led < 6; led++)
                if (i % 6 == led)
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[2];
                else
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[0];
            yield return new WaitForSeconds(0.065f);
        }
        Module.HandleStrike();
        _countdownTimer = StartCoroutine(CountdownTimer());
        LogAllPossibleMovesForWhite(_checkerBoard);
        yield return new WaitForSeconds(2f);
        _lockModule = false;
    }

    private IEnumerator SolveAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        Audio.PlaySoundAtTransform("CHfailsafe", transform);
        int interval = 54;
        for (int i = 0; i < 216; i++)
        {
            string randStr = "abcdefghij0123456789-";
            if (i % interval == 0)
                Audio.PlaySoundAtTransform("CHtick", transform);
            string strA = i < interval * 1 ? randStr[Rnd.Range(0, randStr.Length)].ToString() : "g";
            string strB = i < interval * 2 ? randStr[Rnd.Range(0, randStr.Length)].ToString() : "-";
            string strC = i < interval * 3 ? randStr[Rnd.Range(0, randStr.Length)].ToString() : "g";
            DisplayText.text = strA + strB + strC;
            for (int led = 0; led < 6; led++)
                if (i % 6 == led)
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[3];
                else
                    LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[0];
            yield return new WaitForSeconds(0.065f);
        }
        for (int led = 0; led < 6; led++)
            LedObjs[led].GetComponent<MeshRenderer>().material = LedMats[1];
        _moduleSolved = true;
        Module.HandlePass();
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = @"Help message";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
