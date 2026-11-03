using UnityEngine;

public sealed class CourtyardSceneryMotion : MonoBehaviour
{
    private Vector3 origin;

    private void Awake() => origin = transform.localPosition;

    private void Update()
    {
        if (gameObject.name == "Sunlit pollen")
        {
            float phase = Time.time * .55f + origin.x * 2 + origin.z;
            transform.localPosition = origin + new Vector3(Mathf.Sin(phase) * .55f,
                Mathf.Sin(phase * 1.7f) * .21f, Mathf.Cos(phase) * .25f);
        }
        else if (gameObject.name == "Windblown grass")
        {
            transform.localRotation = Quaternion.Euler(0, 0, 9f + Mathf.Sin(Time.time * 2 - origin.x) * 8f);
        }
        else if (gameObject.name == "Stream surface glint")
        {
            float downstream = Mathf.Repeat(origin.z + 17f - Time.time * .8f, 36f) - 17f;
            transform.localPosition = new Vector3(origin.x, origin.y, downstream);
        }
        else
        {
            float sway = Mathf.Sin(Time.time * 1.4f + origin.x) * .055f;
            transform.localPosition = origin + Vector3.right * sway;
        }
    }
}
