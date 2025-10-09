using NotModdedModulesVol3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public partial class NotLightCycleScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;

    public GameObject[] LightObjs;
    public Material[] LightOffMats;
    public Material[] LightOnMats;
    public TextMesh[] ColorblindTexts;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _colorblindMode;

    private HexGrid _hexGrid;
    private static readonly Hex[] _cornerHexPositions = Enumerable.Range(0, 6).Select(i => new Hex(0, 0).GetNeighbor(i).GetNeighbor(i).GetNeighbor(i)).ToArray();

    private int[] _lightColors = new int[6];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

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
            Debug.LogFormat("[Not Light Cycle #{0}] Tetrahex #{1}: {2}", _moduleId, i + 1, _hexGrid.AppliedTetraHexes[i]);
        Debug.LogFormat("[Not Light Cycle #{0}] Grid: {1}", _moduleId, _hexGrid.LogGrid());
    }

    private void SetColorblindMode(bool mode)
    {
        foreach (var text in ColorblindTexts)
            text.gameObject.SetActive(mode);
    }

    private HexGrid GenerateHexagonGrid()
    {
        var hg = new HexGrid(new List<HexInfo>(), new List<TetraHex>());
        var tetraHex = GenerateTetrahex();
        hg.ApplyTetraHex(tetraHex);
        return hg;
    }

    private TetraHex GenerateTetrahex()
    {
        NewTetrahex:
        var genSeq = Enumerable.Range(0, 5).Select(i => Rnd.Range(0, 6)).ToArray();
        if (genSeq[0] == (genSeq[1] + 1) % 6 || genSeq[0] == (genSeq[1] + 5) % 6 || genSeq.Distinct().Count() < 3)
            goto NewTetrahex;

        var hexColor = (HexColor)Rnd.Range(0, 6);
        var hexList = new List<HexInfo> { new HexInfo(_cornerHexPositions[genSeq[0]], hexColor) };

        for (int i = 1; i < genSeq.Length; i++)
        {
            var nextHex = hexList[i - 1].Hex.GetNeighbor(genSeq[i]);
            if (nextHex.Q > 3 || nextHex.R > 3)
            {
                if (i != 1)
                    goto NewTetrahex;
                else
                    nextHex = hexList[i - 1].Hex;
            }
            hexList.Add(new HexInfo(nextHex, hexColor));
        }
        var list = hexList.Skip(1).ToList();
        if (list.Distinct().Count() != 4)
            goto NewTetrahex;

        return new TetraHex(list, genSeq, hexColor);
    }

    private IEnumerable<int> FindPath(HexGrid hg)
    {
        return null;
    }

    private void SetLightMaterial(int ix, int color, bool isOn)
    {
        LightObjs[ix].GetComponent<MeshRenderer>().material = isOn ? LightOnMats[color] : LightOffMats[color];
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
