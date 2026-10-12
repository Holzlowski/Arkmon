using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaperMonsterCard : MonoBehaviour
{
    public bool enemy;
    private Unit current;
    private TextMeshProUGUI nameLabel, healthLabel, pointsLabel, statusLabel;
    private Image portrait, healthFill;
    private int shownHealth = -1, shownPoints = -1, shownRounds = -1;
    private bool shownDown;
    private RectTransform pointsRoot;
    private Image[] diamonds;

    public void Build(bool isEnemy)
    {
        enemy = isEnemy;
        PaperUIStyle style = PaperUIStyle.Load();
        portrait = PaperUIFactory.Image("Monster portrait", transform, null, Vector2.zero,
            new Vector2(0.34f, 1), new Vector2(12, 22), new Vector2(-4, -20), false);
        portrait.preserveAspect = true;
        nameLabel = PaperUIFactory.Text("Monster name", transform, "", 24,
            new Vector2(0.35f, 0.66f), new Vector2(1, 0.89f), Vector2.zero, new Vector2(-22, 0));
        nameLabel.fontStyle = FontStyles.Bold;
        Image track = PaperUIFactory.Image("Health track", transform, null,
            new Vector2(0.35f, 0.55f), new Vector2(1, 0.65f), Vector2.zero, new Vector2(-24, 0));
        track.color = style.ink;
        healthFill = PaperUIFactory.Image("Health fill", track.transform, null, Vector2.zero, Vector2.one,
            new Vector2(2, 2), new Vector2(-2, -2));
        healthFill.type = Image.Type.Filled; healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.color = enemy ? new Color(0.76f, 0.39f, 0.22f) : new Color(0.46f, 0.65f, 0.33f);
        healthLabel = PaperUIFactory.Text("Health value", transform, "", 18,
            new Vector2(0.35f, 0.35f), new Vector2(1, 0.54f), Vector2.zero, new Vector2(-24, 0));
        pointsLabel = PaperUIFactory.Text("Action points", transform, "", 20,
            new Vector2(0.35f, 0.18f), new Vector2(1, 0.35f), Vector2.zero, new Vector2(-24, 0));
        pointsLabel.gameObject.SetActive(false);
        pointsRoot = PaperUIFactory.Rect("Action point diamonds", transform,
            new Vector2(.35f, .2f), new Vector2(1f, .34f), Vector2.zero, new Vector2(-24, 0));
        statusLabel = PaperUIFactory.Text("Monster status", transform, "", 15,
            new Vector2(0.1f, 0.025f), new Vector2(0.95f, 0.2f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center);
    }

    private void LateUpdate()
    {
        Unit next = enemy ? (UnitManager.Instance != null
            ? UnitManager.Instance.GetEnemyUnitList().Find(u => u != null && !u.IsDead) : null)
            : (UnitActionSystem.Instance != null ? UnitActionSystem.Instance.GetSelectedUnit() : null);
        if (next == null || next.IsDead)
        { if (nameLabel.text != "") { nameLabel.text = ""; healthLabel.text = ""; pointsLabel.text = ""; statusLabel.text = ""; portrait.enabled = false; healthFill.fillAmount = 0; pointsRoot.gameObject.SetActive(false); } current = null; return; }
        if (next != current)
        {
            current = next; shownHealth = shownPoints = shownRounds = -1;
            pointsRoot.gameObject.SetActive(true);
            nameLabel.text = PaperUIStyle.MonsterName(current);
            PaperUIStyle style = PaperUIStyle.Load();
            portrait.sprite = current.GetComponent<RamAction>() != null ? style.sheepPortrait : style.flacaPortrait;
            portrait.enabled = portrait.sprite != null;
        }
        HealthSystem health = current.GetComponent<HealthSystem>();
        if (shownHealth != health.Health)
        { shownHealth = health.Health; healthLabel.text = $"{health.Health} / {health.MaxHealth}"; healthFill.fillAmount = health.GetHealthNormalized(); }
        if (shownPoints != current.GetActionPoints())
        {
            shownPoints = current.GetActionPoints();
            int max = current.Definition != null ? current.Definition.maxActionPoints : 3;
            if (diamonds == null || diamonds.Length != max)
            {
                PaperUIFactory.ClearChildren(pointsRoot);
                diamonds = new Image[max];
                for (int i = 0; i < max; i++)
                {
                    Image border = PaperUIFactory.Image("AP " + (i + 1), pointsRoot, null,
                        new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(i * 30 + 4, -9), new Vector2(i * 30 + 22, 9));
                    border.color = PaperUIStyle.Load().ink;
                    border.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    diamonds[i] = PaperUIFactory.Image("AP fill", border.transform, null,
                        Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
                }
            }
            for (int i = 0; i < max; i++) diamonds[i].color = i < shownPoints
                ? PaperUIStyle.Load().amber : PaperUIStyle.Load().cream;
        }
        BurningStatus burning = current.GetComponent<BurningStatus>();
        int rounds = burning != null && burning.IsBurning ? burning.RemainingRounds : 0;
        if (rounds != shownRounds || shownDown != current.IsKnockedDown)
        { shownRounds = rounds; shownDown = current.IsKnockedDown; statusLabel.text = PaperUIStyle.Status(current); }
    }
}
