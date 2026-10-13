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
            new Vector2(0.24f, 0), new Vector2(0.81f, .78f), new Vector2(0, 15), new Vector2(-6, -3));
        RectTransform tabs = PaperUIFactory.Rect("Command categories", band.transform,
            new Vector2(.24f, .79f), new Vector2(.81f, 1), new Vector2(0, 0), new Vector2(-6, -6));
        Button basic = Category("Aktionen", tabs, style, 0, .29f);
        Button attacks = Category("Attacken", tabs, style, .31f, .60f);
        Button previous = Category("<", tabs, style, .65f, .73f);
        Button next = Category(">", tabs, style, .91f, .99f);
        var page = PaperUIFactory.Text("Attack page", tabs, "", 16,
            new Vector2(.74f, 0), new Vector2(.9f, 1), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
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
