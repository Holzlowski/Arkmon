public static class IdaConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Guten Morgen, Prinz Elian! Einen Moment, ich lege den Hobel beiseite.",
                "Ich passe gerade den Griff für Martas Gartenschaufel an. Er soll gut in der Hand liegen.",
                "Der alte Griff ist eingerissen. Ein Splitter wäre beim Arbeiten wirklich unangenehm.",
                "Kann ich etwas für dich tun, Ida?",
                "Du willst mir helfen? Das ist lieb. Heute habe ich alles, was ich brauche.",
                "Schau ruhig bei Marta vorbei. Und für deinen großen Tag wünsche ich dir eine ruhige Hand!"
            },
            presentations=new[]{
                Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Neutral),Mood(DialogueEmotion.Worried),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Neutral},
                Mood(DialogueEmotion.Surprised),Mood(DialogueEmotion.Friendly)
            },
            endAfterDialogue=true
        }},"Ida","",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)=>new DialogueLinePresentation{emotion=emotion};
}