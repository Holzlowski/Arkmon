using UnityEngine;

public static class MartaConversation
{
    public static void Open(DialogueBoxController dialogue)
    {
        string[] lines;
        DialogueLinePresentation[] presentations;
        if(CourtyardGarden.Completed && CourtyardGarden.ThanksReceived)
        {
            lines=new[]{"Die Beete sehen schon viel besser aus. Danke noch einmal, Elian!",
                CourtyardGarden.HasCan?"Stell die Kanne bitte wieder ans erste Beet. Dann finde ich sie später gleich.":"Du hast sogar die Kanne zurückgestellt. Nun genieß deinen Morgen!"};
            presentations=new[]{Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Friendly)};
        }
        else if(CourtyardGarden.Completed)
        {
            lines=new[]{
                "Oh! Du hast die Beete schon alle gegossen?",
                "Vielen Dank, Elian. Jetzt kommen die jungen Pflanzen gut durch den Morgen.",
                "Ein Prinz, der auch an die kleinen Dinge denkt. Das tut unserem Hof gut.",
                "Nun halte ich dich nicht länger auf. Für deinen großen Tag drücke ich dir die Daumen!"
            };
            presentations=new[]{Mood(DialogueEmotion.Surprised),Mood(DialogueEmotion.Friendly),
                Mood(DialogueEmotion.Neutral),Mood(DialogueEmotion.Friendly)};
        }
        else if(CourtyardGarden.HasCan)
        {
            lines=new[]{
                $"Danke, dass du mir hilfst, Elian. {CourtyardGarden.WateredCount} von den drei Beeten haben schon Wasser bekommen.",
                "Geh mit der Kanne an ein trockenes Beet und gieße es mit [E].",
                "Danach stell die Kanne bitte wieder an ihren Platz."
            };
            presentations=new[]{Mood(DialogueEmotion.Friendly),Mood(DialogueEmotion.Neutral),Mood(DialogueEmotion.Friendly)};
        }
        else
        {
            lines=new[]{
                "Guten Morgen, Prinz Elian! Schön, dass du bei mir vorbeischaust.",
                "Guten Morgen, Marta. Heute kommt die Arkanmeisterin. Ich bin schon ein bisschen aufgeregt.",
                "Heute schon? Dann ist das wirklich ein besonderer Morgen!",
                "Ich sortiere gerade die Ernte. Ein Teil davon kommt später in die Küche.",
                "Nur die jungen Pflanzen machen mir Sorgen. Die Erde in den Beeten ist ganz trocken.",
                "Wenn du noch einen Moment Zeit hast: Die Gießkanne steht am ersten Beet. Ein wenig Wasser würde ihnen guttun."
            };
            presentations=new[]{Mood(DialogueEmotion.Friendly),
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Neutral},
                Mood(DialogueEmotion.Surprised),Mood(DialogueEmotion.Neutral),
                Mood(DialogueEmotion.Worried),Mood(DialogueEmotion.Friendly)};
        }
        CourtyardGarden.OfferHelp();
        if(CourtyardGarden.Completed && !CourtyardGarden.ThanksReceived)CourtyardGarden.BeginThanks();
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=lines,presentations=presentations,endAfterDialogue=true
        }},"Marta","",null);
    }
    private static DialogueLinePresentation Mood(DialogueEmotion emotion)
        =>new DialogueLinePresentation{emotion=emotion};
}