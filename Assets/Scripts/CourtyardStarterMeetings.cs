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
    internal static Starter? ChosenPartner { get; private set; }
    internal static bool PartnerBound { get; set; }
    internal static bool HasKeeperMantle { get; set; }
    internal static bool ConfirmPartner(Starter starter)
    {
        if (!FieldTrialWon || ChosenPartner.HasValue || !Enum.IsDefined(typeof(Starter), starter)) return false;
        ChosenPartner = starter;
        return true;
    }

    internal static void ResetProgress()
    {
        met = 0;
        FieldTrialWon = RecalledForTrial = AtTrialField = false;
        ChosenPartner = null;
        PartnerBound = HasKeeperMantle = false;
    }

    internal static string Objective => !CourtyardStarterSummoning.Completed ? null
        : PartnerBound ? "Arkanbund · " + ChosenPartner.Value + " ist Elians Partner"
        : ChosenPartner.HasValue ? "Arkanbund · Sprecht mit der Arkanmeisterin"
        : FieldTrialWon ? "Partnerwahl · Wählt eines der vier Arkmon"
        : AtTrialField ? "Letzte Prüfung · Sprecht mit der Arkanmeisterin auf dem Feld"
        : ReadyForBriefing ? "Letzte Prüfung · Kehrt zur Arkanmeisterin zurück"
        : "Arkmon · Lernt die vier im Hof kennen";
}
