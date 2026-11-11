using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShadowStepAction : BaseAction
{
    private readonly Dictionary<GridPosition,GameObject> shadowPatches = new();
    private Material shadowMaterial;
    private Transform shadowRoot;

    private static bool IsShadowField(GridPosition cell)
    {
        if (!LevelGrid.Instance.IsValidGridPosition(cell) || !Pathfinding.Instance.IsWalkableGridPosition(cell)) return false;
        var cover = CoverSystem.ObstacleAt(cell+new GridPosition(1,0));
        return cover != null && (cover.ProvidesCover || cover.BlocksSight);
    }

    private void Start()
    {
        var shader = Resources.Load<Shader>("PaperGroundShadow");
        if (shader == null) return;
        shadowMaterial = new Material(shader) { name = "Rauk shadow field" };
        shadowMaterial.SetColor("_ShadowColor",new Color(.16f,.12f,.23f,.78f));
        var grid = LevelGrid.Instance;
        float cellSize = Vector3.Distance(grid.GetWorldPosition(new GridPosition(0,0)),grid.GetWorldPosition(new GridPosition(1,0)));
        shadowRoot = new GameObject("Rauk shadow fields").transform;
        shadowRoot.SetParent(grid.transform,false);
        for (int x = 0; x < grid.GetWidth(); x++)
            for (int z = 0; z < grid.GetHeight(); z++)
            {
                var cell = new GridPosition(x,z);
                if (!IsShadowField(cell)) continue;
                var patch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                patch.name = $"Shadow field {cell}";
                Destroy(patch.GetComponent<Collider>());
                patch.transform.SetParent(shadowRoot,false);
                patch.transform.SetPositionAndRotation(
                    grid.GetWorldPosition(cell)+new Vector3(cellSize*.12f,.047f,0),Quaternion.Euler(90,0,0));
                patch.transform.localScale = new Vector3(cellSize*1.32f,cellSize*.92f,1);
                var renderer = patch.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = shadowMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                shadowPatches.Add(cell,patch);
            }
    }

    private void LateUpdate()
    {
        foreach (var patch in shadowPatches)
        {
            bool shaded = IsShadowField(patch.Key);
            if (patch.Value.activeSelf != shaded) patch.Value.SetActive(shaded);
        }
    }

    protected override void OnDisable()
    {
        if (shadowRoot != null) shadowRoot.gameObject.SetActive(false);
        base.OnDisable();
    }

    private void OnEnable()
    {
        if (shadowRoot != null) shadowRoot.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (shadowRoot != null) Destroy(shadowRoot.gameObject);
        if (shadowMaterial != null) Destroy(shadowMaterial);
    }

    public override string GetActionName() => "Schattensprung";

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        var result = new List<GridPosition>();
        if (!unit.CanPerformAction(this)) return result;
        var origin = unit.GetGridPosition();
        var grid = LevelGrid.Instance;
        int range = Definition != null ? Definition.range : 4;
        for (int x = -range; x <= range; x++)
            for (int z = -range; z <= range; z++)
            {
                var cell = origin + new GridPosition(x,z);
                if (IsShadowField(cell) && !grid.HasAnyUnitOnGridPosition(cell)) result.Add(cell);
            }
        return result;
    }

    public override void TakeAction(GridPosition cell, Action completed)
    {
        if (isActive) return;
        if (!IsValidActionGridPosition(cell)) { completed?.Invoke(); return; }
        var destination = LevelGrid.Instance.GetWorldPosition(cell);
        ActionStart(completed);
        CombatStrikeVisual.Show(unit.transform.position+Vector3.up*.6f,destination+Vector3.up*.15f,
            new Color(.62f,.42f,.90f),.16f,"Shadow step");
        unit.MoveToWorldPosition(destination);
        ActionComplete();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition cell)
        => new EnemyAIAction { gridPosition = cell, actionValue = 1 };
}
