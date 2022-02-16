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

    private bool _pegsRaised;
    private bool _canInteract;
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

    private KMSelectable.OnInteractHandler PegPress(int peg)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            if (!_pegsRaised)
            {
                Debug.LogFormat("[Not Perspective Pegs #{0}] Raising pegs...", _moduleId);
                StartCoroutine(RaiseAllPegs());
                return false;
            }
            if (!_canInteract)
                return false;

            _moduleSolved = true;
            StartCoroutine(SolveAnimation());

            return false;
        };
    }

    private void SetColorblindMode(bool mode)
    {
        for (int i = 0; i < 25; i++)
            ColorblindText[i].gameObject.SetActive(mode);
    }

    private IEnumerator RaiseAllPegs()
    {
        _pegsRaised = true;
        var duration = 1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            for (int peg = 0; peg < 5; peg++)
                PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int peg = 0; peg < 5; peg++)
            PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, 0f);
        _canInteract = true;
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("PPSolveSound", transform);
        var duration = 0.05f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[0].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[0].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[0].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            PegObjs[1].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[0].transform.localPosition = new Vector3(0f, 0f, 0f);
        PegObjs[1].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.35f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[1].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            PegObjs[2].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[1].transform.localPosition = new Vector3(0f, 0f, 0f);
        PegObjs[2].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[2].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            PegObjs[3].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[2].transform.localPosition = new Vector3(0f, 0f, 0f);
        PegObjs[3].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.25f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[3].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            PegObjs[4].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[3].transform.localPosition = new Vector3(0f, 0f, 0f);
        PegObjs[4].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.25f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[4].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[4].transform.localPosition = new Vector3(0f, 0f, 0f);
        yield return new WaitForSeconds(0.65f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            for (int peg = 0; peg < 5; peg++)
                PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int peg = 0; peg < 5; peg++)
            PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        Module.HandlePass();
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
