using System;
using UnityEngine;
using Rnd = UnityEngine.Random;

public partial class PerspecticolourFlashScript
{
    public class Orientation
    {
        public CubeFace[] _faceMap;

        public Orientation()
        {
            _faceMap = new CubeFace[6];
            _faceMap[(int)CubeFace.TopFace] = CubeFace.TopFace;
            _faceMap[(int)CubeFace.FrontFace] = CubeFace.FrontFace;
            _faceMap[(int)CubeFace.RightFace] = CubeFace.RightFace;
            _faceMap[(int)CubeFace.BackFace] = CubeFace.BackFace;
            _faceMap[(int)CubeFace.LeftFace] = CubeFace.LeftFace;
            _faceMap[(int)CubeFace.BottomFace] = CubeFace.BottomFace;
        }

        public CubeFace MapFace(CubeFace face)
        {
            return _faceMap[(int)face];
        }

        private void RotateX()
        {
            CubeFace t = _faceMap[(int)CubeFace.TopFace];
            _faceMap[(int)CubeFace.TopFace] = _faceMap[(int)CubeFace.FrontFace];
            _faceMap[(int)CubeFace.FrontFace] = _faceMap[(int)CubeFace.BottomFace];
            _faceMap[(int)CubeFace.BottomFace] = _faceMap[(int)CubeFace.BackFace];
            _faceMap[(int)CubeFace.BackFace] = t;
        }

        private void RotateY()
        {
            CubeFace t = _faceMap[(int)CubeFace.FrontFace];
            _faceMap[(int)CubeFace.FrontFace] = _faceMap[(int)CubeFace.RightFace];
            _faceMap[(int)CubeFace.RightFace] = _faceMap[(int)CubeFace.BackFace];
            _faceMap[(int)CubeFace.BackFace] = _faceMap[(int)CubeFace.LeftFace];
            _faceMap[(int)CubeFace.LeftFace] = t;
        }

        private void RotateZ()
        {
            CubeFace t = _faceMap[(int)CubeFace.TopFace];
            _faceMap[(int)CubeFace.TopFace] = _faceMap[(int)CubeFace.LeftFace];
            _faceMap[(int)CubeFace.LeftFace] = _faceMap[(int)CubeFace.BottomFace];
            _faceMap[(int)CubeFace.BottomFace] = _faceMap[(int)CubeFace.RightFace];
            _faceMap[(int)CubeFace.RightFace] = t;
        }

        public static Orientation RandomOrientation()
        {
            var o = new Orientation();
            int rots = Rnd.Range(6, 12);
            for (int i = 0; i < rots; i++)
            {
                int axis = Rnd.Range(0, 3);
                if (axis == 0)
                    o.RotateX();
                else if (axis == 1)
                    o.RotateY();
                else
                    o.RotateZ();
            }
            return o;
        }
    }
}