using UnityEngine;

// Optional local observation, deliberately independent of inventory and quest progress.
public sealed class CourtyardWorkshopWorkpiece : MonoBehaviour,IInteractableAsPlayer
{
    public string Label=>"Werkstück ansehen";
    public Vector3 GetTransform()=>transform.position;
    public void InteractWithPlayer()
    {
        var dialogue=DialogueBoxController.instance;
        var player=GameObject.FindWithTag("Player");
        if(dialogue==null || player==null || dialogue.IsOpen || DemoOpening.IsPresenting ||
            SceneLoadManager.IsTransitioning)return;
        var controller=player.GetComponent<PlayerController>();
        if(controller==null || Vector3.Distance(player.transform.position,GetTransform())>controller.interacRange)return;
        var ida=GameObject.Find("Courtyard Carpenter");
        if(ida!=null)player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(ida.transform.position-player.transform.position);
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Ist das Martas Gartenschaufel? Der alte Griff hat ja einen richtigen Riss.",
                "Gut gesehen! Den ersetze ich. Der Schaufelkopf ist noch bestens in Ordnung.",
                "Deshalb hobelst du das Holz so sorgfältig?",
                "Ja. Erst passe ich das Holz ein, dann runde und glätte ich den Griff. Er soll gut in Martas Hand liegen.",
                "Dann kann sie damit bestimmt noch viele Jahre im Garten arbeiten.",
                "Das hoffe ich auch. Oft braucht ein vertrautes Werkzeug nur ein wenig Zuwendung."
            },
            presentations=new[]{
                Line(IntroChapter.PlayerName,DialogueEmotion.Worried),Line("Ida",DialogueEmotion.Friendly),
                Line(IntroChapter.PlayerName,DialogueEmotion.Neutral),Line("Ida",DialogueEmotion.Neutral),
                Line(IntroChapter.PlayerName,DialogueEmotion.Friendly),Line("Ida",DialogueEmotion.Friendly)},
            endAfterDialogue=true}},IntroChapter.PlayerName,"",null);
    }
    static DialogueLinePresentation Line(string speaker,DialogueEmotion emotion)=>
        new DialogueLinePresentation{speaker=speaker,emotion=emotion};
}
