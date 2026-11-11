using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BurningStatus))]
public class BurningObstacle : MonoBehaviour
{
    public enum MaterialKind { Vegetation, Stone, Wood }
    [SerializeField] private MaterialKind materialKind;
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField, Min(1)] private int maxHealth = 15;
    [SerializeField] private bool providesCover;
    [SerializeField] private bool blocksSight;
    [SerializeField, Range(0, 1)] private float absorption = .5f;
    public bool IsVegetation => materialKind != MaterialKind.Stone;
    public bool ProvidesCover => providesCover && !IsDestroyed && isActiveAndEnabled;
    public bool BlocksSight => blocksSight && !IsDestroyed && isActiveAndEnabled;
    public float Absorption => absorption;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed { get; private set; }
    public int Health { get; private set; }
    public string DisplayName
    {
        get
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("bush")) return "Busch";
            if (lower.Contains("tree")) return "Baum";
            if (lower.Contains("fence")) return "Holzzaun";
            if (lower.Contains("crate")) return "Holzkiste";
            if (lower.Contains("rock") || lower.Contains("stone")) return "Stein";
            return name.Replace("(Clone)", "").Trim();
        }
    }
    private bool registered;
    private GridPosition position;
    private void Awake() { Health = maxHealth; }
    public void Configure(MaterialKind kind, int health, bool cover, bool sightBlock = false)
    {
        materialKind = kind; maxHealth = Mathf.Max(1, health); Health = maxHealth;
        providesCover = cover; blocksSight = sightBlock;
    }
    private IEnumerator Start()
    {
        yield return null; // Wait for LevelGrid's pathfinding setup.
        if (LevelGrid.Instance == null || Pathfinding.Instance == null) yield break;
        position = LevelGrid.Instance.GetGridPosition(transform.position);
        if (!LevelGrid.Instance.IsValidGridPosition(position)) yield break;
        registered = true;
        foreach (var cell in OccupiedCells())
            Pathfinding.Instance.SetIsWalkableGridPosition(cell, false);
        if (GridSystemVisual.Instance != null) GridSystemVisual.Instance.Refresh();
    }
    public GridPosition GetGridPosition() => position;

    internal void SetFootprint(Vector2Int size) => footprint = Vector2Int.Max(size, Vector2Int.one);

    internal IEnumerable<GridPosition> OccupiedCells()
    {
        for (int x = 0; x < Mathf.Max(1, footprint.x); x++)
            for (int z = 0; z < Mathf.Max(1, footprint.y); z++)
            {
                var cell = position + new GridPosition(x, z);
                if (LevelGrid.Instance == null || LevelGrid.Instance.IsValidGridPosition(cell)) yield return cell;
            }
    }

    internal bool Occupies(GridPosition position)
    {
        if (!registered) return false;
        foreach (var cell in OccupiedCells())
            if (cell == position) return true;
        return false;
    }

    internal bool IsAdjacentTo(GridPosition position)
    {
        foreach (var cell in OccupiedCells())
            if (Mathf.Max(Mathf.Abs(cell.x - position.x), Mathf.Abs(cell.z - position.z)) <= 1)
                return true;
        return false;
    }
    public void Damage(int amount)
    {
        if (IsDestroyed || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount);
        GetComponent<ObstacleConditionVisual>()?.RevealDamage();
        GetComponent<ObstacleConditionVisual>()?.Refresh();
        if (Health > 0) return;
        IsDestroyed = true;
        GetComponent<BurningStatus>().Extinguish();
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        gameObject.SetActive(false);
        Physics.SyncTransforms();
        if (registered && Pathfinding.Instance != null)
            foreach (var cell in OccupiedCells())
                Pathfinding.Instance.RefreshWalkability(cell);
        if (GridSystemVisual.Instance != null) GridSystemVisual.Instance.Refresh();
        Destroy(gameObject);
    }
}
