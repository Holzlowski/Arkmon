using UnityEngine;

// Camera-facing cut-paper limbs; the two-bone arms follow actual handle/support points.
public sealed class PaperPlayerGardenRig
{
    readonly Transform root, torso;
    readonly SpriteRenderer original, body, nearLeg, farLeg;
    readonly SpriteRenderer nearUpper, nearFore, farUpper, farFore;
    readonly Sprite family, room;
    public bool Available => family != null && room != null && nearFore.sprite != null;
    public Vector3 GripPosition { get; private set; }
    public Vector3 HandPosition { get; private set; }

    public PaperPlayerGardenRig(Transform pivot, SpriteRenderer original)
    {
        this.original=original;
        root=new GameObject("Elian garden paper rig").transform;root.SetParent(pivot,false);
        family=Resources.Load<Sprite>("Garden/Family");room=Resources.Load<Sprite>("Garden/Room");
        farUpper=Part("Far upper arm","Garden/UpperArm",0);farFore=Part("Far gripping hand","Garden/FarForearm",1);
        farLeg=Part("Far garden leg","Elian-Walk-FarLeg",1);nearLeg=Part("Near garden leg","Elian-Walk-NearLeg",2);
        body=Part("Garden torso","Garden/Family",3);torso=body.transform;
        nearUpper=Part("Near upper arm","Garden/UpperArm",8);nearFore=Part("Near gripping hand","Garden/Forearm",7);
        root.gameObject.SetActive(false);
    }
    SpriteRenderer Part(string name,string resource,int order)
    {
        var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(root,false);
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=Resources.Load<Sprite>(resource);
        renderer.sharedMaterial=original.sharedMaterial;renderer.sortingLayerID=original.sortingLayerID;
        renderer.sortingOrder=original.sortingOrder+order;return renderer;
    }
    public void Present(bool familyOutfit,bool flip,bool walking,float phase,float bend,float tilt,float reach,
        SpriteRenderer can,bool attached)
    {
        root.gameObject.SetActive(Available);if(!Available)return;
        original.forceRenderingOff=true;
        float size=original.sprite.bounds.size.y/2.07f;
        root.localScale=new Vector3(flip?-size:size,size,1);
        body.sprite=familyOutfit?family:room;
        float bob=walking?.012f*(1-Mathf.Cos(phase*2)):0;
        torso.localPosition=new Vector3(.04f*bend,.65f-.35f*bend+bob,-.015f);
        torso.localRotation=Quaternion.Euler(0,0,-40*bend);
        Leg(nearLeg,walking?phase:0,bend,.055f,-.01f);
        Leg(farLeg,walking?phase+Mathf.PI:0,bend,-.055f,.005f);
        foreach(var part in new[]{body,nearLeg,farLeg,nearUpper,nearFore,farUpper,farFore})part.color=original.color;
        farLeg.color=farUpper.color=farFore.color=original.color*new Color(.88f,.88f,.88f,1);

        Vector3 grip=Vector3.Lerp(new Vector3(.25f,1.04f+bob,0),new Vector3(.65f,.60f,0),bend);
        if(!attached)grip=Vector3.Lerp(new Vector3(.20f,.92f,0),root.InverseTransformPoint(can.transform.position),reach);
        if(attached)
        {
            can.transform.position=root.TransformPoint(grip);
            can.transform.rotation=root.rotation*Quaternion.Euler(0,0,(flip?1:-1)*tilt);
            can.transform.localScale=new Vector3(flip?-1:1,1,1)*Mathf.Abs(root.lossyScale.y);
        }
        can.sortingLayerID=original.sortingLayerID;can.sortingOrder=original.sortingOrder+6;
        var support=root.InverseTransformPoint(can.transform.TransformPoint(new Vector3(-.03f,-.30f,0)));
        if(!attached)support=Vector3.Lerp(new Vector3(.29f,.88f,0),support,reach);
        Vector3 shoulder= root.InverseTransformPoint(torso.TransformPoint(new Vector3(-.19f,.57f,0)));
        Vector3 farShoulder=root.InverseTransformPoint(torso.TransformPoint(new Vector3(.20f,.56f,0)));
        HandPosition=root.TransformPoint(Arm(nearUpper,nearFore,shoulder,grip,1));
        Arm(farUpper,farFore,farShoulder,support,-1);
        GripPosition=can.transform.position;
    }
    void Leg(SpriteRenderer leg,float phase,float bend,float hip,float z)
    {
        float stride=Mathf.Sin(phase);
        leg.transform.localPosition=new Vector3(hip,.75f-.22f*bend+Mathf.Abs(stride)*.055f,z);
        leg.transform.localScale=new Vector3(1,1-.29f*bend,1);
        leg.transform.localRotation=Quaternion.Euler(0,0,stride*(stride<0?45:25)+(hip>0?-14:12)*bend);
        float lift=root.position.y+.025f-leg.bounds.min.y;
        if(lift>0)leg.transform.position+=Vector3.up*lift;
    }
    static Vector2 End(Sprite sprite,Vector2 normalized)=>(normalized*sprite.rect.size-sprite.pivot)/sprite.pixelsPerUnit;
    static Vector3 Arm(SpriteRenderer upper,SpriteRenderer fore,Vector3 shoulder,Vector3 target,float elbowSide)
    {
        Vector2 upperEnd=End(upper.sprite,new Vector2(.43f,.01f));
        Vector2 foreEnd=End(fore.sprite,new Vector2(.68f,.27f));
        float a=upperEnd.magnitude,b=foreEnd.magnitude;
        Vector2 delta=(Vector2)(target-shoulder);float distance=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);
        Vector2 direction=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.down;
        float along=(a*a-b*b+distance*distance)/(2*distance);
        float across=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Vector3 elbow=shoulder+(Vector3)(direction*along+new Vector2(-direction.y,direction.x)*across*elbowSide);
        Vector3 hand=shoulder+(Vector3)(direction*distance);
        PlaceSegment(upper.transform,shoulder,elbow,upperEnd);
        PlaceSegment(fore.transform,elbow,hand,foreEnd);
        return hand;
    }
    static void PlaceSegment(Transform part,Vector3 from,Vector3 to,Vector2 source)
    {
        part.localPosition=from;
        part.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg-Mathf.Atan2(source.y,source.x)*Mathf.Rad2Deg);
    }
    public void Hide(){if(root!=null)root.gameObject.SetActive(false);}
    public void Dispose(){if(root!=null)Object.Destroy(root.gameObject);}
}