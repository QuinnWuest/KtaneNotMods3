using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

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

    private static readonly int[][][] _quinaryLogicResults = new int[5][][]
    {
        new int[5][] { new int[5] { 0, 1, 2, 3, 4 },  new int[5] { 1, 1, 2, 3, 4 }, new int[5] { 2, 2, 2, 3, 4 }, new int[5] { 3, 3, 3, 3, 4 }, new int[5] { 4, 4, 4, 4, 4 }, },
        new int[5][] { new int[5] { 0, 0, 0, 0, 0 },  new int[5] { 0, 1, 1, 1, 1 }, new int[5] { 0, 1, 2, 2, 2 }, new int[5] { 0, 1, 2, 3, 3 }, new int[5] { 0, 1, 2, 3, 4 }, },
        new int[5][] { new int[5] { 0, 1, 2, 3, 4 },  new int[5] { 1, 2, 3, 4, 0 }, new int[5] { 2, 3, 4, 0, 1 }, new int[5] { 3, 4, 0, 1, 2 }, new int[5] { 4, 0, 1, 2, 3 }, },
        new int[5][] { new int[5] { 4, 4, 4, 4, 4 },  new int[5] { 3, 4, 3, 4, 3 }, new int[5] { 2, 2, 4, 4, 2 }, new int[5] { 1, 2, 3, 4, 1 }, new int[5] { 0, 0, 0, 0, 4 }, },
        new int[5][] { new int[5] { 2, 1, 0, 0, 0 },  new int[5] { 3, 2, 1, 0, 0 }, new int[5] { 4, 3, 2, 1, 0 }, new int[5] { 4, 4, 3, 2, 1 }, new int[5] { 4, 4, 4, 3, 2 } }
    };

    private static readonly string[] _flashSounds = new string[] { "NotPerspectivePegs0", "NotPerspectivePegs1", "NotPerspectivePegs2", "NotPerspectivePegs3", "NotPerspectivePegs4" };
    private static readonly string[] _colorNames = new string[] { "BLUE", "GREEN", "PURPLE", "RED", "YELLOW" };
    private static readonly string[] _pegPositions = new string[] { "TOP", "TOP-RIGHT", "BOTTOM-RIGHT", "BOTTOM-LEFT", "TOP-LEFT" };

    private bool _canInteract;
    private int[] _pegColors = new int[25];
    private bool _colorblindMode;

    private int[] _flashPegPosition = new int[5];
    private int[] _flashPegPerspective = new int[5];
    private int[] _flashPegColor = new int[5];
    private int[] _flashPegColorIx = new int[5];

    private int[] _pegAnswers = new int[5];
    private int _currentStage;
    private Coroutine _flashSequence;
    private Coroutine _timer;
    private int _pressIx;
    private bool _hasInteracted;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        Module.OnActivate += Activate;
        SetColorblindMode(_colorblindMode);
        for (int i = 0; i < PegSels.Length; i++)
            PegSels[i].OnInteract += PegPress(i);
        _flashSequence = StartCoroutine(FlashSequence());
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
        for (int i = 0; i < 5; i++)
        {
            _flashPegPosition[i] = Rnd.Range(0, 5);
            _flashPegPerspective[i] = Rnd.Range(0, 5);
            _flashPegColor[i] = _pegColors[_flashPegPosition[i] * 5 + _flashPegPerspective[i]];
            _flashPegColorIx[i] = _flashPegPosition[i] * 5 + _flashPegPerspective[i];
            _pegAnswers[i] = (_quinaryLogicResults[(_flashPegPosition[i] + (5 - i)) % 5][(_flashPegPerspective[i] + (5 - i)) % 5][_flashPegColor[i]] + i) % 5;
            Debug.LogFormat("[Not Perspective Pegs #{0}] Stage {1}.", _moduleId, i + 1);
            Debug.LogFormat("[Not Perspective Pegs #{0}] Without offset: Position {1}, Perspective {2}, Color {3}", _moduleId, _flashPegPosition[i], _flashPegPerspective[i], _colorNames[_flashPegColor[i]]);
            Debug.LogFormat("[Not Perspective Pegs #{0}] With offset: Position {1}, Perspective {2}, Color {3}", _moduleId, (_flashPegPosition[i] + (5 - i)) % 5, (_flashPegPerspective[i] + (5 - i)) % 5, _colorNames[_flashPegColor[i]]);
            Debug.LogFormat("[Not Perspective Pegs #{0}] Resulting peg, with offset: {1}", _moduleId, _pegAnswers[i]);
            Debug.LogFormat("[Not Perspective Pegs #{0}] Resulting peg position: {1}", _moduleId, _pegPositions[_pegAnswers[i]]);
        }
    }

    private void Activate()
    {
        Audio.PlaySoundAtTransform("NotPerspectivePegsActivate", transform);
        StartCoroutine(RaisePegs());
    }

    private IEnumerator RaisePegs()
    {
        var duration = 0.75f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < 5; i++)
                PegObjs[i].transform.localPosition = new Vector3(PegObjs[i].transform.localPosition.x, PegObjs[i].transform.localPosition.y, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int i = 0; i < 5; i++)
            PegObjs[i].transform.localPosition = new Vector3(PegObjs[i].transform.localPosition.x, PegObjs[i].transform.localPosition.y, 0.25f);
        _canInteract = true;
    }

    private IEnumerator FlashSequence()
    {
        yield return new WaitForSeconds(0.75f);
        while (true)
        {
            for (int i = 0; i < _currentStage + 1; i++)
            {
                yield return new WaitForSeconds(0.3f);
                if (_hasInteracted)
                    Audio.PlaySoundAtTransform(_flashSounds[_flashPegColor[i]], transform);
                PegFaceObjs[_flashPegColorIx[i]].GetComponent<MeshRenderer>().material = OnMats[_flashPegColor[i]];
                yield return new WaitForSeconds(0.3f);
                PegFaceObjs[_flashPegColorIx[i]].GetComponent<MeshRenderer>().material = OffMats[_flashPegColor[i]];
            }
            yield return new WaitForSeconds(0.8f);
        }
    }

    private IEnumerator Timer()
    {
        var elapsed = 0f;
        var duration = 10f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        _pressIx = 0;
        if (_flashSequence != null)
            StopCoroutine(_flashSequence);
        _flashSequence = StartCoroutine(FlashSequence());
    }

    private KMSelectable.OnInteractHandler PegPress(int peg)
    {
        return delegate ()
        {
            if (_moduleSolved || !_canInteract)
                return false;
            // Regular peg pressing
            _hasInteracted = true;
            PegSels[peg].AddInteractionPunch(0.5f);
            Audio.PlaySoundAtTransform(_flashSounds[peg], PegSels[peg].transform);
            if (_flashSequence != null)
                StopCoroutine(_flashSequence);
            for (int i = 0; i < 25; i++)
                PegFaceObjs[i].GetComponent<MeshRenderer>().material = OffMats[_pegColors[i]];

            //Answer checking
            if (peg == _pegAnswers[_pressIx])
            {
                if (_timer != null)
                    StopCoroutine(_timer);
                _timer = StartCoroutine(Timer());
                _pressIx++;
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Not Perspective Pegs #{0}] Pressed the {1} peg when the {2} peg was expected. Strike.", _moduleId, _pegPositions[peg], _pegPositions[_pegAnswers[_pressIx]]);
                _pressIx = 0;
                if (_timer != null)
                    StopCoroutine(_timer);
                _flashSequence = StartCoroutine(FlashSequence());
            }
            if (_pressIx == _currentStage + 1)
            {
                Debug.LogFormat("[Not Perspective Pegs #{0}] Completed Stage {1}.", _moduleId, _currentStage + 1);
                _pressIx = 0;
                _currentStage++;
                if (_timer != null)
                    StopCoroutine(_timer);
                if (_currentStage != 5)
                    _flashSequence = StartCoroutine(FlashSequence());
                else
                {
                    _canInteract = false;
                    StartCoroutine(SolveAnimation());
                }
            }
            return false;
        };
    }

    private void SetColorblindMode(bool mode)
    {
        for (int i = 0; i < 25; i++)
            ColorblindText[i].gameObject.SetActive(mode);
    }

    private IEnumerator SolveAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        Audio.PlaySoundAtTransform("NotPerspectivePegsSolve", transform);
        var duration = 0.05f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[0].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[0].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[0].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            PegObjs[1].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[0].transform.localPosition = new Vector3(0f, 0f, 0.25f);
        PegObjs[1].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.35f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[1].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            PegObjs[2].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[1].transform.localPosition = new Vector3(0f, 0f, 0.25f);
        PegObjs[2].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[2].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            PegObjs[3].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[2].transform.localPosition = new Vector3(0f, 0f, 0.25f);
        PegObjs[3].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.25f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[3].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            PegObjs[4].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[3].transform.localPosition = new Vector3(0f, 0f, 0.25f);
        PegObjs[4].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        yield return new WaitForSeconds(0.25f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            PegObjs[4].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-1.5f, 0.25f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PegObjs[4].transform.localPosition = new Vector3(0f, 0f, 0.25f);
        yield return new WaitForSeconds(0.65f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            for (int peg = 0; peg < 5; peg++)
                PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.25f, -1.5f, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int peg = 0; peg < 5; peg++)
            PegObjs[peg].transform.localPosition = new Vector3(0f, 0f, -1.5f);
        _moduleSolved = true;
        Module.HandlePass();
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = "Phase 0: !{0} press t tr br bl tl [Presses top, top right, bottom right, bottom left, top left pegs.] Pegs can also be numbered from 0-4, 0 at the top, going clockwise.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        var pieces = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length >= 2 && pieces[0] == "press")
        {
            var list = new List<KMSelectable>();
            for (int i = 1; i < pieces.Length; i++)
            {
                switch (pieces[i])
                {
                    case "0":
                    case "t":
                        list.Add(PegSels[0]);
                        break;
                    case "1":
                    case "tr":
                        list.Add(PegSels[1]);
                        break;
                    case "2":
                    case "br":
                        list.Add(PegSels[2]);
                        break;
                    case "3":
                    case "bl":
                        list.Add(PegSels[3]);
                        break;
                    case "4":
                    case "tl":
                        list.Add(PegSels[4]);
                        break;
                    default:
                        yield break;
                }
            }
            yield return null;
            for (int i = 0; i < list.Count; i++)
            {
                list[i].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!_canInteract)
            yield return true;
        while (_currentStage != 5)
        {
            PegSels[_pegAnswers[_pressIx]].OnInteract();
            yield return new WaitForSeconds(0.3f);
        }
        while (!_moduleSolved)
            yield return true;
    }
}
