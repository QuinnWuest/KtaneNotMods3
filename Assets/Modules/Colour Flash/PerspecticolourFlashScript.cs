using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class PerspecticolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public TextMesh ScreenText;
    public GameObject[] ButtonObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine[] _pressAnimations = new Coroutine[2];

    private static readonly int[][] _nets = new int[24][] {
        new int[6] { 1, 2, 3, 4, 5, 6 },
        new int[6] { 1, 3, 4, 5, 2, 6 },
        new int[6] { 1, 4, 5, 2, 3, 6 },
        new int[6] { 1, 5, 2, 3, 4, 6 },
        new int[6] { 2, 1, 5, 6, 3, 4 },
        new int[6] { 2, 3, 1, 5, 6, 4 },
        new int[6] { 2, 5, 6, 3, 1, 4 },
        new int[6] { 2, 6, 3, 1, 5, 4 },
        new int[6] { 3, 1, 2, 6, 4, 5 },
        new int[6] { 3, 2, 6, 4, 1, 5 },
        new int[6] { 3, 4, 1, 2, 6, 5 },
        new int[6] { 3, 6, 4, 1, 2, 5 },
        new int[6] { 4, 1, 3, 6, 5, 2 },
        new int[6] { 4, 3, 6, 5, 1, 2 },
        new int[6] { 4, 5, 1, 3, 6, 2 },
        new int[6] { 4, 6, 5, 1, 3, 2 },
        new int[6] { 5, 1, 4, 6, 2, 3 },
        new int[6] { 5, 2, 1, 4, 6, 3 },
        new int[6] { 5, 4, 6, 2, 1, 3 },
        new int[6] { 5, 6, 2, 1, 4, 3 },
        new int[6] { 6, 2, 5, 4, 3, 1 },
        new int[6] { 6, 3, 2, 5, 4, 1 },
        new int[6] { 6, 4, 3, 2, 5, 1 },
        new int[6] { 6, 5, 4, 3, 2, 1 }
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        YesButton.OnInteract += YesPress;
        NoButton.OnInteract += NoPress;
        YesButton.OnInteractEnded += YesRelease;
        NoButton.OnInteractEnded += NoRelease;
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
