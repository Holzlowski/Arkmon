using UnityEngine;

// Only the illustration turns. The obstacle's collider and grid position stay fixed.
[DisallowMultipleComponent]
public class PaperPropVisual : MonoBehaviour
{
    [SerializeField] private Transform paperPivot;

    private void OnEnable()
    {
        if (paperPivot != null) PaperGroundShadow.Attach(gameObject, paperPivot.GetComponent<Renderer>());
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (paperPivot != null && camera != null)
            paperPivot.rotation = camera.transform.rotation;
    }
}
