using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RecolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public TextMesh ScreenText;
    public GameObject[] ButtonObjs;
    private readonly Coroutine[] _pressAnimations = new Coroutine[2];

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    public enum Colour
    {
        Red,
        Yellow,
        Green,
        Blue,
        Magenta,
        White
    }

    public class ColourDisplay : IEquatable<ColourDisplay>
    {
        public Colour Word;
        public Colour Colour;

        public ColourDisplay(Colour word, Colour colour)
        {
            Word = word;
            Colour = colour;
        }

        public bool Equals(ColourDisplay other)
        {
            return other != null && other.Word == Word && other.Colour == Colour;
        }
    }

    private static readonly Color32[] _possibleColors = new Color32[]
    {
        new Color32(255, 0, 0, 255),
        new Color32(255, 255, 0, 255),
        new Color32(0, 255, 0, 255),
        new Color32(0, 0, 255, 255),
        new Color32(255, 0, 255, 255),
        new Color32(255, 255, 255, 255)
    };
    private Colour?[] _grid = new Colour?[36];
    private int _direction;
    private List<ColourDisplay[]> _displays = new List<ColourDisplay[]>();
    private List<bool> _solutions;
    private Coroutine _screenCycle;
    private List<ColourDisplay> _recolouredCells = new List<ColourDisplay>();
    private List<ColourDisplay> _stageTwoDisplays = new List<ColourDisplay>();
    private int _stageOneIndex;
    private int _stageTwoIndex;
    private int _overallStage;
    private const int _stageOneCap = 6;
    private const int _stageTwoCap = 9;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        YesButton.OnInteract += YesPress;
        NoButton.OnInteract += NoPress;
        YesButton.OnInteractEnded += YesRelease;
        NoButton.OnInteractEnded += NoRelease;

        var sn = BombInfo.GetSerialNumber();
        if (sn[0] >= 'A' && sn[0] <= 'Z')
        {
            if (sn[1] >= 'A' && sn[1] <= 'Z')
                _direction = 0; // letter letter
            else
                _direction = 3; // letter number
        }
        else
        {
            if (sn[1] >= 'A' && sn[1] <= 'Z')
                _direction = 1; // number letter
            else
                _direction = 2; // number number
        }
        Debug.LogFormat("[Recolour Flash #{0}] The serial number starts with {1}-{2}. Direction is {3}.", _moduleId, (sn[0] >= 'A' && sn[0] <= 'Z') ? "LETTER" : "NUMBER", (sn[1] >= 'A' && sn[1] <= 'Z') ? "LETTER" : "NUMBER", new string[] { "UP", "RIGHT", "DOWN", "LEFT" }[_direction]);

        TooManyStageOneIters:
        int iter = 0;
        var posShuffle = Enumerable.Range(0, 36).ToArray().Shuffle();
        var logList = new List<string>();
        _solutions = new List<bool>();
        _grid = new Colour?[36];
        _displays = new List<ColourDisplay[]>();
        _recolouredCells = new List<ColourDisplay>();

        NextSequence:
        if (iter == _stageOneCap)
            goto TooManyStageOneIters;

        NewDisplays:
        var position = new ColourDisplay((Colour)(posShuffle[iter] / 6), (Colour)(posShuffle[iter] % 6));
        var colouring = new ColourDisplay((Colour)Rnd.Range(0, 6), (Colour)Rnd.Range(0, 6));
        if (position.Equals(colouring))
            goto NewDisplays;

        _displays.Add(new ColourDisplay[2] { position, colouring });
        int rowA = (int)position.Colour;
        int colA = (int)position.Word;
        int rowB = _direction == 0 ? (rowA == 0 ? 5 : rowA - 1) : _direction == 2 ? (rowA == 5 ? 0 : rowA + 1) : rowA;
        int colB = _direction == 1 ? (colA == 5 ? 0 : colA + 1) : _direction == 3 ? (colA == 0 ? 5 : colA - 1) : colA;
        int posA = rowA * 6 + colA;
        int posB = rowB * 6 + colB;
        var oldGrid = _grid.ToArray();
        _grid[posA] = colouring.Word;
        _grid[posB] = colouring.Colour;
        logList.Add(string.Format("[Recolour Flash #{0}] At position {1}-{2}, coloured cell {3}.", _moduleId, (Colour)colA, (Colour)rowA, _grid[posA]));
        logList.Add(string.Format("[Recolour Flash #{0}] At position {1}-{2}, coloured cell {3}.", _moduleId, (Colour)colB, (Colour)rowB, _grid[posB]));
        bool hasBeenRecolored = CheckForRecolour(_grid, oldGrid);
        if (!hasBeenRecolored)
        {
            logList.Add(string.Format("[Recolour Flash #{0}] No recolour. Next sequence.", _moduleId));
            _solutions.Add(false);
            iter++;
            goto NextSequence;
        }

        logList.Add(string.Format("[Recolour Flash #{0}] Recolour! {1}.", _moduleId, _recolouredCells.Select(i => i.Word + " was replaced by " + i.Colour).Join(", and ")));
        _solutions.Add(true);
        for (int i = 0; i < logList.Count; i++)
            Debug.Log(logList[i]);

        TooManyStageTwoIters:
        _stageTwoDisplays = new List<ColourDisplay>();
        int ixA = -1;
        int ixB = -1;
        for (int i = 0; i < 36; i++)
            if (i / 6 != i % 6)
                _stageTwoDisplays.Add(new ColourDisplay((Colour)(i % 6), (Colour)(i / 6)));
        _stageTwoDisplays.Shuffle();
        for (int i = 0; i < _stageTwoDisplays.Count; i++)
        {
            if (_stageTwoDisplays[i].Equals(_recolouredCells[0]))
                ixA = i;
            if (_recolouredCells.Count == 2 && _stageTwoDisplays[i].Equals(_recolouredCells[1]))
                ixB = i;
        }
        if (ixB == -1)
            ixB = ixA;
        if (ixA > _stageTwoCap && ixB > _stageTwoCap)
            goto TooManyStageTwoIters;
        _screenCycle = StartCoroutine(ScreenCycle());
    }

    private bool CheckForRecolour(Colour?[] a, Colour?[] b)
    {
        bool match = false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != null && b[i] != null && a[i] != b[i])
            {
                _recolouredCells.Add(new ColourDisplay((Colour)a[i], (Colour)b[i]));
                match = true;
            }
        return match;
    }

    private IEnumerator ScreenCycle()
    {
        while (!_moduleSolved)
        {
            ScreenText.text = _displays[_stageOneIndex][0].Word.ToString();
            ScreenText.color = _possibleColors[(int)_displays[_stageOneIndex][0].Colour];
            yield return new WaitForSeconds(0.75f);
            ScreenText.text = _displays[_stageOneIndex][1].Word.ToString();
            ScreenText.color = _possibleColors[(int)_displays[_stageOneIndex][1].Colour];
            yield return new WaitForSeconds(0.75f);
            ScreenText.text = "";
            yield return new WaitForSeconds(0.75f);
        }
        yield break;
    }

    private void StrikeReset()
    {
        _overallStage = 0;
        _stageTwoIndex = 0;
        _stageOneIndex = 0;
        if (_screenCycle != null)
            StopCoroutine(_screenCycle);
        _screenCycle = StartCoroutine(ScreenCycle());
        Module.HandleStrike();
    }

    private bool YesPress()
    {
        YesButton.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, YesButton.transform);
        if (_pressAnimations[0] != null)
            StopCoroutine(_pressAnimations[0]);
        _pressAnimations[0] = StartCoroutine(PressAnimation(0, true));
        if (_moduleSolved)
            return false;
        if (_overallStage == 0)
        {
            if (_solutions[_stageOneIndex])
            {
                Debug.LogFormat("[Recolour Flash #{0}] Correctly pressed 'YES' upon a recolour.", _moduleId);
                if (_screenCycle != null)
                    StopCoroutine(_screenCycle);
                _overallStage = 1;
                ScreenText.text = _stageTwoDisplays[_stageTwoIndex].Word.ToString();
                ScreenText.color = _possibleColors[(int)_stageTwoDisplays[_stageTwoIndex].Colour];
            }
            else
            {
                Debug.LogFormat("[Recolour Flash #{0}] Incorrectly pressed 'YES' due to an absence of a recolour. Strike.", _moduleId);
                StrikeReset();
            }
        }
        else if (_overallStage == 1)
        {
            if (_recolouredCells.Any(i => i.Equals(_stageTwoDisplays[_stageTwoIndex])))
            {
                Debug.LogFormat("[Recolour Flash #{0}] Correctly pressed 'YES' when a recolour was displayed. Module solved.", _moduleId);
                _moduleSolved = true;
                Module.HandlePass();
            }
            else
            {
                Debug.LogFormat("[Recolour Flash #{0}] Incorrectly pressed 'YES' because a recolour was not displayed. Strike .", _moduleId);
                StrikeReset();
            }
        }
        return false;
    }

    private bool NoPress()
    {
        NoButton.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, NoButton.transform);
        if (_pressAnimations[1] != null)
            StopCoroutine(_pressAnimations[1]);
        _pressAnimations[1] = StartCoroutine(PressAnimation(1, true));
        if (_moduleSolved)
            return false;
        if (_overallStage == 0)
        {
            if (!_solutions[_stageOneIndex])
            {
                Debug.LogFormat("[Recolour Flash #{0}] Correctly pressed 'NO' due to the absence of a recolour.", _moduleId);
                if (_screenCycle != null)
                    StopCoroutine(_screenCycle);
                _stageOneIndex++;
                _screenCycle = StartCoroutine(ScreenCycle());
            }
            else
            {
                Debug.LogFormat("[Recolour Flash #{0}] Incorrectly pressed 'NO' when a recolour took place. Strike.", _moduleId);
                StrikeReset();
            }
        }
        else if (_overallStage == 1)
        {
            if (!_recolouredCells.Any(i => i.Equals(_stageTwoDisplays[_stageTwoIndex])))
            {
                Debug.LogFormat("[Recolour Flash #{0}] Correctly pressed 'NO' because a recolour was not displayed.", _moduleId);
                _stageTwoIndex++;
                ScreenText.text = _stageTwoDisplays[_stageTwoIndex].Word.ToString();
                ScreenText.color = _possibleColors[(int)_stageTwoDisplays[_stageTwoIndex].Colour];
            }
            else
            {
                Debug.LogFormat("[Recolour Flash #{0}] Incorrectly pressed 'NO' when a recolour was displayed. Strike.", _moduleId);
                StrikeReset();
            }
        }
        return false;
    }

    private void YesRelease()
    {
        if (_pressAnimations[0] != null)
            StopCoroutine(_pressAnimations[0]);
        _pressAnimations[0] = StartCoroutine(PressAnimation(0, false));
    }

    private void NoRelease()
    {
        if (_pressAnimations[1] != null)
            StopCoroutine(_pressAnimations[1]);
        _pressAnimations[1] = StartCoroutine(PressAnimation(1, false));
    }

    private IEnumerator PressAnimation(int btn, bool pushIn)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        var curPos = ButtonObjs[btn].transform.localPosition;
        while (elapsed < duration)
        {
            ButtonObjs[btn].transform.localPosition = new Vector3(curPos.x, Easing.InOutQuad(elapsed, curPos.y, pushIn ? 0.01f : 0.0146f, duration), curPos.z);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press yes [Press the 'YES' button.] | !{0} press no [Press the 'NO' button.] | 'press' is optional. | Button presses can be chained.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = Regex.Replace(command.Trim().ToLowerInvariant(), @"\s+", " ");
        if (command.StartsWith("press "))
            command = command.Substring(6);
        var arr = command.Split(' ');
        var btns = new List<KMSelectable>();
        for (int i = 0; i < arr.Length; i++)
        {
            Debug.Log("<" + arr[i] + ">");
            if (arr[i] == "yes")
                btns.Add(YesButton);
            else if (arr[i] == "no")
                btns.Add(NoButton);
            else
                yield break;
        }
        yield return null;
        yield return btns;
    }
    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleSolved)
        {
            if (_overallStage == 0)
            {
                if (!_solutions[_stageOneIndex])
                {
                    NoButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    NoButton.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    YesButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    YesButton.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (_overallStage == 1)
            {
                if (!_recolouredCells.Any(i => i.Equals(_stageTwoDisplays[_stageTwoIndex])))
                {
                    NoButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    NoButton.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    YesButton.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    YesButton.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        yield break;
    }
}