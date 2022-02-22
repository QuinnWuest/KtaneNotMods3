using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
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

    private const int _bitmapSize = 4;
    private List<bool[][]> _bitmaps = new List<bool[][]>();
    private int _bitmapIx;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine _cycleBitmaps;

    private static readonly Color[] _lightColors = new[] { new Color(1, .9f, .9f), new Color(.9f, 1, .9f), new Color(.9f, .9f, 1), new Color(1, 1, .9f), new Color(.9f, 1, 1), new Color(1, .9f, 1) };
    private static readonly Color[] _darkColors = new[] { new Color(.75f, .5f, .5f), new Color(.5f, .75f, .5f), new Color(.5f, .5f, .75f), new Color(.75f, .75f, .5f), new Color(.5f, .75f, .75f), new Color(.75f, .5f, .75f) };
    private static readonly string[] _colorNames = new string[] { "red", "green", "blue", "yellow", "cyan", "magenta" };

    private Texture[] _bitmapTextures = new Texture[_bitmapSize];
    private int[] _colorIxs = new int[_bitmapSize];
    private bool[] _hasBeenSatisfied = new bool[_bitmapSize];
    
    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);

        _colorIxs = Enumerable.Range(0, _lightColors.Length).ToArray().Shuffle().Take(_bitmapSize).ToArray();
        for (int k = 0; k < _bitmapSize; k++)
        {
            _bitmaps.Add(new bool[8][]);
            for (int j = 0; j < 8; j++)
            {
                _bitmaps[k][j] = new bool[8];
                for (int i = 0; i < 8; i++)
                    _bitmaps[k][j][i] = Rnd.Range(0, 2) == 0;
            }
        }

        for (int i = 0; i < _bitmapTextures.Length; i++)
            _bitmapTextures[i] = GenTexture(i);

        Bitmap.material.shader = Shader.Find("Unlit/Transparent");
        _cycleBitmaps = StartCoroutine(CycleBitmaps());
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
            for (_bitmapIx = 0; _bitmapIx < _bitmaps.Count; _bitmapIx++)
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
        // Texture generation from original bitmaps. I do not understand how this works.
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
        for (int x = 0; x < _bitmaps[bmIx].Length; x++)
            for (int y = 0; y < _bitmaps[bmIx][x].Length; y++)
                if (_bitmaps[bmIx][x][y])
                    for (int i = 0; i < cellWidth; i++)
                        for (int j = 0; j < cellWidth; j++)
                            tex.SetPixel(
                                bitmapSize - 1 - offsets[x] - i,
                                offsets[y] + j,
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
