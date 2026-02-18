using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NotOrientationCubeScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable SetSel;
    public KMSelectable[] MovementSels;
    public GameObject EyeObj;
    public GameObject ViewDialA;
    public GameObject ViewDialB;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private bool[][] _grids;
    private int[] _xs;
    private const int _size = 4;
    private static readonly string[] _cardinals = new string[] { "north", "east", "south", "west" };
    private static readonly string[] _wallOrientations = new string[] { "in front", "right", "behind", "left" };

    private int _currentGrid;
    private int _currentPosition;
    private int _currentOrientation;
    private bool[] _curGridWalls = new bool[25];

    private Coroutine _rotateViewDialA;
    private Coroutine _rotateViewDialB;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        SetSel.OnInteract += SetPress;
        for (int i = 0; i < MovementSels.Length; i++)
            MovementSels[i].OnInteract += MovementPress(i);

        var rng = RuleSeedable.GetRNG();
        Debug.LogFormat("[Not Orientation Cube #{0}] Using rule seed {1}.", _moduleId, rng.Seed);

        _grids = new bool[12][];
        _xs = new int[12];
        var usedGrids = new List<bool[]>();

        for (var gridIx = 0; gridIx < _grids.Length; gridIx++)
        {
            var grid = FindGrid(new bool[16], 0, usedGrids, rng);
            if (grid == null)
            {
                Debug.LogFormat("<Not Orientation Cube #{0}> Fatal error: no grid!", _moduleId);
                throw new InvalidOperationException();
            }
            _grids[gridIx] = grid;

            var whites = Enumerable.Range(0, 16).Where(i => !grid[i]).ToArray();
            var w = whites[rng.Next(0, 6)];
            var wX = w % 4;
            var wY = w / 4;
            _xs[gridIx] = wY * 5 + wX;

            for (var rot = 0; rot < 4; rot++)
            {
                usedGrids.Add(grid);
                grid = RotateGrid(grid);
            }
        }

        _currentGrid = Rnd.Range(0, _grids.Length);
        Debug.LogFormat("[Not Orientation Cube #{0}] Chose grid #{1}.", _moduleId, _currentGrid + 1);
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (x == 4 || y == 4)
                    _curGridWalls[y * 5 + x] = false;
                else
                    _curGridWalls[y * 5 + x] = _grids[_currentGrid][y * 4 + x];
            }
        }

        var randPos = Enumerable.Range(0, 25).Where(i => !_curGridWalls[i] && GetWalls(i).Count > 1 && i % 5 != 4 && i / 5 != 4).ToArray();
        var rs = randPos.PickRandom();
        _currentPosition = rs;
        _currentOrientation = Rnd.Range(0, 4);
        ViewDialA.transform.localEulerAngles = new Vector3(0, _currentOrientation * 90f, 0);

        Debug.LogFormat("[Not Orientation Cube #{0}] Starting position is at {1}, facing {2}. Walls: {3}", _moduleId, GetPosStr(_currentPosition), _cardinals[_currentOrientation], _moduleId, GetWalls(_currentPosition).Select(b => (b + _currentOrientation) % 4).OrderBy(j => j).Select(j => _wallOrientations[j]).Join(", "));


        _rotateViewDialB = StartCoroutine(RotateViewDialB(_currentPosition));
    }

    private string GetPosStr(int pos)
    {
        if (pos == 24)
            return "outside the corner";
        int x = pos % 5;
        int y = pos / 5;
        if (y == 4)
            return "outside column " + "ABCD"[x];
        if (x == 4)
            return "outside row " + "1234"[y];
        return "ABCD"[x].ToString() + "1234"[y].ToString();
    }

    private List<int> GetWalls(int pos)
    {
        var list = new List<int>();
        int x = pos % 5;
        int y = pos / 5;
        var r = (x + 1) % 5 + y * 5;
        var l = (x + 4) % 5 + y * 5;
        var d = ((y + 1) % 5) * 5 + x;
        var u = ((y + 4) % 5) * 5 + x;
        if (_curGridWalls[u])
            list.Add(0);
        if (_curGridWalls[r])
            list.Add(1);
        if (_curGridWalls[d])
            list.Add(2);
        if (_curGridWalls[l])
            list.Add(3);
        return list;
    }

    private KMSelectable.OnInteractHandler MovementPress(int i)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, MovementSels[i].transform);
            MovementSels[i].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            if (i == 0 || i == 1)
            {
                int x = _currentPosition % 5;
                int y = _currentPosition / 5;

                keepGoing:
                if (_currentOrientation == 0)
                    x = (x + (i * 2 - 1) + 5) % 5;
                else if (_currentOrientation == 2)
                    x = (x + (-i * 2 - 1) + 5) % 5;

                else if (_currentOrientation == 1)
                    y = (y + (i * 2 - 1) + 5) % 5;
                else if (_currentOrientation == 3)
                    y = (y + (-i * 2 - 1) + 5) % 5;

                _currentPosition = ((y * 5 + x) + 25) % 25;
                if (_curGridWalls[_currentPosition])
                    goto keepGoing;

                if (_rotateViewDialB != null)
                    StopCoroutine(_rotateViewDialB);
                _rotateViewDialB = StartCoroutine(RotateViewDialB(_currentPosition));

                Debug.LogFormat("[Not Orientation Cube #{0}] Moved to {1}. Walls: {2}", _moduleId, GetPosStr(_currentPosition), GetWalls(_currentPosition).Select(b => (b + _currentOrientation) % 4).OrderBy(j => j).Select(j => _wallOrientations[j]).Join(", "));
                return false;
            }
            if (i == 2 || i == 3)
            {
                Audio.PlaySoundAtTransform("NotOrientationCubeServo", ViewDialA.transform);
                _currentOrientation = (_currentOrientation + (i * 2 - 1)) % 4;
                if (_rotateViewDialA != null)
                    StopCoroutine(_rotateViewDialA);
                _rotateViewDialA = StartCoroutine(RotateViewDialA(_currentOrientation));

                Debug.LogFormat("[Not Orientation Cube #{0}] Now facing {1}. Walls: {2}", _moduleId, _cardinals[_currentOrientation], _moduleId, GetWalls(_currentPosition).Select(b => (b + _currentOrientation) % 4).OrderBy(j => j).Select(j => _wallOrientations[j]).Join(", "));
                return false;
            }
            return false;
        };
    }

    private IEnumerator RotateViewDialA(int ori)
    {
        var rotStart = ViewDialA.transform.localEulerAngles.y;
        var rotEnd = ori * 90f;

        if (rotStart - rotEnd > 180f)
            rotEnd += 360f;
        if (rotEnd - rotStart > 180f)
            rotStart += 360f;

        var duration = 0.25f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ViewDialA.transform.localEulerAngles = new Vector3(0, Easing.InOutQuad(elapsed, rotStart, rotEnd, duration), 0);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ViewDialA.transform.localEulerAngles = new Vector3(0, rotEnd % 360, 0);
    }

    private IEnumerator RotateViewDialB(int pos)
    {
        var walls = GetWalls(pos);
        if (walls.Count == 0)
        {
            EyeObj.SetActive(false);
            yield break;
        }
        if (!EyeObj.activeInHierarchy)
            EyeObj.SetActive(true);
        int wallIx = 0;
        while (true)
        {
            var rotStart = ViewDialB.transform.localEulerAngles.y;
            var rotEnd = walls[wallIx] * 90f;

            if (rotStart - rotEnd > 180f)
                rotEnd += 360f;
            if (rotEnd - rotStart > 180f)
                rotStart += 360f;

            var duration = 0.35f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                ViewDialB.transform.localEulerAngles = new Vector3(-90, Easing.InOutQuad(elapsed, rotStart, rotEnd, duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            ViewDialB.transform.localEulerAngles = new Vector3(-90, rotEnd, 0);
            yield return new WaitForSeconds(0.65f);
            wallIx = (wallIx + 1) % walls.Count;
        }
    }

    private bool SetPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SetSel.transform);
        SetSel.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        if (_currentPosition == _xs[_currentGrid])
        {
            Debug.LogFormat("[Not Orientation Cube #{0}] Correctly submitted {1}. Module solved.", _moduleId, GetPosStr(_currentPosition));
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, SetSel.transform);
            _moduleSolved = true;
            if (_rotateViewDialA != null)
                StopCoroutine(_rotateViewDialA);
            if (_rotateViewDialB != null)
                StopCoroutine(_rotateViewDialB);
            EyeObj.SetActive(false);
            Module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[Not Orientation Cube #{0}] Incorrectly submitted {1}. Strike.", _moduleId, GetPosStr(_currentPosition));
            Module.HandleStrike();
        }
        return false;
    }

    private static bool[] RotateGrid(bool[] grid, int numberOfTimes = 1)
    {
        for (var n = 0; n < numberOfTimes; n++)
            grid = grid.Select((_, i) => grid[(i % _size) * _size + (_size - 1) - (i / _size)]).ToArray();
        return grid;
    }

    private bool[] FindGrid(bool[] grid, int ix, List<bool[]> gridsAlready, MonoRandom rnd)
    {
        if (ix % _size == 0)
            for (var prevRow = 0; prevRow * _size < ix - _size; prevRow++)
                if (Enumerable.Range(0, _size).All(x => grid[prevRow * _size + x] == grid[ix - _size + x]))
                    return null;
        if (ix == _size * _size)
        {
            rnd.ShuffleFisherYates(grid);

            for (var col = 0; col < _size; col++)
                for (var col2 = 0; col2 < col; col2++)
                    if (Enumerable.Range(0, _size).All(y => grid[y * _size + col] == grid[y * _size + col2]))
                        return null;

            if (grid.Where(i => i).Count() != 10)
                return null;

            if (Enumerable.Range(0, _size).Any(i => Enumerable.Range(i * _size, _size).Select(j => grid[j]).Distinct().Count() == 1))
                return null;

            if (Enumerable.Range(0, _size).Select((_, i) => Enumerable.Range(0, _size).Select(j => grid[j * _size + i])).Any(i => i.Distinct().Count() == 1))
                return null;

            if (gridsAlready.All(gr =>
            {
                for (var j = 0; j < _size * _size; j++)
                    if (gr[j] != grid[j])
                        return true;
                return false;
            }))
                return grid;

            return null;
        }
        var pixel = rnd.Next(0, 2) != 0;
        grid[ix] = pixel;
        var success = FindGrid(grid, ix + 1, gridsAlready, rnd);
        if (success != null)
            return success;
        grid[ix] = !pixel;
        return FindGrid(grid, ix + 1, gridsAlready, rnd);
    }

#pragma warning disable 0414
    private string TwitchHelpMessage = "";
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
