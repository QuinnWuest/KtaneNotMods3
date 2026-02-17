using KModkit;
using NotModdedModulesVol3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Xsl;
using UnityEngine;
using Rnd = UnityEngine.Random;

public partial class NotLightCycleScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;
    public KMSelectable ButtonSel;

    public GameObject[] ColoredLightObjs;
    public Material[] ColoredLightOffMats;
    public Material[] ColoredLightOnMats;
    public TextMesh[] ColorblindTexts;

    public GameObject[] WhiteLightObjs;
    public Material WhiteLightOffMat;
    public Material WhiteLightOnMat;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _doneWithEverything;
    private bool _colorblindMode;

    private HexGrid _hexGrid;
    private static readonly Hex[] _cornerHexPositions = Enumerable.Range(0, 6).Select(i => new Hex(0, 0).GetNeighbor(i).GetNeighbor(i).GetNeighbor(i)).ToArray();
    private int[] _lightColors = new int[6];
    private HexInfo[] _path;

    private bool _holdingButton;
    private bool _buttonTimerTriggered;
    private Coroutine _buttonHoldTimer;
    private bool _inSubmissionMode;
    private bool _boned;

    private int _thIx;
    private Coroutine _tetraHexAnimation;
    private Coroutine _lightCycleAnimation;
    private int _currentLightColorIx;

    private MarkResult _markResult;
    private List<HexColor> _solutionHexColors = new List<HexColor>();
    private List<HexColor> _inputtedHexColors = new List<HexColor>();

    private static readonly HexColor[] _rgb = new HexColor[] { HexColor.Red, HexColor.Green, HexColor.Blue };
    private bool _goBackwards;

    private static readonly string[] _rules = new string[]
    {
        "Rule A/Z: The previous hex is closer to the center than this hex.",
        "Rule B/Y: The previous hex is adjacent to a corner hex.",
        "Rule C/X: The previous hex shares a color with any of the corner hexes.",
        "Rule D/W: The previous hex is red or white or green.",
        "Rule E/V: The previous hex is on the outer ring of hexes.",
        "Rule F/U: The previous hex is marked.",
        "Rule G/T: The previous hex is not magenta or white or red.",
        "Rule H/S: The previous hex shares its color with this hex.",
        "Rule I/R: The previous hex is the center hex or adjacent to it.",
        "Rule J/Q: The previous hex is white or green or magenta.",
        "Rule K/P: The previous hex is not marked.",
        "Rule L/O: The previous hex does not share a color with this hex.",
        "Rule M/N: The previous hex is red or blue or magenta.",
    };

    public class MarkResult
    {
        public List<HexColor> MarkedColors;
        public List<string> StepLogs;
        public List<int> AppliedRules;

        public MarkResult(List<HexColor> mc, List<string> sl, List<int> ar)
        {
            MarkedColors = mc;
            StepLogs = sl;
            AppliedRules = ar;
        }
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        ButtonSel.OnInteract += ButtonPress;
        ButtonSel.OnInteractEnded += ButtonRelease;

        _colorblindMode = ColorblindMode.ColorblindModeActive;
        _lightColors = Enumerable.Range(0, 6).ToArray().Shuffle();
        for (int i = 0; i < 6; i++)
        {
            SetLightMaterial(i, _lightColors[i], false);
            ColorblindTexts[i].text = ((HexColor)_lightColors[i]).ToString();
        }
        SetColorblindMode(_colorblindMode);

        _hexGrid = GenerateHexagonGrid();
        for (int i = 0; i < _hexGrid.AppliedTetraHexes.Count; i++)
        {
            Debug.LogFormat("[Not Light Cycle #{0}] Tetrahex #{1}: {2}", _moduleId, i + 1, _hexGrid.AppliedTetraHexes[i]);
            Debug.LogFormat("[Not Light Cycle #{0}] Grid: {1}", _moduleId, _hexGrid.LogGridByTetrahexCount(i + 1));
        }
        _path = FindPath(_hexGrid).ToArray();
        Debug.LogFormat("[Not Light Cycle #{0}] Path found from Corner {1} to Corner {2}.", _moduleId, GetCornerPositionFromHex(_path.First().Hex) + 1, GetCornerPositionFromHex(_path.Last().Hex) + 1);
        Debug.LogFormat("[Not Light Cycle #{0}] Hexes of path: {1}.", _moduleId, _path.Join(", "));

        if (_rgb.Contains(_path.First().Color) && _rgb.Contains(_hexGrid.AppliedTetraHexes.Last().Color))
            _goBackwards = true;
        _markResult = CalculateMarkedHexes(_path, _goBackwards);
        _solutionHexColors = _markResult.MarkedColors.ToList();


        Debug.LogFormat("[Not Light Cycle #{0}] {1}", _moduleId, _markResult.StepLogs[0]);
        Debug.LogFormat("[Not Light Cycle #{0}] Starting at Row {1}. Moving {2} the table.", _moduleId, BombInfo.GetSerialNumber()[3], _goBackwards ? "up" : "down");

        for (int i = 1; i < _markResult.StepLogs.Count; i++)
        {
            Debug.LogFormat("[Not Light Cycle #{0}] {1}", _moduleId, _rules[_markResult.AppliedRules[i - 1]]);
            Debug.LogFormat("[Not Light Cycle #{0}] {1}", _moduleId, _markResult.StepLogs[i]);
        }

        Debug.LogFormat("[Not Light Cycle #{0}] Colors to submit: {1}.", _moduleId, _markResult.MarkedColors.Join(", "));
        _thIx = 0;
        StartTetrahexAnimation(_thIx);
    }

    private bool ButtonPress()
    {
        ButtonSel.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (_moduleSolved || _doneWithEverything)
            return false;
        _holdingButton = true;
        if (_inSubmissionMode)
        {
            SubmitCurrentColor(_currentLightColorIx);
            return false;
        }
        if (_buttonTimerTriggered)
            return false;
        if (_buttonHoldTimer != null)
            StopCoroutine(_buttonHoldTimer);
        _buttonHoldTimer = StartCoroutine(ButtonHoldTimer());
        return false;
    }

    private void ButtonRelease()
    {
        if (_moduleSolved || _doneWithEverything)
            return;
        _holdingButton = false;
        if (_boned)
        {
            _boned = false;
            return;
        }
        if (_buttonHoldTimer != null)
            StopCoroutine(_buttonHoldTimer);

        if (_buttonTimerTriggered)
        {
            _buttonTimerTriggered = false;
            if (_thIx != _hexGrid.AppliedTetraHexes.Count - 1)
            {
                Debug.LogFormat("[Not Light Cycle #{0}] Attempted to enter submission after viewing {1} tetrahex{2}, but no path connecting two opposite corners exist yet. Strike.", _moduleId, _thIx + 1, _thIx == 0 ? "" : "es");
                Module.HandleStrike();
                ResetToFirstTetraHex();
                return;
            }

            SubmitCurrentColor(_currentLightColorIx);
            return;
        }

        if (_inSubmissionMode)
        {
            SubmitCurrentColor(_currentLightColorIx);
            return;
        }

        if (_thIx == _hexGrid.AppliedTetraHexes.Count - 1)
        {
            Debug.LogFormat("[Not Light Cycle #{0}] Attempted to generate a new tetrahex, but a path connecting two opposite corners was found. Strike.", _moduleId);
            Module.HandleStrike();
            ResetToFirstTetraHex();
            return;
        }

        _thIx++;
        StartTetrahexAnimation(_thIx);
        return;
    }

    private IEnumerator ButtonHoldTimer()
    {
        yield return new WaitForSeconds(0.75f);
        _buttonTimerTriggered = true;
        if (_tetraHexAnimation != null)
            StopCoroutine(_tetraHexAnimation);
        _lightCycleAnimation = StartCoroutine(AnimateLightCycle(_currentLightColorIx));
        _inSubmissionMode = true;
    }

    private void SubmitCurrentColor(int currentLedIx)
    {
        var shown = (HexColor)_lightColors[currentLedIx];

        int step = _inputtedHexColors.Count;
        if (step >= _solutionHexColors.Count)
            return;
        HexColor expected = _solutionHexColors[step];

        if (shown != expected)
        {
            Debug.LogFormat("[Not Light Cycle #{0}] Submitted {1}, but {2} was expected. Strike.", _moduleId, shown, expected);
            Module.HandleStrike();
            _boned = _holdingButton;
            ResetToFirstTetraHex();
            return;
        }

        _inputtedHexColors.Add(shown);
        Debug.LogFormat("[Not Light Cycle #{0}] Correctly submitted {1}.", _moduleId, shown);
        for (int wLed = 0; wLed < 6; wLed++)
        {
            if (currentLedIx == wLed)
                WhiteLightObjs[wLed].GetComponent<MeshRenderer>().material = WhiteLightOnMat;
            else
                WhiteLightObjs[wLed].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
        }

        int total = _solutionHexColors.Count;
        int submittedSoFar = _inputtedHexColors.Count;
        bool isLast = (submittedSoFar == total);
        string clipName = "LCDing";
        if (isLast)
            clipName += 6;
        else
            clipName += Mathf.Min(submittedSoFar, 5);

        Audio.PlaySoundAtTransform(clipName, transform);

        if (_inputtedHexColors.Count == _solutionHexColors.Count)
        {
            Debug.LogFormat("[Not Light Cycle #{0}] All colors have been submitted. Module solved.", _moduleId);
            StartCoroutine(SolveAnimation());
            _doneWithEverything = true;
            if (_lightCycleAnimation != null)
                StopCoroutine(_lightCycleAnimation);
            if (_tetraHexAnimation != null)
                StopCoroutine(_tetraHexAnimation);
        }
    }

    private void ResetToFirstTetraHex()
    {
        Debug.LogFormat("[Not Light Cycle #{0}] Resetting to the first tetrahex.", _moduleId);
        _inSubmissionMode = false;
        _buttonTimerTriggered = false;

        _inputtedHexColors.Clear();

        if (_buttonHoldTimer != null)
            StopCoroutine(_buttonHoldTimer);
        if (_lightCycleAnimation != null)
            StopCoroutine(_lightCycleAnimation);

        _thIx = 0;
        StartTetrahexAnimation(_thIx);
    }

    private int GetCornerPositionFromHex(Hex hex)
    {
        for (int i = 0; i < 6; i++)
            if (hex.Equals(_cornerHexPositions[i]))
                return i;
        throw new InvalidOperationException(string.Format("Hex {0} is not a corner hex!", hex));
    }

    private void StartTetrahexAnimation(int thix)
    {
        if (_tetraHexAnimation != null)
            StopCoroutine(_tetraHexAnimation);
        for (int led = 0; led < 6; led++)
            WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
        _tetraHexAnimation = StartCoroutine(AnimateTetraHex(_hexGrid.AppliedTetraHexes[thix]));
    }

    private void SetLightMaterial(int ix, int color, bool isOn)
    {
        ColoredLightObjs[ix].GetComponent<MeshRenderer>().material = isOn ? ColoredLightOnMats[color] : ColoredLightOffMats[color];
    }

    private IEnumerator AnimateTetraHex(TetraHex th)
    {
        var colorIx = Array.IndexOf(_lightColors, (int)th.Color);
        _currentLightColorIx = colorIx;
        for (int i = 0; i < 6; i++)
            SetLightMaterial(i, _lightColors[i], i == colorIx);
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < th.NumberSequence.Length; i++)
            {
                for (int led = 0; led < 6; led++)
                {
                    if (th.NumberSequence[i] == led)
                        WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOnMat;
                    else
                        WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
                }
                yield return new WaitForSeconds(0.5f);
                for (int led = 0; led < 6; led++)
                    WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
            }
            for (int led = 0; led < 6; led++)
                WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator AnimateLightCycle(int startingIx)
    {
        for (int wLed = 0; wLed < 6; wLed++)
            WhiteLightObjs[wLed].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
        while (true)
        {
            for (int ix = startingIx; ix < 6 + startingIx; ix++)
            {
                for (int cLed = 0; cLed < 6; cLed++)
                {
                    _currentLightColorIx = ix % 6;
                    if (_currentLightColorIx == cLed)
                        ColoredLightObjs[cLed].GetComponent<MeshRenderer>().material = ColoredLightOnMats[_lightColors[cLed]];
                    else
                        ColoredLightObjs[cLed].GetComponent<MeshRenderer>().material = ColoredLightOffMats[_lightColors[cLed]];
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void SetColorblindMode(bool mode)
    {
        foreach (var text in ColorblindTexts)
            text.gameObject.SetActive(mode);
    }

    private HexGrid GenerateHexagonGrid()
    {
        tryAgain:
        var hg = new HexGrid(new List<HexInfo>(), new List<TetraHex>());
        int maxTetraHexes = 9;

        for (int i = 0; i < maxTetraHexes; i++)
        {
            var th = GenerateTetraHex();
            hg.ApplyTetraHex(th);
            int connected = CountConnectedCornerPairs(hg);
            if (connected > 2)
                goto tryAgain;
            if (connected == 2)
                return hg;
        }
        goto tryAgain;
    }

    private TetraHex GenerateTetraHex()
    {
        NewTetrahex:
        var genSeq = Enumerable.Range(0, 5).Select(i => Rnd.Range(0, 6)).ToArray();
        if (genSeq.Distinct().Count() < 3)
            goto NewTetrahex;
        for (int i = 0; i < 5; i++)
            if (genSeq[i] == genSeq[(i + 1) % 5])
                goto NewTetrahex;

        var hexColor = (HexColor)Rnd.Range(0, 6);
        var hexList = new List<HexInfo> { new HexInfo(_cornerHexPositions[genSeq[0]], hexColor) };

        for (int i = 1; i < genSeq.Length; i++)
        {
            var nextHex = hexList[i - 1].Hex.GetNeighbor(genSeq[i]);
            if (nextHex.Distance > 3)
                goto NewTetrahex;
            hexList.Add(new HexInfo(nextHex, hexColor));
        }
        var list = hexList.Skip(1).ToList();
        if (list.Select(h => h.Hex).Distinct().Count() != 4)
            goto NewTetrahex;

        return new TetraHex(list, genSeq, hexColor);
    }

    private IEnumerable<HexInfo> FindPath(HexGrid hg)
    {
        var connectedStarts = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            var start = _cornerHexPositions[i];
            var end = _cornerHexPositions[(i + 3) % 6];
            if (IsReachable(hg, start, end))
                connectedStarts.Add(i);
        }

        if (connectedStarts.Count == 0)
            return Enumerable.Empty<HexInfo>();

        int startIx;
        var last = hg.AppliedTetraHexes.LastOrDefault();
        if (last != null)
        {
            var lastHexes = last.HexInfo.Select(h => h.Hex);
            var preferred = connectedStarts.Where(ix => lastHexes.Contains(_cornerHexPositions[ix])).ToList();
            startIx = preferred.Count > 0 ? preferred.Min() : connectedStarts.Min();
        }
        else
            startIx = connectedStarts.Min();

        var startHex = _cornerHexPositions[startIx];
        var endHex = _cornerHexPositions[(startIx + 3) % 6];

        var distFromStart = BFSDistances(hg, startHex);
        if (!distFromStart.ContainsKey(endHex))
            return Enumerable.Empty<HexInfo>();

        int shortestLength = distFromStart[endHex];
        var distFromEnd = BFSDistances(hg, endHex);

        var all = new List<int[]>();
        RecurseGetShortestPath(hg, startHex, endHex, distFromStart, distFromEnd, shortestLength, new List<int>(), all);

        if (all.Count == 0)
            return Enumerable.Empty<HexInfo>();

        int maxChanges = all.Max(p => CountColorChanges(hg, startHex, p));
        var best = all.Where(p => CountColorChanges(hg, startHex, p) == maxChanges).ToList();

        if (best.Count > 1)
        {
            var preferredFirstDir = DirectionTowardFinish(hg, startHex, endHex, distFromEnd);
            best.Sort((a, b) => CompareByDivergenceRule(a, b, preferredFirstDir));
        }

        var path = new List<HexInfo>();
        var cur = startHex;
        path.Add(hg.GetHexAt(cur));
        foreach (var d in best[0])
        {
            cur = cur.GetNeighbor(d);
            path.Add(hg.GetHexAt(cur));
        }
        return path;
    }

    private bool IsReachable(HexGrid hg, Hex start, Hex end)
    {
        return BFSDistances(hg, start).ContainsKey(end);
    }

    private Dictionary<Hex, int> BFSDistances(HexGrid hg, Hex start)
    {
        var dist = new Dictionary<Hex, int>();
        var q = new Queue<Hex>();

        if (!ExistsInHexGrid(hg, start))
            return dist;

        dist[start] = 0;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int d = dist[cur];

            for (int dir = 0; dir < 6; dir++)
            {
                var nx = cur.GetNeighbor(dir);
                if (!ExistsInHexGrid(hg, nx))
                    continue;
                if (dist.ContainsKey(nx))
                    continue;

                dist[nx] = d + 1;
                q.Enqueue(nx);
            }
        }
        return dist;
    }

    private bool ExistsInHexGrid(HexGrid hg, Hex h)
    {
        return hg.GetHexAt(h) != null;
    }

    private void RecurseGetShortestPath(HexGrid hg, Hex cur, Hex end, Dictionary<Hex, int> distFromStart, Dictionary<Hex, int> distFromEnd, int shortestLength, List<int> currentDirs, List<int[]> output)
    {
        if (cur == end)
        {
            output.Add(currentDirs.ToArray());
            return;
        }

        int curD = distFromStart[cur];

        for (int dir = 0; dir < 6; dir++)
        {
            var nx = cur.GetNeighbor(dir);
            if (!distFromStart.ContainsKey(nx))
                continue;
            if (distFromStart[nx] != curD + 1)
                continue;
            if (!distFromEnd.ContainsKey(nx))
                continue;
            if (distFromStart[nx] + distFromEnd[nx] != shortestLength)
                continue;

            currentDirs.Add(dir);
            RecurseGetShortestPath(hg, nx, end, distFromStart, distFromEnd, shortestLength, currentDirs, output);
            currentDirs.RemoveAt(currentDirs.Count - 1);
        }
    }

    private int CountColorChanges(HexGrid hg, Hex start, int[] dirs)
    {
        var cur = start;
        var prevColor = hg.GetHexAt(cur).Color;
        int changes = 0;

        foreach (var dir in dirs)
        {
            cur = cur.GetNeighbor(dir);
            var c = hg.GetHexAt(cur).Color;
            if (c != prevColor)
                changes++;
            prevColor = c;
        }
        return changes;
    }

    private int CompareByDivergenceRule(int[] a, int[] b, int preferredFirstDir)
    {
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            if (a[i] == b[i])
                continue;

            int baseDir = (i == 0) ? preferredFirstDir : a[i - 1];
            int ra = RankClockwise(a[i], baseDir);
            int rb = RankClockwise(b[i], baseDir);
            return ra.CompareTo(rb);
        }
        return a.Length.CompareTo(b.Length);
    }

    private int RankClockwise(int dir, int baseDir)
    {
        int d = (dir - baseDir) % 6;
        if (d < 0)
            d += 6;
        return d;
    }

    private int DirectionTowardFinish(HexGrid hg, Hex start, Hex goal, Dictionary<Hex, int> distFromEnd)
    {
        int bestDir = 0;
        int bestDist = int.MaxValue;

        for (int dir = 0; dir < 6; dir++)
        {
            var nx = start.GetNeighbor(dir);
            if (!ExistsInHexGrid(hg, nx))
                continue;
            if (!distFromEnd.ContainsKey(nx))
                continue;

            int d = distFromEnd[nx];
            if (d < bestDist)
            {
                bestDist = d;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private int CountConnectedCornerPairs(HexGrid hg)
    {
        int count = 0;
        for (int i = 0; i < 6; i++)
            if (IsReachable(hg, _cornerHexPositions[i], _cornerHexPositions[(i + 3) % 6]))
                count++;
        return count;
    }

    private MarkResult CalculateMarkedHexes(HexInfo[] path, bool gb)
    {
        bool[] markedHexes = new bool[path.Length];
        markedHexes[0] = true;

        var loggingLines = new List<string>();

        int row = SerialCharToRowIndex(BombInfo.GetSerialNumber()[3]);

        loggingLines.Add(string.Format("Hex #1 with color {0} is marked.", path[0].Color));
        var ar = new List<int>();

        for (int i = 1; i < path.Length; i++)
        {
            var prev = path[i - 1];
            var cur = path[i];

            bool shouldMark = IsThisHexMarked(row, prev, cur, markedHexes[i - 1]);
            ar.Add(row);
            markedHexes[i] = shouldMark;

            loggingLines.Add(string.Format("Hex #{0} with color {1} is {2}.", i + 1, path[i].Color, shouldMark ? "marked" : "not marked"));

            row = (row + (gb ? -1 : 1) + 13) % 13;
        }

        var markedColors = Enumerable.Range(0, path.Length).Where(i => markedHexes[i]).Select(j => path[j].Color).ToList();

        return new MarkResult(markedColors, loggingLines, ar);
    }

    private int SerialCharToRowIndex(char c)
    {
        return Math.Min(c - 'A', 'Z' - c);
    }

    private bool IsThisHexMarked(int row, HexInfo prev, HexInfo cur, bool prevMarked)
    {
        switch (row)
        {
            case 0: // A & Z
                return prev.Hex.Distance < cur.Hex.Distance;
            case 1: // B & Y
                return IsAdjacentToAnyCorner(prev.Hex);
            case 2: // C & X
                return PrevSharesColorWithAnyCorner(prev.Color);
            case 3: // D & W
                return prev.Color == HexColor.Red || prev.Color == HexColor.White || prev.Color == HexColor.Green;
            case 4: // E & V
                return prev.Hex.Distance == 3;
            case 5: // F & U
                return prevMarked;
            case 6: // G & T
                return prev.Color != HexColor.Magenta && prev.Color != HexColor.White && prev.Color != HexColor.Red;
            case 7: // H & S
                return prev.Color == cur.Color;
            case 8: // I & R
                return prev.Hex.Distance <= 1;
            case 9: // J & Q
                return prev.Color == HexColor.White || prev.Color == HexColor.Green || prev.Color == HexColor.Magenta;
            case 10: // K & P
                return !prevMarked;
            case 11: // L & O
                return prev.Color != cur.Color;
            case 12: // M & N
                return prev.Color == HexColor.Red || prev.Color == HexColor.Blue || prev.Color == HexColor.Magenta;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool IsAdjacentToAnyCorner(Hex h)
    {
        foreach (var corner in _cornerHexPositions)
            if (AreAdjacent(h, corner))
                return true;
        return false;
    }

    private bool AreAdjacent(Hex a, Hex b)
    {
        for (int dir = 0; dir < 6; dir++)
            if (a.GetNeighbor(dir).Equals(b))
                return true;
        return false;
    }

    private bool PrevSharesColorWithAnyCorner(HexColor prevColor)
    {
        foreach (var corner in _cornerHexPositions)
        {
            var hi = _hexGrid.GetHexAt(corner);
            if (hi != null && hi.Color == prevColor)
                return true;
        }
        return false;
    }

    private IEnumerator SolveAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        for (int led = 0; led < 6; led++)
        {
            WhiteLightObjs[led].GetComponent<MeshRenderer>().material = WhiteLightOffMat;
            ColoredLightObjs[led].GetComponent<MeshRenderer>().material = ColoredLightOffMats[_lightColors[led]];
        }
        for (int ix = 0; ix < 6; ix++)
        {
            WhiteLightObjs[ix].GetComponent<MeshRenderer>().material = WhiteLightOnMat;
            for (int cLed = 0; cLed < 6; cLed++)
            {
                if (ix == cLed)
                    ColoredLightObjs[cLed].GetComponent<MeshRenderer>().material = ColoredLightOnMats[_lightColors[cLed]];
                else
                    ColoredLightObjs[cLed].GetComponent<MeshRenderer>().material = ColoredLightOffMats[_lightColors[cLed]];
            }
            Audio.PlaySoundAtTransform("LCDing" + (ix + 1), transform);
            yield return new WaitForSeconds(0.05f);
        }
        for (int cLed = 0; cLed < 6; cLed++)
            ColoredLightObjs[cLed].GetComponent<MeshRenderer>().material = ColoredLightOffMats[_lightColors[cLed]];
        _moduleSolved = true;
        Module.HandlePass();
        yield break;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "";
#pragma warning restore 0414

    // Taken from Colored Switches TP support, with a few changes.
    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
