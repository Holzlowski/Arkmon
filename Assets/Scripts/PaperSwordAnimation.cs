using UnityEngine;

// Rotates only cutout limbs in the camera plane; the body and logical grid root stay upright.
public class PaperSwordAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite armSprite;
    [SerializeField] private Sprite forearmSprite;
    [SerializeField] private Sprite swordSprite;
    public bool IsSwinging { get; private set; }
    public const float HitTime = 0.42f;
    public const float Duration = 0.85f;
    private Transform mirrorRoot, shoulder, sword, tip;
    private SpriteRenderer armRenderer, forearmRenderer, swordRenderer;
    private TrailRenderer fireTrail;
    private Material trailMaterial, swordMaterial;
    private float elapsed;
    private bool fire;
    private bool holdImpactFrame;
    private PaperCharacterVisual visual;

    private void Awake()
    {
        visual = GetComponent<PaperCharacterVisual>();
        if (bodyRenderer == null || armSprite == null || swordSprite == null) return;
        float height = bodyRenderer.sprite.bounds.size.y;
        mirrorRoot = new GameObject("Sword mirror").transform;
        mirrorRoot.SetParent(bodyRenderer.transform, false);
        shoulder = new GameObject("Sword shoulder").transform;
        shoulder.SetParent(mirrorRoot, false);
        shoulder.localPosition = new Vector3(-bodyRenderer.sprite.bounds.size.x * 0.27f, height * 0.44f, 0.005f);
        // Tuck the shoulder cap beneath the cloak instead of drawing a joint on the chest.
        armRenderer = Part("Sword arm", armSprite, shoulder, bodyRenderer.sortingOrder - 1);
        float armScale = height * 0.13f / armSprite.bounds.size.y;
        armRenderer.transform.localScale = Vector3.one * armScale;
        if (forearmSprite != null)
        {
            // Reuse the hand/forearm pixels in front; the shoulder cap stays behind the cloak.
            forearmRenderer = Part("Sword forearm", forearmSprite, shoulder, bodyRenderer.sortingOrder + 2);
            forearmRenderer.transform.localScale = Vector3.one * armScale;
            forearmRenderer.transform.localPosition = new Vector3(
                -(armSprite.bounds.size.x - forearmSprite.bounds.size.x) * armScale, 0, -0.02f);
        }
        sword = new GameObject("Sword wrist").transform;
        sword.SetParent(shoulder, false);
        sword.localPosition = new Vector3(-armSprite.bounds.size.x * armScale * 0.82f, 0, -0.002f);
        swordRenderer = Part("Wooden sword", swordSprite, sword, bodyRenderer.sortingOrder + 1);
        swordMaterial = new Material(bodyRenderer.sharedMaterial);
        swordRenderer.sharedMaterial = swordMaterial;
        float swordScale = height * 0.45f / swordSprite.bounds.size.y;
        swordRenderer.transform.localScale = Vector3.one * swordScale;
        tip = new GameObject("Sword tip").transform;
        tip.SetParent(sword, false);
        tip.localPosition = Vector3.up * swordSprite.bounds.max.y * swordScale;
        FireVisualStyle fireStyle = Resources.Load<FireVisualStyle>("Paper Fire");
        trailMaterial = fireStyle != null && fireStyle.swordTrailMaterial != null
            ? new Material(fireStyle.swordTrailMaterial)
            : new Material(Shader.Find("Arkmon/Soft Fire Trail"));
        trailMaterial.SetColor("_BaseColor", new Color(1.6f, 0.5f, 0.05f));
        fireTrail = tip.gameObject.AddComponent<TrailRenderer>();
        fireTrail.sharedMaterial = trailMaterial;
        fireTrail.sortingLayerID = bodyRenderer.sortingLayerID;
        fireTrail.sortingOrder = bodyRenderer.sortingOrder + 3;
        fireTrail.time = 0.18f; fireTrail.startWidth = 0.065f; fireTrail.endWidth = 0f;
        fireTrail.minVertexDistance = 0.008f;
        fireTrail.numCornerVertices = 4; fireTrail.numCapVertices = 4;
        fireTrail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            alphaKeys = new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.55f, 0.5f), new GradientAlphaKey(0, 1) }
        };
        fireTrail.emitting = false; fireTrail.Clear();
        Pose(-15f, 40f);
    }

    private SpriteRenderer Part(string name, Sprite sprite, Transform parent, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.sharedMaterial = bodyRenderer.sharedMaterial;
        renderer.sortingLayerID = bodyRenderer.sortingLayerID; renderer.sortingOrder = order;
        return renderer;
    }

    public void BeginSwing(Vector3 target, bool flaming)
    {
        if (shoulder == null) return;
        visual.FaceWorldDirection(target - transform.position);
        IsSwinging = true; elapsed = 0; fire = flaming; holdImpactFrame = false;
        fireTrail.Clear();
    }

    private void LateUpdate()
    {
        if (shoulder == null) return;
        mirrorRoot.localScale = new Vector3(bodyRenderer.flipX ? -1 : 1, 1, 1);
        armRenderer.color = bodyRenderer.color;
        if (forearmRenderer != null) forearmRenderer.color = bodyRenderer.color;
        if (visual.IsDefeated) { CancelSwing(); SetSwordColor(bodyRenderer.color); return; }
        if (!IsSwinging) { Pose(-15, 40); SetSwordColor(bodyRenderer.color); return; }
        if (holdImpactFrame) holdImpactFrame = false; else AnimateTo(elapsed + Time.deltaTime);
        SetSwordColor(fire ? Color.Lerp(bodyRenderer.color, new Color(1.6f, 0.85f, 0.3f),
            Mathf.Sin(Mathf.Clamp01(elapsed / Duration) * Mathf.PI)) : bodyRenderer.color);
        if (elapsed >= Duration) CancelSwing();
    }

    private void PoseAt(float time)
    {
        if (time < 0.28f)
        {
            float t = Mathf.SmoothStep(0, 1, time / 0.28f);
            Pose(Mathf.Lerp(-15, -90, t), Mathf.Lerp(40, 80, t));
        }
        else if (time < HitTime)
        {
            float t = Mathf.SmoothStep(0, 1, (time - 0.28f) / (HitTime - 0.28f));
            Pose(Mathf.Lerp(-90, -185, t), Mathf.Lerp(80, 90, t));
        }
        else if (time < HitTime + 0.08f)
        {
            float t = Mathf.SmoothStep(0, 1, (time - HitTime) / 0.08f);
            Pose(Mathf.Lerp(-185, -202, t), Mathf.Lerp(90, 102, t));
        }
        else
        {
            float t = Mathf.SmoothStep(0, 1, (time - HitTime - 0.08f) / (Duration - HitTime - 0.08f));
            Pose(Mathf.Lerp(-202, -15, t), Mathf.Lerp(102, 40, t));
        }
    }

    private void AnimateTo(float time)
    {
        float end = Mathf.Clamp(time, elapsed, Duration);
        int steps = Mathf.Clamp(Mathf.CeilToInt((end - elapsed) / 0.008f), 1, 64);
        // Sample the curved blade path even when one frame spans most of the swing.
        for (int i = 1; i <= steps; i++)
        {
            float sample = Mathf.Lerp(elapsed, end, i / (float)steps);
            PoseAt(sample);
            if (fire && sample >= 0.28f && sample <= HitTime + 0.04f)
                fireTrail.AddPosition(tip.position);
        }
        elapsed = end;
    }

    public void MarkImpact()
    {
        if (!IsSwinging || shoulder == null) return;
        AnimateTo(HitTime);
        elapsed = HitTime;
        holdImpactFrame = true;
        Pose(-185, 90);
        if (fire) SetSwordColor(new Color(1.6f, 0.85f, 0.3f));
        // Manual path samples avoid a straight, angular stripe between rendered frames.
    }

    private void SetSwordColor(Color color)
    {
        swordRenderer.color = color;
        // URP Unlit uses the material color rather than sprite vertex tint.
        swordMaterial.color = color;
    }

    private void Pose(float armAngle, float swordAngle)
    {
        shoulder.localRotation = Quaternion.Euler(0, 0, armAngle);
        sword.localRotation = Quaternion.Euler(0, 0, swordAngle);
    }
    public Vector3 GetTipPosition() => tip != null ? tip.position : transform.position;
    public void CancelSwing()
    {
        IsSwinging = false; holdImpactFrame = false;
        if (shoulder == null) return;
        Pose(-15, 40); SetSwordColor(bodyRenderer.color);
        fireTrail.emitting = false; fireTrail.Clear();
    }
    private void OnDisable() => CancelSwing();
    private void OnDestroy()
    {
        if (trailMaterial != null) Destroy(trailMaterial);
        if (swordMaterial != null) Destroy(swordMaterial);
    }
}
