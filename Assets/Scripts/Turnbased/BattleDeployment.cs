using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Disable scene templates before their Start methods can register them as units.
[DefaultExecutionOrder(-1)]
public class BattleDeployment : MonoBehaviour
{
    public static BattleDeployment Instance { get; private set; }
    [SerializeField] private BattleConfiguration defaultConfiguration;
    [SerializeField] private GameObject trainerPrefab;
    [SerializeField] private MonsterSummonEffect summonEffect;
    public bool IsReady { get; private set; }
    public bool IsSummoning { get; private set; }
    public Transform Trainer { get; private set; }
    private BattleConfiguration configuration;
    private readonly List<Unit> templates = new List<Unit>();
    private readonly List<GridPosition> available = new List<GridPosition>();
    private int nextMonster;
    private bool initialized;
    private TextMeshProUGUI placementLabel;

    private void Awake()
    {
        Instance = this;
        configuration = SceneLoadManager.instance != null
            && SceneLoadManager.instance.PendingBattle != null
            ? SceneLoadManager.instance.PendingBattle : defaultConfiguration;
        if (configuration == null) { IsReady = true; return; }
        foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            templates.Add(unit);
            unit.gameObject.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        if (IsReady) yield break;
        // Pathfinding and grid visuals initialize in Start.
        yield return null;
        if (!ValidateTeams()) yield break;
        RefreshAvailable();
        if (available.Count < configuration.playerTeam.Length)
        {
            Fail("Not enough free deployment cells for the player team.");
            yield break;
        }
        List<GridPosition> enemyCells = GetCells(true);
        if (enemyCells.Count < configuration.enemyTeam.Length)
        {
            Fail("Not enough free deployment cells for the enemy team.");
            yield break;
        }
        CreateTrainer();
        CreatePrompt();
        for (int i = 0; i < configuration.enemyTeam.Length; i++)
        {
            // Spread opponents over their side instead of filling a single corner.
            int index = (i + 1) * enemyCells.Count / (configuration.enemyTeam.Length + 1);
            Instantiate(configuration.enemyTeam[i], LevelGrid.Instance.GetWorldPosition(enemyCells[index]), Quaternion.identity);
        }
        foreach (Unit template in templates) Destroy(template.gameObject);
        initialized = true;
        ShowCells();
    }

    private bool ValidateTeams()
    {
        if (configuration.playerTeam == null || configuration.playerTeam.Length == 0
            || configuration.enemyTeam == null || configuration.enemyTeam.Length == 0)
        { Fail("Battle configuration needs both teams."); return false; }
        if (trainerPrefab == null || summonEffect == null)
        { Fail("Battle deployment needs a trainer prefab and summon effect."); return false; }
        foreach (Unit unit in configuration.playerTeam)
            if (unit == null || unit.IsEnemy()) { Fail("Player team needs friendly unit prefabs."); return false; }
        foreach (Unit unit in configuration.enemyTeam)
            if (unit == null || !unit.IsEnemy()) { Fail("Enemy team needs enemy unit prefabs."); return false; }
        return true;
    }

    private void Fail(string message)
    {
        Debug.LogError(message, this);
        // Keep the existing scene battle usable when a configuration is invalid.
        foreach (Unit template in templates) if (template != null) template.gameObject.SetActive(true);
        IsReady = true;
    }

    private List<GridPosition> GetCells(bool enemy)
    {
        List<GridPosition> cells = new List<GridPosition>();
        LevelGrid grid = LevelGrid.Instance;
        for (int x = 0; x < grid.GetWidth(); x++)
            for (int z = 0; z < grid.GetHeight(); z++)
            {
                GridPosition cell = new GridPosition(x, z);
                if (configuration.Contains(cell, grid.GetWidth(), grid.GetHeight(), enemy)
                    && Pathfinding.Instance.IsWalkableGridPosition(cell)
                    && !grid.HasAnyUnitOnGridPosition(cell)) cells.Add(cell);
            }
        return cells;
    }

    private void RefreshAvailable()
    {
        available.Clear();
        available.AddRange(GetCells(false));
    }

    private void ShowCells()
    {
        GridSystemVisual.Instance.HideAllGridPositions();
        GridSystemVisual.Instance.ShowGridPositionList(available, GridSystemVisual.GridVisualType.Blue);
        placementLabel.text = $"Platziere Monster {nextMonster + 1}/{configuration.playerTeam.Length}: Wähle ein blaues Feld.";
    }

    private void CreatePrompt()
    {
        GameObject canvasObject = new GameObject("Deployment instructions", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        GameObject label = new GameObject("Placement prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(canvasObject.transform, false);
        placementLabel = label.GetComponent<TextMeshProUGUI>();
        placementLabel.fontSize = 22;
        placementLabel.alignment = TextAlignmentOptions.Center;
        placementLabel.raycastTarget = false;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -55);
        rect.sizeDelta = new Vector2(-40, 55);
    }

    private void CreateTrainer()
    {
        GameObject trainer = Instantiate(trainerPrefab);
        trainer.name = "Battle Trainer";
        Trainer = trainer.transform;
        foreach (MonoBehaviour behaviour in trainer.GetComponentsInChildren<MonoBehaviour>())
            if (!(behaviour is PaperCharacterVisual)) behaviour.enabled = false;
        foreach (Collider collider in trainer.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Rigidbody body in trainer.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        trainer.tag = "Untagged";
        LevelGrid grid = LevelGrid.Instance;
        Vector3 center = grid.GetWorldPosition(new GridPosition(grid.GetWidth() / 2, grid.GetHeight() / 2));
        Vector3 position = grid.GetWorldPosition(configuration.GetTrainerPosition(grid.GetWidth(), grid.GetHeight()));
        Trainer.position = position;
        Pathfinding.Instance.SetIsWalkableGridPosition(
            configuration.GetTrainerPosition(grid.GetWidth(), grid.GetHeight()), false);
        Trainer.rotation = Quaternion.LookRotation(center - position);
        // Begin with the trainer and nearby deployment cells in view.
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            Vector3 inward = (center - position).normalized;
            float cellSize = Vector3.Distance(grid.GetWorldPosition(new GridPosition(0, 0)),
                grid.GetWorldPosition(new GridPosition(1, 0)));
            cameraController.transform.position = position + inward * cellSize;
        }
    }

    private void Update()
    {
        if (!initialized || IsReady || IsSummoning) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (InputManager.Instance.IsMouseButtonDownThisFrame())
            TryPlace(LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition()));
    }

    public bool TryPlace(GridPosition position)
    {
        if (!initialized || IsReady || IsSummoning || !available.Contains(position)) return false;
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(position)
            || !Pathfinding.Instance.IsWalkableGridPosition(position)) return false;
        IsSummoning = true; // Lock before the coroutine's first yield.
        placementLabel.text = "Dein Monster erscheint …";
        StartCoroutine(Summon(position));
        return true;
    }

    private IEnumerator Summon(GridPosition position)
    {
        Vector3 destination = LevelGrid.Instance.GetWorldPosition(position);
        Trainer.rotation = Quaternion.LookRotation(new Vector3(destination.x, Trainer.position.y, destination.z) - Trainer.position);
        PaperCharacterVisual visual = Trainer.GetComponent<PaperCharacterVisual>();
        Vector3 origin = visual != null ? visual.GetSummonOrigin() : Trainer.position + Vector3.up * 1.2f;
        yield return summonEffect.Play(origin, destination,
            configuration.playerTeam[nextMonster].GetSummonColor());
        Instantiate(configuration.playerTeam[nextMonster++], destination, Quaternion.identity);
        // Let Unit.Start register the new unit before beginning combat.
        yield return null;
        IsSummoning = false;
        if (nextMonster == configuration.playerTeam.Length)
        {
            IsReady = true;
            placementLabel.gameObject.SetActive(false);
            GridSystemVisual.Instance.HideAllGridPositions();
        }
        else { RefreshAvailable(); ShowCells(); }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
