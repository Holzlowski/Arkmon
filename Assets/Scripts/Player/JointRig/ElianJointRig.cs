using System;
using UnityEngine;

// Authorable native Transform chains + Unity SkinnedMeshRenderers. No gait, tween or quest code.
[ExecuteAlways]
public sealed class ElianJointRig : MonoBehaviour
{
    [Serializable] public sealed class Chain
    {
        public Transform first, middle, tip;
        public Vector2 bindFirst, bindSecond;
        public float soleLift;
        public SkinnedMeshRenderer surface;
        public float FirstLength => bindFirst.magnitude;
        public float SecondLength => bindSecond.magnitude;
        public Vector3 Solve(Transform space, Vector2 goal, bool positiveBend, float tipDegrees)
        {
            Vector2 start=space.InverseTransformPoint(first.position);
            Vector2 delta=goal-start;
            float a=FirstLength,b=SecondLength;
            float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
            Vector2 direction=delta.sqrMagnitude>.000001f?delta.normalized:Vector2.down;
            float along=(a*a-b*b+d*d)/(2*d);
            float cross=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector2 joint=start+direction*along+new Vector2(-direction.y,direction.x)*cross*(positiveBend?1:-1);
            Vector2 end=start+direction*d;
            float Angle(Vector2 v)=>Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg;
            first.rotation=space.rotation*Quaternion.Euler(0,0,Angle(joint-start)-Angle(bindFirst));
            middle.rotation=space.rotation*Quaternion.Euler(0,0,Angle(end-joint)-Angle(bindSecond));
            tip.rotation=space.rotation*Quaternion.Euler(0,0,tipDegrees);
            return space.TransformPoint(end);
        }
    }
    [Serializable] public sealed class HandDrawing
    {
        public Mesh mesh;
        public Material material;
        public Vector2 palm;
    }
    public Transform pelvis,torso,head;
    public SkinnedMeshRenderer garment;
    public Chain rightLeg,leftLeg,rightArm,leftArm;
    public Transform rightHand,leftHand;
    public MeshFilter rightHandArt,leftHandArt;
    public MeshRenderer rightHandSurface,leftHandSurface;
    public HandDrawing rightGrip,leftRelaxed,leftSupport;
    public SpriteRenderer bucket;
    public Vector2 bucketBase = new Vector2(0,-.552f);
    public ElianJointPose pose;
    public bool showJoints;
    public float RightReachError { get; private set; }
    public float LeftReachError { get; private set; }
    public Vector3 RightPalm => rightHand.TransformPoint(rightGrip.palm);
    public Vector3 LeftPalm => leftHand.TransformPoint((pose!=null&&pose.supportsBucket?leftSupport:leftRelaxed).palm);
    public float RightWristAngle => Vector2.Angle(rightArm.tip.position-rightArm.middle.position,RightPalm-rightArm.tip.position);
    public float LeftWristAngle => Vector2.Angle(leftArm.tip.position-leftArm.middle.position,LeftPalm-leftArm.tip.position);
    public Vector3 SupportContact => bucket.transform.TransformPoint(bucketBase);

    static Vector2 Rotate(Vector2 v,float angle)=>Quaternion.Euler(0,0,angle)*v;
    public void ApplyPose(ElianJointPose value)
    {
        if(value==null||pelvis==null)return;
        pose=value;
        if(garment!=null)garment.SetBlendShapeWeight(0,value.tunicFold);
        pelvis.localPosition=value.pelvis;
        torso.localRotation=Quaternion.Euler(0,0,value.torsoDegrees);
        head.rotation=transform.rotation*Quaternion.Euler(0,0,value.headDegrees);
        rightLeg.Solve(transform,value.rightSole+Vector2.up*rightLeg.soleLift,true,0);
        leftLeg.Solve(transform,value.leftSole+Vector2.up*leftLeg.soleLift,true,0);
        SetHand(rightHandArt,rightHandSurface,rightGrip);
        Vector2 wrist=value.bucketHandle-Rotate(rightGrip.palm,value.rightHandDegrees);
        rightArm.Solve(transform,wrist,false,value.rightHandDegrees);
        bucket.transform.localRotation=Quaternion.identity;
        bucket.transform.localPosition=transform.InverseTransformPoint(RightPalm);
        bucket.transform.localScale=Vector3.one;
        RightReachError=Vector2.Distance(transform.InverseTransformPoint(RightPalm),value.bucketHandle);
        var drawing=value.supportsBucket?leftSupport:leftRelaxed;
        SetHand(leftHandArt,leftHandSurface,drawing);
        Vector2 leftContact=value.supportsBucket?(Vector2)transform.InverseTransformPoint(SupportContact):value.leftPalm;
        leftArm.Solve(transform,leftContact-Rotate(drawing.palm,value.leftHandDegrees),!value.supportsBucket,value.leftHandDegrees);
        LeftReachError=Vector2.Distance(transform.InverseTransformPoint(LeftPalm),leftContact);
    }
    static void SetHand(MeshFilter filter,MeshRenderer renderer,HandDrawing drawing)
    {
        filter.sharedMesh=drawing.mesh;renderer.sharedMaterial=drawing.material;
    }
    void OnEnable(){if(pose!=null)ApplyPose(pose);}
    void OnValidate(){if(pose!=null&&rightLeg!=null&&rightLeg.first!=null)ApplyPose(pose);}
    public Transform[] Joints => new[]{rightLeg.first,rightLeg.middle,rightLeg.tip,leftLeg.first,leftLeg.middle,leftLeg.tip,
        rightArm.first,rightArm.middle,rightArm.tip,leftArm.first,leftArm.middle,leftArm.tip,pelvis,head};
    public void OnDrawGizmos()
    {
        if(!showJoints||pelvis==null)return;
        Gizmos.color=new Color(.12f,.7f,.85f);
        foreach(var chain in new[]{rightLeg,leftLeg,rightArm,leftArm})
        {
            if(chain.first==null)continue;
            Gizmos.DrawLine(chain.first.position,chain.middle.position);Gizmos.DrawLine(chain.middle.position,chain.tip.position);
            foreach(var point in new[]{chain.first,chain.middle,chain.tip})Gizmos.DrawSphere(point.position,.018f);
        }
        Gizmos.color=Color.yellow;Gizmos.DrawLine(pelvis.position,head.position);
    }
}