using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotPerspectivePegsScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;

    public KMSelectable[] PegSels;
    public GameObject[] PegObjs;
    public GameObject[] PegFaceObjs;
    public Material[] OffMats;
    public Material[] OnMats;
    public TextMesh[] ColorblindText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private bool _pegsMoved;
    private int[] _pegColors = new int[25];

    private bool _colorblindMode;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        SetColorblindMode(_colorblindMode);

        for (int i = 0; i < PegSels.Length; i++)
            PegSels[i].OnInteract += PegPress(i);

        var tempPegList = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var tempArr = Enumerable.Range(0, 5).ToArray().Shuffle();
            for (int j = 0; j < 5; j++)
            {
                tempPegList.Add(tempArr[j]);
                _pegColors[i * 5 + j] = tempPegList[i * 5 + j];
                PegFaceObjs[i * 5 + j].GetComponent<MeshRenderer>().material = OffMats[_pegColors[i * 5 + j]];
                ColorblindText[i * 5 + j].text = "BGPRY"[_pegColors[i * 5 + j]].ToString();
            }
        }
    }

    private KMSelectable.OnInteractHandler PegPress(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            return false;
        };
    }

    private void SetColorblindMode(bool mode)
    {
        for (int i = 0; i < 25; i++)
            ColorblindText[i].gameObject.SetActive(mode);
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
