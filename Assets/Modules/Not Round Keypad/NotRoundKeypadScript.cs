using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class NotRoundKeypadScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly char[] _charList = new char[]{
        'Ѽ', 'æ', '©', 'Ӭ', 'Ҩ', 'Ҋ', 'ϗ', 'Ϟ',
        'Ԇ', 'Ϙ', 'Ѯ', 'ƛ', 'Ω', '¶', 'ψ', '¿',
        'Ϭ', 'Ͼ', 'Ͽ', '★', '☆', 'ټ', '҂', 'Ѣ',
        'Ѭ', 'Ѧ', 'Җ'
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = @"";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand (string command)
    {
        command = Regex.Replace(command.ToLowerInvariant().Trim(), @"^\s+", " ");
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
