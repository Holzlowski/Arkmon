using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionPointText;
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private HealthSystem healthSystem;

    private TextMeshProUGUI statusText;
    private Image statusIcon;
    private GameObject statusPanel;
    private int shownRounds = -1;
    private bool shownDown;

    private void Start()
    {
        BuildPaperView();
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        healthSystem.OnDamage += HealthSystem_OnDamaged;
        UpdateActionPoints();
        UpdateHealthBar();
    }

    private void UpdateActionPoints()
    {
        actionPointText.text = unit.GetActionPoints() + " AP";
    }

    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void UpdateHealthBar()
    {
        healthBarImage.fillAmount = healthSystem.GetHealthNormalized();
    }

    private void HealthSystem_OnDamaged(object sender, EventArgs e)
    {
        UpdateHealthBar();
    }

    private void BuildPaperView()
    {
        PaperUIStyle style = PaperUIStyle.Load();
        if (style == null) return;
        LookAtCamera legacyBillboard = GetComponent<LookAtCamera>();
        if (legacyBillboard != null) legacyBillboard.enabled = false;
        PaperUIFactory.HideChildren(transform);
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(260, 90);
        rect.localScale = Vector3.one * 0.006f;
        Image track = PaperUIFactory.Image("Health ink track", transform, null,
            new Vector2(0.06f, 0.07f), new Vector2(0.74f, 0.22f), Vector2.zero, Vector2.zero);
        track.color = style.ink;
        healthBarImage = PaperUIFactory.Image("Health paper fill", track.transform, null,
            Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        healthBarImage.type = Image.Type.Filled; healthBarImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarImage.color = unit.IsEnemy() ? new Color(.76f, .39f, .22f) : new Color(.46f, .65f, .33f);
        actionPointText = PaperUIFactory.Text("World action points", transform, "", 21,
            new Vector2(.78f, .0f), new Vector2(1f, .29f), Vector2.zero, Vector2.zero);
        actionPointText.color = style.ink;
        Image panel = PaperUIFactory.Image("World status paper", transform, style.panel,
            new Vector2(0, .28f), new Vector2(1, .9f), Vector2.zero, Vector2.zero);
        statusPanel = panel.gameObject;
        statusIcon = PaperUIFactory.Image("Status icon", panel.transform, style.fireIcon,
            new Vector2(.04f, .14f), new Vector2(.18f, .86f), Vector2.zero, Vector2.zero, false);
        statusIcon.preserveAspect = true;
        statusText = PaperUIFactory.Text("World status", panel.transform, "", 20,
            new Vector2(.2f, .08f), new Vector2(.95f, .92f), Vector2.zero, Vector2.zero);
        statusPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (statusText == null || unit == null || unit.IsDead) return;
        Camera camera = Camera.main;
        PaperCharacterVisual paper = unit.GetComponent<PaperCharacterVisual>();
        if (camera != null && paper != null)
        {
            transform.rotation = camera.transform.rotation;
            transform.position = paper.GetRecallOrigin() + camera.transform.up * .65f;
        }
        BurningStatus burning = unit.GetComponent<BurningStatus>();
        int rounds = burning != null && burning.IsBurning ? burning.RemainingRounds : 0;
        if (rounds == shownRounds && unit.IsKnockedDown == shownDown) return;
        shownRounds = rounds; shownDown = unit.IsKnockedDown;
        statusPanel.SetActive(rounds > 0 || shownDown);
        statusText.text = PaperUIStyle.Status(unit);
        statusIcon.sprite = rounds > 0 ? PaperUIStyle.Load().fireIcon : PaperUIStyle.Load().downIcon;
    }

    private void OnDestroy()
    {
        Unit.OnAnyActionPointsChanged -= Unit_OnAnyActionPointsChanged;
        if (healthSystem != null) healthSystem.OnDamage -= HealthSystem_OnDamaged;
    }
}
