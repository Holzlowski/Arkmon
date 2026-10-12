using UnityEngine;

// Decorative clouds only: no collision or effect on battle rules.
public class CloudDrift : MonoBehaviour
{
    [SerializeField] private float speed = 0.6f;
    [SerializeField] private float halfWidth = 120f;

    private void Update()
    {
        Vector3 position = transform.localPosition;
        position.x = Mathf.Repeat(position.x + speed * Time.deltaTime + halfWidth,
            halfWidth * 2f) - halfWidth;
        transform.localPosition = position;
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = camera.transform.rotation;
    }
}
