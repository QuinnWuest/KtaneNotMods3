using NotModdedModulesVol3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexGrid
{
    public List<HexInfo> Info { get; private set; }
    public List<TetraHex> AppliedTetraHexes { get; private set; }

    public HexGrid(List<HexInfo> info, List<TetraHex> aths)
    {
        Info = info;
        AppliedTetraHexes = aths;
    }

    public HexGrid ApplyTetraHex(TetraHex tetraHex)
    {
        var basePositions = Info.ToList().Select(i => i.Hex).ToList();
        var tiPositions = tetraHex.HexInfo.Select(i => i.Hex).ToList();
        var color = tetraHex.Color;
        for (int i = 0; i < tetraHex.HexInfo.Count; i++)
        {
            var ix = basePositions.IndexOf(tiPositions[i]);
            if (ix == -1)
                Info.Add(tetraHex.HexInfo[i]);
            else
                Info[ix] = tetraHex.HexInfo[i];
        }
        AppliedTetraHexes.Add(tetraHex);
        return new HexGrid(Info, AppliedTetraHexes);
    }

    public HexInfo GetHexAt(Hex hex)
    {
        return Info.FirstOrDefault(i => i.Hex.Q == hex.Q && i.Hex.R == hex.R);
    }

    public string LogGridByTetrahexCount(int tetraCount)
    {
        tetraCount = Mathf.Clamp(tetraCount, 0, AppliedTetraHexes.Count);

        var tempInfo = new List<HexInfo>();

        for (int i = 0; i < tetraCount; i++)
            ApplyTetraHexToInfo(tempInfo, AppliedTetraHexes[i]);

        return tempInfo.Join(", ");
    }

    private static void ApplyTetraHexToInfo(List<HexInfo> info, TetraHex t)
    {
        foreach (var hi in t.HexInfo)
        {
            int ix = info.FindIndex(x => x.Hex == hi.Hex);
            if (ix >= 0)
                info[ix] = hi;
            else
                info.Add(hi);
        }
    }
}