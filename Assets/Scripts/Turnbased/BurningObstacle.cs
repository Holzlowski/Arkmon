using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BurningStatus))]
public class BurningObstacle : MonoBehaviour
{
    public enum MaterialKind { Vegetation, Stone }
    [SerializeField] private MaterialKind materialKind;
    [SerializeField, Min(1)] private int maxHealth = 15;
    public bool IsVegetation => materialKind == MaterialKind.Vegetation;
    public bool IsDestroyed { get; private set; }
    public int Health { get; private set; }
    private bool registered;
    private GridPosition position;
    private void Awake() { Health = maxHealth; }
    private IEnumerator Start()
    {
        yield return null; // Wait for LevelGrid's pathfinding setup.
        if (LevelGrid.Instance == null || Pathfinding.Instance == null) yield break;
        position = LevelGrid.Instance.GetGridPosition(transform.position);
        if (!LevelGrid.Instance.IsValidGridPosition(position)) yield break;
        registered = true;
        Pathfinding.Instance.SetIsWalkableGridPosition(position, false);
        if (GridSystemVisual.Instance != null) GridSystemVisual.Instance.Refresh();
    }
    public GridPosition GetGridPosition() => position;
    public void Damage(int amount)
    {
        if (IsDestroyed || !IsVegetation || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount);
        if (Health > 0) return;
        IsDestroyed = true;
        GetComponent<BurningStatus>().Extinguish();
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        gameObject.SetActive(false);
        Physics.SyncTransforms();
        if (registered && Pathfinding.Instance != null)
            Pathfinding.Instance.RefreshWalkability(position);
        if (GridSystemVisual.Instance != null) GridSystemVisual.Instance.Refresh();
        Destroy(gameObject);
    }
}
