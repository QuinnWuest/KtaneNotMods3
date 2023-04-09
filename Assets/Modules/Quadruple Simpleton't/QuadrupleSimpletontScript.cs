using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class QuadrupleSimpletontScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] ButtonSels;
    public TextMesh[] ButtonTexts;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly string[] _btnPosNames = new string[] { "top left", "top right", "bottom left", "bottom right" };
    private static readonly string[] _btnTextOptions = new string[] { "PUSH ME!", "BUSH IT!", "BUSH ME!", "PUSH HE!", "BUSH HE!", "HUSH IT!", "HUSH ME!", "HUSH HE!" };
    private int _trueBtn;
    private bool _isEvenTimer;
    private bool[] _isHighlighted = new bool[4];
    private int[][] _fakeBtnTexts = new int[4][] { new int[2], new int[2], new int[2], new int[2] };
    private int[][] _trueBtnTexts = new int[4][] { new int[2], new int[2], new int[2], new int[2] };
    private int[] _tableValues = new int[64];
    private List<int> _btnValuesEven = new List<int>();
    private List<int> _btnValuesOdd = new List<int>();
    private List<int> _allBtnValues = new List<int>();
    private List<int> _presses = new List<int>();
    private List<int> _solution = new List<int>();
    private List<string> _trueTxtsLogEven = new List<string>();
    private List<string> _trueTxtsLogOdd = new List<string>();
    private List<string> _fakeTxtsLogEven = new List<string>();
    private List<string> _fakeTxtsLogOdd = new List<string>();
    private int[] _btnPos = new int[3];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        var rnd = RuleSeedable.GetRNG();
        if (rnd.Seed != 1)
            Debug.LogFormat("[Quadruple Simpleton't #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);
        _tableValues = rnd.ShuffleFisherYates(Enumerable.Range(0, 64).ToArray()).Select(i => i + 1).ToArray();
        for (int i = 0; i < ButtonSels.Length; i++)
        {
            int j = i;
            ButtonSels[i].OnInteract += delegate () { ButtonPress(j); return false; };
            ButtonSels[i].OnInteractEnded += delegate () { ButtonRelease(j); };
            ButtonSels[i].OnHighlight += delegate () { ButtonHighlight(j); };
            ButtonSels[i].OnHighlightEnded += delegate () { ButtonHighlightEnd(j); };
        }
        tryAgain:
        _trueBtn = Rnd.Range(0, 4);
        for (int i = 0; i < _fakeBtnTexts.Length; i++)
            _fakeBtnTexts[i] = Enumerable.Range(0, _btnTextOptions.Length).ToArray().Shuffle().Take(2).ToArray();
        for (int i = 0; i < _trueBtnTexts.Length; i++)
            _trueBtnTexts[i] = Enumerable.Range(0, _btnTextOptions.Length).ToArray().Shuffle().Take(2).ToArray();
        _trueTxtsLogEven = new List<string>();
        _trueTxtsLogOdd = new List<string>();
        _fakeTxtsLogEven = new List<string>();
        _fakeTxtsLogOdd = new List<string>();
        _btnValuesEven = new List<int>();
        _btnValuesOdd = new List<int>();
        _allBtnValues = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (i == _trueBtn)
                continue;
            _trueTxtsLogEven.Add(_btnTextOptions[_trueBtnTexts[i][0]]);
            _trueTxtsLogOdd.Add(_btnTextOptions[_trueBtnTexts[i][1]]);
            _fakeTxtsLogEven.Add(_btnTextOptions[_fakeBtnTexts[i][0]]);
            _fakeTxtsLogOdd.Add(_btnTextOptions[_fakeBtnTexts[i][1]]);
        }

        for (int i = 0; i < 4; i++)
        {
            if (i == _trueBtn)
                continue;
            _btnValuesEven.Add(_tableValues[_fakeBtnTexts[i][0] + _trueBtnTexts[i][0] * 8]);
            _btnValuesOdd.Add(_tableValues[_fakeBtnTexts[i][1] + _trueBtnTexts[i][1] * 8]);
            _allBtnValues.Add(_tableValues[_fakeBtnTexts[i][0] + _trueBtnTexts[i][0] * 8]);
            _allBtnValues.Add(_tableValues[_fakeBtnTexts[i][1] + _trueBtnTexts[i][1] * 8]);
        }
        if (_allBtnValues.Distinct().Count() != 6)
            goto tryAgain;
        for (int i = 0; i < _btnPos.Length; i++)
            _btnPos[i] = i < _trueBtn ? i : i + 1;
        StartCoroutine(CycleText());
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The true button is at the {1} position.", _moduleId, _btnPosNames[_trueBtn]);
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The texts on the true button while highlighting the fake buttons on an even digit are: '{1}'.", _moduleId, _fakeTxtsLogEven.Join("', '"));
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The texts on the true button while highlighting the fake buttons on an odd digit are: '{1}'.", _moduleId, _fakeTxtsLogOdd.Join("', '"));
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The texts on the fake buttons while highlighting the true button on an even digit are: '{1}'.", _moduleId, _trueTxtsLogEven.Join("', '"));
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The texts on the fake buttons while highlighting the true button on an odd digit are: '{1}'.", _moduleId, _trueTxtsLogOdd.Join("', '"));
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The even values of the fake buttons are: {1}", _moduleId, _btnValuesEven.Join(" "));
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The odd values of the fake buttons are: {1}", _moduleId, _btnValuesOdd.Join(" "));
        _allBtnValues.Sort();
        for (int i = 0; i < _allBtnValues.Count; i++)
        {
            if (_btnValuesEven[0] == _allBtnValues[i] || _btnValuesOdd[0] == _allBtnValues[i])
                _solution.Add(0);
            else if (_btnValuesEven[1] == _allBtnValues[i] || _btnValuesOdd[1] == _allBtnValues[i])
                _solution.Add(1);
            else if (_btnValuesEven[2] == _allBtnValues[i] || _btnValuesOdd[2] == _allBtnValues[i])
                _solution.Add(2);
        }
        Debug.LogFormat("[Quadruple Simpleton't #{0}] The order to press the fake buttons are: {1}", _moduleId, _solution.Select(i => _btnPosNames[_btnPos[i]]).Join(", "));
    }

    private void ButtonPress(int btn)
    {
        ButtonSels[btn].AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, ButtonSels[btn].transform);
        if (_moduleSolved)
            return;
        if (btn == _trueBtn)
        {
            {
                if (_solution.Count != _presses.Count)
                    goto strike;
                for (int i = 0; i < _solution.Count; i++)
                    if (_solution[i] != _presses[i])
                        goto strike;
                _moduleSolved = true;
                Module.HandlePass();
                Audio.PlaySoundAtTransform("Victory", transform);
                for (int i = 0; i < 4; i++)
                    ButtonTexts[i].text = "VICTORY!";
                Debug.LogFormat("[Quadruple Simpleton't #{0}] Successfully submitted {1}. Module solved.", _moduleId, _presses.Select(i => _btnPosNames[_btnPos[i]]).Join(", "));
                return;
            }
            strike:
            Module.HandleStrike();
            Debug.LogFormat("[Quadrule Simpleton't #{0}] Incorrectly submitted: {1}. Strike.", _moduleId, _presses.Count == 0 ? "nothing" : _presses.Select(i => _btnPosNames[_btnPos[i]]).Join(", "));
            _presses = new List<int>();
            return;
        }
        var val = btn > _trueBtn ? btn - 1 : btn;
        _presses.Add(val);
    }

    private void ButtonRelease(int btn)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, ButtonSels[btn].transform);
        if (_moduleSolved)
            return;
    }

    private void ButtonHighlight(int btn)
    {
        if (_moduleSolved)
            return;
        _isHighlighted[btn] = true;
    }

    private void ButtonHighlightEnd(int btn)
    {
        if (_moduleSolved)
            return;
        for (int i = 0; i < 4; i++)
        {
            _isHighlighted[i] = false;
            ButtonTexts[i].text = "PUSH IT!";
        }
    }

    private IEnumerator CycleText()
    {
        while (!_moduleSolved)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i != _trueBtn && _isHighlighted[i])
                    ButtonTexts[_trueBtn].text = _btnTextOptions[_fakeBtnTexts[i][_isEvenTimer ? 0 : 1]];
                else if (_isHighlighted[i])
                {
                    for (int j = 0; j < 4; j++)
                        if (j != _trueBtn)
                            ButtonTexts[j].text = _btnTextOptions[_trueBtnTexts[j][_isEvenTimer ? 0 : 1]];
                }
            }
            yield return null;
        }
    }

    private void Update()
    {
        var even = (int)BombInfo.GetTime() % 2 == 0;
        if (_isEvenTimer != even)
            _isEvenTimer = even;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press TL TR BL BR [Presses the top left, top right, bottom left, bottom right buttons.] | !{0} highlight TL [Highlights the top left button.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            var cmdArr = new string[] { "A1", "B1", "A2", "B2", "TL", "TR", "BL", "BR", "A", "B", "C", "D", "1", "2", "3", "4" };
            var pressList = new List<string>();
            for (int i = 1; i < parameters.Length; i++)
            {
                var cmd = parameters[i].ToUpperInvariant();
                if (!cmdArr.Contains(cmd))
                {
                    yield return "sendtochaterror The given input '" + cmd + "' is invalid!";
                    yield break;
                }
                pressList.Add(cmd);
            }
            for (int i = 0; i < pressList.Count; i++)
            {
                ButtonSels[Array.IndexOf(cmdArr, pressList[i]) % 4].OnInteract();
                yield return new WaitForSeconds(0.1f);
                ButtonSels[Array.IndexOf(cmdArr, pressList[i]) % 4].OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*highlight\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            var cmdArr = new string[] { "A1", "B1", "A2", "B2", "TL", "TR", "BL", "BR", "A", "B", "C", "D", "1", "2", "3", "4" };
            if (parameters.Length != 2)
            {
                yield return "sendtochaterror Too many parameters!";
                yield break;
            }
            var cmd = parameters[1].ToUpperInvariant();
            if (!cmdArr.Contains(cmd))
            {
                yield return "sendtochaterror The given input '" + cmd + "' is invalid!";
                yield break;
            }
            ButtonSels[Array.IndexOf(cmdArr, cmd) % 4].OnHighlight();
            yield return new WaitForSeconds(6f);
            ButtonSels[Array.IndexOf(cmdArr, cmd) % 4].OnHighlightEnded();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < _presses.Count; i++)
            if (_presses[i] != _solution[i])
                _presses = new List<int>();
        if (_presses.Count > 6)
            _presses = new List<int>();
        for (int i = _presses.Count; i < 6; i++)
        {
            ButtonSels[_btnPos[_solution[i]]].OnInteract();
            yield return new WaitForSeconds(0.1f);
            ButtonSels[_btnPos[_solution[i]]].OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        ButtonSels[_trueBtn].OnInteract();
        yield return new WaitForSeconds(0.1f);
        ButtonSels[_trueBtn].OnInteractEnded();
        yield return new WaitForSeconds(0.1f);
    }
}
