using System;
using System.Collections.Generic;
using UnityEngine;

public enum LineType
{
    Question,
    Answer
}

[Serializable]
public class DialogueLine
{
    public LineType type;

    [TextArea(2, 6)]
    public string text;
}

[Serializable]
public class ChoiceLine
{
    [HideInInspector] public LineType type = LineType.Answer;

    [TextArea(1, 3)]
    public string text;

    public DialogueLine AsDialogueLine()
        => new DialogueLine { type = LineType.Answer, text = text };
}

[Serializable]
public class Section
{
    [Header("Prompt (shown before choices)")]
    public DialogueLine prompt;

    [Header("Choice A (always Answer style)")]
    public ChoiceLine choiceA;
    public List<DialogueLine> branchALines = new();

    [Header("Choice B (always Answer style)")]
    public ChoiceLine choiceB;
    public List<DialogueLine> branchBLines = new();
}

[CreateAssetMenu(
    menuName = "Dialogue/8-Section Dialogue",
    fileName = "Dialogue_8Sections"
)]
public class DialogueScript : ScriptableObject
{
    public List<Section> sections = new();
}
