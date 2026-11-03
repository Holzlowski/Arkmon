using UnityEngine;

public class DemoInteractable : MonoBehaviour, IInteractableAsPlayer
{
    public enum Kind { Wardrobe, RoomDoor, Window, CourtyardGate, Description, Lamp }
    public Kind kind;
    [TextArea] public string description;
    public Vector3 GetTransform() => transform.position;
    private Transform presentationTarget;

    // The reachable interaction point can sit in the room; the marker belongs at the prop.
    public Vector3 GetMarkerPosition()
    {
        if (kind != Kind.Wardrobe && kind != Kind.RoomDoor) return GetTransform();
        if (presentationTarget == null)
            presentationTarget = GameObject.Find(kind == Kind.Wardrobe ? "Wardrobe" : "Courtyard door")?.transform;
        if (presentationTarget == null) return GetTransform();
        if (kind == Kind.RoomDoor)
            return presentationTarget.TransformPoint(new Vector3(presentationTarget.GetComponent<TowerRoomDoor>() != null ? -.8f : -.35f, 0, 0));
        var collider = presentationTarget.GetComponent<Collider>();
        if (collider != null)
            return new Vector3(collider.bounds.center.x, 0, collider.bounds.min.z - .28f);
        return presentationTarget.position + Vector3.back * .7f;
    }
    private void ViewWindow(string message, string speaker)
    {
        DialogueBoxController.instance.ShowMessage(message, speaker);
        FindFirstObjectByType<TowerRoomCamera>()?.ShowWindowView();
    }

    public void InteractWithPlayer()
    {
        if (DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning || DialogueBoxController.instance == null || DialogueBoxController.instance.IsOpen) return;
        if(IntroChapter.Active)
        {
            switch(kind)
            {
                case Kind.Wardrobe:
                    if(!IntroChapter.HasFamilyOutfit) DemoOpening.TakeMantle();
                    else DialogueBoxController.instance.ShowMessage("Mein Familiengewand sitzt. Ich bin bereit für den Empfang.",IntroChapter.PlayerName);
                    return;
                case Kind.RoomDoor: DemoOpening.LeaveRoom(); return;
                case Kind.Lamp: return;
                case Kind.Window: ViewWindow("Hinter den goldenen Feldern liegen die Dächer der Stadt. Von hier oben wirkt alles so friedlich. Ob mein Weg bald dorthin führt?",IntroChapter.PlayerName); return;
                case Kind.CourtyardGate: DialogueBoxController.instance.ShowMessage("Hier erwarten wir die Arkanmeisterin. Bis sie eintrifft, kann ich mich noch ein wenig im Hof umsehen.",IntroChapter.PlayerName); return;
                default: DialogueBoxController.instance.ShowMessage(description,IntroChapter.PlayerName); return;
            }
        }
        switch (kind)
        {
            case Kind.Wardrobe:
                if (!DemoOpening.HasMantle) DemoOpening.TakeMantle();
                else DialogueBoxController.instance.ShowMessage("Meinen Mantel habe ich schon an. Auf zur Prüfung!", "Der Junge");
                break;
            case Kind.RoomDoor: DemoOpening.LeaveRoom(); break;
            case Kind.Lamp: DemoOpening.TakeLamp(); break;
            case Kind.Window: ViewWindow("Goldene Felder bis zum Horizont … Irgendwann werde ich all das erkunden. Heute wartet erst einmal meine Prüfung.", "Der Junge"); break;
            case Kind.CourtyardGate: DialogueBoxController.instance.ShowMessage(DemoOpening.PassedFirstTrial
                ? "Kafla und ich haben die Prüfung bestanden. Bevor wir die goldenen Felder erkunden, erwarten wir noch die Arkanmeisterin am Hof."
                : "Bevor ich hinausziehe, wartet John mit meiner Prüfung auf mich.", "Der Junge"); break;
            default: DialogueBoxController.instance.ShowMessage(description, "Der Junge"); break;
        }
    }
}
