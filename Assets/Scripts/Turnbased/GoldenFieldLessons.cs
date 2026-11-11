using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal sealed class GoldenFieldLessons : MonoBehaviour
{
    private GameObject panel;
    private TMP_Text text;
    private bool advance;

    internal IEnumerator Play(BattleConfiguration battle, Transform mentor, MonsterSummonEffect effect,
        BattlePresentationCamera camera)
    {
        BuildPanel();
        var grid = LevelGrid.Instance;
        Vector3 vessel = mentor.GetComponent<PaperCharacterVisual>().GetSummonOrigin();
        Vector3 center = grid.GetWorldPosition(new GridPosition(5, 5));
        GridPosition[] stations = { new(3, 6), new(4, 3), new(4, 5), new(5, 3) };
        GridPosition[] targets = { new(2, 7), new(1, 3), new(4, 6), new(2, 4) };
        Unit dummy = null;

        for (int i = 0; i < stations.Length; i++)
        {
            if (dummy != null)
            {
                dummy.MoveToWorldPosition(grid.GetWorldPosition(targets[i]));
                dummy.GetComponent<HealthSystem>().Heal(int.MaxValue / 2);
                dummy.GetComponent<BurningStatus>().Extinguish();
            }            yield return camera.FocusTrainer(mentor);
            yield return effect.Play(vessel, grid.GetWorldPosition(stations[i]), battle.playerTeam[i].GetSummonColor());
            var starter = Spawn(battle.playerTeam[i], stations[i], targets[i]);
            if (dummy == null)
            {
                yield return effect.Play(vessel, grid.GetWorldPosition(targets[i]), battle.enemyTeam[0].GetSummonColor());
                dummy = Spawn(battle.enemyTeam[0], targets[i], stations[i]);
            }
            yield return camera.ReturnToOverview(center);
            yield return null;
            switch (i)
            {
                case 0: yield return TeachKafla(starter); break;
                case 1: yield return TeachPanfrie(starter, dummy); break;
                case 2: yield return TeachLiora(starter, dummy); break;
                case 3: yield return TeachRauk(starter, dummy); break;
            }
            if (starter != null && !starter.IsDead)
            {
                var visual = starter.GetComponent<PaperCharacterVisual>();
                Vector3 origin = visual != null ? visual.GetRecallOrigin() : starter.transform.position + Vector3.up * .5f;
                yield return effect.PlayRecall(origin, vessel, starter.GetSummonColor(),
                    visual != null ? visual.SetRecallProgress : null, starter.RetireDemonstration);
            }
            yield return null;
        }

        dummy.RetireDemonstration();
        yield return Explain("Jetzt seid Ihr an der Reihe. Haltet das Siegel drei Runden lang oder besiegt alle drei Golems.");
        Destroy(panel);
        yield return null;
    }

    private IEnumerator TeachKafla(Unit kafla)
    {
        yield return Explain("Kafla ist im Nahkampf stark. Wählt »Attacken«, dann Flammenschwert und ein Nachbarfeld.");
        yield return Demonstrate(kafla.GetAction<FlameSwordAction>(), new GridPosition(2, 7));
        yield return Explain("Glutfunke trifft mehrere Felder. Achtet auf Verbündete. Büsche können Feuer fangen und es an Nachbarn weitergeben.");
        var bush = CoverSystem.ObstacleAt(new GridPosition(2, 6));
        yield return Demonstrate(kafla.GetAction<EmberAction>(), new GridPosition(2, 6));
        yield return new WaitForSeconds(.55f);
        RestoreCover(bush, BurningObstacle.MaterialKind.Vegetation);
    }

    private IEnumerator TeachPanfrie(Unit panfrie, Unit golem)
    {
        yield return Explain("Panfrie schützt sich oder einen Nachbarn mit Schildwache. Auch der Fels zwischen ihm und dem Golem fängt einen Teil des Splitterschusses ab.");
        yield return Demonstrate(panfrie.GetAction<ShieldWatchAction>(), new GridPosition(4, 3));
        var rock = CoverSystem.ObstacleAt(new GridPosition(3, 3));
        yield return Demonstrate(golem.GetAction<StoneShardAction>(), new GridPosition(4, 3));
        RestoreCover(rock, BurningObstacle.MaterialKind.Stone);
        golem.MoveToWorldPosition(LevelGrid.Instance.GetWorldPosition(new GridPosition(4, 4)));
        yield return Explain("Schildhieb trifft aus der Nähe. Die Eiszapfensalve reicht weiter und bremst den Gegner. Danach müssen die Zapfen nachwachsen.");
        yield return Demonstrate(panfrie.GetAction<ShieldBashAction>(), new GridPosition(4, 4));
        yield return Demonstrate(panfrie.GetAction<IceVolleyAction>(), new GridPosition(4, 4));
    }

    private IEnumerator TeachLiora(Unit liora, Unit golem)
    {
        yield return Explain("Lioras Dornenranke hält einen Gegner fest. Mit Rosenstich kann sie ihm aus kurzer Entfernung zusetzen.");
        yield return Demonstrate(liora.GetAction<ThornVineAction>(), new GridPosition(4, 6));
        yield return Demonstrate(liora.GetAction<RoseThrustAction>(), new GridPosition(4, 6));
        yield return Demonstrate(golem.GetAction<StoneFistAction>(), new GridPosition(4, 5));
        yield return Explain("Rosenlicht heilt Liora oder einen Verbündeten. So kann sie jemanden im Kampf halten, der schon getroffen wurde.");
        yield return Demonstrate(liora.GetAction<RoseLightAction>(), new GridPosition(4, 5));
    }

    private IEnumerator TeachRauk(Unit rauk, Unit golem)
    {
        yield return Explain("Rauk kann niedrige Hindernisse übergleiten. Mit Schattensprung erreicht er ein freies Schattenfeld hinter Deckung.");
        yield return Demonstrate(rauk.GetAction<ShadowStepAction>(), new GridPosition(2, 3));
        golem.transform.forward = Vector3.forward;
        yield return Explain("Federwurf trifft aus der Distanz. Mit Dolchhieb richtet Rauk von hinten oder mit einem Verbündeten auf der Gegenseite mehr Schaden an.");
        yield return Demonstrate(rauk.GetAction<FeatherThrowAction>(), new GridPosition(2, 4));
        yield return Demonstrate(rauk.GetAction<DaggerStrikeAction>(), new GridPosition(2, 4));
        yield return Explain("Schaut, wie Rauk über den niedrigen Fels gleitet. Das übersprungene Feld kostet ihn doppelt so viel Laufweite.");
        yield return Demonstrate(rauk.GetAction<MoveAction>(), new GridPosition(4, 3));
    }

    private static IEnumerator Demonstrate(BaseAction action, GridPosition cell)
    {
        if (action == null || !action.IsValidActionGridPosition(cell))
        {
            Debug.LogError("Invalid field lesson action: " + (action != null ? action.GetActionName() : "missing") + " at " + cell);
            yield break;
        }
        bool done = false;
        action.TakeAction(cell, () => done = true);
        yield return new WaitUntil(() => done || action == null);
        yield return new WaitForSeconds(.25f);
    }

    private static Unit Spawn(Unit prefab, GridPosition cell, GridPosition facing)
    {
        var grid = LevelGrid.Instance;
        Vector3 position = grid.GetWorldPosition(cell);
        return Instantiate(prefab, position, Quaternion.LookRotation(grid.GetWorldPosition(facing) - position));
    }

    private static void RestoreCover(BurningObstacle cover, BurningObstacle.MaterialKind kind)
    {
        if (cover == null || cover.IsDestroyed) return;
        cover.GetComponent<BurningStatus>().Extinguish();
        cover.Configure(kind, cover.MaxHealth, cover.ProvidesCover, cover.BlocksSight);
        cover.GetComponent<ObstacleConditionVisual>()?.Refresh();
    }

    private void BuildPanel()
    {
        var canvas = new GameObject("Field lesson dialogue", typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;
        PaperUIFactory.Scale(canvas);
        var style = PaperUIStyle.Load();
        var image = PaperUIFactory.Image("Field lesson paper", canvas.transform, style.panel,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-505, -182), new Vector2(505, -20));
        panel = image.gameObject;
        PaperUIFactory.Text("Speaker", image.transform, ArkanmeisterinConversation.Speaker, 24,
            Vector2.zero, Vector2.one, new Vector2(25, 107), new Vector2(-170, -8));
        text = PaperUIFactory.Text("Lesson", image.transform, "", 23,
            Vector2.zero, Vector2.one, new Vector2(25, 13), new Vector2(-185, -55));
        var button = PaperUIFactory.Button("Continue field lesson", image.transform, style.card,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-172, 23), new Vector2(-19, 75));
        PaperUIFactory.Text("Continue", button.transform, "Weiter · E", 21, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        button.onClick.AddListener(Advance);
        panel.SetActive(false);
    }

    private void Advance() => advance = true;

    private IEnumerator Explain(string line)
    {
        advance = false;
        text.text = line;
        panel.SetActive(true);
        yield return null;
        while (!advance && !ExplorationInput.Pressed(Key.E)) yield return null;
        panel.SetActive(false);
    }
}