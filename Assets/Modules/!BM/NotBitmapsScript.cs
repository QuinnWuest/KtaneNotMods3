using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

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

    private static readonly bool[][] _fixedBitmaps = new string[]
    {
        "0111010001100011000101110",
        "0111011111111111111101110",
        "0101010101010101010101010",
        "0101011111011101111101010",
        "0010001010010101000111111",
        "0010001010100010101000100",
        "0010001110111110111000100",
        "1111110001100011000111111",
        "1111111111110111111111111",
        "1111110001010100101000100",
        "1111111111011100111000100",
        "0111011011100011101101110"
    }
        .Select(sa => sa.Select(ch => ch == '0').ToArray()).ToArray();

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

        _colorIxs = Enumerable.Range(0, _lightColors.Length).ToArray().Shuffle().Take(_numBitmaps).ToArray();
        _bitmaps = new BitmapInfo[4];
        var rnd = new System.Random(Rnd.Range(0, int.MaxValue));
        for (int k = 0; k < _numBitmaps; k++)
            _bitmaps[k] = generateRandomBitmap(rnd);

        for (int i = 0; i < _bitmapTextures.Length; i++)
            _bitmapTextures[i] = GenTexture(i);

        Bitmap.material.shader = Shader.Find("Unlit/Transparent");
        _cycleBitmaps = StartCoroutine(CycleBitmaps());
    }

    private BitmapInfo generateRandomBitmap(System.Random rnd)
    {
        var grid = new bool?[64];
        var which = rnd.Next(0, _fixedBitmaps.Length);
        var fx = rnd.Next(0, 4);
        var fy = rnd.Next(0, 4);
        for (var x = 0; x < 5; x++)
            for (var y = 0; y < 5; y++)
                grid[x + fx + 8 * (y + fy)] = _fixedBitmaps[which][x + 5 * y];
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
                if (_fixedBitmaps.Any(fb => fb.SequenceEqual(subbitmap)))
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
            Debug.LogFormat("[Not Bitmaps #{0}] Pressed {1} while the {2} bitmap was shown.", _moduleId, btn + 1, _colorNames[_colorIxs[_bitmapIx]]);
            _hasBeenSatisfied[_bitmapIx] = true;
            if (_cycleBitmaps != null)
                StopCoroutine(_cycleBitmaps);
            if (!_hasBeenSatisfied.Contains(false))
            {
                _moduleSolved = true;
                Module.HandlePass();
                Bitmap.gameObject.SetActive(false);
                return false;
            }
            _cycleBitmaps = StartCoroutine(CycleBitmaps());
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
                yield return new WaitForSeconds(1.5f);
            }
        }
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
    private readonly string TwitchHelpMessage = @"!{0} press 2 [Press button 2.] | 'press' is optional.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:press\s+)?([1-4])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        ButtonSels[int.Parse(m.Groups[1].Value) - 1].OnInteract();
    }
}
