using UnityEngine;

// Animate only the paper child; the logical unit, collider and grid cell stay fixed.
public class PaperCharacterVisual : MonoBehaviour
{
    [SerializeField] private Transform paperPivot;
    [SerializeField, Min(0.1f)] private float cycleDuration = 2.4f;
    [SerializeField] private float breathingAmount = 0.012f;
    [SerializeField] private float bobHeight = 0.015f;
    [SerializeField, Min(0.1f)] private float visualScale = 1f;
    [SerializeField, Min(0.1f)] private float walkCycleDuration = 0.32f;
    [SerializeField] private float walkHeight = 0.055f;
    [SerializeField] private Vector3 summonOffset = new Vector3(-0.18f, 0.9f, 0f);
    private Vector3 restPosition;
    private Vector3 restScale;
    private Quaternion restRotation;
    private Vector3 previousPosition;
    private BaseAction[] actions;
    private float phase;
    private float idleWeight;
    private bool initialized;
    private SpriteRenderer spriteRenderer;
    private Unit unit;
    private ShootAction shootAction;
    private SwordAction swordAction;
    private RamAction ramAction;
    private float walkPhase;
    private float walkWeight;
    public bool IsDefeated { get; private set; }
    private float defeatProgress;
    private float recallProgress;
    private Color restColor;

    private void OnEnable()
    {
        if (paperPivot == null) return;
        restPosition = paperPivot.localPosition;
        restScale = paperPivot.localScale;
        restRotation = paperPivot.localRotation;
        previousPosition = transform.position;
        actions = GetComponents<BaseAction>();
        spriteRenderer = paperPivot.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) restColor = spriteRenderer.color;
        IsDefeated = false;
        defeatProgress = 0f;
        recallProgress = 0f;
        unit = GetComponent<Unit>();
        shootAction = GetComponent<ShootAction>();
        swordAction = GetComponent<SwordAction>();
        ramAction = GetComponent<RamAction>();
        phase = Random.value * Mathf.PI * 2f;
        idleWeight = 0f;
        walkWeight = 0f;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (paperPivot == null || !paperPivot.gameObject.activeInHierarchy) return;
        if (IsDefeated)
        {
            defeatProgress = Mathf.MoveTowards(defeatProgress, 1f, Time.deltaTime / 0.35f);
            paperPivot.localPosition = restPosition;
            paperPivot.localScale = Vector3.Scale(restScale * visualScale * (1f - recallProgress),
                new Vector3(Mathf.Lerp(1f, 1.1f, defeatProgress), Mathf.Lerp(1f, 0.6f, defeatProgress), 1f));
            if (Camera.main != null) paperPivot.rotation = Camera.main.transform.rotation;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(restColor,
                    new Color(restColor.r * 0.65f, restColor.g * 0.65f, restColor.b * 0.75f, restColor.a), defeatProgress);
            return;
        }
        Vector3 movement = transform.position - previousPosition;
        movement.y = 0f;
        bool walking = movement.sqrMagnitude > 0.000001f;
        bool idle = !walking;
        previousPosition = transform.position;
        foreach (BaseAction action in actions) if (action.IsActive) idle = false;
        idleWeight = Mathf.MoveTowards(idleWeight, idle ? 1f : 0f, Time.deltaTime * 6f);
        phase += Time.deltaTime * Mathf.PI * 2f / cycleDuration;
        walkWeight = Mathf.MoveTowards(walkWeight, walking ? 1f : 0f, Time.deltaTime * 10f);
        walkPhase += Time.deltaTime * Mathf.PI * 2f / walkCycleDuration;
        float wave = Mathf.Sin(phase) * idleWeight;
        float step = Mathf.Abs(Mathf.Sin(walkPhase)) * walkWeight;
        paperPivot.localPosition = restPosition + Vector3.up * visualScale
            * ((wave + idleWeight) * 0.5f * bobHeight + step * walkHeight);
        paperPivot.localScale = Vector3.Scale(restScale * visualScale,
            new Vector3(1f - wave * breathingAmount * 0.5f + step * 0.025f,
                1f + wave * breathingAmount - step * 0.04f, 1f));
        Camera camera = Camera.main;
        if (camera != null)
        {
            // Match the camera's image plane: no yaw toward targets and no rocking.
            paperPivot.rotation = camera.transform.rotation;
            Vector3 direction = unit != null ? GetOpponentDirection() : movement;
            float horizontal = Vector3.Dot(direction, camera.transform.right);
            if (spriteRenderer != null && Mathf.Abs(horizontal) > 0.025f)
                spriteRenderer.flipX = horizontal < 0f;
        }
    }

    private Vector3 GetOpponentDirection()
    {
        Unit target = shootAction != null ? shootAction.TargetUnit : null;
        if (target == null && swordAction != null) target = swordAction.TargetUnit;
        if (target == null && ramAction != null) target = ramAction.TargetUnit;
        if (target != null && target.IsDead) target = null;
        if (target == null && UnitManager.Instance != null)
        {
            float closest = float.PositiveInfinity;
            foreach (Unit candidate in UnitManager.Instance.GetUnitList())
            {
                if (candidate == null || candidate.IsDead || candidate.IsEnemy() == unit.IsEnemy()) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < closest) { closest = distance; target = candidate; }
            }
        }
        return target != null && !target.IsDead ? target.transform.position - transform.position : Vector3.zero;
    }

    public Vector3 GetSummonOrigin()
    {
        Vector3 offset = summonOffset;
        if (spriteRenderer != null && spriteRenderer.flipX) offset.x = -offset.x;
        return paperPivot != null ? paperPivot.TransformPoint(offset) : transform.position + Vector3.up * 1.2f;
    }

    public void FaceWorldDirection(Vector3 direction)
    {
        if (Camera.main == null || spriteRenderer == null) return;
        float horizontal = Vector3.Dot(direction, Camera.main.transform.right);
        if (Mathf.Abs(horizontal) > 0.025f) spriteRenderer.flipX = horizontal < 0f;
    }

    public void BeginDefeat()
    {
        IsDefeated = true;
        defeatProgress = 0f;
        recallProgress = 0f;
    }

    public void SetRecallProgress(float progress) => recallProgress = Mathf.Clamp01(progress);

    public Vector3 GetRecallOrigin() => spriteRenderer != null ? spriteRenderer.bounds.center
        : transform.position + Vector3.up * 0.5f;

    private void OnDisable()
    {
        if (paperPivot == null || !initialized) return;
        paperPivot.localPosition = restPosition;
        paperPivot.localScale = restScale;
        paperPivot.localRotation = restRotation;
        if (spriteRenderer != null) spriteRenderer.color = restColor;
    }
}
