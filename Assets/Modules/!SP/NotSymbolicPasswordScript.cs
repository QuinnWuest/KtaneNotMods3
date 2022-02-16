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

    private static string _symbChars = "©★☆ټҖΩѬѼϗϬϞѦæԆӬҊѮ¿¶ϾϿΨҨ҂ϘƛѢ";

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
                _currentPosition[i * 3 + j] = rndStart + j + (i * SIZE);
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
        Debug.LogFormat("[Not Symbolic Password #{0}] Stage {1}, Starting position: {2}", _moduleId, _currentStage + 1, _currentPosition.Select(i => _symbChars[_imageGrid[i]]).Join(""));
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
            Debug.LogFormat("[Not Symbolic Password #{0}] Successfully submitted {1}. {2}.", _moduleId, _currentPosition.Select(i => _symbChars[_imageGrid[i]]).Join(""), (_currentStage + 2) != 4 ? "Moving to Stage " + (_currentStage + 2) : "Module solved");
            _currentStage++;
            if (_currentStage != 3)
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
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    if (_currentStage != 3)
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
        if (_currentStage == 3)
        {
            _moduleSolved = true;
            Module.HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            yield break;
        }
        _isAnimating = false;
    }
#pragma warning disable 0414
    private string TwitchHelpMessage = "Phase 0: !{0} press submit [Presses the submit button.] | 'press' is optional.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        var pieces = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length >= 2 && pieces[0] == "cycle")
        {
            if (_isAnimating)
            {
                yield return "sendtochaterror You can't cycle anything while the animation is playing!";
                yield break;
            }
            var list = new List<KMSelectable>();
            int button;
            for (int i = 1; i < pieces.Length; i++)
            {
                switch (pieces[i])
                {
                    case "l": case "left": list.Add(ArrowBtnSels[0]); break;
                    case "m": case "middle": case "c": case "center": case "centre": list.Add(ArrowBtnSels[1]); break;
                    case "r": case "right": list.Add(ArrowBtnSels[2]); break;

                    case "t":
                    case "top":
                    case "u":
                    case "up":
                    case "upper":
                        if ((i + 1) == pieces.Length)
                            yield break;
                        switch (pieces[i + 1])
                        {
                            case "l": case "left": button = 6; break;
                            case "r": case "right": button = 7; break;
                            default: yield break;
                        }
                        list.Add(ArrowBtnSels[button]);
                        i++;
                        break;
                    case "tl":
                    case "topleft":
                    case "ul":
                    case "upleft":
                    case "upperleft":
                        list.Add(ArrowBtnSels[6]);
                        break;

                    case "tr":
                    case "topright":
                    case "ur":
                    case "upright":
                    case "upperright":
                        list.Add(ArrowBtnSels[7]);
                        break;
                    case "b":
                    case "bottom":
                    case "d":
                    case "down":
                    case "lower":
                        if ((i + 1) == pieces.Length)
                            yield break;
                        switch (pieces[i + 1])
                        {
                            case "l": case "left": button = 8; break;
                            case "r": case "right": button = 9; break;
                            default: yield break;
                        }
                        list.Add(ArrowBtnSels[button]);
                        i++;
                        break;
                    case "bl":
                    case "bottomleft":
                    case "dl":
                    case "downleft":
                    case "lowerleft":
                        list.Add(ArrowBtnSels[8]);
                        break;
                    case "br":
                    case "bottomright":
                    case "dr":
                    case "downright":
                    case "lowerright":
                        list.Add(ArrowBtnSels[9]);
                        break;
                    default:
                        yield break;
                }
            }
            yield return null;
            yield return list;
        }
        else if (pieces.Length == 1 && pieces[0] == "submit")
        {
            yield return null;
            yield return SubmitBtnSel;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int s = _currentStage; s < 3; s++)
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
