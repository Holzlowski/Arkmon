using UnityEngine;

public class TowerRoomDoor : MonoBehaviour
{
    [SerializeField] private Transform hinge;
    public Vector3 ApproachPosition => transform.TransformPoint(new Vector3(-1.85f, 1, 0));
    public Vector3 OpeningStandPosition => transform.TransformPoint(new Vector3(-1.85f, 1, 1.45f));
    public Vector3 PassagePosition => transform.TransformPoint(new Vector3(-.1f, 1, 0));
    public float Openness { get; private set; }
    public void SetHinge(Transform value) => hinge = value;
    public void SetOpenness(float value)
    {
        Openness = Mathf.Clamp01(value);
        if (hinge != null) hinge.localRotation = Quaternion.Euler(0, -68 * Openness, 0);
    }
}
