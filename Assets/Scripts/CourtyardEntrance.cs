using UnityEngine;

public class CourtyardEntrance : MonoBehaviour,IInteractableAsPlayer
{
    public enum Destination { Tower,ThroneHall,Cellar,OuterGate }
    public Destination destination;
    public string Label=>destination==Destination.Tower?"Ins Turmzimmer":destination==Destination.ThroneHall?"Burgeingang ansehen":destination==Destination.Cellar?"Kellerzugang ansehen":"Burgtor ansehen";
    public Vector3 GetTransform()=>transform.position;
    public void InteractWithPlayer()
    {
        if(DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning || DialogueBoxController.instance==null || DialogueBoxController.instance.IsOpen)return;
        if(CourtyardGarden.HasCan || CourtyardWaterTask.HasBucket)
        {
            DialogueBoxController.instance.ShowMessage(CourtyardGarden.HasCan
                ? "Elian, stell die Gießkanne bitte erst wieder an ihren Platz im Garten."
                : "Bring den Eimer bitte erst zurück zum Stall, Elian.",CourtyardGarden.HasCan?"Marta":"Hannes");
            return;
        }
        if(destination==Destination.Tower){IntroChapter.MarkRoomReturn();SceneLoadManager.instance.LoadExplorationScene(IntroChapter.TowerScene);return;}
        if(destination==Destination.OuterGate && IntroChapter.Active && ArkanmeisterinArrival.AtGate(DialogueBoxController.instance))return;
        string text=destination==Destination.ThroneHall?"Dahinter liegt der Thronsaal. Für den Empfang bleibe ich erst einmal im Hof."
            :destination==Destination.Cellar?"Die Stufen führen hinunter zum Lagerraum. Was wohl noch unter unserer Burg liegt?"
            :"Hier erwarten wir die Arkanmeisterin. Bis sie eintrifft, kann ich mich noch im Hof umsehen.";
        DialogueBoxController.instance.ShowMessage(text,IntroChapter.PlayerName);
    }
}
