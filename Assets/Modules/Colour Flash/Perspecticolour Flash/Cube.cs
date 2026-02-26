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

        public string ToOrientedString(Orientation orientation)
        {
            var order = Enumerable.Range(0, 6).Select(i => (CubeFace)i).ToArray();
            var faces = order.Select(f => GetColourFromFace(orientation.MapFace(f)).ToString()[0]);
            return string.Format("{0} ({1})", Coord, faces.Join(""));
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