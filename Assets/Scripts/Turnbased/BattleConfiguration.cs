using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Battle Configuration")]
public class BattleConfiguration : ScriptableObject
{
    public enum DeploymentSide { Left, Right, Bottom, Top }
    public DeploymentSide playerSide = DeploymentSide.Left;
    [Min(1)] public int deploymentRadius = 3;
    // Enemy placement still uses rows/columns on the opposite edge.
    [Min(1)] public int deploymentDepth = 2;
    public Unit[] playerTeam;
    public Unit[] enemyTeam;

    public GridPosition GetTrainerPosition(int width, int height)
    {
        switch (playerSide)
        {
            case DeploymentSide.Right: return new GridPosition(Mathf.Max(0, width - 2), height / 2);
            case DeploymentSide.Bottom: return new GridPosition(width / 2, Mathf.Min(1, height - 1));
            case DeploymentSide.Top: return new GridPosition(width / 2, Mathf.Max(0, height - 2));
            default: return new GridPosition(Mathf.Min(1, width - 1), height / 2);
        }
    }

    public bool Contains(GridPosition position, int width, int height, bool enemy = false)
    {
        if (position.x < 0 || position.z < 0 || position.x >= width || position.z >= height) return false;
        if (!enemy)
        {
            GridPosition origin = GetTrainerPosition(width, height);
            if (position == origin) return false;
            float dx = position.x - origin.x;
            float dz = position.z - origin.z;
            float radius = Mathf.Max(1, deploymentRadius);
            if (dx * dx + dz * dz > radius * radius) return false;
            // Even a large radius must not reach the opposing deployment zone.
            return !Contains(position, width, height, true);
        }
        DeploymentSide side = playerSide;
        if (enemy) side = side == DeploymentSide.Left ? DeploymentSide.Right
            : side == DeploymentSide.Right ? DeploymentSide.Left
            : side == DeploymentSide.Bottom ? DeploymentSide.Top : DeploymentSide.Bottom;
        // Keep opposing deployment zones disjoint, including small test grids.
        int size = side == DeploymentSide.Left || side == DeploymentSide.Right ? width : height;
        int depth = Mathf.Clamp(deploymentDepth, 1, Mathf.Max(1, size / 2));
        switch (side)
        {
            case DeploymentSide.Left: return position.x < depth;
            case DeploymentSide.Right: return position.x >= width - depth;
            case DeploymentSide.Bottom: return position.z < depth;
            default: return position.z >= height - depth;
        }
    }
}
