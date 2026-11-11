using TMPro;
using UnityEngine;

internal sealed class FieldSealTrial : MonoBehaviour
{
    internal const string Resource = "Field Seal Trial";
    internal const int Rounds = 3;
    internal bool IsSealLost => seal == null || seal.IsDead;
    internal bool TimeSurvived => TurnSystem.Instance.GetTurnNumber() > Rounds*2;

    private Unit seal;
    private HealthSystem health;
    private TextMeshProUGUI label;

    internal static GridPosition[] GetCells(GridPosition anchor, bool large)
        => large ? new[] { anchor, anchor + new GridPosition(0, -1),
            anchor + new GridPosition(1, -1), anchor + new GridPosition(1, 0) } : new[] { anchor };

    internal void Build(GridPosition[] cells)
    {
        var marker = new GameObject("Arkanes Übungssiegel");
        marker.SetActive(false);
        marker.transform.SetParent(transform,false);
        marker.transform.position = LevelGrid.Instance.GetWorldPosition(cells[0]);
        health = marker.AddComponent<HealthSystem>();
        health.SetStartingHealth(120);
        seal = marker.AddComponent<Unit>();
        seal.SetTrialCells(cells);

        var baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "Siegelstein";
        baseDisc.transform.SetParent(marker.transform,false);
        baseDisc.transform.localScale = new Vector3(.82f,.11f,.82f);
        baseDisc.transform.localPosition = Vector3.up*.11f;
        Destroy(baseDisc.GetComponent<Collider>());
        var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Arkaner Kristall";
        crystal.transform.SetParent(marker.transform,false);
        crystal.transform.localPosition = Vector3.up*.52f;
        crystal.transform.localScale = new Vector3(.42f,.42f,.42f);
        crystal.transform.localRotation = Quaternion.Euler(30,45,30);
        Destroy(crystal.GetComponent<Collider>());
        var crystalTint = new MaterialPropertyBlock();
        crystalTint.SetColor("_BaseColor",new Color(.50f,.32f,.86f));
        crystal.GetComponent<Renderer>().SetPropertyBlock(crystalTint);
        marker.SetActive(true);

        var canvas = new GameObject("Field trial objective",typeof(RectTransform),typeof(Canvas));
        canvas.transform.SetParent(transform,false);
        var ui = canvas.GetComponent<Canvas>();
        ui.renderMode = RenderMode.ScreenSpaceOverlay;
        ui.sortingOrder = 25;
        PaperUIFactory.Scale(ui);
        var style = PaperUIStyle.Load();
        var panel = PaperUIFactory.Image("Seal objective",canvas.transform,style?.panel,
            new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-245,-133),new Vector2(245,-82),false);
        label = PaperUIFactory.Text("Seal health",panel.transform,"",20,Vector2.zero,Vector2.one,
            new Vector2(12,6),new Vector2(-12,-6),TextAlignmentOptions.Center);
    }

    private void LateUpdate()
    {
        if (label == null || UnitManager.Instance == null) return;
        label.transform.parent.gameObject.SetActive(UnitManager.Instance.IsBattleRunning);
        if (!UnitManager.Instance.IsBattleRunning || health == null) return;
        int round = Mathf.Min(Rounds,(TurnSystem.Instance.GetTurnNumber()+1)/2);
        label.text = $"Haltet das Siegel · {round}/{Rounds} · {health.Health}/{health.MaxHealth} LP";
    }
}
