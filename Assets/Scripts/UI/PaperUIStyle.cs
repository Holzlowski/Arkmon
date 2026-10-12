using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Paper UI Style")]
public class PaperUIStyle : ScriptableObject
{
    public Sprite panel, card, sageButton, selectedCard;
    public Sprite moveIcon, clawIcon, swordIcon, escapeIcon, fireIcon, downIcon;
    public Sprite flacaPortrait, sheepPortrait, hunterPortrait;
    public TMP_FontAsset font;
    public Color ink = new Color(0.22f, 0.14f, 0.09f);
    public Color sage = new Color(0.37f, 0.44f, 0.27f);
    public Color amber = new Color(0.93f, 0.52f, 0.12f);
    public Color cream = new Color(1f, 0.95f, 0.82f);
    public static PaperUIStyle Load() => Resources.Load<PaperUIStyle>("Paper UI");

    public Sprite Icon(BaseAction action)
    {
        if (action is MoveAction) return moveIcon;
        if (action is FlameSwordAction) return swordIcon;
        if (action is ClawAction) return clawIcon;
        if (action is EscapeAction) return escapeIcon;
        if (action is StandUpAction || action is RamAction) return downIcon;
        return clawIcon;
    }

    public static string ActionName(BaseAction action)
        => action is MoveAction ? "Bewegen" : action is EscapeAction ? "Fliehen" : action.GetActionName();

    public static string Status(Unit unit)
    {
        if (unit == null || unit.IsDead) return "";
        BurningStatus burning = unit.GetComponent<BurningStatus>();
        string fire = burning != null && burning.IsBurning
            ? $"Brennt · {burning.RemainingRounds} {(burning.RemainingRounds == 1 ? "Runde" : "Runden")}" : "";
        return unit.IsKnockedDown ? (fire.Length > 0 ? "Am Boden · " + fire : "Am Boden") : fire;
    }

    public static string MonsterName(Unit unit) => unit.Definition != null ? unit.Definition.name
        : unit.gameObject.name.Replace("(Clone)", "").Trim();
}
