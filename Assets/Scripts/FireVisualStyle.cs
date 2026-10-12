using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Fire Visual Style")]
public class FireVisualStyle : ScriptableObject
{
    public Sprite[] frames;
    public Material material;
    public Material swordTrailMaterial;
    [Min(1f)] public float framesPerSecond = 12f;
}
