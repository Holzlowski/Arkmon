using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Optional, non-blocking advice for the first accepted John battle only.
public class FirstTrialGuide : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI text;
    private bool moved, attacked, endedTurn, dismissed;
    private TurnSystem turns;
    private readonly Queue<string> observations = new Queue<string>();
    private readonly HashSet<string> explained = new HashSet<string>();
    private string observation;
    private float observationUntil;
    public string CurrentHint
    {
        get
        {
            if (!string.IsNullOrEmpty(observation) && Time.unscaledTime < observationUntil) return observation;
            Unit selected = UnitActionSystem.Instance != null ? UnitActionSystem.Instance.GetSelectedUnit() : null;
            if (UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning && turns != null && turns.IsPlayerTurn()
                && selected != null && selected.GetActionPoints() == 0)
                return "John · Kafla hat seine AP verbraucht. Klicke auf »Zug beenden«; im nächsten eigenen Zug kannst du wieder handeln.";
            return Hint(UnitManager.Instance != null ? UnitManager.Instance.State : UnitManager.BattleState.Initializing,
                moved, attacked, endedTurn, turns == null || turns.IsPlayerTurn());
        }
    }

    public static string Hint(UnitManager.BattleState state, bool moved, bool attacked, bool endedTurn, bool playerTurn)
    {
        if (state == UnitManager.BattleState.Deploying || state == UnitManager.BattleState.Initializing)
            return "John · Ruf Kafla mit deiner Lampe auf eines der leuchtenden Felder. Danach beschwöre ich Distelwolle.";
        if (!playerTurn) return "John · Jetzt handelt Distelwolle. Beobachte seinen Rammstoß: Ein Hindernis hinter Kafla kann ihn zu Fall bringen.";
        if (endedTurn) return "John · Jeder neue Zug bringt frische AP. Feuer kann überspringen; Kafla ist gegen Verbrennen immun. Nutze Abwehr, wenn du warten möchtest.";
        if (attacked) return "John · Gut! Mit deinen übrigen AP kannst du handeln oder Abwehr wählen. Wenn du fertig bist, klicke auf »Zug beenden«.";
        if (moved) return "John · Öffne »Attacken«, wähle eine Fähigkeit und zeige auf ein Ziel. Die Vorschau zeigt Schaden, AP und Verbrennen. Glutfunke trifft auch Verbündete!";
        return "John · Wähle links »Aktionen«, dann »Bewegen« und ein freies Feld. Die Steine können Distelwolles direkten Rammweg versperren. Du darfst auch direkt angreifen, wenn er nahe steht.";
    }

    private void Start()
    {
        turns = TurnSystem.Instance;
        var canvas = new GameObject("John trial advice", typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 25;
        PaperUIFactory.Scale(canvas);
        var image = PaperUIFactory.Image("John advice paper", canvas.transform, PaperUIStyle.Load().panel,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -170), new Vector2(530, -20));
        panel = image.gameObject; image.raycastTarget = false;
        text = PaperUIFactory.Text("John advice", image.transform, "", 20, Vector2.zero, Vector2.one,
            new Vector2(22, 12), new Vector2(-45, -12));
        var close = PaperUIFactory.Button("Hide trial advice", image.transform, PaperUIStyle.Load().card,
            Vector2.one, Vector2.one, new Vector2(-40, -36), new Vector2(-8, -8));
        PaperUIFactory.Text("Close", close.transform, "×", 22, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        close.onClick.AddListener(() => dismissed = true);
        BaseAction.OnAnyActionCompleted += Completed;
        BurningStatus.OnAnyIgnited += Ignited;
        Unit.OnAnyKnockdownChanged += KnockedDown;
        if (turns != null) turns.OnTurnChanged += ChangedTurn;
    }
    private void Completed(object sender, EventArgs e)
    {
        if (!(sender is BaseAction action) || action.GetUnit() == null || action.GetUnit().IsEnemy()) return;
        moved |= action is MoveAction;
        attacked |= action is MeleeAttackAction || action is EmberAction || action is ShootAction || action is RamAction;
        if (action is MoveAction && UnitManager.Instance != null)
            foreach (Unit enemy in UnitManager.Instance.GetEnemyUnitList())
            {
                if (enemy == null || enemy.IsDead) continue;
                var cover = CoverSystem.Preview(enemy.GetGridPosition(), action.GetUnit().GetGridPosition(), 20, action.GetUnit());
                if (cover.cover != null || cover.blocked)
                { Observe("cover", "John · Gut positioniert! Du nutzt ein Hindernis zwischen euch. Für Rammstoß braucht Distelwolle eine freie gerade Bahn. Gegen Fernangriffe kann Deckung Schaden abfangen – dabei wird sie beschädigt."); break; }
            }
    }
    private void Observe(string key, string hint) { if (!dismissed && explained.Add(key)) observations.Enqueue(hint); }
    private void Ignited(object sender, EventArgs e)
    {
        if (sender is BurningStatus fire && fire.Obstacle != null)
            Observe("fire", "John · Holz und Pflanzen fangen durch Feuer sicher an zu brennen. Das Feuer schadet über mehrere Runden und kann überspringen. Kafla ist immun; Steine brennen nicht.");
    }
    private void KnockedDown(object sender, EventArgs e)
    {
        if (sender is Unit unit && !unit.IsEnemy() && unit.IsKnockedDown)
            Observe("knockdown", "John · Das Hindernis hat den Stoß gestoppt, aber Kafla liegt am Boden. Wähle »Aktionen« und »Aufstehen«. Das kostet 1 AP.");
    }
    private void ChangedTurn(object sender, EventArgs e) { if (!turns.IsPlayerTurn()) endedTurn = true; }
    private void LateUpdate()
    {
        if (panel == null) return;
        if (Time.unscaledTime >= observationUntil && observations.Count > 0 && !BaseAction.IsAnyActionActive)
        { observation = observations.Dequeue(); observationUntil = Time.unscaledTime + 8; }
        var state = UnitManager.Instance.State;
        bool finished = state == UnitManager.BattleState.Won || state == UnitManager.BattleState.Lost || state == UnitManager.BattleState.Escaped;
        panel.SetActive(!dismissed && !finished && !SceneLoadManager.IsTransitioning);
        text.text = CurrentHint;
    }
    private void OnDestroy()
    {
        BaseAction.OnAnyActionCompleted -= Completed;
        BurningStatus.OnAnyIgnited -= Ignited;
        Unit.OnAnyKnockdownChanged -= KnockedDown;
        if (turns != null) turns.OnTurnChanged -= ChangedTurn;
    }
}
