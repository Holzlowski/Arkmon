public static class HannesConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        if (CourtyardWaterTask.Completed)
        {
            CourtyardWaterTask.ReceiveThanks();
            dialogue.ShowMessage("Danke fürs frische Wasser, Elian! Die Pferde sind versorgt. Eine ruhige Hand hast du schon — die wird dir heute helfen.","Hannes");return;
        }
        if (CourtyardWaterTask.HasBucket)
        {
            dialogue.ShowMessage(CourtyardWaterTask.BucketFull
                ? "Frisches Wasser! Gieß es bitte in die Tränke neben dem Stall."
                : "Fülle den Eimer am Brunnen zwischen Stall und Garten. Danach kommt das Wasser in die Pferdetränke.","Hannes");return;
        }
        CourtyardWaterTask.OfferHelp();
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Guten Morgen, Prinz Elian! Ich bürste ihr nur noch das Fell.",
                "Das Heu ist schon verteilt. Nur die Tränke braucht noch frisches Wasser.",
                "Kann ich dir helfen, Hannes?",
                "Sehr gern! Der leere Eimer steht neben der Tränke. Fülle ihn am Brunnen zwischen Stall und Garten.",
                "Dann bringe ich das Wasser zu den Pferden.",
                "Genau. Gieß es in die Tränke und stell den Eimer danach wieder an seinen Platz. Danke, Elian!"
            },presentations=new[]{
                Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Neutral),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Friendly},
                Mood(DialogueEmotion.Friendly),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Friendly},
                Mood(DialogueEmotion.Friendly)},endAfterDialogue=true
        }},"Hannes","",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)=>new DialogueLinePresentation{emotion=emotion};
}
