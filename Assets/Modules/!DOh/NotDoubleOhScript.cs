using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotDoubleOhScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] ArrowBtnSels;
    public KMSelectable SubmitBtnSel;
    public GameObject[] LeftSegObjs;
    public GameObject[] RightSegObjs;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly bool[][] _segmentConfigs = new bool[10][]
    {
        new bool[7] { true, true, true, false, true, true, true },
        new bool[7] { false, false, true, false, false, true, false },
        new bool[7] { true, false, true, true, true, false, true },
        new bool[7] { true, false, true, true, false, true, true },
        new bool[7] { false, true, true, true, false, true, false },
        new bool[7] { true, true, false, true, false, true, true },
        new bool[7] { true, true, false, true, true, true, true },
        new bool[7] { true, false, true, false, false, true, false },
        new bool[7] { true, true, true, true, true, true, true },
        new bool[7] { true, true, true, true, false, true, true }
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        SubmitBtnSel.OnInteract += SubmitPress;
        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ArrowBtnSels[btn].transform);
            ArrowBtnSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            Debug.LogFormat("[Not Double-Oh #{0}] Pressed arrow button {1}.", _moduleId, btn + 1);
            return false;
        };
    }

    private bool SubmitPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitBtnSel.transform);
        SubmitBtnSel.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        Debug.LogFormat("[Not Double-Oh #{0}] Pressed submit.", _moduleId);
        return false;
    }
}
