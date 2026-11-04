using UnityEngine;

[CreateAssetMenu]
public class DialogueTree : ScriptableObject
{
    public DialogueSection[] sections;
}

[System.Serializable]
public struct DialogueSection
{
    [TextArea]
    public string[] dialogue;
    [Tooltip("Optional entry per dialogue line. Missing entries use neutral and the conversation speaker.")]
    public DialogueLinePresentation[] presentations;
    public bool battleAfterDialogue;
    public bool endAfterDialogue;
    public BranchPoint branchPoint;
}

[System.Serializable]
public struct BranchPoint
{
    [TextArea]
    public string question;
    public DialogueLinePresentation presentation;
    public Answer[] answers;
}

[System.Serializable]
public struct Answer
{
    public string answerLabel;
    public int nextElement;
}