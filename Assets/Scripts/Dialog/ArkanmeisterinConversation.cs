using UnityEngine;

// Provisional story lines live here so they can be revised independently of the staging.
public static class ArkanmeisterinConversation
{
    public const string Speaker = "Arkanmeisterin";
    public const string Morning = "Heute ist es so weit. Die Arkanmeisterin kommt im Laufe des Tages mit ihrer Kutsche ans Burgtor. Hoffentlich klappt es diesmal. Aber erst ziehe ich mein Familiengewand an. Es hängt im Schrank.";
    public const string GuardArrival = "Puh … Prinz Elian! Die Arkanmeisterin ist da!";

    internal const int HoodCueLineNumber = 3;
    internal const string VesselOffer = "Hier, Elian. Dieses Gefäß gehört Euch. Haltet es mit beiden Händen und konzentriert Euch.";
    internal const string TrialRetry = "Versuchen wir es noch einmal, Elian. Nehmt Euch Zeit.";

    internal static void Greet(DialogueBoxController dialogue, Sprite hooded, Sprite unveiled,
        System.Func<System.Collections.IEnumerator> pullHoodBack)
    {
        dialogue.ShowConversation(new[]{new DialogueSection{
            dialogue=new[]{
                "Guten Morgen, Elian. Ihr seid schon eine ganze Weile wach, nicht wahr?",
                "Seit Sonnenaufgang. Ich frage mich, ob das Gefäß heute auf mich reagiert.",
                "Ich kenne dieses Gefühl. Seht mich an, Elian.",
                "Dann hören wir heute gemeinsam hin."
            },
            presentations=new[]{
                new DialogueLinePresentation{portrait=hooded,emotion=DialogueEmotion.Neutral},
                new DialogueLinePresentation{speaker=IntroChapter.PlayerName,emotion=DialogueEmotion.Worried},
                new DialogueLinePresentation{portrait=hooded,emotion=DialogueEmotion.Neutral},
                new DialogueLinePresentation{portrait=unveiled,emotion=DialogueEmotion.Friendly,BeforePresentation=pullHoodBack}
            },
            endAfterDialogue=true
        }},Speaker,"",null);
    }

    internal static void WalkTogether(DialogueBoxController dialogue, Sprite portrait,
        System.Func<System.Collections.IEnumerator> nearPath, System.Func<System.Collections.IEnumerator> nearCenter)
    {
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = new[] {
                "Kommt, Elian. Gehen wir ein Stück.",
                "Was, wenn es wieder nicht antwortet?",
                "Dann gebt Ihr ihm Zeit. Hier in der Mitte haben wir Platz."
            },
            presentations = new[] {
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly },
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName, emotion = DialogueEmotion.Worried,
                    BeforePresentation = nearPath },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly,
                    BeforePresentation = nearCenter }
            },
            endAfterDialogue = true
        } }, Speaker,"",null);
    }

    internal static void Congratulate(DialogueBoxController dialogue, Sprite portrait, bool john, bool guard,
        bool marta, bool hannes, bool ida)
    {
        var lines = new System.Collections.Generic.List<string> {
            "Gut gemacht, Elian. Ihr habt es selbst zum Leuchten gebracht."
        };
        var faces = new System.Collections.Generic.List<DialogueLinePresentation> {
            new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly }
        };
        if (john)
        {
            lines.Add("Da war es! Ich hab es genau gesehen.");
            faces.Add(new DialogueLinePresentation { speaker = "John", emotion = DialogueEmotion.Friendly });
        }
        if (guard)
        {
            lines.Add("Gut gemacht, Prinz Elian!");
            faces.Add(new DialogueLinePresentation { speaker = CourtyardGuard.Speaker, emotion = DialogueEmotion.Friendly });
        }
        if (marta)
        {
            lines.Add("Oh, wie hell es leuchtet! Habt Ihr das gesehen?");
            faces.Add(new DialogueLinePresentation { speaker = "Marta", emotion = DialogueEmotion.Friendly });
        }
        if (hannes)
        {
            lines.Add("Ha! Das haben bestimmt sogar die Pferde gesehen.");
            faces.Add(new DialogueLinePresentation { speaker = "Hannes", emotion = DialogueEmotion.Friendly });
        }
        if (ida)
        {
            lines.Add("Saubere Arbeit, Elian. Ihr seid drangeblieben.");
            faces.Add(new DialogueLinePresentation { speaker = "Ida", emotion = DialogueEmotion.Friendly });
        }

        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = lines.ToArray(), presentations = faces.ToArray(), endAfterDialogue = true
        } }, Speaker,"",null);
    }

    internal static void ExplainArkmon(DialogueBoxController dialogue, Sprite portrait) =>
        SpeakAsMentor(dialogue,portrait,
            "Aus dem Arkan werden Wesen geboren, die wir Arkmon nennen. Unsere Gefäße können sie hüten und zu uns rufen.",
            "Ein Arkmon und sein Hüter sind Partner. Sie helfen einander, das arkane Gleichgewicht zu bewahren.",
            "Je besser Ihr das Arkan versteht, desto mehr Arkmon könnt Ihr in Eurem Gefäß hüten. Seht her, Elian.");

    internal static void SendStartersToExplore(DialogueBoxController dialogue, Sprite portrait) =>
        SpeakAsMentor(dialogue,portrait,
            "Kafla, Panfrie, Liora, Rauk. Schaut euch ruhig ein wenig im Hof um.");

    internal static void InviteStarterMeetings(DialogueBoxController dialogue, Sprite portrait) =>
        SpeakAsMentor(dialogue,portrait,
            "Sprecht mit ihnen und lernt sie kennen, Elian. Wer Euer Partner wird, entscheidet Ihr später.",
            "Kafla findet Ihr am Trainingsplatz und Panfrie beim Brunnen. Liora zieht es zum Garten, Rauk eher zum Burgtor.",
            "Kommt danach zu mir zurück. Eine letzte Prüfung wartet heute noch auf Euch.");

    internal static void SpeakAsMentor(DialogueBoxController dialogue, Sprite portrait, params string[] lines)
    {
        var faces = new DialogueLinePresentation[lines.Length];
        for (int i=0; i<faces.Length; i++)
            faces[i] = new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly };
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = lines, presentations = faces, endAfterDialogue = true
        } }, Speaker,"",null);
    }

    internal static void RecallBeforeField(DialogueBoxController dialogue, Sprite portrait)
        => SpeakAsMentor(dialogue, portrait, "Ihr habt nun alle vier kennengelernt. Ich rufe sie zurück. Die letzte Prüfung wartet draußen auf dem Feld.");

    internal static void WalkToField(DialogueBoxController dialogue, Sprite portrait,
        System.Func<System.Collections.IEnumerator> outsideGate)
    {
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = new[] {
                "Ihr kennt die vier jetzt ein wenig. Ein Hüter muss auch wissen, wann er welche Fähigkeit einsetzt.",
                "Und wenn ich mich falsch entscheide?",
                "Dann schaut Ihr, was sich verändert hat, und entscheidet neu. Dort vorn haben wir Platz zum Üben."
            },
            presentations = new[] {
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly },
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName, emotion = DialogueEmotion.Worried },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly, BeforePresentation = outsideGate }
            },
            endAfterDialogue = true
        } }, Speaker, "", null);
    }

    internal static void AwardKeeperMantle(DialogueBoxController dialogue, Sprite portrait,
        string partner, string reaction, System.Func<System.Collections.IEnumerator> putOnMantle)
    {
        var mentor = new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly };
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = new[] {
                "Ihr habt die Prüfung bestanden, Elian. Von heute an dürft Ihr den Mantel eines Arkanhüters tragen.",
                "Diesen hier habe ich einst Eurem Vater überreicht. Jetzt gehört er Euch.",
                reaction
            },
            presentations = new[] {
                mentor, mentor,
                new DialogueLinePresentation { speaker = partner, BeforePresentation = putOnMantle }
            },
            endAfterDialogue = true
        } }, Speaker, "", null);
    }

    internal static void WalkBackToCourtyard(DialogueBoxController dialogue, Sprite portrait,
        CourtyardStarterMeetings.Starter partner, System.Func<System.Collections.IEnumerator> onPath,
        System.Func<System.Collections.IEnumerator> insideGate)
    {
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = new[] {
                "Kommt, Elian. Gehen wir zurück zum Hof.",
                $"Kann {partner} mich im Gefäß noch spüren?",
                $"Ja. Auch im Gefäß bleibt {partner} mit Eurem Arkan verbunden.",
                "Mit jeder Erfahrung lernt Ihr einander besser kennen. So könnt Ihr beide gemeinsam stärker werden."
            },
            presentations = new[] {
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly },
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName, emotion = DialogueEmotion.Neutral },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly, BeforePresentation = onPath },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly, BeforePresentation = insideGate }
            },
            endAfterDialogue = true
        } }, Speaker, "", null);
    }

    public static void SpeakAgain(DialogueBoxController dialogue, Sprite portrait)
    {
        if (CourtyardStarterMeetings.FieldTrialWon)
            SpeakAsMentor(dialogue, portrait, CourtyardStarterMeetings.ChosenPartner.HasValue
                ? $"Ihr habt {CourtyardStarterMeetings.ChosenPartner.Value} gewählt, Elian."
                : "Ihr habt das Siegel gemeinsam beschützt. Schaut Euch die vier noch einmal an, Elian.");
        else if (CourtyardStarterMeetings.ReadyForBriefing)
        {
            var configuration = Resources.Load<BattleConfiguration>("Golden Field Trial");
            if (configuration == null)
            {
                SpeakAsMentor(dialogue,portrait,"Die letzte Prüfung wartet draußen auf dem Feld.");
                return;
            }
            var face = new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly };
            dialogue.ShowConversation(new[] {
                new DialogueSection {
                    dialogue = new[] { "Hier ist Platz für Eure letzte Prüfung.",
                        "Haltet das Siegel drei Runden lang. Ihr könnt allen vier Arkmon Befehle geben." },
                    presentations = new[] { face, face },
                    branchPoint = new BranchPoint {
                        question = "Seid Ihr bereit, Elian?",
                        presentation = face,
                        answers = new[] { new Answer { answerLabel = "Ich bin bereit.", nextElement = 1 },
                            new Answer { answerLabel = "Ich brauche noch einen Moment.", nextElement = 2 } }
                    }
                },
                new DialogueSection { dialogue = new[] { "Dann beginnen wir." }, presentations = new[] { face }, battleAfterDialogue = true },
                new DialogueSection { dialogue = new[] { "Nehmt Euch Zeit. Sagt mir Bescheid, wenn Ihr bereit seid." }, presentations = new[] { face }, endAfterDialogue = true }
            },Speaker,"BattleScene",configuration);
        }
        else
            SpeakAsMentor(dialogue,portrait,
                "Nehmt Euch ruhig Zeit, Elian. Kommt zu mir zurück, sobald Ihr mit allen vier gesprochen habt.");
    }
}
