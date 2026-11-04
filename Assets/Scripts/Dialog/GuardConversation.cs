public static class GuardConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Guten Morgen, Prinz Elian! Schon unterwegs? Schön, Euch im Hof zu sehen.",
                "Ist draußen alles ruhig?",
                "Ja. Ich schaue regelmäßig am Tor nach und mache meine Runde durch den Hof.",
                "Heute kommt doch die Arkanmeisterin. Ich bin schon ein bisschen aufgeregt.",
                "Das kann ich gut verstehen. Holt ruhig noch einmal tief Luft. Ihr müsst nicht alles auf einmal schaffen.",
                "Danke. Das versuche ich.",
                "Gern. Ich halte die Augen offen. Schaut Euch in Ruhe um!"
            },
            presentations=new[]{
                Mood(DialogueEmotion.Friendly),Elian(DialogueEmotion.Neutral),Mood(DialogueEmotion.Neutral),
                Elian(DialogueEmotion.Worried),Mood(DialogueEmotion.Friendly),Elian(DialogueEmotion.Friendly),Mood(DialogueEmotion.Friendly)
            },endAfterDialogue=true
        }},CourtyardGuard.Speaker,"",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)=>new DialogueLinePresentation{emotion=emotion};
    private static DialogueLinePresentation Elian(DialogueEmotion emotion)=>new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=emotion};
}