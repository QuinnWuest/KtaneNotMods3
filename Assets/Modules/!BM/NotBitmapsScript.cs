using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class NotBitmapsScript : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule Module;
    public KMAudio Audio;

    public GameObject Screen;
    public Mesh PlaneMesh;
    public KMSelectable[] ButtonSels;
    public MeshRenderer Bitmap;

    private const int _numBitmaps = 4;
    private BitmapInfo[] _bitmaps;
    private int _bitmapIx;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine _cycleBitmaps;

    private static readonly Color[] _lightColors = new[] { new Color(1, .9f, .9f), new Color(.9f, 1, .9f), new Color(.9f, .9f, 1), new Color(1, 1, .9f), new Color(.9f, 1, 1), new Color(1, .9f, 1) };
    private static readonly Color[] _darkColors = new[] { new Color(.75f, .5f, .5f), new Color(.5f, .75f, .5f), new Color(.5f, .5f, .75f), new Color(.75f, .75f, .5f), new Color(.5f, .75f, .75f), new Color(.75f, .5f, .75f) };
    private static readonly string[] _colorNames = new string[] { "red", "green", "blue", "yellow", "cyan", "magenta" };

    private readonly Texture[] _bitmapTextures = new Texture[_numBitmaps];
    private int[] _colorIxs;
    private readonly bool[] _hasBeenSatisfied = new bool[_numBitmaps];
    private int[] _correctButtons = new int[4];

    private static readonly bool[][] _shapeConfigs = new string[]
    {
        ".###.#..###.#.###..#.###.",
        ".###.#.#.###.###.#.#.###.",
        ".#.#.#.#.#.#.#.#.#.#.#.#.",
        ".#.#.#####.#.#.#####.#.#.",
        "..#...#.#..#.#.#...######",
        "..#...#.#.#...#.#.#...#..",
        "..#...###.##.##.###...#..",
        ".###.##..##.#.##..##.###.",
        "#...#.###..#.#..###.#...#",
        "######...#.#.#..#.#...#..",
        "#...###.##.#.#..###...#..",
        ".###.##.###...###.##.###.",
        "##.###...#..#..#...###.##",
        ".#.#.##.##..#..##.##.#.#.",
        "##.###.#.#.###.#.#.###.##",
        "..#...###..#.#.##.###...#"
    }
        .Select(sa => sa.Select(ch => ch == '.').ToArray()).ToArray();

    public KMColorblindMode ColorblindMode;
    public TextMesh ColorblindText;
    private bool _colorblindMode;

    struct BitmapInfo
    {
        public bool[] Pixels;
        public int CenterX;
        public int CenterY;
        public int Which;
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        SetColorblindMode(_colorblindMode);

        _colorIxs = Enumerable.Range(0, _lightColors.Length).ToArray().Shuffle().Take(_numBitmaps).ToArray();
        _bitmaps = new BitmapInfo[_numBitmaps];
        var rnd = new System.Random(Rnd.Range(0, int.MaxValue));
        newBitmaps:
        for (int k = 0; k < _numBitmaps; k++)
            _bitmaps[k] = generateRandomBitmap(rnd);
        if (_bitmaps.Select(i => i.Which).ToArray().Distinct().Count() != _numBitmaps)
            goto newBitmaps;
        for (int i = 0; i < _bitmapTextures.Length; i++)
            _bitmapTextures[i] = GenTexture(i);

        Bitmap.material.shader = Shader.Find("Unlit/Transparent");
        _cycleBitmaps = StartCoroutine(CycleBitmaps());
        for (int i = 0; i < _bitmaps.Length; i++)
        {
            Debug.LogFormat("[Not Bitmaps #{0}] In the {1} bitmap, configuration {2} is present.", _moduleId, _colorNames[_colorIxs[i]], "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_bitmaps[i].Which]);
            _correctButtons[i] = DetermineCorrectPress(_bitmaps[i].CenterX, _bitmaps[i].CenterY, _bitmaps[i].Which);
            Debug.LogFormat("[Not Bitmaps #{0}] Press {1} while the {2} bitmap is shown.", _moduleId, _correctButtons[i] + 1, _colorNames[_colorIxs[i]]);
        }
    }

    private void SetColorblindMode(bool mode)
    {
        ColorblindText.gameObject.SetActive(mode);
    }

    private BitmapInfo generateRandomBitmap(System.Random rnd)
    {
        // Written by Timwi.
        var grid = new bool?[64];
        var which = rnd.Next(0, _shapeConfigs.Length);
        var fx = rnd.Next(0, 4);
        var fy = rnd.Next(0, 4);
        for (var x = 0; x < 5; x++)
            for (var y = 0; y < 5; y++)
                grid[x + fx + 8 * (y + fy)] = _shapeConfigs[which][x + 5 * y];
        return new BitmapInfo
        {
            Pixels = generateRandomBitmapRecurse(rnd, grid),
            Which = which,
            CenterX = fx + 2,
            CenterY = fy + 2
        };
    }

    private bool[] generateRandomBitmapRecurse(System.Random rnd, bool?[] grid)
    {
        // Written by Timwi.
        var px = grid.IndexOf(b => b == null);
        if (px == -1)
            return grid.Select(b => b.Value).ToArray();
        var rv = rnd.Next(0, 2);

        for (var iv = 0; iv < 2; iv++)
        {
            grid[px] = ((iv + rv) % 2) != 0;
            if (px % 8 >= 4 && px / 8 >= 4)
            {
                // Check that the subbitmap just completed is not one of the fixed ones
                var subbitmap = Enumerable.Range(0, 25).Select(i => grid[(i % 5) + (px % 8 - 4) + 8 * ((i / 5) + (px / 8 - 4))].Value).ToArray();
                if (_shapeConfigs.Any(fb => fb.SequenceEqual(subbitmap)))
                    continue;
            }
            var subresult = generateRandomBitmapRecurse(rnd, grid);
            if (subresult != null)
                return subresult;
        }
        grid[px] = null;
        return null;
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonSels[btn].transform);
            ButtonSels[btn].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            if (btn == _correctButtons[_bitmapIx])
            {
                _hasBeenSatisfied[_bitmapIx] = true;
                Debug.LogFormat("[Not Bitmaps #{0}] Correctly pressed {1} while the {2} bitmap with shape {3} was shown.", _moduleId, btn + 1, _colorNames[_colorIxs[_bitmapIx]], "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_bitmaps[_bitmapIx].Which]);
                _colorIxs[_bitmapIx] = 69;
                if (_cycleBitmaps != null)
                    StopCoroutine(_cycleBitmaps);
                if (!_hasBeenSatisfied.Contains(false))
                {
                    ColorblindText.gameObject.SetActive(false);
                    _moduleSolved = true;
                    Module.HandlePass();
                    Bitmap.gameObject.SetActive(false);
                    Debug.LogFormat("[Not Bitmaps #{0}] All four bitmaps have been satisfied. Module solved.", _moduleId);
                    return false;
                }
                _cycleBitmaps = StartCoroutine(CycleBitmaps());
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Not Bitmaps #{0}] Incorrectly pressed {1} while the {2} bitmap with shape {3} was shown, when {4} was expected. Strike.", _moduleId, btn + 1, _colorNames[_colorIxs[_bitmapIx]], "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[_bitmaps[_bitmapIx].Which], _correctButtons[_bitmapIx] + 1);
            }
            return false;
        };
    }

    private IEnumerator CycleBitmaps()
    {
        while (true)
        {
            for (_bitmapIx = 0; _bitmapIx < _bitmaps.Length; _bitmapIx++)
            {
                if (_hasBeenSatisfied[_bitmapIx])
                {
                    yield return null;
                    continue;
                }
                Bitmap.material.mainTexture = _bitmapTextures[_bitmapIx];
                ColorblindText.text = _colorNames[_colorIxs[_bitmapIx]].ToUpperInvariant();
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    private int DetermineCorrectPress(int X, int Y, int shape)
    {
        int newX = X;
        int newY = Y;
        if (shape == 4)
        {
            newX--;
            newY--;
        }
        if (shape == 5)
        {
            newX++;
            newY--;
        }
        if(shape == 6)
        {
            newX--;
            newY++;
        }
        if (shape == 7)
        {
            newX++;
            newY++;
        }
        int fX = newX % 8 < 4 ? 0 : 1;
        int fY = newY / 4 == 0 ? 0 : 2;
        int quad = fX + fY;
        int val = 0;
        var clockwiseArr = new int[4] { 0, 1, 3, 2 };
        var sn = BombInfo.GetSerialNumber();
        switch (shape)
        {
            case 0:
                val = quad;
                break;
            case 1:
                val = (quad + 2) % 4;
                break;
            case 2:
                val = quad ^ 1;
                break;
            case 3:
                val = 3 - quad;
                break;
            case 8:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + sn[2] - '0') % 4);
                break;
            case 9:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + sn[5] - '0') % 4);
                break;
            case 10:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + 3 * BombInfo.GetIndicators().Count()) % 4);
                break;
            case 11:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + 3 * BombInfo.GetPortCount()) % 4);
                break;
            case 12:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + (sn[0] >= '0' && sn[0] <= '9' ? sn[0] - '0' : sn[0] - 'A' + 1)) % 4);
                break;
            case 13:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + (sn[1] >= '0' && sn[1] <= '9' ? sn[1] - '0' : sn[1] - 'A' + 1)) % 4);
                break;
            case 14:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + 3 * (sn[3] - 'A' + 1)) % 4);
                break;
            case 15:
                val = Array.IndexOf(clockwiseArr, (Array.IndexOf(clockwiseArr, quad) + 3 * (sn[4] - 'A' + 1)) % 4);
                break;
            default:
                val = quad;
                break;
        }
        return val;
    }

    private Texture GenTexture(int bmIx)
    {
        // Texture generation from original Bitmaps (written by Timwi).
        const int padding = 9;
        const int thickSpacing = 6;
        const int thinSpacing = 3;
        const int cellWidth = 30;
        const int bitmapSize = 8 * cellWidth + 6 * thinSpacing + 1 * thickSpacing + 2 * padding;
        var tex = new Texture2D(bitmapSize, bitmapSize, TextureFormat.ARGB32, false);
        for (int x = 0; x < bitmapSize; x++)
            for (int y = 0; y < bitmapSize; y++)
                tex.SetPixel(x, y, new Color(0, 0, 0));
        Action<int, Color[]> drawLine = (int c, Color[] colors) =>
        {
            for (int j = 0; j < bitmapSize; j++)
            {
                tex.SetPixel(c, j, colors[_colorIxs[bmIx]]);
                tex.SetPixel(j, c, colors[_colorIxs[bmIx]]);
            }
        };
        var offsets = new List<int>();
        var crd = 0;
        for (int p = 0; p < padding; p++)
            drawLine(crd++, _lightColors);
        for (int i = 0; i < 3; i++)
        {
            offsets.Add(crd);
            crd += cellWidth;
            for (int q = 0; q < thinSpacing; q++)
                drawLine(crd++, _lightColors);
        }
        offsets.Add(crd);
        crd += cellWidth;
        for (int q = 0; q < thickSpacing; q++)
            drawLine(crd++, _lightColors);
        for (int i = 0; i < 3; i++)
        {
            offsets.Add(crd);
            crd += cellWidth;
            for (int q = 0; q < thinSpacing; q++)
                drawLine(crd++, _lightColors);
        }
        offsets.Add(crd);
        crd += cellWidth;
        for (int p = 0; p < padding; p++)
            drawLine(crd++, _lightColors);
        for (int ix = 0; ix < _bitmaps[bmIx].Pixels.Length; ix++)
            if (_bitmaps[bmIx].Pixels[ix])
                for (int i = 0; i < cellWidth; i++)
                    for (int j = 0; j < cellWidth; j++)
                        tex.SetPixel(
                            bitmapSize - 1 - offsets[ix % 8] - i,
                            offsets[ix / 8] + j,
                            _darkColors[_colorIxs[bmIx]]);

        tex.Apply();
        return tex;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press green 2 [Press button 2 while the green bitmap is shown.] | Bitmap colors are: red, green, blue, yellow, cyan, magenta. | !{0} colorblind [Toggles colorblind mode.] | 'press' is optional.";
#pragma warning restore 414

    // "red", "green", "blue", "yellow", "cyan", "magenta" 
    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:colorblind|cb)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            _colorblindMode = !_colorblindMode;
            SetColorblindMode(_colorblindMode);
        }
        m = Regex.Match(command, @"^\s*(?:press\s+)?((?<red>red)|(?<green>green)|(?<blue>blue)|(?<yellow>yellow)|(?<cyan>cyan)|magenta)\s+([1-4])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        int color = -1;
        if (m.Groups["red"].Success)
            color = 0;
        else if (m.Groups["green"].Success)
            color = 1;
        else if (m.Groups["blue"].Success)
            color = 2;
        else if (m.Groups["yellow"].Success)
            color = 3;
        else if (m.Groups["cyan"].Success)
            color = 4;
        else
            color = 5;
        if (!_colorIxs.Contains(color) || color == -1)
        {
            yield return "sendtochaterror The color " + _colorNames[color] + " is not present in the cycling bitmaps!";
            yield break;
        }
        while (_colorIxs[_bitmapIx] != color)
            yield return null;
        yield return new WaitForSeconds(0.2f);
        ButtonSels[int.Parse(m.Groups[2].Value) - 1].OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleSolved)
        {
            ButtonSels[_correctButtons[_bitmapIx]].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
