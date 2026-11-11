using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BurningSystem : MonoBehaviour
{
    [SerializeField] private FireRules rules;
    private FireRules fallback;
    public FireRules Rules => rules != null ? rules : fallback;
    public bool IsResolving { get; private set; }
    private void Awake() { fallback = ScriptableObject.CreateInstance<FireRules>(); }

    public void ResolveRound()
    {
        if (IsResolving) return;
        IsResolving = true;
        try
        {
            // Freeze both sources and targets before damage or ignition: no instant chain.
            BurningStatus[] targets = BurningStatus.Active.Where(s => s != null && s.CanBurn
                && s.gameObject.scene == gameObject.scene)
                .OrderBy(s => s.Position.x).ThenBy(s => s.Position.z).ThenBy(s => s.GetInstanceID()).ToArray();
            BurningStatus[] sources = targets.Where(s => s.IsBurning).ToArray();
            var exposed = new HashSet<BurningStatus>();
            foreach (BurningStatus source in sources)
                foreach (BurningStatus target in targets)
                {
                    if (target.IsBurning) continue;
                    if (Touching(source, target)) exposed.Add(target);
                }
            foreach (BurningStatus source in sources) if (source != null) source.Tick();
            // Iterate in stable grid order; multiple neighbours give only one attempt.
            foreach (BurningStatus target in targets)
                if (target != null && exposed.Contains(target))
                    target.TryIgnite(target.Unit != null ? Rules.monsterSpreadChance : Rules.vegetationSpreadChance,
                        Rules.durationRounds, Rules.damagePerRound);
        }
        finally { IsResolving = false; }
    }

    private static bool Touching(BurningStatus source, BurningStatus target)
    {
        IEnumerable<GridPosition> sourceCells = source.Obstacle != null ? source.Obstacle.OccupiedCells() : new[] { source.Position };
        IEnumerable<GridPosition> targetCells = target.Obstacle != null ? target.Obstacle.OccupiedCells() : new[] { target.Position };
        foreach (var a in sourceCells)
            foreach (var b in targetCells)
                if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z) == 1)
                    return true;
        return false;
    }

    private void OnDestroy()
    {
        if (fallback == null) return;
        if (Application.isPlaying) Destroy(fallback); else DestroyImmediate(fallback);
    }
}
