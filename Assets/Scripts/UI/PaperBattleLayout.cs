using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PaperBattleLayout
{
    public static bool Actions(UnitActionSystemUI controller, out Transform container, out TextMeshProUGUI points)
    {
        container = null; points = null;
        PaperUIStyle style = PaperUIStyle.Load();
        if (style == null) return false;
        PaperUIFactory.HideChildren(controller.transform);
        PaperUIFactory.Scale(controller.GetComponentInParent<Canvas>());
        Image band = PaperUIFactory.Image("Paper battle command bar", controller.transform, style.panel,
            Vector2.zero, new Vector2(1, 0), new Vector2(12, 12), new Vector2(-12, 224));
        band.gameObject.AddComponent<BattlePhaseVisibility>();
        band.raycastTarget = true;
        Image card = PaperUIFactory.Image("Selected monster card", band.transform, style.card,
            Vector2.zero, new Vector2(0.23f, 1), new Vector2(12, 10), new Vector2(-6, -10));
        card.gameObject.AddComponent<PaperMonsterCard>().Build(false);
        points = PaperUIFactory.Text("AP compatibility label", controller.transform, "", 16,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        points.gameObject.SetActive(false);
        RectTransform actions = PaperUIFactory.Rect("Action cards", band.transform,
            new Vector2(0.40f, 0), new Vector2(0.81f, .78f), new Vector2(0, 15), new Vector2(-6, -3));
        RectTransform tabs = PaperUIFactory.Rect("Command categories", band.transform,
            new Vector2(.40f, .79f), new Vector2(.81f, 1), new Vector2(0, 0), new Vector2(-6, -6));
        RectTransform categories = PaperUIFactory.Rect("Choose command area", band.transform,
            new Vector2(.24f, 0), new Vector2(.39f, 1), new Vector2(0, 15), new Vector2(-8, -12));
        Button basic = PaperUIFactory.Button("Aktionen tab", categories, style.card, new Vector2(0,.53f), Vector2.one, Vector2.zero, Vector2.zero);
        Button attacks = PaperUIFactory.Button("Attacken tab", categories, style.card, Vector2.zero, new Vector2(1,.47f), Vector2.zero, Vector2.zero);
        PaperUIFactory.Text("Actions category", basic.transform, "<b>Aktionen</b>\n<size=70%>Bewegen · Abwehr · Fliehen</size>", 23, Vector2.zero, Vector2.one, new Vector2(12,8), new Vector2(-12,-8), TextAlignmentOptions.Center);
        PaperUIFactory.Text("Attacks category", attacks.transform, "<b>Attacken</b>\n<size=70%>Kaflas Fähigkeiten</size>", 23, Vector2.zero, Vector2.one, new Vector2(12,8), new Vector2(-12,-8), TextAlignmentOptions.Center);
        var heading = PaperUIFactory.Text("Command step", tabs, "1 · Wähle einen Bereich", 19, Vector2.zero, new Vector2(.66f,1), Vector2.zero, Vector2.zero);
        var landing = PaperUIFactory.Text("Command choice instructions", band.transform,
            "Was soll Kafla tun?\n\nLinks einen Bereich wählen.\nDanach einen Befehl und sein Zielfeld wählen.", 23,
            new Vector2(.40f,0), new Vector2(.81f,.78f), new Vector2(12,20), new Vector2(-12,-8), TextAlignmentOptions.Center);
        controller.ConfigureMenuPresentation(landing.gameObject, heading);
        Button back = Category("Zurück", tabs, style, .68f, .84f);
        controller.ConfigureBack(back);
        Button previous = Category("<", tabs, style, .85f, .90f);
        Button next = Category(">", tabs, style, .95f, 1);
        var page = PaperUIFactory.Text("Attack page", tabs, "", 16,
            new Vector2(.90f, 0), new Vector2(.95f, 1), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        controller.ConfigureCategoryTabs(basic, attacks, previous, next, page);
        var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        container = actions;
        Image enemy = PaperUIFactory.Image("Opponent monster card", controller.transform, style.card,
            new Vector2(1, 1), Vector2.one, new Vector2(-345, -166), new Vector2(-18, -18));
        enemy.gameObject.AddComponent<PaperMonsterCard>().Build(true);
        enemy.gameObject.AddComponent<BattlePhaseVisibility>();
        enemy.raycastTarget = true;
        var tooltip = PaperUIFactory.Image("Action explanation", controller.transform, style.panel,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-290, 234), new Vector2(290, 308));
        var text = PaperUIFactory.Text("Explanation", tooltip.transform, "", 18,
            Vector2.zero, Vector2.one, new Vector2(24, 12), new Vector2(-24, -12), TextAlignmentOptions.Center);
        controller.SetTooltip(tooltip.gameObject, text);
        tooltip.gameObject.AddComponent<BattlePhaseVisibility>();
        tooltip.gameObject.SetActive(false);
        controller.gameObject.AddComponent<AttackPreview>().Build(controller.transform);
        controller.gameObject.AddComponent<BattleCommandPrompt>().Build(controller);
        return true;
    }

    private static Button Category(string label, Transform parent, PaperUIStyle style, float left, float right)
    {
        Button button = PaperUIFactory.Button(label + " tab", parent, style.card,
            new Vector2(left, 0), new Vector2(right, 1), Vector2.zero, Vector2.zero);
        PaperUIFactory.Text("Tab label", button.transform, label, 19, Vector2.zero, Vector2.one,
            new Vector2(12, 5), new Vector2(-12, -5), TextAlignmentOptions.Center);
        return button;
    }

    public static bool Turns(TurnSystemUI controller, out Button end, out TextMeshProUGUI turn, out GameObject enemyTurn)
    {
        end = null; turn = null; enemyTurn = null;
        PaperUIStyle style = PaperUIStyle.Load();
        if (style == null) return false;
        PaperUIFactory.HideChildren(controller.transform);
        Image tab = PaperUIFactory.Image("Turn paper tab", controller.transform, style.panel,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-225, -74), new Vector2(225, -10));
        tab.gameObject.AddComponent<BattlePhaseVisibility>();
        turn = PaperUIFactory.Text("Turn label", tab.transform, "", 24, Vector2.zero, Vector2.one,
            new Vector2(18, 8), new Vector2(-18, -8), TextAlignmentOptions.Center);
        end = PaperUIFactory.Button("End turn paper button", controller.transform, style.sageButton,
            new Vector2(0.82f, 0), new Vector2(1, 0), new Vector2(2, 66), new Vector2(-28, 158));
        end.gameObject.AddComponent<BattlePhaseVisibility>();
        var text = PaperUIFactory.Text("End turn label", end.transform, "Zug beenden", 24,
            Vector2.zero, Vector2.one, new Vector2(18, 16), new Vector2(-18, -16), TextAlignmentOptions.Center);
        text.color = style.cream;
        enemyTurn = new GameObject("Enemy turn state", typeof(RectTransform));
        enemyTurn.transform.SetParent(controller.transform, false);
        return true;
    }
}
