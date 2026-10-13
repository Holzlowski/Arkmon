using UnityEngine;

public class DemoInteractable : MonoBehaviour, IInteractableAsPlayer
{
    public enum Kind { Wardrobe, RoomDoor, Window, CourtyardGate, Description }
    public Kind kind;
    [TextArea] public string description;
    public Vector3 GetTransform() => transform.position;
    public void InteractWithPlayer()
    {
        if (SceneLoadManager.IsTransitioning || DialogueBoxController.instance == null || DialogueBoxController.instance.IsOpen) return;
        switch (kind)
        {
            case Kind.Wardrobe:
                if (!DemoOpening.HasMantle) DemoOpening.TakeMantle();
                else DialogueBoxController.instance.ShowMessage("Meinen Mantel habe ich schon an. Auf zur Prüfung!", "Der Junge");
                break;
            case Kind.RoomDoor: DemoOpening.LeaveRoom(); break;
            case Kind.Window: DialogueBoxController.instance.ShowMessage("Goldene Felder bis zum Horizont … Irgendwann werde ich all das erkunden. Heute wartet erst einmal meine Prüfung.", "Der Junge"); break;
            case Kind.CourtyardGate: DialogueBoxController.instance.ShowMessage(DemoOpening.PassedFirstTrial
                ? "Kafla und ich haben die Prüfung bestanden. Bevor wir die goldenen Felder erkunden, erwarten wir noch die Arkanmeisterin am Hof."
                : "Bevor ich hinausziehe, wartet John mit meiner Prüfung auf mich.", "Der Junge"); break;
            default: DialogueBoxController.instance.ShowMessage(description, "Der Junge"); break;
        }
    }
}
