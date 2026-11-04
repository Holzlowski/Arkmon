public static class HannesConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Guten Morgen, Prinz Elian! Ich bürste nur noch diesen Sattel sauber.",
                "Die Pferde haben schon Heu und frisches Wasser. Bei ihnen beginnt der Tag immer zuerst.",
                "Du schaust heute so ernst. Macht dir die Prüfung Sorgen?",
                "Ein bisschen. Ich möchte meine Familie stolz machen.",
                "Du glaubst doch nicht, dass sie dafür erst eine bestandene Prüfung brauchen!",
                "Atme ruhig durch. Wir drücken dir alle die Daumen."
            },
            presentations=new[]{
                Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Neutral),Mood(DialogueEmotion.Worried),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Neutral},
                Mood(DialogueEmotion.Surprised),Mood(DialogueEmotion.Friendly)
            },endAfterDialogue=true
        }},"Hannes","",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)=>new DialogueLinePresentation{emotion=emotion};
}