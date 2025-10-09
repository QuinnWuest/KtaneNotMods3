using NotModdedModulesVol3;

public class HexInfo
{
    public Hex Hex { get; private set; }
    public HexColor Color {  get; private set; }

    public HexInfo(Hex hex, HexColor color)
    {
        Hex = hex;
        Color = color;
    }
}