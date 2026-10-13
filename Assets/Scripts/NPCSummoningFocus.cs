using UnityEngine;
using System.Linq;

// Origin follows the charm drawn into the NPC sprite; no separate orb geometry.
public class NPCSummoningFocus : MonoBehaviour
{
    private SpriteRenderer body;
    public Vector3 SummonOrigin
    {
        get
        {
            Camera camera = Camera.main;
            if (body == null || camera == null) return transform.position + Vector3.up;
            PaperCharacterVisual visual = GetComponent<PaperCharacterVisual>();
            if (visual != null && visual.IsSummoningPose) return visual.GetSummonOrigin();
            float side = body.flipX ? 1f : -1f;
            return body.bounds.center + camera.transform.right * side * body.bounds.size.x * .22f
                - camera.transform.up * body.bounds.size.y * .12f - camera.transform.forward * .07f;
        }
    }
    private void Start() => body = GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.enabled && r.sprite != null);
}
