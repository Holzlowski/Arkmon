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
    private Sprite restingSprite;
    private bool summoningPose;
    private Unit unit;
    private ShootAction shootAction;
    private SwordAction swordAction;
    private RamAction ramAction;
    private EmberAction emberAction;
    private MeleeAttackAction[] meleeActions;
    private float walkPhase;
    private float walkWeight;
    public bool IsDefeated { get; private set; }
    private float defeatProgress;
    private float knockdownProgress;
    private float recallProgress;
    private Color restColor;
    private float hitRemaining;
    private float outfitProgress = -1;
    private float wakeProgress = -1;
    private PlayerController playerController;
    private Sprite[] roomWalk, mantleWalk, roomNoLampWalk, mantleNoLampWalk, familyWalk;
    private float travelled;
    [SerializeField, Min(.2f)] private float walkStrideLength = 2.4f;
    private PaperPlayerWalkRig playerWalkRig;
    private PaperPlayerGardenRig gardenRig;
    private SpriteRenderer gardenCan;
    private float gardenBend, gardenTilt, gardenReach;
    private bool gardenAttached;
    public Vector3 GardenHandPosition => gardenRig != null ? gardenRig.HandPosition : transform.position;
    public void SetGardenPose(SpriteRenderer can, bool attached, float bend = 0, float tilt = 0, float reach = 1)
    {
        gardenCan = can; gardenAttached = attached;
        gardenBend = bend; gardenTilt = tilt; gardenReach = reach;
        if (can != null && gardenRig == null && spriteRenderer != null)
            gardenRig = new PaperPlayerGardenRig(paperPivot, spriteRenderer);
        if (can == null) { gardenRig?.Hide(); if(spriteRenderer != null) spriteRenderer.forceRenderingOff = false; }
    }
    public bool IsPlayingWalkCycle { get; private set; }
    public void SetWakeUpProgress(float progress) => wakeProgress = progress;
    public void SetOutfitChangeProgress(float progress) => outfitProgress = progress;
    public void PlayHitReaction() => hitRemaining = .22f;

    private void OnEnable()
    {
        if (paperPivot == null) return;
        restPosition = paperPivot.localPosition;
        restScale = paperPivot.localScale;
        restRotation = paperPivot.localRotation;
        previousPosition = transform.position;
        actions = GetComponents<BaseAction>();
        spriteRenderer = paperPivot.GetComponent<SpriteRenderer>();
        PaperGroundShadow.Attach(gameObject, spriteRenderer);
        if (spriteRenderer != null) { restColor = spriteRenderer.color; restingSprite = spriteRenderer.sprite; }
        IsDefeated = false;
        defeatProgress = 0f;
        knockdownProgress = 0f;
        recallProgress = 0f;
        hitRemaining = 0f;
        unit = GetComponent<Unit>();
        playerController = GetComponent<PlayerController>();
        if(playerController != null)
        {
            playerWalkRig?.Dispose();
            playerWalkRig = new PaperPlayerWalkRig(paperPivot, spriteRenderer);
            roomWalk = Resources.LoadAll<Sprite>("Prince-Room-Walk");
            mantleWalk = Resources.LoadAll<Sprite>("Prince-Mantle-Walk");
            roomNoLampWalk = Resources.LoadAll<Sprite>("Prince-Room-NoLamp-Walk");
            mantleNoLampWalk = Resources.LoadAll<Sprite>("Prince-Mantle-NoLamp-Walk");
            familyWalk = System.Array.FindAll(Resources.LoadAll<Sprite>("Elian-Family-Star-Sheet"),s=>s.name.StartsWith("Family-Walk-"));
            System.Array.Sort(familyWalk,(a,b)=>string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(roomWalk,(a,b)=>string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(mantleWalk,(a,b)=>string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(roomNoLampWalk,(a,b)=>string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(mantleNoLampWalk,(a,b)=>string.CompareOrdinal(a.name,b.name));
        }
        travelled = 0;
        shootAction = GetComponent<ShootAction>();
        swordAction = GetComponent<SwordAction>();
        ramAction = GetComponent<RamAction>();
        emberAction = GetComponent<EmberAction>();
        meleeActions = GetComponents<MeleeAttackAction>();
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
            playerWalkRig?.Hide();
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
        StandUpAction standing = unit != null ? unit.GetAction<StandUpAction>() : null;
        if (standing != null && standing.IsActive) knockdownProgress = 1f - standing.Progress;
        else knockdownProgress = Mathf.MoveTowards(knockdownProgress,
            unit != null && unit.IsKnockedDown ? 1f : 0f, Time.deltaTime / 0.35f);
        Vector3 movement = transform.position - previousPosition;
        movement.y = 0f;
        bool walking = movement.sqrMagnitude > 0.000001f;
        if(playerController != null) walking = playerController.isActiveAndEnabled && playerController.IsMoving;
        UpdateWalkFrame(walking, movement.magnitude);
        bool idle = !walking;
        previousPosition = transform.position;
        foreach (BaseAction action in actions) if (action.IsActive) idle = false;
        idleWeight = Mathf.MoveTowards(idleWeight, idle ? 1f : 0f, Time.deltaTime * 6f);
        phase += Time.deltaTime * Mathf.PI * 2f / cycleDuration;
        walkWeight = Mathf.MoveTowards(walkWeight, walking ? 1f : 0f, Time.deltaTime * 10f);
        if(playerController != null)walkWeight=walking?1:0;
        walkPhase += Time.deltaTime * Mathf.PI * 2f / walkCycleDuration;
        float wave = Mathf.Sin(phase) * idleWeight;
        float step = IsPlayingWalkCycle ? 0f : Mathf.Abs(Mathf.Sin(walkPhase)) * walkWeight;
        paperPivot.localPosition = restPosition + Vector3.up * visualScale
            * ((wave + idleWeight) * 0.5f * bobHeight + step * walkHeight * (IsPlayingWalkCycle ? .2f : 1));
        paperPivot.localScale = Vector3.Scale(restScale * visualScale,
            new Vector3(1f - wave * breathingAmount * 0.5f + step * 0.025f,
                1f + wave * breathingAmount - step * 0.04f, 1f));
        // Flatten the paper silhouette without rotating the logical unit or its collider.
        paperPivot.localScale = Vector3.Scale(paperPivot.localScale,
            new Vector3(Mathf.Lerp(1f, 1.15f, knockdownProgress),
                Mathf.Lerp(1f, 0.35f, knockdownProgress), 1f));
        if (outfitProgress >= 0)
        {
            float motion = Mathf.Sin(Mathf.Clamp01(outfitProgress) * Mathf.PI);
            paperPivot.localScale = Vector3.Scale(paperPivot.localScale, new Vector3(1 + motion * .035f, 1 - motion * .07f, 1));
            paperPivot.localPosition += Vector3.up * -.025f * motion;
        }
        if (wakeProgress >= 0)
        {
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(wakeProgress));
            paperPivot.localScale = Vector3.Scale(paperPivot.localScale,
                new Vector3(Mathf.Lerp(1.04f, 1f, rise), Mathf.Lerp(.78f, 1f, rise), 1));
            paperPivot.localPosition += Vector3.down * (.12f * (1 - rise));
        }
        Camera camera = Camera.main;
        if (spriteRenderer != null)
        {
            hitRemaining = Mathf.Max(0, hitRemaining - Time.deltaTime);
            float hit = Mathf.Clamp01(hitRemaining / .22f);
            spriteRenderer.color = Color.Lerp(restColor, new Color(1.7f, 1.4f, 1.05f, restColor.a), hit);
            if (camera != null) paperPivot.localPosition += transform.InverseTransformDirection(camera.transform.right)
                * (Mathf.Sin(hit * Mathf.PI) * .07f);
        }
        if (camera != null)
        {
            // Match the camera's image plane: no yaw toward targets and no rocking.
            paperPivot.rotation = camera.transform.rotation;
            Vector3 direction = unit != null ? GetOpponentDirection() : movement;
            float horizontal = Vector3.Dot(direction, camera.transform.right);
            if (spriteRenderer != null && wakeProgress < 0 && Mathf.Abs(horizontal) > 0.025f)
                spriteRenderer.flipX = horizontal < 0f;
        }
        bool rigOutfit = restingSprite != null && (restingSprite.name == "Prince-Family" || restingSprite.name == "Prince-Room-NoLamp");
        playerWalkRig?.Present(gardenCan == null && IsPlayingWalkCycle && rigOutfit && !summoningPose,
            restingSprite != null && restingSprite.name == "Prince-Family", spriteRenderer != null && spriteRenderer.flipX,
            travelled / walkStrideLength * Mathf.PI * 2);
        if (gardenCan != null && !summoningPose && gardenRig != null)
            gardenRig.Present(restingSprite != null && restingSprite.name == "Prince-Family",
                spriteRenderer.flipX, walking, travelled / walkStrideLength * Mathf.PI * 2,
                gardenBend, gardenTilt, gardenReach, gardenCan, gardenAttached);
        else gardenRig?.Hide();
    }

    private void UpdateWalkFrame(bool walking, float distance)
    {
        IsPlayingWalkCycle = false;
        if(playerController == null || spriteRenderer == null || summoningPose || restingSprite == null) return;
        bool rigAvailable = playerWalkRig != null && playerWalkRig.Available
            && (restingSprite.name == "Prince-Family" || restingSprite.name == "Prince-Room-NoLamp");
        Sprite[] frames = restingSprite.name == "Prince-Family" ? familyWalk
            : restingSprite.name == "Prince-Mantle" ? mantleWalk
            : restingSprite.name == "Prince-Room" ? roomWalk
            : restingSprite.name == "Prince-Mantle-NoLamp" ? mantleNoLampWalk
            : restingSprite.name == "Prince-Room-NoLamp" ? roomNoLampWalk : null;
        if(!walking || (!rigAvailable && (frames == null || frames.Length == 0)) || wakeProgress >= 0 || outfitProgress >= 0)
        { travelled = 0; spriteRenderer.sprite = restingSprite; return; }
        // Distance-based steps stop at walls and do not keep running after key release.
        // Reject scene/spawn teleports; the phase follows real distance, not fixed-tick cadence.
        if(distance < .5f) travelled += distance;
        travelled = Mathf.Repeat(travelled, walkStrideLength);
        if(rigAvailable) { spriteRenderer.sprite = restingSprite; IsPlayingWalkCycle = true; return; }
        int frame = Mathf.FloorToInt(travelled / walkStrideLength * frames.Length) % frames.Length;
        spriteRenderer.sprite = frames[frame]; IsPlayingWalkCycle = true;
    }

    private Vector3 GetOpponentDirection()
    {
        if (emberAction != null && emberAction.TargetPosition.HasValue)
            return emberAction.TargetPosition.Value - transform.position;
        Unit target = shootAction != null ? shootAction.TargetUnit : null;
        if (target == null && swordAction != null) target = swordAction.TargetUnit;
        if (target == null && ramAction != null) target = ramAction.TargetUnit;
        foreach (MeleeAttackAction melee in meleeActions)
            if (target == null && melee.TargetPosition.HasValue)
                return melee.TargetPosition.Value - transform.position;
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

    public void SetSummoningPose(bool active)
    {
        playerWalkRig?.Hide();
        if (spriteRenderer == null) return;
        Sprite pose = active ? Resources.Load<Sprite>(GetComponent<NPCSummoningFocus>() != null
            ? "John-Summoning" : "Player-Summoning") : restingSprite;
        if (pose != null) spriteRenderer.sprite = pose;
        summoningPose = active && pose != null;
    }

    public void SetRestingSprite(Sprite sprite)
    {
        if (sprite == null || spriteRenderer == null) return;
        restingSprite = sprite;
        if (!summoningPose) spriteRenderer.sprite = sprite;
    }

    public bool IsSummoningPose => summoningPose;

    public Vector3 GetSummonOrigin()
    {
        if (summoningPose && spriteRenderer != null && Camera.main != null)
            return spriteRenderer.bounds.center + Camera.main.transform.right
                * (spriteRenderer.flipX ? -1f : 1f) * spriteRenderer.bounds.size.x
                * (GetComponent<NPCSummoningFocus>() != null ? -.29f : .34f)
                + Camera.main.transform.up * spriteRenderer.bounds.size.y * .14f;
        Vector3 offset = summonOffset;
        if (spriteRenderer != null && spriteRenderer.flipX) offset.x = -offset.x;
        return paperPivot != null ? paperPivot.TransformPoint(offset) : transform.position + Vector3.up * 1.2f;
    }

    public Vector3 GetAttackOrigin()
    {
        if (spriteRenderer == null || Camera.main == null) return transform.position + Vector3.up * 0.6f;
        return spriteRenderer.bounds.center + Camera.main.transform.right
            * spriteRenderer.bounds.size.x * (spriteRenderer.flipX ? -0.35f : 0.35f);
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
        playerWalkRig?.Hide();
        SetGardenPose(null, false);
        IsPlayingWalkCycle = false;
        if (paperPivot == null || !initialized) return;
        paperPivot.localPosition = restPosition;
        paperPivot.localScale = restScale;
        paperPivot.localRotation = restRotation;
        if (spriteRenderer != null) { spriteRenderer.color = restColor; SetSummoningPose(false); }
    }
    private void OnDestroy() { playerWalkRig?.Dispose(); gardenRig?.Dispose(); }
}
