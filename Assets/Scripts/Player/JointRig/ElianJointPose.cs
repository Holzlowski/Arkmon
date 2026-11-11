using UnityEngine;

[CreateAssetMenu(menuName="Arkmon/Elian joint pose")]
public sealed class ElianJointPose : ScriptableObject
{
    public string label;
    public Vector2 pelvis = new Vector2(0,.62f);
    public float torsoDegrees;
    public float headDegrees;
    [Range(0,100)] public float tunicFold;
    public Vector2 rightSole = new Vector2(-.23f,0);
    public Vector2 leftSole = new Vector2(.23f,0);
    public Vector2 bucketHandle = new Vector2(-.42f,.72f);
    public float rightHandDegrees = 180;
    public bool supportsBucket;
    public Vector2 leftPalm = new Vector2(.27f,.72f);
    public float leftHandDegrees = -120;
}