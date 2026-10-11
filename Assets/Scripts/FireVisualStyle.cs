using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Fire Visual Style")]
public class FireVisualStyle : ScriptableObject
{
    public Sprite[] frames;
    public Material material;
    [Min(1f)] public float framesPerSecond = 12f;
}
