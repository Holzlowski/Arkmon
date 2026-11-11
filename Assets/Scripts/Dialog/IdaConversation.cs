public static class IdaConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        if (CourtyardHandleDelivery.Completed)
        {
            dialogue.ShowMessage("Marta hat ihren neuen Schaufelstiel? Danke, Elian! Nun kann sie wieder ohne Splitter arbeiten.", "Ida");
            return;
        }
        if (CourtyardHandleDelivery.HasHandle)
        {
            dialogue.ShowMessage("Bring den reparierten Schaufelstiel bitte zu Marta im Garten. Sie wartet bestimmt schon darauf.", "Ida");
            return;
        }
        if(CourtyardHandleDelivery.Offered)
        { dialogue.ShowMessage("Der Schaufelstiel liegt auf der Werkbank. Nimm ihn mit [E] und bring ihn bitte zu Marta.","Ida");return; }
        var lines = new[]{
            "Guten Morgen, Prinz Elian! Einen Moment, ich lege den Hobel beiseite.",
            "Martas Schaufelstiel ist fertig. Den Riss habe ich ausgebessert und das Holz schön glatt geschliffen.",
            "Kann ich etwas für dich tun, Ida?",
            "Magst du den Stiel zu Marta in den Garten bringen? Dann kann ich hier gleich weiterarbeiten.",
            "Gern. Ich bringe ihn ihr.",
            "Der fertige Stiel liegt auf der Werkbank. Nimm ihn mit [E] und bring ihn bitte zu Marta. Danke dir!"
        };
        CourtyardHandleDelivery.AwaitHandover(lines.Length, false);
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=lines,
            presentations=new[]{
                Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Neutral),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Friendly},
                Mood(DialogueEmotion.Friendly),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Friendly},
                Mood(DialogueEmotion.Friendly)
            },endAfterDialogue=true
        }},"Ida","",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)=>new DialogueLinePresentation{emotion=emotion};
}
