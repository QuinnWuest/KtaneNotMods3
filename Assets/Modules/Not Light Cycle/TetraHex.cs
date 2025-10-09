using System.Collections.Generic;
using System.Linq;

public class TetraHex
{
    public List<HexInfo> HexInfo {  get; private set; }
    public int[] NumberSequence { get; private set; }
    public HexColor Color {  get; private set; }

    public TetraHex(List<HexInfo> hexInfo, int[] numberSequence, HexColor hexColor)
    {
        HexInfo = hexInfo;
        NumberSequence = numberSequence;
        Color = hexColor;
    }

    public override string ToString()
    {
        return string.Format("Sequence: {0}; Color: {1}; Hex Positions: {2}", NumberSequence.Join(", "), Color.ToString(), HexInfo.Select(i => i.Hex).Join(", "));
    }
}
