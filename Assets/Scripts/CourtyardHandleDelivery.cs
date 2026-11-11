using UnityEngine;

// Session progress survives courtyard reloads; a new intro resets it explicitly.
public sealed class CourtyardHandleDelivery : MonoBehaviour
{
    public static bool Offered { get; private set; }
    public static bool TakeHandle() { if(!Offered || HasHandle || Completed)return false;HasHandle=true;return true; }
    public static bool HasHandle { get; private set; }
    public static bool Completed { get; private set; }
    public static string ObjectiveText => HasHandle ? "Ida helfen · Bringe den reparierten Schaufelstiel zu Marta" : Offered && !Completed ? "Ida helfen · Nimm den Schaufelstiel von der Werkbank" : null;
    static CourtyardHandleDelivery instance;
    int remaining;
    bool delivering;
    internal static void Install()
    {
        if (instance != null) return;
        instance = new GameObject("Ida handle delivery").AddComponent<CourtyardHandleDelivery>();
    }
    public static void ResetProgress() { HasHandle = Completed = Offered = false; if (instance != null) instance.remaining = 0; }
    // Commit only when the actual handover line is reached, not when opening/cancelling a conversation.
    public static void AwaitHandover(int lineCount, bool toMarta)
    {
        Install(); instance.remaining = lineCount; instance.delivering = toMarta;
    }
    void OnEnable()
    {
        DialogueBoxController.OnLinePresented += OnLine;
        DialogueBoxController.OnDialogueEnded += Cancel;
    }
    void OnLine(string speaker, DialogueEmotion emotion)
    {
        if (remaining <= 0 || --remaining != 0) return;
        if (delivering) { if (HasHandle) { HasHandle = false; Completed = true; CourtyardTaskFeedback.Complete("Ida"); } }
        else if (!Completed) Offered = true;
    }
    void LateUpdate()
    {
        var prop=GameObject.Find("Marta trowel repair")?.GetComponent<SpriteRenderer>();
        if(prop!=null)prop.enabled=!HasHandle && !Completed;
    }
    void Cancel() => remaining = 0;
    void OnDisable()
    {
        DialogueBoxController.OnLinePresented -= OnLine;
        DialogueBoxController.OnDialogueEnded -= Cancel;
        remaining = 0;
    }
    void OnDestroy() { if (instance == this) instance = null; }
}
