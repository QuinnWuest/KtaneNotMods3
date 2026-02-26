using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class PerspecticolourFlashScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable YesButton;
    public KMSelectable NoButton;
    public GameObject ModuleObj;
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

        _cubeByCoord = new Dictionary<Coord, Cube>();
        for (int i = 0; i < _cubeDiagram.Length; i++)
            _cubeByCoord[_cubeDiagram[i].Coord] = _cubeDiagram[i];

        while (true)
        {
            _currentCubes = Enumerable.Range(0, _cubeDiagram.Length).ToArray().Shuffle().Take(2).Select(c => _cubeDiagram[c]).ToArray();
            _diagramOrientation = Orientation.RandomOrientation();

            var path = FindPath(new CubePosState(_currentCubes[0].Coord, _currentCubes[1].Coord));
            if (path.Count >= 3 && path.Count <= 8)
                break;
        }

        Debug.LogFormat("[Perspecticolour Flash #{0}] Word cube: {1}", _moduleId, _currentCubes[0].ToOrientedString(_diagramOrientation));
        Debug.LogFormat("[Perspecticolour Flash #{0}] Colour cube: {1}", _moduleId, _currentCubes[1].ToOrientedString(_diagramOrientation));
        Debug.LogFormat("[Perspecticolour Flash #{0}] (Face colors are ordered: Top, Front, Right, Back, Left, Bottom.)", _moduleId);
        Debug.LogFormat("[Perspecticolour Flash #{0}] The active cube is the {1} cube.", _moduleId, new[] { "Word", "Colour" }[GetActiveCubeIndex(_currentCubes[0], _currentCubes[1])]);
    }

    private void Update()
    {
        if (_moduleSolved)
            return;

        var modUp = ModuleObj.transform.up;
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

            AttemptMove(ButtonSels[btn] == YesButton);
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
        var cur = new CubePosState(_currentCubes[0].Coord, _currentCubes[1].Coord);

        CubePosState next;
        int active;
        CubeFace dir;
        Coord from, dest;

        if (!TryApplyMove(cur, _faceCurrentlyViewing, isPush, out next, out active, out dir, out from, out dest))
        {
            Debug.LogFormat("[Perspecticolour Flash #{0}] Attempted to move the active cube from {1} to {2}. Strike.",
                _moduleId, from, dest);
            Module.HandleStrike();
            return;
        }

        _currentCubes[0] = _cubeByCoord[next.A];
        _currentCubes[1] = _cubeByCoord[next.B];

        int newActive = GetActiveCubeIndex(_currentCubes[0], _currentCubes[1]);
        Debug.LogFormat("[Perspecticolour Flash #{0}] Moved the {1} Cube to: {2}.", _moduleId, active == 0 ? "Word" : "Colour", _currentCubes[active].ToOrientedString(_diagramOrientation));
        Debug.LogFormat("[Perspecticolour Flash #{0}] The active cube is now the {1} cube.", _moduleId, new[] { "Word", "Colour" }[newActive]);

        UpdateScreen();

        if (_currentCubes[0].IsEqualCoord(_currentCubes[1]))
        {
            Debug.LogFormat("[Perspecticolour Flash #{0}] Cubes have reached the same position. Module solved.", _moduleId);
            _moduleSolved = true;
            Module.HandlePass();
        }
    }

    private bool TryMove(CubePosState s, CubeFace viewFace, bool isPush, out CubePosState next)
    {
        int active;
        CubeFace dir;
        Coord from, dest;
        return TryApplyMove(s, viewFace, isPush, out next, out active, out dir, out from, out dest);
    }

    private bool TryApplyMove(CubePosState s, CubeFace viewFace, bool isPush, out CubePosState next, out int active, out CubeFace dir, out Coord from, out Coord dest)
    {
        next = s;
        active = GetActiveCubeIndex(s);
        from = (active == 0) ? s.A : s.B;
        dir = _diagramOrientation.MapFace(viewFace);

        if (isPush)
            dir = OppositeFace(dir);

        var to = DeltaForFace(dir);
        dest = new Coord(from.X + to.X, from.Y + to.Y, from.Z + to.Z);

        if (!_cubeByCoord.ContainsKey(dest))
            return false;

        next = (active == 0) ? new CubePosState(dest, s.B) : new CubePosState(s.A, dest);
        return true;
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

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} yes front [Press the 'yes' button when the module is tilted to view the front face.] | Commands can be chained with commas or semicolons. | Buttons and faces can be shortened to their first letter.";
#pragma warning restore 0414

    struct TpCommand
    {
        public KMSelectable Button;
        public CubeFace Face;

        public TpCommand(KMSelectable button, CubeFace face)
        {
            Button = button;
            Face = face;
        }
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = Regex.Replace(command.Trim().ToLowerInvariant(), @"^\s+", " ");
        var cmds = command.Split(new[] { ',', ';' });

        var tpCmds = new List<TpCommand>();
        foreach (var cmd in cmds)
        {
            var m = Regex.Match(cmd, @"^\s*(?<btn>(yes|y|no|n))\s+(?<face>(top|t|front|f|right|r|back|b|left|l))\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!m.Success)
                yield break;
            tpCmds.Add(new TpCommand
            (
                ButtonSels["yn".IndexOf(m.Groups["btn"].Value[0])],
                (CubeFace)"tfrbl".IndexOf(m.Groups["face"].Value[0])
            ));
        }
        yield return null;
        foreach (var tpcmd in tpCmds)
            yield return ExecuteTpCommand(tpcmd);
        yield break;
    }

    private IEnumerator ExecuteTpCommand(TpCommand command)
    {
        if (command.Face != CubeFace.TopFace)
            yield return TiltModule(command.Face);
        yield return new WaitForSeconds(0.1f);
        yield return PressButton(command.Button);
        yield return new WaitForSeconds(0.1f);
        if (command.Face != CubeFace.TopFace)
            yield return TiltModule(CubeFace.TopFace);
    }

    private IEnumerator PressButton(KMSelectable button)
    {
        button.OnInteract();
        yield return new WaitForSeconds(0.1f);
        button.OnInteractEnded();
    }

    private static readonly Dictionary<CubeFace, Vector3> _rotations = new Dictionary<CubeFace, Vector3>
    {
        { CubeFace.TopFace, new Vector3(0, 0, 0) },
        { CubeFace.FrontFace, new Vector3(60, 0, 0) },
        { CubeFace.RightFace, new Vector3(0, 0, 60) },
        { CubeFace.BackFace, new Vector3(-60, 0, 0) },
        { CubeFace.LeftFace, new Vector3(0, 0, -60) }
    };

    private IEnumerator TiltModule(CubeFace cubeFace)
    {
        var rotStart = ModuleObj.transform.localEulerAngles;
        var rotEnd = _rotations[cubeFace];

        if (rotStart.x - rotEnd.x > 180f)
            rotEnd.x += 360f;
        if (rotEnd.x - rotStart.x > 180f)
            rotStart.x += 360f;
        if (rotStart.z - rotEnd.z > 180f)
            rotEnd.z += 360f;
        if (rotEnd.z - rotStart.z > 180f)
            rotStart.z += 360f;
        var duration = 0.5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ModuleObj.transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsed, rotStart.x, rotEnd.x, duration), 0, Easing.InOutQuad(elapsed, rotStart.z, rotEnd.z, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        ModuleObj.transform.localEulerAngles = new Vector3(rotEnd.x, 0, rotEnd.z);
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleSolved)
        {
            var start = new CubePosState(_currentCubes[0].Coord, _currentCubes[1].Coord);
            var path = FindPath(start);

            if (path == null || path.Count == 0)
                throw new InvalidOperationException("Failed to find a path in autosolver.");

            for (int i = 0; i < path.Count; i++)
            {
                var button = path[i].IsPush ? YesButton : NoButton;
                yield return ExecuteTpCommand(new TpCommand(button, path[i].ViewFace));
            }
        }
        yield break;

    }

    public struct CubePosState : IEquatable<CubePosState>
    {
        public Coord A;
        public Coord B;

        public CubePosState(Coord a, Coord b)
        {
            A = a;
            B = b;
        }

        public bool Equals(CubePosState other)
        {
            return other.A.Equals(A) && other.B.Equals(B);
        }

        public override bool Equals(object obj)
        {
            return obj is CubePosState && Equals((CubePosState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + A.GetHashCode();
                h = h * 31 + B.GetHashCode();
                return h;
            }
        }

        public bool IsMatched()
        {
            return A.Equals(B);
        }
    }

    public struct Move
    {
        public CubeFace ViewFace;
        public bool IsPush;

        public Move(CubeFace viewFace, bool isPush)
        {
            ViewFace = viewFace;
            IsPush = isPush;
        }
    }

    private int GetActiveCubeIndex(CubePosState s)
    {
        Cube a = _cubeByCoord[s.A];
        Cube b = _cubeByCoord[s.B];

        CubeFace bottom = _diagramOrientation.MapFace(CubeFace.BottomFace);

        bool aRgb = IsRgb(a.GetColourFromFace(bottom));
        bool bRgb = IsRgb(b.GetColourFromFace(bottom));

        return (aRgb == bRgb) ? 0 : 1;
    }

    public struct ParentInfo
    {
        public CubePosState Parent;
        public Move Move;

        public ParentInfo(CubePosState parent, Move move)
        {
            Parent = parent;
            Move = move;
        }
    }

    private List<Move> FindPath(CubePosState start)
    {
        var q = new Queue<CubePosState>();
        var parent = new Dictionary<CubePosState, ParentInfo>();

        q.Enqueue(start);
        parent[start] = new ParentInfo(start, new Move(CubeFace.TopFace, true));

        while (q.Count > 0)
        {
            var qi = q.Dequeue();
            if (qi.IsMatched())
                return ReconstructPath(start, qi, parent);

            var faces = Enumerable.Range(0, 5).Select(i => (CubeFace)i).ToArray();
            for (int i = 0; i < faces.Length; i++)
            {
                var vf = faces[i];
                for (int j = 0; j < 2; j++)
                {
                    bool isPush = j == 0;
                    CubePosState next;
                    if (!TryMove(qi, vf, isPush, out next))
                        continue;
                    if (parent.ContainsKey(next))
                        continue;

                    parent[next] = new ParentInfo(qi, new Move(vf, isPush));
                    q.Enqueue(next);
                }
            }
        }
        return new List<Move>();
    }

    private List<Move> ReconstructPath(CubePosState start, CubePosState goal, Dictionary<CubePosState, ParentInfo> parent)
    {
        List<Move> rev = new List<Move>();
        CubePosState cur = goal;

        while (!cur.Equals(start))
        {
            ParentInfo pi = parent[cur];
            rev.Add(pi.Move);
            cur = pi.Parent;
        }

        rev.Reverse();
        return rev;
    }
}