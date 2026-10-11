using UnityEngine;

// Sprite animation stays on the camera-facing paper layer and never changes fire rules.
public class BurningVisual : MonoBehaviour
{
    private FireVisualStyle style;
    private SpriteRenderer owner;
    private SpriteRenderer[] flames;
    private float[] phases, rates;
    private bool plant;
    private float baseHeight;

    private void Awake()
    {
        style = Resources.Load<FireVisualStyle>("Paper Fire");
        if (style == null || style.frames == null || style.frames.Length == 0) return;
        owner = transform.parent.GetComponentInChildren<SpriteRenderer>();
        plant = transform.parent.GetComponent<BurningObstacle>() != null;
        flames = new SpriteRenderer[plant ? 3 : 2];
        phases = new float[flames.Length]; rates = new float[flames.Length];
        // Cosmetic variation does not consume the battle's random numbers.
        var random = new System.Random(GetInstanceID());
        baseHeight = plant ? 0.85f : 0.43f;
        for (int i = 0; i < flames.Length; i++)
        {
            var child = new GameObject("Animated paper flame", typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            flames[i] = child.GetComponent<SpriteRenderer>();
            flames[i].sprite = style.frames[0];
            flames[i].sharedMaterial = style.material;
            if (owner != null)
            {
                flames[i].sortingLayerID = owner.sortingLayerID;
                flames[i].sortingOrder = owner.sortingOrder + 4;
            }
            phases[i] = (float)random.NextDouble() * style.frames.Length;
            rates[i] = 0.85f + (float)random.NextDouble() * 0.3f;
        }
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (flames == null || camera == null || transform.parent == null) return;
        float ownerWidth = owner != null ? owner.sprite.bounds.size.x * owner.transform.lossyScale.x : 0.8f;
        float ownerHeight = owner != null ? owner.sprite.bounds.size.y * owner.transform.lossyScale.y : 1f;
        Vector3 anchor = owner != null ? owner.transform.position : transform.parent.position;
        for (int i = 0; i < flames.Length; i++)
        {
            int frame = Mathf.FloorToInt(Time.time * style.framesPerSecond * rates[i] + phases[i]) % style.frames.Length;
            flames[i].sprite = style.frames[frame];
            float spread = plant ? (ownerHeight > 2f ? 0.08f : 0.22f) : 0.3f;
            float horizontal = (i - (flames.Length - 1) * 0.5f) * ownerWidth * spread;
            float vertical = plant ? (i == 1 ? ownerHeight * 0.35f : ownerHeight * 0.08f) : ownerHeight * (i == 0 ? 0.1f : 0.32f);
            Transform flame = flames[i].transform;
            flame.position = anchor + camera.transform.right * horizontal + camera.transform.up * vertical
                - camera.transform.forward * 0.08f;
            flame.rotation = camera.transform.rotation;
            float flicker = 1f + Mathf.Sin(Time.time * 7f * rates[i] + phases[i]) * 0.025f;
            float scale = baseHeight * (i == 1 ? 1.12f : 1f) * flicker / style.frames[0].bounds.size.y;
            // Parent scale may differ between monsters and props; use a world-size flame.
            Vector3 parentScale = transform.lossyScale;
            flame.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
        }
    }
}
