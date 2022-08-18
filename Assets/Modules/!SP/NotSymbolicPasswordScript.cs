using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotSymbolicPasswordScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable SubmitBtnSel;
    public KMSelectable[] ArrowBtnSels;
    public GameObject[] SymbolObjs;
    public Material[] SymbolMats;
    public GameObject[] ZeroObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private const int SIZE = 16;

    private static readonly int[] _imageGrid = new int[SIZE * SIZE];
    private int[] _currentPosition = new int[6];
    private int _currentStage;
    private bool _isAnimating = true;

    private static readonly string _symbChars = "©★☆ټҖΩѬѼϗϬϞѦæԆӬҊѮ¿¶ϾϿΨҨ҂ϘƛѢ";

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
        SubmitBtnSel.OnInteract += SubmitPress;
        Module.OnActivate += Activate;
        var rnd = RuleSeedable.GetRNG();
        var imgIx = Enumerable.Range(0, 27).ToArray();
        var list = new List<int>();
        var remainingImgs = new List<int>();
        for (int i = 0; i < (SIZE * SIZE); i++)
        {
            if (remainingImgs.Count == 0)
            {
                remainingImgs.AddRange(imgIx);
                rnd.ShuffleFisherYates(remainingImgs);
            }
            var ix = rnd.Next(0, remainingImgs.Count);
            list.Add(remainingImgs[ix]);
            _imageGrid[i] = remainingImgs[ix];
            remainingImgs.RemoveAt(ix);
        }
        Debug.LogFormat("[Not Symbolic Password #{0}] Using rule seed {1}.", _moduleId, rnd.Seed);
        Generate();
    }

    private void Activate()
    {
        for (int i = 0; i < 6; i++)
            SymbolObjs[i].SetActive(true);
        StartCoroutine(ShuffleAnimation());
    }

    private void Generate()
    {
        var rndStart = Rnd.Range(0, (SIZE * SIZE) - SIZE);
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 3; j++)
                _currentPosition[i * 3 + j] = (rndStart + j + (i * SIZE)) % (SIZE * SIZE);
        for (int img = 0; img < SymbolObjs.Length; img++)
            SymbolObjs[img].GetComponent<MeshRenderer>().material = SymbolMats[_imageGrid[_currentPosition[img]]];
        newShuff:
        for (int presses = 0; presses < 100; presses++)
        {
            var rndArr = new int[] { 0, 1, 2, 6, 7 };
            var rand = rndArr[Rnd.Range(0, 5)];
            MovePosition(rand, true);
        }
        if (CheckEquality())
            goto newShuff;
        for (int img = 0; img < SymbolObjs.Length; img++)
            SymbolObjs[img].GetComponent<MeshRenderer>().material = SymbolMats[_imageGrid[_currentPosition[img]]];
        Debug.LogFormat("[Not Symbolic Password #{0}] Starting position: {1}", _moduleId, _currentPosition.Select(i => _symbChars[_imageGrid[i]]).Join(""));
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int btn)
    {
        return delegate ()
        {
            ArrowBtnSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved || _isAnimating)
                return false;
            Audio.PlaySoundAtTransform("SPPress", transform);
            MovePosition(btn, false);
            for (int img = 0; img < SymbolObjs.Length; img++)
                SymbolObjs[img].GetComponent<MeshRenderer>().material = SymbolMats[_imageGrid[_currentPosition[img]]];
            return false;
        };
    }

    private bool SubmitPress()
    {
        SubmitBtnSel.AddInteractionPunch(0.5f);
        if (_moduleSolved || _isAnimating)
            return false;
        if (CheckEquality())
        {
            Debug.LogFormat("[Not Symbolic Password #{0}] Successfully submitted {1}. Module solved.", _moduleId, _currentPosition.Select(i => _symbChars[_imageGrid[i]]).Join(""));
            _currentStage++;
            if (_currentStage != 1)
                Generate();
            StartCoroutine(ShuffleAnimation());
            return false;
        }
        Module.HandleStrike();
        Debug.LogFormat("[Not Symbolic Password #{0}] Incorrectly submitted {1}, which is not a valid position. Strike.", _moduleId, _currentPosition.Select(i => _symbChars[_imageGrid[i]]).Join(""));
        return false;
    }

    private void MovePosition(int dir, bool init)
    {
        switch (dir)
        {
            case 0:
                _currentPosition[0] = (_currentPosition[0] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                _currentPosition[3] = (_currentPosition[3] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                break;
            case 1:
                _currentPosition[1] = (_currentPosition[1] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                _currentPosition[4] = (_currentPosition[4] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                break;
            case 2:
                _currentPosition[2] = (_currentPosition[2] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                _currentPosition[5] = (_currentPosition[5] + (SIZE * SIZE) - SIZE) % (SIZE * SIZE);
                break;
            case 3:
                _currentPosition[0] = (_currentPosition[0] + SIZE) % (SIZE * SIZE);
                _currentPosition[3] = (_currentPosition[3] + SIZE) % (SIZE * SIZE);
                break;
            case 4:
                _currentPosition[1] = (_currentPosition[1] + SIZE) % (SIZE * SIZE);
                _currentPosition[4] = (_currentPosition[4] + SIZE) % (SIZE * SIZE);
                break;
            case 5:
                _currentPosition[2] = (_currentPosition[2] + SIZE) % (SIZE * SIZE);
                _currentPosition[5] = (_currentPosition[5] + SIZE) % (SIZE * SIZE);
                break;
            case 6:
                _currentPosition[0] = _currentPosition[0] / SIZE * SIZE + ((_currentPosition[0] % SIZE) + (SIZE - 1)) % SIZE;
                _currentPosition[1] = _currentPosition[1] / SIZE * SIZE + ((_currentPosition[1] % SIZE) + (SIZE - 1)) % SIZE;
                _currentPosition[2] = _currentPosition[2] / SIZE * SIZE + ((_currentPosition[2] % SIZE) + (SIZE - 1)) % SIZE;
                break;
            case 7:
                _currentPosition[0] = _currentPosition[0] / SIZE * SIZE + ((_currentPosition[0] % SIZE) + 1) % SIZE;
                _currentPosition[1] = _currentPosition[1] / SIZE * SIZE + ((_currentPosition[1] % SIZE) + 1) % SIZE;
                _currentPosition[2] = _currentPosition[2] / SIZE * SIZE + ((_currentPosition[2] % SIZE) + 1) % SIZE;
                break;
            case 8:
                _currentPosition[3] = _currentPosition[3] / SIZE * SIZE + ((_currentPosition[3] % SIZE) + (SIZE - 1)) % SIZE;
                _currentPosition[4] = _currentPosition[4] / SIZE * SIZE + ((_currentPosition[4] % SIZE) + (SIZE - 1)) % SIZE;
                _currentPosition[5] = _currentPosition[5] / SIZE * SIZE + ((_currentPosition[5] % SIZE) + (SIZE - 1)) % SIZE;
                break;
            case 9:
                _currentPosition[3] = _currentPosition[3] / SIZE * SIZE + ((_currentPosition[3] % SIZE) + 1) % SIZE;
                _currentPosition[4] = _currentPosition[4] / SIZE * SIZE + ((_currentPosition[4] % SIZE) + 1) % SIZE;
                _currentPosition[5] = _currentPosition[5] / SIZE * SIZE + ((_currentPosition[5] % SIZE) + 1) % SIZE;
                break;
            default:
                break;
        }
    }

    private bool CheckEquality()
    {
        return _currentPosition[0] == _currentPosition[1] / SIZE * SIZE + ((_currentPosition[1] % SIZE) + (SIZE - 1)) % SIZE && _currentPosition[0] == _currentPosition[2] / SIZE * SIZE + ((_currentPosition[2] % SIZE) + (SIZE - 2)) % SIZE && _currentPosition[0] == (_currentPosition[3] + (SIZE * SIZE - SIZE)) % (SIZE * SIZE);
    }

    private IEnumerator ShuffleAnimation()
    {
        _isAnimating = true;
        if (_currentStage != 0)
            Audio.PlaySoundAtTransform("SPCorrect", transform);
        for (int i = 0; i < 37; i++)
        {
            for (int obj = 0; obj < 6; obj++)
            {
                if (i / 6 >= obj + 1)
                {
                    if (i % 6 == 0)
                        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    if (_currentStage != 1)
                        SymbolObjs[obj].GetComponent<MeshRenderer>().material = SymbolMats[_imageGrid[_currentPosition[obj]]];
                    else
                    {
                        SymbolObjs[obj].SetActive(false);
                        ZeroObjs[obj].SetActive(true);
                    }
                }
                else
                    SymbolObjs[obj].GetComponent<MeshRenderer>().material = SymbolMats[_imageGrid[Rnd.Range(0, SymbolMats.Length)]];
            }
            yield return new WaitForSeconds(0.1f);
        }
        if (_currentStage == 1)
        {
            _moduleSolved = true;
            Module.HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            yield break;
        }
        _isAnimating = false;
    }
#pragma warning disable 0414
    private string TwitchHelpMessage = "!{0} r1l4 [Moves row 1 left 4 times.] | !{0} c3d5 [Moves column 3 down 5 times.] | !{0} submit [Presses the submit button.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var m = Regex.Match(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            yield return "strike";
            yield return "solve";
            while (_isAnimating)
                yield return null;
            SubmitBtnSel.OnInteract();
            yield break;
        }
        var list = new List<KMSelectable>();
        var parameters = command.Split(' ');
        for (int i = 0; i < parameters.Length; i++)
        {
            var s = parameters[i];
            var sel = TPSelDecider(s);
            if (sel == null)
            {
                yield return "sendtochaterror " + s + " is not a valid command!";
                yield break;
            }
            int val;
            if (!int.TryParse(s.Substring(3), out val))
            {
                yield return "sendtochaterror " + s + " is not a valid command!";
                yield break;
            }
            if (val < 0)
            {
                yield return "sendtochaterror you cant press a button less than 1 times!";
                yield break;
            }
            if (val > 16)
            {
                yield return "sendtochaterror you cant press a button more than 16 times!";
                yield break;
            }
            for (int j = 0; j < val; j++)
                list.Add(sel);
        }
        yield return null;
        while (_isAnimating)
            yield return null;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private KMSelectable TPSelDecider(string s)
    {
        if (s.Length < 4)
            return null;
        if (s[0] == 'r')
        {
            if (s[1] == '1')
            {
                if (s[2] == 'l')
                    return ArrowBtnSels[6];
                else if (s[2] == 'r')
                    return ArrowBtnSels[7];
                else
                    return null;
            }
            else if (s[1] == '2')
            {
                if (s[2] == 'l')
                    return ArrowBtnSels[8];
                else if (s[2] == 'r')
                    return ArrowBtnSels[9];
                else
                    return null;
            }
            else
                return null;
        }
        else if (s[0] == 'c')
        {
            if (s[1] == '1')
            {
                if (s[2] == 'u')
                    return ArrowBtnSels[0];
                else if (s[2] == 'd')
                    return ArrowBtnSels[3];
                else
                    return null;
            }
            else if (s[1] == '2')
            {
                if (s[2] == 'u')
                    return ArrowBtnSels[1];
                else if (s[2] == 'd')
                    return ArrowBtnSels[4];
                else
                    return null;
            }
            else if (s[1] == '3')
            {
                if (s[2] == 'u')
                    return ArrowBtnSels[2];
                else if (s[2] == 'd')
                    return ArrowBtnSels[5];
                else
                    return null;
            }
        }
        return null;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int s = _currentStage; s < 1; s++)
        {
            while (_isAnimating)
                yield return true;
            while (_currentPosition[0] != _currentPosition[1] / SIZE * SIZE + ((_currentPosition[1] % SIZE) + (SIZE - 1)) % SIZE)
            {
                ArrowBtnSels[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (_currentPosition[0] != _currentPosition[2] / SIZE * SIZE + ((_currentPosition[2] % SIZE) + (SIZE - 2)) % SIZE)
            {
                ArrowBtnSels[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (_currentPosition[0] != (_currentPosition[3] + (SIZE * SIZE - SIZE)) % (SIZE * SIZE))
            {
                ArrowBtnSels[9].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            SubmitBtnSel.OnInteract();
        }
        while (!_moduleSolved)
            yield return true;
    }
}
