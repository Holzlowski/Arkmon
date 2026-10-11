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
    [SerializeField] private GameObject opponentTrainerPrefab;
    [SerializeField] private MonsterSummonEffect summonEffect;
    [SerializeField, Min(0.1f)] private float defeatHoldDuration = 0.65f;
    public bool IsReady { get; private set; }
    public bool IsSummoning { get; private set; }
    public Transform Trainer { get; private set; }
    public Transform OpponentTrainer { get; private set; }
    public bool IsOpponentSummoning { get; private set; }
    public bool IsRecalling => recallingCount > 0;
    private int recallingCount;
    private readonly HashSet<Unit> recallingUnits = new HashSet<Unit>();
    private List<GridPosition> enemyCells;
    private CameraController cameraController;
    private readonly Dictionary<GridPosition, bool> trainerWalkability = new Dictionary<GridPosition, bool>();
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
        CreateTrainer();
        RefreshAvailable();
        if (available.Count < configuration.playerTeam.Length)
        {
            Fail("Not enough free deployment cells for the player team.");
            yield break;
        }
        enemyCells = GetCells(true);
        // Spread the team along its edge, rather than filling depth first.
        bool verticalEdge = configuration.playerSide == BattleConfiguration.DeploymentSide.Left
            || configuration.playerSide == BattleConfiguration.DeploymentSide.Right;
        enemyCells.Sort((a, b) => verticalEdge
            ? (a.z != b.z ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x))
            : (a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z)));
        if (enemyCells.Count < configuration.enemyTeam.Length)
        {
            Fail("Not enough free deployment cells for the enemy team.");
            yield break;
        }
        CreatePrompt();
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
        foreach (var cell in trainerWalkability)
            Pathfinding.Instance.SetIsWalkableGridPosition(cell.Key, cell.Value);
        if (Trainer != null) Destroy(Trainer.gameObject);
        if (OpponentTrainer != null) Destroy(OpponentTrainer.gameObject);
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
        LevelGrid grid = LevelGrid.Instance;
        Trainer = CreateActor(trainerPrefab, "Battle Trainer",
            configuration.GetTrainerPosition(grid.GetWidth(), grid.GetHeight()));
        OpponentTrainer = CreateActor(opponentTrainerPrefab != null ? opponentTrainerPrefab : trainerPrefab,
            "Opponent Trainer", configuration.GetOpponentTrainerPosition(grid.GetWidth(), grid.GetHeight()));
        cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.transform.position = GetCameraFocus(Trainer);
    }

    private Transform CreateActor(GameObject prefab, string actorName, GridPosition cell)
    {
        LevelGrid grid = LevelGrid.Instance;
        Vector3 center = grid.GetWorldPosition(new GridPosition(grid.GetWidth() / 2, grid.GetHeight() / 2));
        Vector3 position = grid.GetWorldPosition(cell);
        Vector3 facing = center - position;
        GameObject trainer = Instantiate(prefab, position, facing.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(facing) : Quaternion.identity);
        trainer.name = actorName;
        foreach (MonoBehaviour behaviour in trainer.GetComponentsInChildren<MonoBehaviour>())
            if (!(behaviour is PaperCharacterVisual)) behaviour.enabled = false;
        foreach (Collider collider in trainer.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Rigidbody body in trainer.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        trainer.tag = "Untagged";
        if (!trainerWalkability.ContainsKey(cell))
            trainerWalkability.Add(cell, Pathfinding.Instance.IsWalkableGridPosition(cell));
        Pathfinding.Instance.SetIsWalkableGridPosition(cell, false);
        PaperCharacterVisual paper = trainer.GetComponent<PaperCharacterVisual>();
        if (paper != null) paper.FaceWorldDirection(center - position);
        return trainer.transform;
    }

    private Vector3 GetCameraFocus(Transform actor)
    {
        LevelGrid grid = LevelGrid.Instance;
        Vector3 center = grid.GetWorldPosition(new GridPosition(grid.GetWidth() / 2, grid.GetHeight() / 2));
        float size = Vector3.Distance(grid.GetWorldPosition(new GridPosition(0, 0)),
            grid.GetWorldPosition(new GridPosition(1, 0)));
        return actor.position + (center - actor.position).normalized * size;
    }

    private IEnumerator PanCamera(Vector3 destination)
    {
        if (cameraController == null) yield break;
        Vector3 start = cameraController.transform.position;
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            cameraController.transform.position = Vector3.Lerp(start, destination,
                Mathf.SmoothStep(0f, 1f, elapsed / 0.4f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraController.transform.position = destination;
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
        if (visual != null) visual.FaceWorldDirection(destination - Trainer.position);
        Vector3 origin = visual != null ? visual.GetSummonOrigin() : Trainer.position + Vector3.up * 1.2f;
        yield return summonEffect.Play(origin, destination,
            configuration.playerTeam[nextMonster].GetSummonColor());
        Instantiate(configuration.playerTeam[nextMonster++], destination, Quaternion.identity);
        // Let Unit.Start register the new unit before beginning combat.
        yield return null;
        if (nextMonster == configuration.playerTeam.Length)
        {
            GridSystemVisual.Instance.HideAllGridPositions();
            yield return SummonOpponentTeam();
            IsReady = true;
            IsSummoning = false;
            placementLabel.gameObject.SetActive(false);
        }
        else { IsSummoning = false; RefreshAvailable(); ShowCells(); }
    }

    private IEnumerator SummonOpponentTeam()
    {
        IsOpponentSummoning = true;
        Vector3 previousCamera = cameraController != null ? cameraController.transform.position : Vector3.zero;
        if (cameraController != null) cameraController.IsPresentationLocked = true;
        try
        {
            placementLabel.text = "Der Gegner beschwört sein Team …";
            yield return PanCamera(GetCameraFocus(OpponentTrainer));
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < configuration.enemyTeam.Length; i++)
            {
                placementLabel.text = $"Gegner beschwört Monster {i + 1}/{configuration.enemyTeam.Length} …";
                int index = (i + 1) * enemyCells.Count / (configuration.enemyTeam.Length + 1);
                Vector3 destination = LevelGrid.Instance.GetWorldPosition(enemyCells[index]);
                PaperCharacterVisual visual = OpponentTrainer.GetComponent<PaperCharacterVisual>();
                if (visual != null) visual.FaceWorldDirection(destination - OpponentTrainer.position);
                Vector3 origin = visual != null ? visual.GetSummonOrigin() : OpponentTrainer.position + Vector3.up;
                yield return summonEffect.Play(origin, destination, configuration.enemyTeam[i].GetSummonColor());
                Instantiate(configuration.enemyTeam[i], destination, Quaternion.identity);
                yield return null; // Register the opponent before spawning the next one.
            }
            yield return PanCamera(previousCamera);
        }
        finally
        {
            IsOpponentSummoning = false;
            if (cameraController != null) cameraController.IsPresentationLocked = false;
        }
    }

    public bool TryRecall(Unit unit)
    {
        if (unit == null || !unit.IsDead || recallingUnits.Contains(unit)) return false;
        PaperCharacterVisual visual = unit.GetComponent<PaperCharacterVisual>();
        Transform actor = unit.IsEnemy() ? OpponentTrainer : Trainer;
        if (!isActiveAndEnabled || summonEffect == null || visual == null || actor == null) return false;
        visual.BeginDefeat();
        recallingUnits.Add(unit);
        recallingCount++;
        StartCoroutine(Recall(unit, visual, actor));
        return true;
    }

    private IEnumerator Recall(Unit unit, PaperCharacterVisual visual, Transform actor)
    {
        Color tint = unit.GetSummonColor();
        try
        {
            yield return new WaitForSeconds(defeatHoldDuration);
            if (unit == null || visual == null || actor == null) yield break;
            PaperCharacterVisual trainerVisual = actor.GetComponent<PaperCharacterVisual>();
            Vector3 vessel = trainerVisual != null ? trainerVisual.GetSummonOrigin() : actor.position + Vector3.up;
            yield return summonEffect.PlayRecall(visual.GetRecallOrigin(), vessel, tint,
                progress => { if (visual != null) visual.SetRecallProgress(progress); },
                () => { if (unit != null) unit.gameObject.SetActive(false); });
        }
        finally
        {
            if (unit != null) Destroy(unit.gameObject);
            recallingUnits.Remove(unit);
            recallingCount = recallingUnits.Count;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (Unit unit in new List<Unit>(recallingUnits))
            if (unit != null) Destroy(unit.gameObject);
        recallingUnits.Clear();
        recallingCount = 0;
        if (summonEffect != null) summonEffect.CancelAll();
        if (cameraController != null) cameraController.IsPresentationLocked = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
