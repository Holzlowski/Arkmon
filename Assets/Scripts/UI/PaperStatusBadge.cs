using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Separate badges let burning and knockdown remain readable at the same time.
public class PaperStatusBadge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    private PaperMonsterCard card;
    private bool fire;
    private TextMeshProUGUI counter, explanation;
    private GameObject tooltip;
    private CanvasGroup group;

    public void Build(PaperMonsterCard owner, bool burning)
    {
        card = owner; fire = burning;
        PaperUIStyle style = PaperUIStyle.Load();
        group = gameObject.AddComponent<CanvasGroup>();
        Image background = GetComponent<Image>(); background.raycastTarget = true;
        var button = gameObject.AddComponent<Button>(); button.targetGraphic = background;
        button.onClick.AddListener(() => tooltip.SetActive(!tooltip.activeSelf));
        Image icon = PaperUIFactory.Image("Status badge icon", transform, fire ? style.fireIcon : style.downIcon,
            Vector2.zero, new Vector2(.26f, 1), new Vector2(6, 3), new Vector2(-1, -3), false);
        icon.preserveAspect = true;
        counter = PaperUIFactory.Text("Status badge label", transform, "", 15,
            new Vector2(.28f, 0), Vector2.one, Vector2.zero, new Vector2(-5, 0));
        bool upper = owner.enemy;
        var panel = PaperUIFactory.Image("Status explanation", owner.transform, style.panel,
            upper ? new Vector2(1, 0) : new Vector2(0, 1), upper ? new Vector2(1, 0) : new Vector2(0, 1),
            upper ? new Vector2(-370, -104) : new Vector2(0, 6), upper ? new Vector2(0, -6) : new Vector2(370, 104));
        tooltip = panel.gameObject;
        explanation = PaperUIFactory.Text("Status explanation text", panel.transform, "", 17,
            Vector2.zero, Vector2.one, new Vector2(20, 12), new Vector2(-20, -12));
        tooltip.SetActive(false);
    }

    private void LateUpdate()
    {
        if (card == null) return;
        Unit unit = card.Current;
        BurningStatus burning = unit != null ? unit.GetComponent<BurningStatus>() : null;
        bool visible = unit != null && !unit.IsDead && (fire ? burning != null && burning.IsBurning : unit.IsKnockedDown);
        group.alpha = visible ? 1 : 0; group.interactable = group.blocksRaycasts = visible;
        if (!visible) { tooltip.SetActive(false); return; }
        counter.text = fire ? $"Brennt · {burning.RemainingRounds} R." : "Am Boden";
        explanation.text = fire
            ? $"Brennt: {burning.DamagePerRound} Schaden am Rundenende. Noch {burning.RemainingRounds} Runde(n). Kann benachbarte Monster und Pflanzen entzünden."
            : "Am Boden: Nur Aufstehen ist möglich (1 AP). Der Zustand bleibt bestehen, bis das Monster aufsteht.";
    }
    public void OnPointerEnter(PointerEventData data) => tooltip.SetActive(group.alpha > 0);
    public void OnPointerExit(PointerEventData data) => tooltip.SetActive(false);
    public void OnSelect(BaseEventData data) => tooltip.SetActive(group.alpha > 0);
    public void OnDeselect(BaseEventData data) => tooltip.SetActive(false);
}
