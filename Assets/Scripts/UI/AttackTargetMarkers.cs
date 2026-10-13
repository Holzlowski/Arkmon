using System.Collections.Generic;
using UnityEngine;

// A small reusable pool; no colliders and no changes to the actual attack plan.
public class AttackTargetMarkers : MonoBehaviour
{
    public enum Role { Hit, Ally, Blocked, Pushed, Attacker, Cover }
    public struct Mark
    {
        public GridPosition cell;
        public Role role;
        public Mark(GridPosition cell, Role role) { this.cell = cell; this.role = role; }
    }
    private readonly List<LineRenderer> pool = new List<LineRenderer>();
    private Material material;
    public static List<Mark> Plan(BaseAction action, GridPosition cell)
    {
        var marks = new List<Mark>();
        if (action == null || !action.IsValidActionGridPosition(cell)) return marks;
        Unit attacker = action.GetUnit();
        if (action is EmberAction fan)
        {
            var impact = fan.GetImpactGridPositionList(cell);
            foreach (var affected in fan.GetAffectedGridPositionList(cell))
            {
                bool ally = LevelGrid.Instance.GetUnitListAtGridPosition(affected, attacker)
                    .Exists(u => u != null && !u.IsDead && u.IsEnemy() == attacker.IsEnemy());
                marks.Add(new Mark(affected, !impact.Contains(affected) ? Role.Blocked : ally ? Role.Ally : Role.Hit));
            }
        }
        else if (action is RamAction ram && ram.PreviewPush(cell, out GridPosition pushed, out bool down))
        {
            marks.Add(new Mark(cell, Role.Hit));
            Unit target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
            bool survives = target.GetComponent<HealthSystem>().Health > target.PreviewDamage(ram.DamageAmount, true);
            GridPosition origin = attacker.GetGridPosition();
            GridPosition direction = new GridPosition(System.Math.Sign(cell.x-origin.x), System.Math.Sign(cell.z-origin.z));
            if (LevelGrid.Instance.IsValidGridPosition(pushed) && survives)
                marks.Add(new Mark(pushed, down ? Role.Blocked : Role.Pushed));
            marks.Add(new Mark(down && survives ? cell + new GridPosition(-direction.x, -direction.z) : cell, Role.Attacker));
        }
        else if (action is MeleeAttackAction || action is ShootAction)
        {
            marks.Add(new Mark(cell, Role.Hit));
            if (action is ShootAction shoot)
            {
                Unit target = LevelGrid.Instance.GetUnitOnGridPosition(cell);
                var plan = CoverSystem.Preview(attacker.GetGridPosition(), cell, shoot.DamageAmount, target);
                if (plan.cover != null) marks.Add(new Mark(plan.cover.GetGridPosition(), Role.Cover));
            }
        }
        return marks;
    }
    public void Clear() { foreach (var line in pool) line.enabled = false; }
    public void Show(BaseAction action, GridPosition cell)
    {
        Clear(); var marks = Plan(action, cell);
        float spacing = LevelGrid.Instance.GetWidth() > 1 ? Vector3.Distance(LevelGrid.Instance.GetWorldPosition(new GridPosition(0,0)),
            LevelGrid.Instance.GetWorldPosition(new GridPosition(1,0))) : 2;
        for (int i = 0; i < marks.Count; i++)
        {
            if (i == pool.Count)
            {
                if (material == null) material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                var line = new GameObject("Target outcome marker", typeof(LineRenderer)).GetComponent<LineRenderer>();
                line.transform.SetParent(transform, false); line.sharedMaterial = material; line.useWorldSpace = true;
                line.loop = true; line.positionCount = 4; line.widthMultiplier = .07f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pool.Add(line);
            }
            var renderer = pool[i]; renderer.enabled = true;
            Color color = marks[i].role == Role.Ally ? new Color(1,.25f,.2f) : marks[i].role == Role.Blocked ? new Color(.55f,.55f,.65f)
                : marks[i].role == Role.Attacker ? new Color(.3f,.85f,1) : marks[i].role == Role.Pushed ? new Color(.7f,.45f,1)
                : marks[i].role == Role.Cover ? new Color(.5f,.85f,.4f) : new Color(1,.7f,.2f);
            renderer.startColor = renderer.endColor = color;
            var properties = new MaterialPropertyBlock(); properties.SetColor("_BaseColor", color); renderer.SetPropertyBlock(properties);
            Vector3 centre = LevelGrid.Instance.GetWorldPosition(marks[i].cell) + Vector3.up * (.13f+i*.009f);
            float size = spacing * (marks[i].role == Role.Attacker ? .3f : .43f);
            renderer.SetPositions(new[] {centre+new Vector3(-size,0,-size),centre+new Vector3(-size,0,size),
                centre+new Vector3(size,0,size),centre+new Vector3(size,0,-size)});
        }
    }
    private void OnDisable() => Clear();
    private void OnDestroy() { if (material != null) Destroy(material); }
}
