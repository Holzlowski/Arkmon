using UnityEngine;

public enum DialogueEmotion { Neutral, Friendly, Surprised, Worried }

[System.Serializable]
public struct DialogueLinePresentation
{
    [Tooltip("Empty uses the conversation's speaker.")]
    public string speaker;
    public DialogueEmotion emotion;
    [Tooltip("Optional override; otherwise the speaker's expression set is used.")]
    public Sprite portrait;
}

// Shared by authored dialogue trees and runtime conversations.
[CreateAssetMenu(menuName="Arkmon/Dialogue Portrait Set")]
public sealed class DialoguePortraitSet : ScriptableObject
{
    public string speaker;
    public Sprite neutral, friendly, surprised, worried;

    public Sprite For(DialogueEmotion emotion)
    {
        Sprite chosen=emotion switch
        {
            DialogueEmotion.Friendly=>friendly,
            DialogueEmotion.Surprised=>surprised,
            DialogueEmotion.Worried=>worried,
            _=>neutral
        };
        return chosen!=null?chosen:neutral;
    }

    private static DialoguePortraitSet[] sets;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()=>sets=null;

    public static Sprite Resolve(string speaker,DialogueEmotion emotion)
    {
        sets??=Resources.LoadAll<DialoguePortraitSet>("DialoguePortraits");
        foreach(var set in sets)
            if(set!=null && set.speaker==speaker)return set.For(emotion);
        return null;
    }
}