using UnityEngine;

// This scene profile separates the new introduction from the archived demo flow.
[DefaultExecutionOrder(-1000)]
public class IntroChapter : MonoBehaviour
{
    public static bool Active { get; private set; }
    public static bool HasFamilyOutfit { get; private set; }
    private static bool roomReturn;
    public static void MarkRoomReturn()=>roomReturn=true;
    public static bool ConsumeRoomReturn(){bool result=roomReturn;roomReturn=false;return result;}
    public const string PlayerName = "Elian";
    public const string FamilyName = "Aurenfeld";
    private void Awake() => Active = true;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetProgress() { Active = false; HasFamilyOutfit = false;roomReturn=false; }
    public static void EquipFamilyOutfit() => HasFamilyOutfit = true;
}
