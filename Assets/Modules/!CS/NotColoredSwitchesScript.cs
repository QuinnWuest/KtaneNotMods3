using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotColoredSwitchesScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;

    public KMSelectable[] SwitchSels;
    public GameObject[] SwitchObjs;

    public GameObject[] LedTopObjs;
    public GameObject[] LedBottomObjs;
    public Material[] LedMats;
    public TextMesh[] ColorblindText;

    private static readonly Color[] _matColors = "c61e1e|21c032|2543ff|ffad0b|b91edb|53d3ff".Split('|').Select(str => new Color(Convert.ToInt32(str.Substring(0, 2), 16) / 255f, Convert.ToInt32(str.Substring(2, 2), 16) / 255f, Convert.ToInt32(str.Substring(4, 2), 16) / 255f)).ToArray();
    private static readonly string[] _colorNames = new string[6] { "RED", "GREEN", "BLUE", "ORANGE", "PURPLE", "TURQUOISE" };
    private static readonly string[] _wordList = new string[] { "ADJUST", "ANCHOR", "BOWTIE", "BUTTON", "CIPHER", "CORNER", "DAMPEN", "DEMOTE", "ENLIST", "EVOLVE", "FORGET", "FINISH", "GEYSER", "GLOBAL", "HAMMER", "HELIUM", "IGNITE", "INDIGO", "JIGSAW", "JULIET", "KARATE", "KEYPAD", "LAMBDA", "LISTEN", "MATTER", "MEMORY", "NEBULA", "NICKEL", "OVERDO", "OXYGEN", "PEANUT", "PHOTON", "QUARTZ", "QUEBEC", "RESIST", "RIDDLE", "SIERRA", "STRIKE", "TEAPOT", "TWENTY", "UNTOLD", "ULTIMA", "VICTOR", "VIOLET", "WITHER", "WRENCH", "XENONS", "XYLOSE", "YELLOW", "YOGURT", "ZENITH", "ZODIAC" };

    private static readonly int[][] _marqueeInfo = new int[26][]
    {
        new int[5] { 14, 17, 31, 17, 17 },
        new int[5] { 30, 17, 30, 17, 30 },
        new int[5] { 14, 17, 16, 17, 14 },
        new int[5] { 30, 17, 17, 17, 30 },
        new int[5] { 31, 16, 31, 16, 31 },
        new int[5] { 31, 16, 31, 16, 16 },
        new int[5] { 14, 16, 23, 17, 14 },
        new int[5] { 17, 17, 31, 17, 17 },
        new int[5] { 31, 4, 4, 4, 31 },
        new int[5] { 31, 2, 2, 2, 12 },
        new int[5] { 17, 18, 28, 18, 17 },
        new int[5] { 16, 16, 16, 16, 31 },
        new int[5] { 17, 27, 21, 17, 17 },
        new int[5] { 17, 15, 21, 19, 17 },
        new int[5] { 14, 17, 17, 17, 14 },
        new int[5] { 30, 17, 30, 16, 16 },
        new int[5] { 14, 17, 21, 18, 13 },
        new int[5] { 30, 17, 30, 17, 17 },
        new int[5] { 15, 16, 14, 1, 30 },
        new int[5] { 31, 4, 4, 4, 4 },
        new int[5] { 17, 17, 17, 17, 14 },
        new int[5] { 17, 17, 10, 10, 4 },
        new int[5] { 17, 17, 21, 27, 17 },
        new int[5] { 17, 10, 4, 10, 17 },
        new int[5] { 17, 10, 4, 4, 4 },
        new int[5] { 31, 2, 4, 8, 31 }
    };

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _activated;

    private bool[] _isAnimating = new bool[5];
    private bool[] _switchStates = new bool[5];
    private Coroutine[] _flipSwitch = new Coroutine[5];

    private string _chosenWord;
    private int _chosenLetter;
    private int[] _otherLetters = new int[5];
    private int[] _otherCipheredLetters = new int[5];


    private int[] _switchColors = new int[5];
    private int? _mostRecentSwitch;

    private bool _colorblindMode;
    private Coroutine _showMarquee;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        SetColorblindMode(_colorblindMode);
        for (int i = 0; i < SwitchSels.Length; i++)
            SwitchSels[i].OnInteract += SwitchPress(i);
        newColors:
        for (int i = 0; i < _switchColors.Length; i++)
        {
            _switchColors[i] = Rnd.Range(0, 6);
            SwitchObjs[i].GetComponent<MeshRenderer>().material.color = _matColors[_switchColors[i]];
            ColorblindText[i].text = _colorNames[_switchColors[i]];
        }
        if (_switchColors.Distinct().Count() != 5)
            goto newColors;
        _chosenWord = _wordList[Rnd.Range(0, _wordList.Length)];
        var shuff = Enumerable.Range(0, 6).ToArray().Shuffle();
        _chosenLetter = _chosenWord[shuff[5]] - 'A';
        for (int i = 0; i < 5; i++)
        {
            _otherLetters[i] = _chosenWord[shuff[i]] - 'A';
            _otherCipheredLetters[i] = Cipher(_otherLetters[i], _switchColors[i], i);
        }
        Debug.LogFormat("[Not Colored Switches #{0}] Chosen word: {1}", _moduleId, _chosenWord);
        Debug.LogFormat("[Not Colored Switches #{0}] Chosen letter: {1}", _moduleId, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_chosenLetter]);
        Debug.LogFormat("[Not Colored Switches #{0}] Remaining letters: {1}", _moduleId, _otherLetters.Select(i => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i]).Join());
        Debug.LogFormat("[Not Colored Switches #{0}] Switch colors: {1}.", _moduleId, _switchColors.Select(i => _colorNames[i]).Join(", "));
        Debug.LogFormat("[Not Colored Switches #{0}] Remaining letters, ciphered: {1}", _moduleId, _otherCipheredLetters.Select(i => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i]).Join());
        Debug.Log(IntToBinary(_marqueeInfo[_otherCipheredLetters[0]][0]).Join(""));
    }

    private void SetColorblindMode(bool mode)
    {
        for (int i = 0; i < 5; i++)
            ColorblindText[i].gameObject.SetActive(mode);
    }

    private KMSelectable.OnInteractHandler SwitchPress(int sw)
    {
        return delegate ()
        {
            if (_moduleSolved || _isAnimating[sw])
                return false;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SwitchSels[sw].transform);
            _switchStates[sw] = !_switchStates[sw];
            _flipSwitch[sw] = StartCoroutine(FlipSwitch(sw));
            if (_showMarquee != null)
                StopCoroutine(_showMarquee);
            int upCount = 0;
            for (int i = 0; i < 5; i++)
            {
                if (_switchStates[i])
                    upCount++;
            }
            if (_activated)
            {
                if (sw == _mostRecentSwitch)
                {
                    _activated = false;
                    int submission = 0;
                    for (int i = 4; i >= 0; i--)
                    {
                        if (_switchStates[i])
                            submission += (int)Math.Pow(2, 4 - i);
                    }
                    var submissionLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZ----------"[submission].ToString();
                    if (submission == _chosenLetter)
                    {
                        _moduleSolved = true;
                        Module.HandlePass();
                        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                        Debug.LogFormat("[Not Colored Switches #{0}] Correctly submitted {1}. Module solved.", _moduleId, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[submission]);
                        for (int j = 0; j < 5; j++)
                        {
                            LedTopObjs[j].GetComponent<MeshRenderer>().material = LedMats[0];
                            LedBottomObjs[j].GetComponent<MeshRenderer>().material = LedMats[0];
                        }
                    }
                    else
                    {
                        Module.HandleStrike();
                        Debug.LogFormat("[Not Colored Switches #{0}] Incorrectly submitted {1}. Strike.", _moduleId, submissionLetter == "-" ? submission.ToString() : submissionLetter);
                        StartCoroutine(Strike());
                        _mostRecentSwitch = null;
                        for (int j = 0; j < 5; j++)
                        {
                            LedTopObjs[j].GetComponent<MeshRenderer>().material = LedMats[0];
                            LedBottomObjs[j].GetComponent<MeshRenderer>().material = LedMats[1];
                        }
                    }
                }
                else
                {
                    _mostRecentSwitch = sw;
                    for (int i = 0; i < 5; i++)
                    {
                        if (i == _mostRecentSwitch)
                            LedTopObjs[i].GetComponent<MeshRenderer>().material = LedMats[2];
                        else
                            LedTopObjs[i].GetComponent<MeshRenderer>().material = LedMats[0];
                    }
                }
            }
            else if (upCount == 1)
            {
                _showMarquee = StartCoroutine(ShowMarquee(_otherCipheredLetters[sw]));
                for (int i = 0; i < 5; i++)
                    LedBottomObjs[i].GetComponent<MeshRenderer>().material = LedMats[2];
            }
            else
            {
                for (int j = 0; j < 5; j++)
                {
                    LedTopObjs[j].GetComponent<MeshRenderer>().material = LedMats[0];
                    LedBottomObjs[j].GetComponent<MeshRenderer>().material = LedMats[1];
                }
                if (upCount != 0)
                {
                    Debug.LogFormat("[Not Colored Switches #{0}] Entering submission mode.", _moduleId);
                    _activated = true;
                    for (int j = 0; j < 5; j++)
                        LedBottomObjs[j].GetComponent<MeshRenderer>().material = LedMats[3];
                }
            }
            return false;
        };
    }

    private IEnumerator FlipSwitch(int sw)
    {
        _isAnimating[sw] = true;
        var duration = 0.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            SwitchObjs[sw].transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsed, _switchStates[sw] ? -55f : 55f, _switchStates[sw] ? 55f : -55f, duration), 0f, 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        SwitchObjs[sw].transform.localEulerAngles = new Vector3(_switchStates[sw] ? 55f : -55f, 0f, 0f);
        _isAnimating[sw] = false;
    }

    private IEnumerator ShowMarquee(int num)
    {
        while (true)
        {
            for (int i = 0; i < 5; i++)
            {
                var bin = IntToBinary(_marqueeInfo[num][i]);
                for (int j = 0; j < 5; j++)
                    LedTopObjs[j].GetComponent<MeshRenderer>().material = bin[j] ? LedMats[1] : LedMats[0];
                yield return new WaitForSeconds(0.3f);
            }
            for (int j = 0; j < 5; j++)
                LedTopObjs[j].GetComponent<MeshRenderer>().material = LedMats[0];
            yield return new WaitForSeconds(0.6f);
        }
    }

    private bool[] IntToBinary(int num)
    {
        return int.Parse(Convert.ToString(num, 2)).ToString("00000").Select(i => i != '0').ToArray();
    }

    private int Cipher(int input, int color, int pos)
    {
        switch (color)
        {
            case 0:
                input = 25 - input;
                break;
            case 1:
                input = (input + 13) % 26;
                break;
            case 2:
                input = (input + pos + 1) % 26;
                break;
            case 3:
                input = (input - (pos + 1) + 26) % 26;
                break;
            case 4:
                input = input * 5 % 26;
                break;
            case 5:
                break;
        }
        return input;
    }

    private IEnumerator Strike()
    {
        for (int i = 0; i < 5; i++)
            _isAnimating[i] = true;
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 5; i++)
        {
            if (_switchStates[i])
            {
                _switchStates[i] = false;
                _flipSwitch[i] = StartCoroutine(FlipSwitch(i));
                yield return new WaitForSeconds(0.1f);
            }
        }
        for (int i = 0; i < 5; i++)
            _isAnimating[i] = false;
    }

    private static string[] _twitchCommands = { "toggle", "switch", "flip", "press" };

#pragma warning disable 0414
    private string TwitchHelpMessage = @"Toggle switches with “!{0} 1 2 3 4”. (Optional “toggle/switch/flip/press” command allowed.) Use “!{0} colorblind” to show the colors of the switches.";
#pragma warning restore 0414

    // Taken from Colored Switches TP support, with a few changes.
    private IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length == 0)
            yield break;
        if (pieces.Length == 1 && (pieces[0] == "colorblind" || pieces[0] == "cb"))
        {
            yield return null;
            _colorblindMode = !_colorblindMode;
            SetColorblindMode(_colorblindMode);
            yield break;
        }
        var skip = _twitchCommands.Contains(pieces[0]) ? 1 : 0;
        if (pieces.Skip(skip).Any(p => { int val; return !int.TryParse(p.Trim(), out val) || val < 1 || val > 5; }))
            yield break;
        yield return null;
        yield return "solve";
        yield return "strike";
        if (pieces.Length > 20)
            yield return "waiting music";
        foreach (var p in pieces.Skip(skip))
        {
            var sw = int.Parse(p.Trim()) - 1;
            SwitchSels[sw].OnInteract();
            while (_isAnimating[sw])
                yield return null;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (!_activated)
        {
            for (int i = 0; i < 5; i++)
            {
                if (_switchStates[i])
                    SwitchSels[i].OnInteract();
                while (_isAnimating[i])
                    yield return null;
            }
            SwitchSels[0].OnInteract();
            while (_isAnimating[0])
                yield return null;
            SwitchSels[1].OnInteract();
            while (_isAnimating[1])
                yield return null;
        }
        var goalBin = IntToBinary(_chosenLetter);
        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 5; i++)
            {
                if (goalBin[i] != _switchStates[i] && i != _mostRecentSwitch)
                {
                    SwitchSels[i].OnInteract();
                    while (_isAnimating[i])
                        yield return null;
                }
            }
        }
        int swIx = 0;
        if (_mostRecentSwitch == 0)
            swIx = 1;
        for (int i = 0; i < 2; i++)
        {
            SwitchSels[swIx].OnInteract();
            while (_isAnimating[swIx])
                yield return null;
        }
    }
}
