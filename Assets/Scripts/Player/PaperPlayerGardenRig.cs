using UnityEngine;
using UnityEngine.Rendering;

// Fixed-length painted arm bones. Tools follow the wrist, with optional two-hand support.
public sealed class PaperPlayerGardenRig
{
    readonly Transform root, torso;
    readonly SpriteRenderer original, body, nearLeg, farLeg;
    readonly SpriteRenderer nearUpper, nearFore, farUpper, farFore;
    readonly Sprite family, room;
    readonly MeshRenderer bodySurface;
    readonly Material bodyMaterial;
    readonly Mesh familyMesh,roomMesh;
    readonly MaterialPropertyBlock bodyProperties=new MaterialPropertyBlock();
    readonly PaperArmBones nearBones,farBones;
    public static readonly Vector3 BucketSupportPoint=new Vector3(.15f,-.50f,0);
    SpriteRenderer borrowedCan;
    Sprite wholeNear,wholeFar;
    Transform canHome;
    Vector3 contactGrip;
    bool hasContact;
    SpriteRenderer contactItem;
    public bool Available => family != null && room != null && nearFore.sprite != null;
    public Vector3 GripPosition => borrowedCan!=null ? borrowedCan.transform.position : root.position;
    public Vector3 HandPosition => nearBones.HandPosition;
    public Vector3 SupportHandPosition => farBones.HandPosition;

    public PaperPlayerGardenRig(Transform pivot, SpriteRenderer original)
    {
        this.original=original;
        root=new GameObject("Elian garden paper rig").transform;root.SetParent(pivot,false);
        var group=root.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID=original.sortingLayerID;group.sortingOrder=original.sortingOrder;
        family=Resources.Load<Sprite>("Elian-Walk-Family");room=Resources.Load<Sprite>("Elian-Walk-Room");
        farUpper=Part("Far upper arm","Garden/UpperArm",0);farFore=Part("Far gripping hand","Garden/UpperArm",1);
        farLeg=Part("Far garden leg","Elian-Walk-FarLeg",1);nearLeg=Part("Near garden leg","Elian-Walk-NearLeg",2);
        body=Part("Garden torso","Elian-Walk-Family",3);torso=body.transform;
        body.enabled=false;
        var surface=new GameObject("Garden torso painted surface",typeof(MeshFilter),typeof(MeshRenderer));surface.transform.SetParent(torso,false);
        bodySurface=surface.GetComponent<MeshRenderer>();bodyMaterial=PaperPlayerWalkRig.CreatePaperMaterial();bodySurface.sharedMaterial=bodyMaterial;
        bodySurface.sortingLayerID=original.sortingLayerID;bodySurface.sortingOrder=original.sortingOrder+3;
        bodySurface.shadowCastingMode=ShadowCastingMode.Off;bodySurface.receiveShadows=false;
        familyMesh=PaperPlayerWalkRig.CreatePaintedMesh(family,"Body");roomMesh=PaperPlayerWalkRig.CreatePaintedMesh(room,"Body");
        nearUpper=Part("Near upper arm","Garden/UpperArm",8);nearFore=Part("Near gripping hand","Garden/UpperArm",7);
        wholeNear=WholeArm(nearFore.sprite);wholeFar=WholeArm(nearFore.sprite);
        nearFore.sprite=wholeNear;farFore.sprite=wholeFar;
        nearUpper.enabled=farUpper.enabled=false;
        nearBones=new PaperArmBones(nearFore,bodyMaterial,"Near");farBones=new PaperArmBones(farFore,bodyMaterial,"Far");
        root.gameObject.SetActive(false);
    }
    static Sprite WholeArm(Sprite source)
    {
        // Use the handless sleeve drawing. Neither old fist is part of the arm mesh.
        var rect=source.rect;float height=Mathf.Round(rect.height/.88f);rect.y-=height-rect.height;rect.height=height;
        var sprite=Sprite.Create(source.texture,rect,new Vector2(.52f,.90f),source.pixelsPerUnit*.75f,0,SpriteMeshType.FullRect);
        sprite.name="Garden sleeve whole shoulder to wrist";return sprite;
    }
    SpriteRenderer Part(string name,string resource,int order)
    {
        var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(root,false);
        var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=Resources.Load<Sprite>(resource);
        renderer.sharedMaterial=original.sharedMaterial;renderer.sortingLayerID=original.sortingLayerID;
        renderer.sortingOrder=original.sortingOrder+order;return renderer;
    }
    public void Present(bool familyOutfit,bool flip,bool walking,float phase,float bend,float tilt,float reach,
        SpriteRenderer can,bool attached,Vector3? contact,Vector3? gripOverride = null,float weight = -1,float support = 0, float freeArmSwing = 0, Vector3? liftedHandle = null, Vector3? lowerSupport = null)
    {
        root.gameObject.SetActive(Available);if(!Available)return;
        original.forceRenderingOff=true;
        float size=2f/2.07f;
        root.localScale=new Vector3(flip?-size:size,size,1);
        body.sprite=familyOutfit?family:room;
        bodySurface.GetComponent<MeshFilter>().sharedMesh=familyOutfit?familyMesh:roomMesh;
        bodyProperties.SetTexture("_BaseMap",body.sprite.texture);bodyProperties.SetColor("_BaseColor",original.color);bodySurface.SetPropertyBlock(bodyProperties);
        if(weight < 0) weight=walking?1:0;
        float bob=.006f*weight*(1-Mathf.Cos(phase*2));
        torso.localPosition=new Vector3(.04f*bend,.65f-.115f*bend+bob,-.015f);
        torso.localRotation=Quaternion.Euler(0,0,-12*bend);
        root.localPosition=Vector3.zero;
        PaperPlayerLegPose.Present(nearLeg,farLeg,root,phase,weight,bend);
        foreach(var part in new[]{body,nearLeg,farLeg,nearUpper,nearFore,farUpper,farFore})part.color=original.color;
        farLeg.color=farUpper.color=farFore.color=original.color*new Color(.88f,.88f,.88f,1);

        if(contactItem!=can){hasContact=false;contactItem=can;}
        if(contact.HasValue){contactGrip=root.InverseTransformPoint(contact.Value);hasContact=true;}
        var extent=body.sprite.rect.size/body.sprite.pixelsPerUnit;
        Vector3 shoulder=root.InverseTransformPoint(torso.TransformPoint(new Vector3(.22f*extent.x,.44f*extent.y,0)));
        Vector3 grip=shoulder+Vector3.Lerp(new Vector3(.14f,-.43f,0),new Vector3(.30f,-.05f,0),Mathf.Clamp01(tilt/70));
        if(!attached)
        {
            contactGrip=root.InverseTransformPoint(can.transform.position);hasContact=true;
            grip=Vector3.Lerp(grip,contactGrip,reach);
        }
        else if(hasContact && bend>.001f)grip=Vector3.Lerp(grip,contactGrip,bend);
        if(gripOverride.HasValue)grip=root.InverseTransformPoint(gripOverride.Value);
        float tip=Mathf.Clamp01(tilt/70);
        if(support>0)
        {
            var lifted=liftedHandle ?? new Vector3(Mathf.Lerp(.08f,.46f,tip),Mathf.Lerp(1.37f,1.36f,tip)+.05f*Mathf.Sin(tip*Mathf.PI),shoulder.z);
            grip=Vector3.Lerp(grip,lifted,support);
        }
        // Both bone lengths stay fixed. The vessel follows the solved wrist.
        grip=nearBones.Pose(shoulder,grip,180-tilt-45*support,original.color,original.sortingOrder+4,original.sortingOrder+5,original.sortingOrder+7);
        if(attached)
        {
            if (borrowedCan != can)
            {
                ReleaseCan(); borrowedCan=can; canHome=can.transform.parent;
                can.transform.SetParent(root,true);
            }
            can.transform.position=root.TransformPoint(grip);
            can.transform.localRotation=Quaternion.Euler(0,0,-tilt);
            can.transform.localScale=Vector3.one;
        }
        else ReleaseCan();
        can.sortingLayerID=original.sortingLayerID;
        can.sortingOrder=attached ? original.sortingOrder+6 : original.sortingOrder;
        Vector3 farShoulder=root.InverseTransformPoint(torso.TransformPoint(new Vector3(-.16f*extent.x,.44f*extent.y,0)));
        Vector3 restHand=farShoulder+new Vector3(-.025f+freeArmSwing,-.48f,0);
        float assisting=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,.95f,support));
        Vector3 lowerGrip=root.InverseTransformPoint(can.transform.TransformPoint(lowerSupport ?? BucketSupportPoint));
        var farTarget=Vector3.Lerp(restHand,lowerGrip,assisting);
        farBones.Pose(farShoulder,farTarget,Mathf.LerpAngle(180,-90-tilt,assisting),original.color,
            original.sortingOrder+(assisting>.01f?4:0),original.sortingOrder+(assisting>.01f?5:0),original.sortingOrder+(assisting>.01f?7:0));

    }
    void ReleaseCan()
    {
        if(borrowedCan!=null)
        {
            // Unity forbids reparenting children while a scene/ancestor is deactivating.
            // Keep ownership until normal presentation resumes; scene teardown owns destruction.
            if(root==null || !root.gameObject.activeInHierarchy || canHome==null || !canHome.gameObject.activeInHierarchy)return;
            borrowedCan.transform.SetParent(canHome,true);
            borrowedCan.sortingLayerID=original.sortingLayerID;
            borrowedCan.sortingOrder=original.sortingOrder;
        }
        borrowedCan=null;canHome=null;
    }
    public void Hide(){ReleaseCan();if(root!=null && root.gameObject.activeSelf)root.gameObject.SetActive(false);}
    public void Dispose(){Hide();if(root!=null)Object.Destroy(root.gameObject);if(wholeNear!=null)Object.Destroy(wholeNear);if(wholeFar!=null)Object.Destroy(wholeFar);nearBones.Dispose();farBones.Dispose();Object.Destroy(familyMesh);Object.Destroy(roomMesh);Object.Destroy(bodyMaterial);}
}