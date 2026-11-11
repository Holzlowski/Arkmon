using UnityEngine;

// The workbench opens Ida’s offer and provides the explicit repaired-handle pickup.
public sealed class CourtyardWorkshopWorkpiece : MonoBehaviour,IInteractableAsPlayer
{
    public string Label=>CourtyardHandleDelivery.Offered && !CourtyardHandleDelivery.HasHandle && !CourtyardHandleDelivery.Completed ? "Schaufelstiel mitnehmen" : "Mit Ida über den Schaufelstiel sprechen";
    public Vector3 GetTransform()=>transform.position;
    public void InteractWithPlayer()
    {
        var dialogue=DialogueBoxController.instance;
        var player=GameObject.FindWithTag("Player");
        if(dialogue==null || player==null || dialogue.IsOpen || DemoOpening.IsPresenting ||
            SceneLoadManager.IsTransitioning)return;
        var controller=player.GetComponent<PlayerController>();
        if(controller==null || Vector3.Distance(player.transform.position,GetTransform())>controller.interacRange)return;
        if (CourtyardHandleDelivery.HasHandle || CourtyardHandleDelivery.Completed)
        {
            dialogue.ShowMessage(CourtyardHandleDelivery.Completed
                ? "Martas Schaufelstiel ist wieder im Garten. Ida arbeitet schon am nächsten Stück."
                : "Den reparierten Schaufelstiel bringe ich jetzt zu Marta.", IntroChapter.PlayerName);
            return;
        }
        if(CourtyardGarden.HasCan || CourtyardWaterTask.HasBucket)
        { dialogue.ShowMessage("Ich stelle erst das geliehene Gefäß zurück. Dann habe ich die Hände für den Stiel frei.",IntroChapter.PlayerName);return; }
        if(CourtyardHandleDelivery.TakeHandle())
        { dialogue.ShowMessage("Ich nehme den reparierten Schaufelstiel mit. Marta wartet im Garten darauf.",IntroChapter.PlayerName);return; }
        IdaConversation.Open(dialogue);
    }
    static DialogueLinePresentation Line(string speaker,DialogueEmotion emotion)=>
        new DialogueLinePresentation{speaker=speaker,emotion=emotion};
}
