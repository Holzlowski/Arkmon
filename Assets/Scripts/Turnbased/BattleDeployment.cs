using System.Collections;
using System.Linq;
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
    internal FieldSealTrial SealTrial { get; private set; }
    internal bool HasGlider => configuration?.playerTeam != null && System.Array.Exists(configuration.playerTeam, u => u != null && u.Definition?.canGlide == true);
    internal MonsterDefinition LeadMonster => configuration?.playerTeam is { Length: > 0 } team ? team[0]?.Definition : null;
    public bool IsSummoning { get; private set; }
    public Transform Trainer { get; private set; }
    public Transform OpponentTrainer { get; private set; }
    public bool IsOpponentSummoning { get; private set; }
    public bool IsRecalling => recallingCount > 0;
    private int recallingCount;
    private readonly HashSet<Unit> recallingUnits = new HashSet<Unit>();
    private List<GridPosition> enemyCells;
    private CameraController cameraController;
    private BattlePresentationCamera presentation;
    private BattleConfiguration configuration;
    private readonly List<Unit> templates = new List<Unit>();
    private readonly List<GridPosition> available = new List<GridPosition>();
    private int nextMonster;
    private bool initialized;
    private TextMeshProUGUI placementLabel;
    private Sprite mentorSprite;
    private Material mentorMaterial;
    private GameObject placementPanel;
    private GoldenFieldArenaPreview goldenArena;
    private int goldenGolemCount = 3;

    private void Awake()
    {
        Instance = this;
        configuration = SceneLoadManager.instance != null
            && SceneLoadManager.instance.PendingBattle != null
            ? SceneLoadManager.instance.PendingBattle : defaultConfiguration;
#if UNITY_EDITOR
        string comparison = UnityEditor.SessionState.GetString("Arkmon.StarterBattle", "");
        if (!string.IsNullOrEmpty(comparison))
        {
            UnityEditor.SessionState.EraseString("Arkmon.StarterBattle");
            configuration = UnityEditor.AssetDatabase.LoadAssetAtPath<BattleConfiguration>(comparison) ?? configuration;
        }
#endif
        if (configuration == null) { IsReady = true; return; }
        foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            templates.Add(unit);
            unit.gameObject.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool(GoldenFieldArenaPreview.SessionKey,false))
        {
            UnityEditor.SessionState.EraseBool(GoldenFieldArenaPreview.SessionKey);
            goldenGolemCount = Mathf.Clamp(UnityEditor.SessionState.GetInt("Arkmon.GoldenGolemCount", 3), 3, 4);
            UnityEditor.SessionState.EraseInt("Arkmon.GoldenGolemCount");
        }
#endif
        if (configuration?.GoldenFieldArt != null) goldenArena = GoldenFieldArenaPreview.Create(configuration.GoldenFieldArt);
        if (IsReady) yield break;
        // Pathfinding and grid visuals initialize in Start.
        yield return null;
        if (goldenArena != null) goldenArena.RefreshCoverWalkability();
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
        if (goldenArena != null && SceneLoadManager.instance != null && SceneLoadManager.instance.PendingBattle == configuration)
            yield return gameObject.AddComponent<GoldenFieldLessons>().Play(configuration, OpponentTrainer, summonEffect, presentation);
        if (configuration.defendSeal)
        {
            var sealCell = new GridPosition(3,5);
            bool large = false;
            if (goldenArena != null) { sealCell = new GridPosition(5,5); large = true; }
            var sealCells = FieldSealTrial.GetCells(sealCell, large);
            if (sealCells.Any(cell => !LevelGrid.Instance.IsValidGridPosition(cell) || !Pathfinding.Instance.IsWalkableGridPosition(cell)))
            { Fail($"The seal requires a free field at {sealCell}."); yield break; }
            SealTrial = gameObject.AddComponent<FieldSealTrial>();
            SealTrial.Build(sealCells);
            if (goldenArena != null) goldenArena.StageSeal(SealTrial.transform.Find("Arkanes Übungssiegel"), sealCells);
            foreach (var cell in sealCells) available.Remove(cell);
        }
        if (goldenArena != null)
        {
            if (!TryStageGoldenFormation())
            {
                Fail("The golden field formation requires distinct, free cells.");
                yield break;
            }
            foreach (Unit template in templates) Destroy(template.gameObject);
            yield return new WaitForSeconds(1.1f);
            Vector3 focus = LevelGrid.Instance.GetWorldPosition(new GridPosition(5, 5));
            yield return presentation.EstablishOverview(focus);
            IsReady = true;
            yield break;
        }
        CreatePrompt();
        foreach (Unit template in templates) Destroy(template.gameObject);
        initialized = true;
        ShowCells();
    }

    private bool TryStageGoldenFormation()
    {
        GridPosition[] defenders = { new(4, 4), new(6, 3), new(5, 6), new(7, 5) };
        GridPosition[] golems = { new(2, 3), new(5, 1), new(9, 5), new(6, 8) };
        var grid = LevelGrid.Instance;
        var used = new HashSet<GridPosition>(FieldSealTrial.GetCells(new GridPosition(5, 5), true));
        if (configuration.playerTeam.Length != defenders.Length || configuration.enemyTeam.Length < goldenGolemCount)
            return false;

        foreach (var cells in new[] { defenders, golems.Take(goldenGolemCount).ToArray() })
            foreach (GridPosition cell in cells)
                if (!used.Add(cell) || !grid.IsValidGridPosition(cell)
                    || !Pathfinding.Instance.IsWalkableGridPosition(cell) || grid.HasAnyUnitOnGridPosition(cell))
                    return false;

        Vector3 seal = grid.GetWorldPosition(new GridPosition(5, 5));
        for (int i = 0; i < defenders.Length; i++)
        {
            Vector3 position = grid.GetWorldPosition(defenders[i]);
            Instantiate(configuration.playerTeam[i], position, Facing(seal, position));
        }
        for (int i = 0; i < goldenGolemCount; i++)
        {
            Vector3 position = grid.GetWorldPosition(golems[i]);
            Instantiate(configuration.enemyTeam[i], position, Facing(position, seal));
        }
        return true;
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
        string name = PaperUIStyle.MonsterName(configuration.playerTeam[nextMonster]);
        placementLabel.text = $"{name} · {nextMonster + 1}/{configuration.playerTeam.Length} · Wähle ein blaues Feld.";
    }

    private static GameObject ImagePrompt(Transform canvas, Transform label, PaperUIStyle style)
    {
        var panel = PaperUIFactory.Image("Deployment paper tab", canvas, style.panel,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-410, -151), new Vector2(410, -86));
        label.SetParent(panel.transform, false);
        PaperUIFactory.Fill((RectTransform)label);
        ((RectTransform)label).offsetMin = new Vector2(18, 10);
        ((RectTransform)label).offsetMax = new Vector2(-18, -10);
        return panel.gameObject;
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
        rect.anchoredPosition = new Vector2(0, -92);
        PaperUIStyle uiStyle = PaperUIStyle.Load();
        if (uiStyle != null)
        {
            PaperUIFactory.Scale(canvas);
            placementLabel.color = uiStyle.ink;
            placementLabel.font = uiStyle.font;
            placementPanel = ImagePrompt(canvasObject.transform, label.transform, uiStyle);
        }
        if (uiStyle == null) rect.sizeDelta = new Vector2(-40, 55);
    }

    private void CreateTrainer()
    {
        LevelGrid grid = LevelGrid.Instance;
        Trainer = CreateActor(trainerPrefab, "Battle Trainer",
            configuration.GetTrainerPosition(grid.GetWidth(), grid.GetHeight()));
        var opposingCell = configuration.GetOpponentTrainerPosition(grid.GetWidth(),grid.GetHeight());
        OpponentTrainer = configuration.defendSeal ? CreateFieldMentor(opposingCell)
            : CreateActor(opponentTrainerPrefab != null ? opponentTrainerPrefab : trainerPrefab,"Opponent Trainer",opposingCell);
        cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            Vector3 focus = GetCameraFocus(Trainer);
            if (goldenArena != null) focus = grid.GetWorldPosition(new GridPosition(5, 5));
            cameraController.PrepareBattleOverview(focus, wideGoldenField: true);
        }
        presentation = gameObject.AddComponent<BattlePresentationCamera>();
    }

    private static Vector3 TrainerPosition(GridPosition cell)
    {
        LevelGrid grid = LevelGrid.Instance;
        Vector3 center = grid.GetWorldPosition(new GridPosition(grid.GetWidth() / 2, grid.GetHeight() / 2));
        Vector3 position = grid.GetWorldPosition(cell);
        // The outside grid cell can still lie inside the decorative stone ring.
        Bounds ring = new Bounds(center, Vector3.zero);
        bool hasRing = false;
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!renderer.name.EndsWith("parapet", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!hasRing) { ring = renderer.bounds; hasRing = true; }
            else ring.Encapsulate(renderer.bounds);
        }
        const float clearance = 1.4f;
        if (cell.x < 0) position.x = hasRing ? ring.min.x - clearance : position.x - clearance;
        else if (cell.x >= grid.GetWidth()) position.x = hasRing ? ring.max.x + clearance : position.x + clearance;
        else if (cell.z < 0) position.z = hasRing ? ring.min.z - clearance : position.z - clearance;
        else position.z = hasRing ? ring.max.z + clearance : position.z + clearance;
        return position;
    }

    private static Vector3 ArenaCenter()
    {
        var grid = LevelGrid.Instance;
        return grid.GetWorldPosition(new GridPosition(grid.GetWidth()/2,grid.GetHeight()/2));
    }

    private Transform CreateFieldMentor(GridPosition cell)
    {
        var trainer = new GameObject(ArkanmeisterinConversation.Speaker);
        trainer.SetActive(false);
        var position = TrainerPosition(cell);
        var center = ArenaCenter();
        trainer.transform.SetPositionAndRotation(position,Facing(position,center));
        var figure = new GameObject("Arkanmeisterin paper figure",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        figure.transform.SetParent(trainer.transform,false);
        mentorSprite = ArkanmeisterinArrival.CreateFigureSprite(Resources.Load<Texture2D>("Arkanmeisterin poses"),false);
        mentorMaterial = new Material(Resources.Load<Shader>("AurenfeldScenicPaper"));
        figure.sprite = mentorSprite;
        figure.sharedMaterial = mentorMaterial;
        var visual = trainer.AddComponent<PaperCharacterVisual>();
        visual.SetBattleTrainerPivot(figure.transform,new Vector3(-.28f,1.05f,-.06f));
        trainer.SetActive(true);
        visual.FaceWorldDirection(center-position);
        return trainer.transform;
    }

    private Transform CreateActor(GameObject prefab, string actorName, GridPosition cell)
    {
        var position = TrainerPosition(cell);
        var center = ArenaCenter();
        GameObject trainer = Instantiate(prefab,position,Facing(position,center));
        trainer.name = actorName;
        foreach (MonoBehaviour behaviour in trainer.GetComponentsInChildren<MonoBehaviour>())
            if (!(behaviour is PaperCharacterVisual) && !(behaviour is NPCSummoningFocus)
                && !(behaviour is PaperGroundShadow)) behaviour.enabled = false;
        if (actorName == "Opponent Trainer" && trainer.GetComponent<NPCSummoningFocus>() == null)
            trainer.AddComponent<NPCSummoningFocus>();
        foreach (Collider collider in trainer.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Rigidbody body in trainer.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        trainer.tag = "Untagged";
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

    private void Update()
    {
        if (!initialized || IsReady || IsSummoning || SceneLoadManager.IsTransitioning) return;
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
        if (cameraController != null) cameraController.IsPresentationLocked = true;
        placementLabel.text = "Dein Monster erscheint …";
        StartCoroutine(Summon(position));
        return true;
    }

    private IEnumerator Summon(GridPosition position)
    {
        Vector3 destination = LevelGrid.Instance.GetWorldPosition(position);
        Trainer.rotation = Quaternion.LookRotation(new Vector3(destination.x, Trainer.position.y, destination.z) - Trainer.position);
        PaperCharacterVisual visual = Trainer.GetComponent<PaperCharacterVisual>();
        if (visual != null) { visual.FaceWorldDirection(destination - Trainer.position); visual.SetSummoningPose(true); }
        yield return presentation.FocusTrainer(Trainer);
        Vector3 origin = visual != null ? visual.GetSummonOrigin() : Trainer.position + Vector3.up * 1.2f;
        try
        {
            summonEffect.OnPulseMoved += presentation.FollowPulse;
            yield return summonEffect.Play(origin, destination,
                configuration.playerTeam[nextMonster].GetSummonColor());
        }
        finally { summonEffect.OnPulseMoved -= presentation.FollowPulse; if (visual != null) visual.SetSummoningPose(false); }
        Instantiate(configuration.playerTeam[nextMonster++], destination, Facing(destination, OpponentTrainer.position));
        // Hold the arrival shot while the new unit registers.
        yield return new WaitForSeconds(.2f);
        if (nextMonster == configuration.playerTeam.Length)
        {
            GridSystemVisual.Instance.HideAllGridPositions();
            yield return SummonOpponentTeam();
            IsReady = true;
            IsSummoning = false;
            placementLabel.gameObject.SetActive(false);
            if (placementPanel != null) placementPanel.SetActive(false);
        }
        else { yield return presentation.ReturnToOverview(GetCameraFocus(Trainer)); IsSummoning = false; RefreshAvailable(); ShowCells(); }
    }

    private IEnumerator SummonOpponentTeam()
    {
        IsOpponentSummoning = true;

        try
        {
            placementLabel.text = "Der Gegner beschwört sein Team …";

            for (int i = 0; i < configuration.enemyTeam.Length; i++)
            {
                placementLabel.text = $"Gegner beschwört Monster {i + 1}/{configuration.enemyTeam.Length} …";
                int index = (i + 1) * enemyCells.Count / (configuration.enemyTeam.Length + 1);
                Vector3 destination = LevelGrid.Instance.GetWorldPosition(enemyCells[index]);
                yield return presentation.FocusTrainer(OpponentTrainer);
                PaperCharacterVisual visual = OpponentTrainer.GetComponent<PaperCharacterVisual>();
                if (visual != null)
                {
                    visual.FaceWorldDirection(destination-OpponentTrainer.position);
                    if (!configuration.defendSeal) visual.SetSummoningPose(true);
                }
                NPCSummoningFocus focus = OpponentTrainer.GetComponent<NPCSummoningFocus>();
                Vector3 origin = focus != null ? focus.SummonOrigin : visual != null ? visual.GetSummonOrigin() : OpponentTrainer.position + Vector3.up;
                summonEffect.OnPulseMoved += presentation.FollowPulse;
                try { yield return summonEffect.Play(origin, destination, configuration.enemyTeam[i].GetSummonColor()); }
                finally { summonEffect.OnPulseMoved -= presentation.FollowPulse; if (visual != null) visual.SetSummoningPose(false); }
                Instantiate(configuration.enemyTeam[i], destination, Facing(destination, Trainer.position));
                yield return new WaitForSeconds(.2f); // Hold the opponent arrival shot.
            }
            LevelGrid grid = LevelGrid.Instance;
            yield return presentation.ReturnToOverview(grid.GetWorldPosition(new GridPosition(grid.GetWidth() / 2, grid.GetHeight() / 2)));
        }
        finally
        {
            IsOpponentSummoning = false;
            if (presentation != null) presentation.Release();
        }
    }

    private static Quaternion Facing(Vector3 from, Vector3 to)
    {
        var direction = to-from;
        direction.y = 0;
        return direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
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
            NPCSummoningFocus focus = actor.GetComponent<NPCSummoningFocus>();
            Vector3 vessel = focus != null ? focus.SummonOrigin : trainerVisual != null ? trainerVisual.GetSummonOrigin() : actor.position + Vector3.up;
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
        if (summonEffect != null) { if (presentation != null) summonEffect.OnPulseMoved -= presentation.FollowPulse; summonEffect.CancelAll(); }
        if (presentation != null) presentation.Release();
        if (cameraController != null) cameraController.IsPresentationLocked = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (mentorSprite != null) Destroy(mentorSprite);
        if (mentorMaterial != null) Destroy(mentorMaterial);
    }
}
