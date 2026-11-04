using UnityEngine;

[CreateAssetMenu(menuName="Arkmon/Courtyard Guard Route")]
public sealed class CourtyardGuardRoute : ScriptableObject
{
    [System.Serializable]
    public struct Stop
    {
        public Vector3 position;
        [Min(0)] public float wait;
        public bool lookLeft;
    }
    [Min(.1f)] public float speed=1.55f;
    public Stop[] stops;
}