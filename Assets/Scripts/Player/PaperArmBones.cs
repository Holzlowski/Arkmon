using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Fixed upper/forearm lengths. The correctly handed fist is a rigid sprite at
// the wrist; it never participates in sleeve deformation.
public sealed class PaperArmBones
{
    readonly Transform anchor,parent,elbowJoint,wristJoint;
    readonly Mesh[] meshes=new Mesh[2];readonly MeshRenderer[] surfaces=new MeshRenderer[2];
    readonly SpriteRenderer hand;
    readonly Sprite sleeve;
    readonly Mesh rigidHandMesh;readonly MeshRenderer handSurface;
    readonly MaterialPropertyBlock handProperties=new MaterialPropertyBlock();
    [System.Serializable] sealed class HandGeometry {public Vector2[] vertices;public int[] triangles;}
    readonly Vector3[] rest,posed;readonly float[] upperWeight;
    readonly Vector2 sourceElbow,sourceWrist,handContact;
    readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    readonly Material sleeveMaterial;
    public float UpperLength { get; }
    public float ForearmLength { get; }
    public Vector3 HandPosition=>hand.transform.TransformPoint(handContact);
    public PaperArmBones(SpriteRenderer original,Material material,string side)
    {
        sleeve=original.sprite;anchor=original.transform;parent=anchor.parent;original.enabled=false;
        Vector2 Point(Vector2 n)=>Vector2.Scale(n*sleeve.rect.size-sleeve.pivot,new Vector2(.70f,1))/sleeve.pixelsPerUnit;
        sourceElbow=Point(new Vector2(.50f,.38f));sourceWrist=Point(new Vector2(.50f,.08f));
        UpperLength=sourceElbow.magnitude;ForearmLength=(sourceWrist-sourceElbow).magnitude;
        elbowJoint=new GameObject(side+" elbow joint").transform;elbowJoint.SetParent(parent,false);
        wristJoint=new GameObject(side+" wrist joint").transform;wristJoint.SetParent(parent,false);
        var handObject=new GameObject(side+" anatomical "+(side=="Near"?"left":"right")+" hand",typeof(SpriteRenderer));handObject.transform.SetParent(wristJoint,false);
        hand=handObject.GetComponent<SpriteRenderer>();hand.sprite=Resources.Load<Sprite>("ElianHands/"+(side=="Near"?"LeftGrip":"RightGrip"));
        if(hand.sprite==null)throw new System.InvalidOperationException("Import Elian's distinct hand art first.");
        var asset=hand.sprite;hand.enabled=false;
        var outline=Resources.Load<TextAsset>("ElianHands/"+(side=="Near"?"LeftGrip":"RightGrip")+"-outline");
        var geometry=JsonUtility.FromJson<HandGeometry>(outline.text);
        var handVertices=new Vector3[geometry.vertices.Length];var handUV=new Vector2[geometry.vertices.Length];
        for(int i=0;i<handVertices.Length;i++)
        {
            handVertices[i]=geometry.vertices[i];
            var pixel=geometry.vertices[i]*asset.pixelsPerUnit+asset.pivot+asset.rect.position;
            handUV[i]=new Vector2(pixel.x/asset.texture.width,pixel.y/asset.texture.height);
        }
        var art=new GameObject(side+" rigid hand art",typeof(MeshFilter),typeof(MeshRenderer));art.transform.SetParent(hand.transform,false);
        rigidHandMesh=new Mesh{name=asset.name+" rigid outline"};rigidHandMesh.vertices=handVertices;rigidHandMesh.uv=handUV;rigidHandMesh.triangles=geometry.triangles;rigidHandMesh.RecalculateBounds();
        art.GetComponent<MeshFilter>().sharedMesh=rigidHandMesh;handSurface=art.GetComponent<MeshRenderer>();handSurface.sharedMaterial=material;
        handSurface.sortingLayerID=original.sortingLayerID;handSurface.shadowCastingMode=ShadowCastingMode.Off;
        handProperties.SetTexture("_BaseMap",asset.texture);
        handContact=(new Vector2(.50f,.60f)*hand.sprite.rect.size-hand.sprite.pivot)/hand.sprite.pixelsPerUnit;
        sleeveMaterial=new Material(Resources.Load<Shader>("PaintedArm"));
        const int cols=14,rows=32;rest=new Vector3[(cols+1)*(rows+1)];posed=new Vector3[rest.Length];upperWeight=new float[rest.Length];
        var uv=new Vector2[rest.Length];var colors=new Color32[rest.Length];var triangles=new[]{new List<int>(),new List<int>()};
        for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
        {
            int k=y*(cols+1)+x;float u=x/(float)cols,w=Mathf.Lerp(.06f,1,y/(float)rows);
            colors[k]=new Color32(255,255,255,(byte)Mathf.RoundToInt(255*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.83f,.96f,w)))));
            rest[k]=Point(new Vector2(u,w));upperWeight[k]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.58f,w));
            uv[k]=new Vector2((sleeve.rect.x+u*sleeve.rect.width)/sleeve.texture.width,(sleeve.rect.y+w*sleeve.rect.height)/sleeve.texture.height);
            if(x==cols || y==rows)continue;
            float band=Mathf.Lerp(.06f,1,(y+.5f)/rows);var face=new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2};
            if(band>=.34f)triangles[0].AddRange(face);if(band<=.43f)triangles[1].AddRange(face);
        }
        for(int i=0;i<2;i++)
        {
            var go=new GameObject(side+(i==0?" painted upper arm":" painted forearm"),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(anchor,false);
            meshes[i]=new Mesh{name=go.name};meshes[i].MarkDynamic();meshes[i].vertices=rest;meshes[i].uv=uv;meshes[i].colors32=colors;meshes[i].SetTriangles(triangles[i],0);
            go.GetComponent<MeshFilter>().sharedMesh=meshes[i];surfaces[i]=go.GetComponent<MeshRenderer>();surfaces[i].sharedMaterial=sleeveMaterial;
            surfaces[i].sortingLayerID=original.sortingLayerID;surfaces[i].shadowCastingMode=ShadowCastingMode.Off;surfaces[i].receiveShadows=false;
        }
    }
    static Vector2 Rotate(Vector2 v,float radians){float c=Mathf.Cos(radians),s=Mathf.Sin(radians);return new Vector2(c*v.x-s*v.y,s*v.x+c*v.y);}
    static float Angle(Vector2 v)=>Mathf.Atan2(v.y,v.x);
    public Vector3 Pose(Vector3 shoulder,Vector3 target,float handDegrees,Color color,int order,int foreOrder,int handOrder)
    {
        Vector2 offset=Vector2.zero,wrist=Vector2.zero,elbow=Vector2.zero;
        void Solve()
        {
            offset=Rotate(handContact,handDegrees*Mathf.Deg2Rad);
            Vector2 delta=(Vector2)(target-shoulder)-offset;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(UpperLength-ForearmLength)+.002f,UpperLength+ForearmLength-.002f);
            Vector2 direction=delta.sqrMagnitude>.000001f?delta.normalized:Vector2.down;
            wrist=(Vector2)shoulder+direction*distance;
            float along=(UpperLength*UpperLength-ForearmLength*ForearmLength+distance*distance)/(2*distance);
            float across=Mathf.Sqrt(Mathf.Max(0,UpperLength*UpperLength-along*along));
            elbow=(Vector2)shoulder+direction*along-new Vector2(-direction.y,direction.x)*across;
        }
        // A handle must not force the palm backwards relative to the forearm.
        for(int i=0;i<5;i++)
        {
            Solve();float fore=Angle(wrist-elbow)*Mathf.Rad2Deg;
            float palm=handDegrees+Angle(handContact)*Mathf.Rad2Deg;
            float flex=Mathf.DeltaAngle(fore,palm);
            if(Mathf.Abs(flex)<=40.01f)break;
            handDegrees=fore+Mathf.Clamp(flex,-40,40)-Angle(handContact)*Mathf.Rad2Deg;
        }
        Solve();
        anchor.localPosition=shoulder;anchor.localRotation=Quaternion.identity;anchor.localScale=Vector3.one;
        elbowJoint.localPosition=new Vector3(elbow.x,elbow.y,shoulder.z);wristJoint.localPosition=new Vector3(wrist.x,wrist.y,shoulder.z);
        wristJoint.localRotation=Quaternion.Euler(0,0,handDegrees);handSurface.sortingOrder=handOrder;handProperties.SetColor("_BaseColor",color);handSurface.SetPropertyBlock(handProperties);
        float upper=Angle(elbow-(Vector2)shoulder)-Angle(sourceElbow),lower=Angle(wrist-elbow)-Angle(sourceWrist-sourceElbow);
        for(int i=0;i<posed.Length;i++)
        {
            Vector2 p=rest[i];var a=Rotate(p,upper);var b=elbow-(Vector2)shoulder+Rotate(p-sourceElbow,lower);
            posed[i]=Vector2.Lerp(b,a,upperWeight[i]);
        }
        properties.SetTexture("_BaseMap",sleeve.texture);properties.SetColor("_BaseColor",color);
        int[] orders={order,foreOrder};for(int i=0;i<2;i++){meshes[i].vertices=posed;meshes[i].RecalculateBounds();surfaces[i].sortingOrder=orders[i];surfaces[i].SetPropertyBlock(properties);}
        Vector2 grip=wrist+offset;return new Vector3(grip.x,grip.y,shoulder.z);
    }
    public void Dispose(){foreach(var mesh in meshes)Object.Destroy(mesh);Object.Destroy(rigidHandMesh);Object.Destroy(sleeveMaterial);}
}
