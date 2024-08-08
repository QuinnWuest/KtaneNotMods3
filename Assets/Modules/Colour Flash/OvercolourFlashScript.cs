using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class OvercolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public TextMesh ScreenText;
    public GameObject[] ButtonObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine[] _pressAnimations = new Coroutine[2];

    private static readonly int[][] _baseColorIxsx = new int[36][]
    {
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 0, 4 },
        new int[2] { 1, 2 },
        new int[2] { 1, 3 },
        new int[2] { 1, 5 },
        new int[2] { 1, 6 },
        new int[2] { 1, 7 },
        new int[2] { 2, 1 },
        new int[2] { 2, 3 },
        new int[2] { 2, 5 },
        new int[2] { 2, 7 },
        new int[2] { 3, 1 },
        new int[2] { 3, 2 },
        new int[2] { 3, 5 },
        new int[2] { 3, 6 },
        new int[2] { 5, 2 },
        new int[2] { 5, 6 },
        new int[2] { 5, 6 },
        new int[2] { 6, 7 }
    };

    private static readonly int[][][] _baseTetrominoes = new int[36][][] {
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 0, 2 },
            new int[2] { 0, 3 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 2, 0 },
            new int[2] { 3, 0 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 5, 2 },
            new int[2] { 0, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 1, 1 },
            new int[2] { 2, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 0, 1 },
            new int[2] { 0, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 2, 0 },
            new int[2] { 2, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 0, 2 },
            new int[2] { 1, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 2, 0 },
            new int[2] { 0, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 1, 1 },
            new int[2] { 1, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 4, 1 },
            new int[2] { 5, 1 },
            new int[2] { 0, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 0, 1 },
            new int[2] { 1, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 5, 1 },
            new int[2] { 0, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 1, 1 },
            new int[2] { 1, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 2, 0 },
            new int[2] { 1, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 5, 1 },
            new int[2] { 0, 1 },
            new int[2] { 0, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 5, 1 },
            new int[2] { 0, 1 },
            new int[2] { 1, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 0, 1 },
            new int[2] { 1, 1 },
            new int[2] { 0, 2 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 1, 0 },
            new int[2] { 1, 1 },
            new int[2] { 2, 1 },
        },
        new int[4][] {
            new int[2] { 0, 0 },
            new int[2] { 5, 1 },
            new int[2] { 0, 1 },
            new int[2] { 5, 2 }
        },
        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null
    };

    private int[][] _colorIxs = new int[36][];
    private int[][][] _tetrominoIxs = new int[36][][];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        YesButton.OnInteract += YesPress;
        NoButton.OnInteract += NoPress;
        YesButton.OnInteractEnded += YesRelease;
        NoButton.OnInteractEnded += NoRelease;

        // Rule seed
        var rnd = RuleSeedable.GetRNG();
        var nums = Enumerable.Range(0, 36).ToArray();
        for (int rand = 0; rand < 12; rand++)
            rnd.ShuffleFisherYates(nums);
        for (int i = 0; i < 36; i++)
        {
            _colorIxs[i] = _baseColorIxsx[nums[i]].ToArray();
            if (_baseTetrominoes[nums[i]] != null)
                _tetrominoIxs[i] = _baseTetrominoes[nums[i]].ToArray();
        }
        GenerateFlashSequence();
    }

    private void GenerateFlashSequence()
    {
        // w0 w1 w2 w3 c0 c1 c2 c3
        tryAgain:
        var colors = Enumerable.Range(0, 8).Select(i => Rnd.Range(0, 6)).ToArray();
        if (colors[0] == colors[1] && colors[4] == colors[5] || colors[1] == colors[2] && colors[5] == colors[6] || colors[2] == colors[3] && colors[6] == colors[7])
            goto tryAgain;
        var tablePos = _colorIxs[colors[0] + colors[4] * 6];
        var tetro = _tetrominoIxs[colors[tablePos[0]] + colors[tablePos[1]] * 6];
        if (tetro == null)
            goto tryAgain;
        Debug.Log(colors.Join("-"));
        Debug.Log(_colorIxs.Select(i => i.Join("")).Join(","));
        Debug.Log(tablePos.Join(", "));
        Debug.Log(tetro.Select(i => i.Join("-")).Join(","));
    }

    private bool YesPress()
    {
        YesButton.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (_pressAnimations[0] != null)
            StopCoroutine(_pressAnimations[0]);
        _pressAnimations[0] = StartCoroutine(PressAnimation(0, true));
        if (_moduleSolved)
            return false;
        return false;
    }

    private bool NoPress()
    {
        NoButton.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (_pressAnimations[1] != null)
            StopCoroutine(_pressAnimations[1]);
        _pressAnimations[1] = StartCoroutine(PressAnimation(1, true));
        if (_moduleSolved)
            return false;
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
    private readonly string TwitchHelpMessage = "!{0} help";
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
