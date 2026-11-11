using UnityEngine;

// Uses the two approved shield illustrations as held poses.
public sealed class PanfrieBattlePose : MonoBehaviour
{
    [SerializeField] private Sprite shieldPose;
    private PaperCharacterVisual visual;
    private ShieldWatchAction watch;
    private Unit unit;
    private bool covered;

    private void Awake()
    {
        visual = GetComponent<PaperCharacterVisual>();
        watch = GetComponent<ShieldWatchAction>();
        unit = GetComponent<Unit>();
    }

    private void LateUpdate()
    {
        bool defending = !unit.IsDead && (watch.IsProtecting || unit.IsGuarding);
        if (covered == defending) return;
        covered = defending;
        visual.SetVesselTrialPose(covered ? shieldPose : null);
    }
}
