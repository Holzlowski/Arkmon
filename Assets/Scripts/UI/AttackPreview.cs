using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackPreview : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI label;
    public void Build(Transform parent)
    {
        var image = PaperUIFactory.Image("Attack outcome preview", parent, PaperUIStyle.Load().panel,
            new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-355, 234), new Vector2(355, 348));
        image.raycastTarget = false; panel = image.gameObject;
        label = PaperUIFactory.Text("Attack outcome", image.transform, "", 19, Vector2.zero, Vector2.one,
            new Vector2(24, 12), new Vector2(-24, -12), TextAlignmentOptions.Center);
        panel.AddComponent<BattlePhaseVisibility>(); panel.SetActive(false);
    }
    public static string Describe(BaseAction action, GridPosition cell)
    {
        if (action == null || !action.IsValidActionGridPosition(cell)) return "";
        if (action is EmberAction fan) return DescribeFan(fan, cell);
        Unit attacker = action.GetUnit(); Unit target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
        int raw = action is MeleeAttackAction melee ? melee.DamageAmount : action is RamAction ram ? ram.DamageAmount
            : action is EmberAction ember ? ember.DamageAmount : action is ShootAction shoot ? shoot.DamageAmount : 0;
        string cost = $"{action.GetActionName()} · {action.GetActionPointCost()} AP";
        if (action is GuardAction) return cost + "\n50 % weniger direkter Schaden bis zum nächsten eigenen Zug. Eine weitere Aktion beendet Abwehr.";
        if (action is StandUpAction) return cost + "\nDein Monster steht auf und kann wieder handeln.";
        if (raw == 0) return "";
        BurningObstacle obstacle = target == null ? CoverSystem.ObstacleAt(cell) : null;
        int damage = target != null ? target.PreviewDamage(raw, true) : raw;
        string details = target != null ? $"{PaperUIStyle.MonsterName(target)}: −{Mathf.Min(damage, target.GetComponent<HealthSystem>().Health)} LP"
            : obstacle != null ? $"{obstacle.name}: −{Mathf.Min(raw, obstacle.Health)} LP" : "";
        if (target != null && action is ShootAction)
        {
            CoverSystem.DamagePlan plan = CoverSystem.Preview(attacker.GetGridPosition(), cell, raw, target);
            damage = plan.unitDamage;
            details = $"{PaperUIStyle.MonsterName(target)}: −{Mathf.Min(plan.unitDamage, target.GetComponent<HealthSystem>().Health)} LP";
            if (plan.cover != null) details += $" · {plan.cover.name}: −{plan.coverDamage} LP";
        }
        bool fire = action is FlameSwordAction || action is EmberAction;
        if (fire)
        {
            BurningStatus status = target != null ? target.GetComponent<BurningStatus>() : obstacle?.GetComponent<BurningStatus>();
            FireRules rules = TurnSystem.Instance.GetComponent<BurningSystem>().Rules;
            bool survives = target != null ? target.GetComponent<HealthSystem>().Health > damage : obstacle != null && obstacle.Health > raw;
            details += !survives ? " · Besiegt/zerstört" : status != null && status.CanBurn ? target == null
                ? " · Entzündet (100 %)" : $" · Verbrennen {Mathf.RoundToInt(rules.monsterHitChance * 100)} %" : " · Gegen Verbrennen immun";
        }
        if (action is RamAction charge && charge.PreviewPush(cell, out GridPosition pushed, out bool down))
            details += down ? " · Hindernis: Gegner fällt um; Angreifer bleibt davor"
                : $" · Stoß nach ({pushed.x}, {pushed.z}); Angreifer übernimmt das Zielfeld";
        if (target != null && target.IsGuarding) details += " · Abwehr aktiv";
        return cost + "\n" + details + (attacker.CanSpendActionPointsToTakeAction(action) ? "" : " · Zu wenig AP");
    }
    private static string DescribeFan(EmberAction action, GridPosition centre)
    {
        var details = new List<string>(); var seen = new HashSet<Unit>();
        var obstacles = new HashSet<BurningObstacle>(); bool friendly = false;
        var cells = action.GetImpactGridPositionList(centre);
        FireRules rules = TurnSystem.Instance.GetComponent<BurningSystem>().Rules;
        foreach (GridPosition cell in cells)
        {
            foreach (Unit target in LevelGrid.Instance.GetUnitListAtGridPosition(cell, action.GetUnit()))
            {
            if (target != null && !target.IsDead && seen.Add(target))
            {
                int damage = target.PreviewDamage(action.DamageAmount, true);
                bool survives = target.GetComponent<HealthSystem>().Health > damage;
                bool canBurn = target.GetComponent<BurningStatus>()?.CanBurn == true;
                details.Add($"{PaperUIStyle.MonsterName(target)}: −{Mathf.Min(damage, target.GetComponent<HealthSystem>().Health)} LP"
                    + (!survives ? " · Besiegt" : canBurn ? $" · Verbrennen {Mathf.RoundToInt(rules.monsterHitChance * 100)} %" : " · Gegen Verbrennen immun"));
                friendly |= target.IsEnemy() == action.GetUnit().IsEnemy();
            }
            }
            foreach (BurningObstacle obstacle in CoverSystem.ObstaclesAt(cell))
                if (obstacles.Add(obstacle)) details.Add($"{obstacle.name}: −{Mathf.Min(action.DamageAmount, obstacle.Health)} LP"
                    + (obstacle.Health <= action.DamageAmount ? " · Zerstört" : obstacle.IsVegetation ? " · Entzündet (100 %)" : " · Gegen Verbrennen immun"));
        }
        string text = $"Glutfunke · {action.GetActionPointCost()} AP · 3 Felder · {action.DamageAmount} Feuerschaden";
        if (friendly) text += " · Achtung: Verbündete werden getroffen";
        if (cells.Count < 3) text += $" · {3-cells.Count} Feld(er) abgeschirmt";
        text += "\n" + (details.Count > 0 ? string.Join(" / ", details) : "Keine Ziele in der Reihe");
        if (!action.GetUnit().CanSpendActionPointsToTakeAction(action)) text += " · Zu wenig AP";
        return text;
    }
    private void LateUpdate()
    {
        if (panel == null) return;
        UnitActionSystem system = UnitActionSystem.Instance;
        if (system == null || system.IsBusy || BaseAction.IsAnyActionActive || !TurnSystem.Instance.IsPlayerTurn()
            || InputManager.Instance == null || UnityEngine.InputSystem.Mouse.current == null
            || UnitManager.Instance == null || !UnitManager.Instance.IsBattleRunning || SceneLoadManager.IsTransitioning
            || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        { panel.SetActive(false); return; }
        string text = Describe(system.GetSelectedAction(), LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition()));
        label.text = text; panel.SetActive(text.Length > 0);
    }
}
