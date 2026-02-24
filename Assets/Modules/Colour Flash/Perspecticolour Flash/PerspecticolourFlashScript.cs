using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;

public partial class PerspecticolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public GameObject BackgroundObj;
    private readonly KMSelectable[] ButtonSels = new KMSelectable[2];

    public TextMesh ScreenText;
    public GameObject[] ButtonObjs;
    private readonly Coroutine[] _pressAnimations = new Coroutine[2];

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private CubeFace _faceCurrentlyViewing = CubeFace.BottomFace;

    private static readonly Dictionary<Colour, Color32> _colourToRgb = new Dictionary<Colour, Color32>()
    {
       { Colour.Red, new Color32(255, 0, 0, 255) },
       { Colour.Yellow, new Color32(255, 255, 0, 255) },
       { Colour.Green, new Color32(0, 255, 0, 255) },
       { Colour.Blue, new Color32(0, 0, 255, 255) },
       { Colour.Magenta, new Color32(255, 0, 255, 255) },
       { Colour.White, new Color32(255, 255, 255, 255) }
    };

    private static readonly Dictionary<CubeFace, CubeFace> _oppositeDirection = new Dictionary<CubeFace, CubeFace>()
    {
        { CubeFace.TopFace, CubeFace.BottomFace },
        { CubeFace.FrontFace, CubeFace.BackFace },
        { CubeFace.RightFace, CubeFace.LeftFace},
        { CubeFace.BackFace, CubeFace.FrontFace },
        { CubeFace.LeftFace, CubeFace.RightFace},
        { CubeFace.BottomFace, CubeFace.TopFace }
    };

    private static readonly Cube[] _cubeDiagram = new Cube[30]
    {
        new Cube(new Coord(-1, -1, -2), new Colour[] { Colour.Green, Colour.Blue, Colour.Red, Colour.Yellow, Colour.White, Colour.Magenta } ),
        new Cube(new Coord(-1, 0, -2),  new Colour[] { Colour.Red, Colour.Magenta, Colour.White, Colour.Green, Colour.Yellow, Colour.Blue } ),
        new Cube(new Coord(0, 0, -2),   new Colour[] { Colour.White, Colour.Yellow, Colour.Red, Colour.Magenta, Colour.Blue, Colour.Green } ),
        new Cube(new Coord(-1, -1, -1), new Colour[] { Colour.White, Colour.Green, Colour.Magenta, Colour.Blue, Colour.Yellow, Colour.Red } ),
        new Cube(new Coord(0, -1, -1),  new Colour[] { Colour.Magenta, Colour.Green, Colour.Yellow, Colour.Blue, Colour.White, Colour.Red } ),
        new Cube(new Coord(0, 0, -1),   new Colour[] { Colour.Blue, Colour.White, Colour.Magenta, Colour.Green, Colour.Red, Colour.Yellow } ),
        new Cube(new Coord(0, 1, -1),   new Colour[] { Colour.Red, Colour.Magenta, Colour.Blue, Colour.Green, Colour.White, Colour.Yellow } ),
        new Cube(new Coord(1, 1, -1),   new Colour[] { Colour.Blue, Colour.White, Colour.Green, Colour.Yellow, Colour.Magenta, Colour.Red } ),
        new Cube(new Coord(-2, 0, 0),   new Colour[] { Colour.Green, Colour.Blue, Colour.Yellow, Colour.Red, Colour.Magenta, Colour.White } ),
        new Cube(new Coord(-2, 1, 0),   new Colour[] { Colour.Yellow, Colour.Green, Colour.White, Colour.Red, Colour.Blue, Colour.Magenta } ),
        new Cube(new Coord(-1, -1, 0),  new Colour[] { Colour.Magenta, Colour.Red, Colour.White, Colour.Green, Colour.Yellow, Colour.Blue } ),
        new Cube(new Coord(-1, 0, 0),   new Colour[] { Colour.Red, Colour.Magenta, Colour.Green, Colour.Blue, Colour.White, Colour.Yellow } ),
        new Cube(new Coord(0, -2, 0),   new Colour[] { Colour.Yellow, Colour.Blue, Colour.Green, Colour.White, Colour.Magenta, Colour.Red } ),
        new Cube(new Coord(0, 1, 0),    new Colour[] { Colour.Blue, Colour.White, Colour.Green, Colour.Red, Colour.Magenta, Colour.Yellow } ),
        new Cube(new Coord(0, 2, 0),    new Colour[] { Colour.Red, Colour.Green, Colour.Blue, Colour.White, Colour.Yellow, Colour.Magenta } ),
        new Cube(new Coord(1, -2, 0),   new Colour[] { Colour.Green, Colour.Red, Colour.Blue, Colour.Magenta, Colour.White, Colour.Yellow } ),
        new Cube(new Coord(1, -1, 0),   new Colour[] { Colour.Blue, Colour.Red, Colour.Green, Colour.White, Colour.Yellow, Colour.Magenta } ),
        new Cube(new Coord(1, 0, 0),    new Colour[] { Colour.Blue, Colour.Red, Colour.White, Colour.Yellow, Colour.Green, Colour.Magenta } ),
        new Cube(new Coord(1, 1, 0),    new Colour[] { Colour.Magenta, Colour.Green, Colour.White, Colour.Red, Colour.Blue, Colour.Yellow } ),
        new Cube(new Coord(2, 0, 0),    new Colour[] { Colour.Magenta, Colour.White, Colour.Blue, Colour.Red, Colour.Green, Colour.Yellow } ),
        new Cube(new Coord(2, 1, 0),    new Colour[] { Colour.Yellow, Colour.Red, Colour.Green, Colour.Magenta, Colour.Blue, Colour.White } ),
        new Cube(new Coord(-1, 0, 1),   new Colour[] { Colour.Blue, Colour.Red, Colour.Green, Colour.Magenta, Colour.Yellow, Colour.White } ),
        new Cube(new Coord(-1, 1, 1),   new Colour[] { Colour.Blue, Colour.Red, Colour.Yellow, Colour.White, Colour.Green, Colour.Magenta } ),
        new Cube(new Coord(-1, 2, 1),   new Colour[] { Colour.White, Colour.Red, Colour.Magenta, Colour.Green, Colour.Blue, Colour.Yellow } ),
        new Cube(new Coord(0, -2, 1),   new Colour[] { Colour.Green, Colour.White, Colour.Blue, Colour.Magenta, Colour.Yellow, Colour.Red } ),
        new Cube(new Coord(0, -1, 1),   new Colour[] { Colour.Magenta, Colour.Green, Colour.Blue, Colour.Yellow, Colour.Red, Colour.White } ),
        new Cube(new Coord(0, 2, 1),    new Colour[] { Colour.Yellow, Colour.White, Colour.Red, Colour.Magenta, Colour.Green, Colour.Blue } ),
        new Cube(new Coord(1, -1, 1),   new Colour[] { Colour.Green, Colour.Red, Colour.Magenta, Colour.Yellow, Colour.White, Colour.Blue } ),
        new Cube(new Coord(0, -1, 2),   new Colour[] { Colour.Red, Colour.Green, Colour.White, Colour.Yellow, Colour.Magenta, Colour.Blue } ),
        new Cube(new Coord(0, 0, 2),    new Colour[] { Colour.White, Colour.Red, Colour.Blue, Colour.Yellow, Colour.Green, Colour.Magenta } ),
    };

    private Cube[] _currentCubes = new Cube[2];
    private Orientation _diagramOrientation;

    private Dictionary<Coord, Cube> _cubeByCoord;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSels[0] = YesButton;
        ButtonSels[1] = NoButton;
        for (int i = 0; i < 2; i++)
        {
            ButtonSels[i].OnInteract += ButtonPress(i);
            ButtonSels[i].OnInteractEnded += ButtonRelease(i);
        }

        _currentCubes = Enumerable.Range(0, _cubeDiagram.Length).ToArray().Shuffle().Take(2).Select(c => _cubeDiagram[c]).ToArray();

        Debug.LogFormat("[Perspecticolour Flash #{0}] Word cube: {1}", _moduleId, _currentCubes[0]);
        Debug.LogFormat("[Perspecticolour Flash #{0}] Colour cube: {1}", _moduleId, _currentCubes[1]);

        _diagramOrientation = Orientation.RandomOrientation();

        _cubeByCoord = new Dictionary<Coord, Cube>();
        for (int i = 0; i < _cubeDiagram.Length; i++)
            _cubeByCoord[_cubeDiagram[i].Coord] = _cubeDiagram[i];
    }

    private void Update()
    {
        if (_moduleSolved)
            return;
        var modUp = BackgroundObj.transform.up;
        var cam = Camera.main.transform;

        var upDot = Vector3.Dot(modUp, cam.up);
        var rightDot = Vector3.Dot(modUp, cam.right);
        var absUp = Mathf.Abs(upDot);
        var absRight = Mathf.Abs(rightDot);

        var threshold = 0.45f;

        CubeFace faceCurrentlyViewing = CubeFace.TopFace;
        if (absUp > absRight && absUp >= threshold)
            faceCurrentlyViewing = upDot > 0 ? CubeFace.FrontFace : CubeFace.BackFace;
        else if (absRight >= threshold)
            faceCurrentlyViewing = rightDot > 0 ? CubeFace.LeftFace : CubeFace.RightFace;

        _faceCurrentlyViewing = faceCurrentlyViewing;
        UpdateScreen();
    }

    private void UpdateScreen()
    {
        var mappedFace = _diagramOrientation.MapFace(_faceCurrentlyViewing);
        ScreenText.text = _currentCubes[0].GetColourFromFace(mappedFace).ToString();
        ScreenText.color = _colourToRgb[_currentCubes[1].GetColourFromFace(mappedFace)];
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            ButtonSels[btn].AddInteractionPunch(0.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonSels[btn].transform);
            if (_pressAnimations[btn] != null)
                StopCoroutine(_pressAnimations[btn]);
            _pressAnimations[btn] = StartCoroutine(PressAnimation(btn, true));
            if (_moduleSolved)
                return false;

            bool isPush = btn == 1;

            AttemptMove(isPush);

            return false;
        };
    }

    private Action ButtonRelease(int btn)
    {
        return delegate ()
        {
            if (_pressAnimations[btn] != null)
                StopCoroutine(_pressAnimations[btn]);
            _pressAnimations[btn] = StartCoroutine(PressAnimation(btn, false));
        };
    }

    private IEnumerator PressAnimation(int btn, bool pushIn)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        var curPos = ButtonObjs[btn].transform.localPosition;
        while (elapsed < duration)
        {
            ButtonObjs[btn].transform.localPosition = new Vector3(curPos.x, Easing.InOutQuad(elapsed, curPos.y, pushIn ? 0.01f : 0.0146f, duration), curPos.z);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void AttemptMove(bool isPush)
    {
        int active = GetActiveCubeIndex(_currentCubes[0], _currentCubes[1]);
        var viewFace = _faceCurrentlyViewing;
        var dir = _diagramOrientation.MapFace(viewFace);
        if (!isPush)
            dir = OppositeFace(dir);

        var delta = DeltaForFace(dir);
        var destination = Add(_currentCubes[active].Coord, delta);

        Cube next;
        if (!_cubeByCoord.TryGetValue(destination, out next))
        {
            Debug.LogFormat("[Perspecticolour Flash #{0}] Invalid move: {1} from {2} to {3}. Strike.", _moduleId, dir, _currentCubes[active].Coord, destination);
            Module.HandleStrike();
            return;
        }

        Debug.LogFormat("[Perspecticolour Flash #{0}] Moved {1} Cube from {2} to {3}.", _moduleId, active == 0 ? "Word" : "Colour", _currentCubes[active].Coord, destination);

        _currentCubes[active] = next;
        UpdateScreen();

        if (_currentCubes[0].IsEqualCoord(_currentCubes[1]))
        {
            Debug.LogFormat("[Perspecticolour Flash #{0}] Cubes have reached the same position. Module solved.", _moduleId);
            _moduleSolved = true;
            Module.HandlePass();
        }
    }

    private int GetActiveCubeIndex(Cube a, Cube b)
    {
        var bottom = _diagramOrientation.MapFace(CubeFace.BottomFace);

        Colour aBottom = a.GetColourFromFace(bottom);
        Colour bBottom = b.GetColourFromFace(bottom);

        return (IsRgb(aBottom) == IsRgb(bBottom)) ? 0 : 1;
    }

    private static bool IsRgb(Colour c)
    {
        return c == Colour.Red || c == Colour.Green || c == Colour.Blue;
    }

    private static CubeFace OppositeFace(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.TopFace:
                return CubeFace.BottomFace;
            case CubeFace.BottomFace:
                return CubeFace.TopFace;
            case CubeFace.FrontFace:
                return CubeFace.BackFace;
            case CubeFace.BackFace:
                return CubeFace.FrontFace;
            case CubeFace.RightFace:
                return CubeFace.LeftFace;
            case CubeFace.LeftFace:
                return CubeFace.RightFace;
            default:
                throw new InvalidOperationException("Invalid face in OppositeFace: " + face);
        }
    }

    private static Coord DeltaForFace(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.RightFace:
                return new Coord(1, 0, 0);
            case CubeFace.LeftFace:
                return new Coord(-1, 0, 0);
            case CubeFace.TopFace:
                return new Coord(0, 1, 0);
            case CubeFace.BottomFace:
                return new Coord(0, -1, 0);
            case CubeFace.FrontFace:
                return new Coord(0, 0, -1);
            case CubeFace.BackFace:
                return new Coord(0, 0, 1);
            default:
                throw new InvalidOperationException("Invalid face in DeltaForFace: " + face);
        }
    }

    private static Coord Add(Coord a, Coord b)
    {
        return new Coord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} command";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield break;
    }
    private IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}