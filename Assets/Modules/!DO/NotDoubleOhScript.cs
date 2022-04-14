using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotDoubleOhScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] ArrowBtnSels;
    public KMSelectable SubmitBtnSel;
    public GameObject[] LeftSegObjs;
    public GameObject[] RightSegObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly bool[][] _segmentConfigs = new bool[8][]
    {
        new bool[7] { true, true, true, true, true, true, false },
        new bool[7] { false, true, false, true, true, true, true },
        new bool[7] { true, true, false, false, true, false, true },
        new bool[7] { false, false, true, true, true, true, true },
        new bool[7] { true, true, false, true, true, false, true },
        new bool[7] { true, true, false, true, true, false, false },
        new bool[7] { true, true, false, false, true, true, true },
        new bool[7] { false, true, true, true, true, true, false }
    };

    private int _currentPosition;
    private int _currentPhase;
    private int[] _functionOrder = new int[8];
    private int[] _goalPos = new int[8];
    private Coroutine _cycleDisplay;
    private int[] _usedDirections = new int[7];
    private int _posIx;

    private static readonly string[] _grid = new string[64];
    private static readonly string[] _functionNames = new string[8] {
        "toggle your position between an even or odd column within the 4×4 subgrid.",
        "toggle your position between an even or odd row within the 4×4 subgrid.",
        "toggle your position between the left or right half within the 4×4 subgrid.",
        "toggle your position between the top or bottom half within the 4×4 subgrid.",
        "toggle your position between the left or right half of the entire 8×8 grid.",
        "toggle your position between the top or bottom half of the entire 8×8 grid.",
        "flip your position horizontally across the entire 8×8 grid.",
        "flip your position vertically across the entire 8×8 grid."
    };
    private static readonly string _buttonSymbols = "↕↔⇔⇕";
    private static readonly string[] _soundNames = new string[4] { "DoubleOPress1", "DoubleOPress2", "DoubleOPress3", "DoubleOPress4" };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        SubmitBtnSel.OnInteract += SubmitPress;
        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
        var rnd = RuleSeedable.GetRNG();
        for (int i = 0; i < _grid.Length; i++)
            _grid[i] = "ABCDEFGH".Substring(i / 8, 1) + "ABCDEFGH".Substring(i % 8, 1);
        rnd.ShuffleFisherYates(_grid);
        Debug.LogFormat("[Not Double-Oh #{0}] Using rule seed {1}.", _moduleId, rnd.Seed);

        for (int i = 0; i < LeftSegObjs.Length; i++)
        {
            LeftSegObjs[i].SetActive(false);
            RightSegObjs[i].SetActive(false);
        }
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ArrowBtnSels[btn].transform);
            ArrowBtnSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            Audio.PlaySoundAtTransform(_soundNames[Rnd.Range(0, 4)], transform);
            if (_currentPhase == 0 || _currentPhase == 2)
                return false;
            if (_currentPhase == 1)
            {
                var ix = btn + ((int)BombInfo.GetTime() % 2 == 0 ? 0 : 4);
                var newPos = MovePos(_currentPosition, ix);
                Debug.LogFormat("[Not Double-Oh #{0}] You pressed {1} on an {2} digit, which moved you from {3} to {4}.", _moduleId, _buttonSymbols[btn], (int)BombInfo.GetTime() % 2 == 0 ? "even" : "odd", _grid[_currentPosition], _grid[newPos]);
                _currentPosition = newPos;
                ShuffleSegments();
            }
            if (_currentPhase == 3)
            {
                var ix = btn + ((int)BombInfo.GetTime() % 2 == 0 ? 0 : 4);
                var newPos = MovePos(_currentPosition, ix);
                for (int seg = 0; seg < 7; seg++)
                {
                    LeftSegObjs[seg].SetActive(_segmentConfigs[_grid[newPos][0] - 'A'][seg]);
                    RightSegObjs[seg].SetActive(_segmentConfigs[_grid[newPos][1] - 'A'][seg]);
                }
                _posIx++;
                if (newPos != _goalPos[_posIx])
                {
                    Module.HandleStrike();
                    Debug.LogFormat("[Not Double-Oh #{0}] Incorrectly pressed {1} on an {2} digit, which moved you from {3} to {4} instead of {5}. Strike.", _moduleId, _buttonSymbols[btn], (int)BombInfo.GetTime() % 2 == 0 ? "even" : "odd", _grid[_currentPosition], _grid[newPos], _grid[_goalPos[_posIx]]);
                    Reset();
                }
                else
                {
                    Debug.LogFormat("[Not Double-Oh #{0}] Correctly pressed {1} on an {2} digit, which moved you from {3} to {4}.", _moduleId, _buttonSymbols[btn], (int)BombInfo.GetTime() % 2 == 0 ? "even" : "odd", _grid[_currentPosition], _grid[newPos]);
                    _currentPosition = newPos;
                    if (_posIx == 7)
                    {
                        _moduleSolved = true;
                        Module.HandlePass();
                        Audio.PlaySoundAtTransform("DoubleOSolve", transform);
                        for (int seg = 0; seg < 7; seg++)
                        {
                            LeftSegObjs[seg].SetActive(false);
                            RightSegObjs[seg].SetActive(false);
                        }
                        Debug.LogFormat("[Not Double-Oh #{0}] Successfully travelled to all goal positions in order. Module solved.", _moduleId);
                    }
                }
            }
            return false;
        };
    }

    private bool SubmitPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitBtnSel.transform);
        SubmitBtnSel.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        Audio.PlaySoundAtTransform(_soundNames[Rnd.Range(0, 4)], transform);
        if (_currentPhase == 0)
        {
            PhaseOne();
            return false;
        }
        if (_currentPhase == 1)
        {
            PhaseTwo();
            return false;
        }
        if (_currentPhase == 2)
        {
            PhaseThree();
            return false;
        }
        return false;
    }

    private void PhaseOne()
    {
        TwitchHelpMessage = "Phase 1: !{0} press horiz1 even [Presses horiz1 on an even digit.] | !{0} press submit [Presses submit button.] | 'press' is optional. Buttons are: horiz1, horiz2, vert1, vert2.";
        _currentPhase = 1;
        _currentPosition = Rnd.Range(0, 64);
        Debug.LogFormat("[Not Double-Oh #{0}] Entering Phase One. Current position: {1}", _moduleId, _grid[_currentPosition]);
        _functionOrder = Enumerable.Range(0, 8).ToArray().Shuffle();
        for (int i = 0; i < _functionOrder.Length; i++)
            Debug.LogFormat("[Not Double-Oh #{0}] Pressing {1} on an {2} digit will {3}", _moduleId, _buttonSymbols[i % 4], i / 4 == 0 ? "even" : "odd", _functionNames[_functionOrder[i]]);
        ShuffleSegments();
    }

    private void ShuffleSegments()
    {
        var segShuffs = new bool[8][] { new bool[7], new bool[7], new bool[7], new bool[7], new bool[7], new bool[7], new bool[7], new bool[7] };
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 7; j++)
                segShuffs[i][j] = _segmentConfigs[i][j];
            segShuffs[i].Shuffle();
        }
        for (int seg = 0; seg < LeftSegObjs.Length; seg++)
        {
            LeftSegObjs[seg].SetActive(segShuffs[_grid[_currentPosition][0] - 'A'][seg]);
            RightSegObjs[seg].SetActive(segShuffs[_grid[_currentPosition][1] - 'A'][seg]);
        }
    }

    private int MovePos(int initPos, int index)
    {
        int newPos = 0;
        switch (_functionOrder[index])
        {
            case 0:
                newPos = initPos % 2 == 0 ? initPos + 1 : initPos - 1;
                break;
            case 1:
                newPos = initPos % 16 / 8 == 0 ? initPos + 8 : initPos - 8;
                break;
            case 2:
                newPos = initPos / 2 % 2 == 0 ? initPos + 2 : initPos - 2;
                break;
            case 3:
                newPos = initPos % 32 / 16 == 0 ? initPos + 16 : initPos - 16;
                break;
            case 4:
                newPos = initPos / 4 % 2 == 0 ? initPos + 4 : initPos - 4;
                break;
            case 5:
                newPos = initPos % 64 / 32 == 0 ? initPos + 32 : initPos - 32;
                break;
            case 6:
                newPos = initPos / 8 * 8 + (7 - initPos % 8);
                break;
            case 7:
                newPos = 56 - (initPos / 8 * 8) + (initPos % 8);
                break;
        }
        return newPos;
    }

    private void PhaseTwo()
    {
        TwitchHelpMessage = "Phase 2: !{0} press submit [Presses the submit button.] | 'press' is optional.";
        newPath:
        _currentPhase = 2;
        _currentPosition = Rnd.Range(0, 64);
        _goalPos = new int[8];
        var tempUsedDirs = new int[8];
        _usedDirections = new int[8];
        for (int i = 0; i < _goalPos.Length; i++)
        {
            if (i == 0)
                _goalPos[i] = _currentPosition;
            else
            {
                newDir:
                var rndDir = Rnd.Range(0, 8);
                if (_usedDirections.Length > 1 && tempUsedDirs[i - 1] == rndDir)
                    goto newDir;
                tempUsedDirs[i] = rndDir;
                _goalPos[i] = MovePos(_goalPos[i - 1], rndDir);
            }
        }
        var tempList = tempUsedDirs.ToList();
        tempList.RemoveAt(0);
        _usedDirections = tempList.ToArray();
        if (_goalPos.Distinct().Count() != 8)
            goto newPath;
        Debug.LogFormat("[Not Double-Oh #{0}] Entering Phase Two. Goals: {1}.", _moduleId, _goalPos.Select(i => _grid[i]).Join(" "));
        Debug.LogFormat("[Not Double-Oh #{0}] Directions required: {1}.", _moduleId, _usedDirections.Select(i => _buttonSymbols[i % 4] + (i / 4 == 0 ? "e" : "o")).Join(", "));
        _cycleDisplay = StartCoroutine(CycleDisplay());
    }

    private IEnumerator CycleDisplay()
    {
        while (true)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int seg = 0; seg < 7; seg++)
                {
                    if (i != 8)
                    {
                        LeftSegObjs[seg].SetActive(_segmentConfigs[_grid[_goalPos[i]][0] - 'A'][seg]);
                        RightSegObjs[seg].SetActive(_segmentConfigs[_grid[_goalPos[i]][1] - 'A'][seg]);
                    }
                    else
                    {
                        LeftSegObjs[seg].SetActive(false);
                        RightSegObjs[seg].SetActive(false);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void PhaseThree()
    {
        TwitchHelpMessage = "Phase 3: !{0} press horiz1 even [Presses horiz1 on an even digit.] | 'press' is optional. Buttons are: horiz1, horiz2, vert1, vert2.";
        Debug.LogFormat("[Not Double-Oh #{0}] Entering Phase Three. Current position: {1}. Good luck!", _moduleId, _grid[_currentPosition]);
        _currentPhase = 3;
        _posIx = 0;
        if (_cycleDisplay != null)
            StopCoroutine(_cycleDisplay);
        for (int seg = 0; seg < 7; seg++)
        {
            LeftSegObjs[seg].SetActive(_segmentConfigs[_grid[_currentPosition][0] - 'A'][seg]);
            RightSegObjs[seg].SetActive(_segmentConfigs[_grid[_currentPosition][1] - 'A'][seg]);
        }
    }

    private void Reset()
    {
        _currentPhase = 0;
        for (int seg = 0; seg < 7; seg++)
        {
            LeftSegObjs[seg].SetActive(false);
            RightSegObjs[seg].SetActive(false);
        }
    }

    private sealed class TpPress : IEquatable<TpPress>
    {
        public int btn;
        public int digit;

        public TpPress(int btn, int digit)
        {
            this.btn = btn;
            this.digit = digit;
        }

        public bool Equals(TpPress other)
        {
            return other != null && other.btn == btn && other.digit == digit;
        }
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = "Phase 0: !{0} press submit [Presses the submit button.] | 'press' is optional.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        var parameters = command.ToLowerInvariant().Split(' ');
        if (_currentPhase == 0 || _currentPhase == 2)
        {
            m = Regex.Match(command, @"^\s*(press\s+)?s(ubmit)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
            {
                yield return null;
                SubmitBtnSel.OnInteract();
                yield break;
            }
            if (parameters.Length < 1 || (parameters.Length < 2 && parameters[0] == "press"))
                yield break;
            int ix = parameters[0] == "press" ? 1 : 0;
            TpPress b1 = GetBtnFromShort(parameters[ix]);
            TpPress b2;
            if (parameters.Length != 1 + ix)
                b2 = GetBtnFromLong(parameters[ix], parameters[ix + 1]);
            else
                b2 = null;
            if (b1 != null || b2 != null)
            {
                yield return null;
                yield return "sendtochaterror You can't press an arrowed button at Phase " + _currentPhase + "!";
                yield break;
            }
        }
        if (_currentPhase == 1 || _currentPhase == 3)
        {
            m = Regex.Match(command, @"^\s*(press\s+)?s(ubmit)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
            {
                yield return null;
                if (_currentPhase == 3)
                {
                    yield return "sendtochaterror You can't press the submit button at Phase 3!";
                    yield break;
                }
                SubmitBtnSel.OnInteract();
                yield break;
            }
            if (parameters.Length < 1 || (parameters.Length < 2 && parameters[0] == "press"))
                yield break;
            int ix = parameters[0] == "press" ? 1 : 0;
            var list = new List<TpPress>();
            for (int i = ix; i < parameters.Length; i++)
            {
                TpPress b1 = GetBtnFromShort(parameters[i]);
                TpPress b2;
                if (i != parameters.Length - 1 + ix)
                    b2 = GetBtnFromLong(parameters[i], parameters[i + 1]);
                else
                    b2 = null;
                if (b1 != null)
                {
                    list.Add(b1);
                    continue;
                }
                if (b2 != null)
                {
                    list.Add(b2);
                    i++;
                    continue;
                }
                yield break;
            }
            yield return null;
            for (int i = 0; i < list.Count; i++)
            {
                while ((int)BombInfo.GetTime() % 2 != list[i].digit)
                    yield return null;
                ArrowBtnSels[list[i].btn].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private TpPress GetBtnFromShort(string str)
    {
        string s = str.ToLowerInvariant();
        if (s.Length != 3 || (s[0] != 'h' && s[0] != 'v') || (s[1] != '1' && str[1] != '2') || (s[2] != 'e' && str[2] != 'o')) return null;
        int b;
        if (s[0] == 'h') { if (s[1] == '1') b = 1; else b = 2; }
        else { if (s[1] == 'h') b = 0; else b = 3; }
        int t = s[2] == 'e' ? 0 : 1;
        return new TpPress(b, t);
    }

    private TpPress GetBtnFromLong(string str1, string str2)
    {
        var btns = new string[] { "vert1", "horiz1", "horiz2", "vert2" };
        var times = new string[] { "even", "odd" };
        if (!btns.Contains(str1) || !times.Contains(str2))
            return null;
        return new TpPress(Array.IndexOf(btns, str1), Array.IndexOf(times, str2));
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (_currentPhase != 3)
        {
            SubmitBtnSel.OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
        for (int i = _posIx; i < 7; i++)
        {
            while ((int)BombInfo.GetTime() % 2 != _usedDirections[i] / 4)
                yield return true;
            ArrowBtnSels[_usedDirections[i] % 4].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
