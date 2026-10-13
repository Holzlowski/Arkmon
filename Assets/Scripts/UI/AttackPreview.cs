using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackPreview : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI label;
    private AttackTargetMarkers markers;
    public bool IsShowing => panel != null && panel.activeInHierarchy;
    public void Build(Transform parent)
    {
        var image = PaperUIFactory.Image("Attack outcome preview", parent, PaperUIStyle.Load().panel,
            new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-405, 234), new Vector2(405, 420));
        image.raycastTarget = false; panel = image.gameObject;
        label = PaperUIFactory.Text("Attack outcome", image.transform, "", 19, Vector2.zero, Vector2.one,
            new Vector2(24, 12), new Vector2(-24, -12), TextAlignmentOptions.Center);
        label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 19;
        panel.AddComponent<BattlePhaseVisibility>(); panel.SetActive(false);
        markers = gameObject.AddComponent<AttackTargetMarkers>();
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
            : obstacle != null ? $"{obstacle.DisplayName}: −{Mathf.Min(raw, obstacle.Health)} LP ({obstacle.Health} → {Mathf.Max(0, obstacle.Health-raw)})" : "";
        if (target != null && action is ShootAction)
        {
            CoverSystem.DamagePlan plan = CoverSystem.Preview(attacker.GetGridPosition(), cell, raw, target);
            damage = plan.unitDamage;
            details = $"{PaperUIStyle.MonsterName(target)}: −{Mathf.Min(plan.unitDamage, target.GetComponent<HealthSystem>().Health)} LP";
            if (plan.cover != null) details += $" · {plan.cover.DisplayName}: −{plan.coverDamage} LP";
            if (plan.cover != null) details += $" (Deckung fängt {plan.coverDamage} Schaden ab)";
        }
        bool fire = action is FlameSwordAction || action is EmberAction;
        if (fire)
        {
            BurningStatus status = target != null ? target.GetComponent<BurningStatus>() : obstacle?.GetComponent<BurningStatus>();
            FireRules rules = TurnSystem.Instance.GetComponent<BurningSystem>().Rules;
            bool survives = target != null ? target.GetComponent<HealthSystem>().Health > damage : obstacle != null && obstacle.Health > raw;
            details += !survives ? " · Besiegt/zerstört" : BurnDescription(status, target == null, rules);
        }
        if (action is RamAction charge && charge.PreviewPush(cell, out GridPosition pushed, out bool down))
        {
            bool survives = target != null && target.GetComponent<HealthSystem>().Health > damage;
            details += !survives ? " · Gegner besiegt; Angreifer übernimmt das Zielfeld"
                : down ? " · Hindernis/Rand: Gegner fällt um; Aufstehen kostet 1 AP. Angreifer bleibt davor"
                : $" · Stoß nach ({pushed.x}, {pushed.z}); Angreifer übernimmt das Zielfeld";
            details += "\nBlau: Endposition des Angreifers · Violett: geschobenes Ziel · Grau: blockiert";
        }
        if (target != null && target.IsGuarding) details += " · Abwehr aktiv";
        return cost + "\n" + details + (attacker.CanSpendActionPointsToTakeAction(action) ? "" : " · Zu wenig AP");
    }
    private static string BurnDescription(BurningStatus status, bool guaranteed, FireRules rules)
    {
        if (status == null || !status.CanBurn) return " · Gegen Verbrennen immun";
        if (status.IsBurning)
            return guaranteed ? $" · Brennt bereits; Brand wird auf {rules.durationRounds} Runden erneuert"
                : $" · Brennt bereits ({status.RemainingRounds} Runden übrig); {Mathf.RoundToInt(rules.monsterHitChance*100)} % auf Erneuerung";
        string chance = guaranteed ? "Entzündet (100 %)" : $"Verbrennen {Mathf.RoundToInt(rules.monsterHitChance * 100)} %";
        return $" · {chance}: {rules.damagePerRound} LP/Runde für {rules.durationRounds} Runden";
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
                details.Add($"{PaperUIStyle.MonsterName(target)}: −{Mathf.Min(damage, target.GetComponent<HealthSystem>().Health)} LP"
                    + (!survives ? " · Besiegt" : BurnDescription(target.GetComponent<BurningStatus>(), false, rules)));
                friendly |= target.IsEnemy() == action.GetUnit().IsEnemy();
            }
            }
            foreach (BurningObstacle obstacle in CoverSystem.ObstaclesAt(cell))
                if (obstacles.Add(obstacle)) details.Add($"{obstacle.DisplayName}: −{Mathf.Min(action.DamageAmount, obstacle.Health)} LP ({obstacle.Health} → {Mathf.Max(0,obstacle.Health-action.DamageAmount)})"
                    + (obstacle.Health <= action.DamageAmount ? " · Wird zerstört" : BurnDescription(obstacle.GetComponent<BurningStatus>(), true, rules)));
        }
        string text = $"Nächster Angriff: Glutfunke · {action.GetActionPointCost()} AP · {action.DamageAmount} Feuerschaden";
        if (friendly) text += " · Achtung: Verbündete werden getroffen";
        if (cells.Count < 3) text += $" · {3-cells.Count} Feld(er) abgeschirmt";
        text += "\n" + (details.Count > 0 ? string.Join("\n", details) : "Keine Ziele in der Reihe");
        // Only show exceptional warnings; permanent colour explanations bury the outcome.
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
        { panel.SetActive(false); markers.Clear(); return; }
        GridPosition cell = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
        string text = Describe(system.GetSelectedAction(), cell);
        if (text.Length > 0) markers.Show(system.GetSelectedAction(), cell); else markers.Clear();
        label.text = text; panel.SetActive(text.Length > 0);
    }
}
