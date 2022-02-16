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

    public KMSelectable ResetBtnSel;
    public KMSelectable[] ArrowBtnSels;
    public GameObject[] SymbolObjs;
    public Material[] SymbolMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int btn)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            
            return false;
        };
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = "Phase 0: !{0} press submit [Presses the submit button.] | 'press' is optional.";
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
