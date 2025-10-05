using RecolourFlash;
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
    public KMSelectable[] ButtonSels;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _activated;

    private const int _timerStart = 108;

    private static T[] NewArray<T>(params T[] array) { return array; }

    private static readonly CheckerBoard _initialCheckerBoard = new CheckerBoard(NewArray
    (
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(0, 0)), null,
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(0, 2)), null,
        new CheckerPiece(CheckerColor.White, new CheckerCoordinate(0, 4)), null,
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(1, 1)),
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(1, 3)),
        null, new CheckerPiece(CheckerColor.White, new CheckerCoordinate(1, 5)),
        null, null, null, null, null, null,
        null, null, null, null, null, null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(4, 0)), null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(4, 2)), null,
        new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(4, 4)), null,
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(5, 1)),
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(5, 3)),
        null, new CheckerPiece(CheckerColor.Black, new CheckerCoordinate(5, 5))
    ), 6);
    private CheckerBoard _checkerBoard;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);

        _checkerBoard = _initialCheckerBoard;

        Debug.Log(_initialCheckerBoard);

        Module.OnActivate += Activate;
    }

    private void Activate()
    {
        _activated = true;
    }

    private KMSelectable.OnInteractHandler ButtonPress(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            return false;
        };
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
