using System;
using System.Linq;

public partial class PerspecticolourFlashScript
{
    public class Cube
    {
        public Coord Coord;
        public Colour[] FaceInfo;
        // faces are ordered top, front, right, back, left, bottom

        public Cube(Coord coord, Colour[] faceInfo)
        {
            Coord = coord;
            FaceInfo = faceInfo;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Coord, FaceInfo.Select(f => f.ToString()[0]).Join(""));
        }

        public Colour GetColourFromFace(CubeFace face)
        {
            return FaceInfo[(int)face];
        }

        public bool IsEqualCoord(Cube other)
        {
            return Coord.Equals(other.Coord);
        }
    }
}