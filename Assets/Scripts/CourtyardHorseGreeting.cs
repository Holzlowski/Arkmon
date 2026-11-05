using UnityEngine;

// A greeting at a reachable point outside the stall, without moving through its gate.
public sealed class CourtyardHorseGreeting : MonoBehaviour,IInteractableAsPlayer
{
    CourtyardHorse horse;
    bool greeted;
    public string Label=>horse!=null && horse.IsFeeding?"Braunes Pferd begrüßen":"Helles Pferd begrüßen";
    public Vector3 GetTransform()=>transform.position;
    public void Configure(CourtyardHorse target)=>horse=target;

    public void InteractWithPlayer()
    {
        var dialogue=DialogueBoxController.instance;
        var player=GameObject.FindWithTag("Player");
        if(horse==null || dialogue==null || player==null || dialogue.IsOpen ||
            DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning)return;
        var controller=player.GetComponent<PlayerController>();
        if(controller==null || Vector3.Distance(player.transform.position,GetTransform())>controller.interacRange)return;
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(horse.transform.position-player.transform.position);
        horse.Greet();
        bool feeding=horse.IsFeeding;
        string greeting=greeted?"Na, du? Ich wollte nur kurz nach dir sehen.":feeding?
            "Guten Morgen! Dein Frühstück scheint dir zu schmecken.":"Hallo, du. Ganz ruhig, ich bin's.";
        string reply=feeding?"Beim Heu lässt er sich Zeit. Ein ruhiger Begleiter – solange du ihm sein Frühstück gönnst.":
            "Sie ist neugierig, aber schnelle Bewegungen mag sie nicht. Sprich ruhig mit ihr, dann kommt das Vertrauen von allein.";
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{greeting,reply,feeding?"Dann lass ich dich in Ruhe weiterfressen.":"Ich hab Zeit. Wir lernen uns schon kennen."},
            presentations=new[]{
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Friendly},
                new DialogueLinePresentation{speaker="Hannes",emotion=DialogueEmotion.Friendly},
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Neutral}},
            endAfterDialogue=true}},IntroChapter.PlayerName,"",null);
        greeted=true;
    }
}
