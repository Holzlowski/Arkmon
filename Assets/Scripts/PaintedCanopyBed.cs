using UnityEngine;

// The 3D furniture stays visible in every pose; only the boy and cover change.
public class PaintedCanopyBed : MonoBehaviour
{
    [SerializeField] private GameObject model;
    [SerializeField] private SpriteRenderer sleepingIllustration;
    [SerializeField] private Transform[] sleepingCover;
    private Vector3[] coverPositions;
    private Vector3 sleepingPosition;
    public Vector3 BedsidePosition => transform.position + new Vector3(1.7f, 1f, -.35f);
    private SpriteRenderer seated;
    public bool IsSeated => seated != null && seated.enabled;
    public bool IsSleeping => sleepingIllustration != null && sleepingIllustration.enabled;
    private void Awake()
    {
        coverPositions = new Vector3[sleepingCover != null ? sleepingCover.Length : 0];
        for(int i=0;i<coverPositions.Length;i++)if(sleepingCover[i]!=null)coverPositions[i]=sleepingCover[i].localPosition;
        if(sleepingIllustration!=null)sleepingPosition=sleepingIllustration.transform.localPosition;
        SetSleeping(false);
    }
    public void SetSeated(bool visible)
    {
        if (visible && seated == null)
        {
            Sprite sprite = Resources.Load<Sprite>("Prince-Sitting"); if (sprite == null) return;
            var obj = new GameObject("Boy seated at bed edge", typeof(SpriteRenderer)); obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(1.15f, .12f, -.35f);
            seated = obj.GetComponent<SpriteRenderer>(); seated.sprite = sprite;
            if (sleepingIllustration != null) seated.sharedMaterial = sleepingIllustration.sharedMaterial;
            obj.transform.localScale = Vector3.one * (1.85f / sprite.bounds.size.y);
        }
        if (seated != null) seated.enabled = visible;
    }
    private void LateUpdate()
    {
        if (seated != null && seated.enabled && Camera.main != null) seated.transform.rotation = Camera.main.transform.rotation;
        if (IsSleeping) sleepingIllustration.transform.localPosition = sleepingPosition + Vector3.up * (Mathf.Sin(Time.unscaledTime*2)*.003f);
    }
    public void SetSleeping(bool sleeping)
    {
        if (model != null) model.SetActive(true);
        if (sleepingIllustration != null) sleepingIllustration.enabled = sleeping;
        if(coverPositions!=null)for(int i=0;i<coverPositions.Length;i++)
            if(sleepingCover[i]!=null)sleepingCover[i].localPosition=coverPositions[i]+Vector3.up*(sleeping?(i<3?.28f:.07f):0);
    }
}
