using System;

internal static class CourtyardStarterMeetings
{
    [Flags] internal enum Starter { Kafla = 1, Panfrie = 2, Liora = 4, Rauk = 8 }
    const Starter Everyone = Starter.Kafla | Starter.Panfrie | Starter.Liora | Starter.Rauk;
    static Starter met;

    internal static bool HasMet(Starter starter) => (met & starter) == starter;
    internal static bool ReadyForBriefing => HasMet(Everyone);
    internal static void Meet(Starter starter)
    {
        if (CourtyardStarterSummoning.Completed) met |= starter;
    }
    internal static bool FieldTrialWon { get; set; }
    internal static bool RecalledForTrial { get; set; }
    internal static bool AtTrialField { get; set; }
    internal static void ResetProgress() { met = 0; FieldTrialWon = RecalledForTrial = AtTrialField = false; }

    internal static string Objective => !CourtyardStarterSummoning.Completed ? null
        : FieldTrialWon ? "Letzte Prüfung · Siegel beschützt"
        : AtTrialField ? "Letzte Prüfung · Sprecht mit der Arkanmeisterin auf dem Feld"
        : ReadyForBriefing ? "Letzte Prüfung · Kehrt zur Arkanmeisterin zurück"
        : "Arkmon · Lernt die vier im Hof kennen";
}
