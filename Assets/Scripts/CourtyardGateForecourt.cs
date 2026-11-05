using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// A small, usable pause beside the eastern lane. Arrival scripting is deliberately separate.
public sealed class CourtyardGateForecourt : MonoBehaviour
{
    public static Vector3 Center => new Vector3(57.3f,0,1.15f);
    public static bool IsBareGround(Vector3 point)
    {
        var d=point-Center;
        return d.x*d.x/(4.8f*4.8f)+d.z*d.z/(3.6f*3.6f)<1;
    }
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    readonly List<Transform> plants=new List<Transform>();
    Material earth,oak,stone;

    internal static void Install()
    {
        if(GameObject.Find("Eastern gate forecourt")!=null || GameObject.Find("Far stream bank")==null)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");
        var wood=Resources.Load<Material>("CourtyardStyle/Stable oak");
        var rock=Resources.Load<Material>("CourtyardStyle/Plaster");
        if(source==null || wood==null || rock==null)return;
        var style=new GameObject("Eastern gate forecourt").AddComponent<CourtyardGateForecourt>();
        style.earth=new Material(source){name="Gate forecourt worn earth"};style.materials.Add(style.earth);
        style.earth.SetColor("_BaseColor",new Color(.68f,.74f,.77f,.72f));
        style.oak=wood;style.stone=new Material(rock){name="Forecourt weathered limestone"};style.materials.Add(style.stone);
        style.stone.SetColor("_BaseColor",new Color(.70f,.68f,.57f));
        Physics.SyncTransforms();style.GroundPatch();style.Sign();style.Margins();
        style.Marker(new Vector3(53.7f,0,-4.5f),-8);
        style.Marker(new Vector3(61.2f,0,4.5f),12);
    }
    static Vector3 Ground(Vector3 at)
    {
        float y=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(at.x,15,at.z),Vector3.down,30,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.8f)y=Mathf.Max(y,hit.point.y);
        at.y=float.IsNegativeInfinity(y)?0:y;return at;
    }
    void GroundPatch()
    {
        const int rings=10,sides=64;var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings;
            float edge=1+.055f*Mathf.Sin(a*3)+.035f*Mathf.Cos(a*5);
            v.Add(Ground(Center+new Vector3(Mathf.Cos(a)*5.3f,0,Mathf.Sin(a)*4.0f)*r*edge)+Vector3.up*.035f);
            colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;int n=ring*sides+side,p=n-sides,next=(side+1)%sides;
            t.AddRange(new[]{p,ring*sides+next,n,p,(ring-1)*sides+next,ring*sides+next});
        }
        var mesh=new Mesh{name="Irregular gate forecourt earth"};meshes.Add(mesh);
        mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var obj=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=earth;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Sign()
    {
        var root=new GameObject("Aurenfeld exterior waymarker").transform;root.SetParent(transform,false);
        root.position=Ground(new Vector3(54.6f,0,5.15f));root.rotation=Quaternion.Euler(0,38,0);
        Box(root,"Waymarker oak post",new Vector3(0,.68f,0),new Vector3(.16f,1.36f,.16f),oak,true);
        Box(root,"Waymarker painted board",new Vector3(0,1.17f,0),new Vector3(2.05f,.46f,.12f),oak,false);
        // A modest pointed end reads toward the castle, without another floating UI prompt.
        var arrow=Box(root,"Waymarker arrow",new Vector3(-1.0f,1.17f,0),new Vector3(.32f,.32f,.115f),oak,false);
        arrow.localRotation=Quaternion.Euler(0,0,45);
        var label=new GameObject("Aurenfeld painted label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(root,false);label.transform.localPosition=new Vector3(.03f,1.17f,-.105f);
        label.rectTransform.sizeDelta=new Vector2(1.9f,.42f);label.fontSize=3.0f;
        label.alignment=TextAlignmentOptions.Center;label.color=new Color(1,.88f,.64f);label.text="Aurenfeld";
    }
    void Marker(Vector3 point,float yaw)
    {
        var root=new GameObject("Forecourt verge stone").transform;root.SetParent(transform,false);
        root.position=Ground(point);root.rotation=Quaternion.Euler(0,yaw,0);
        Box(root,"Low boundary stone",new Vector3(0,.25f,0),new Vector3(.43f,.50f,.43f),stone,true);
        Box(root,"Worn stone cap",new Vector3(0,.50f,0),new Vector3(.48f,.08f,.48f),stone,false);
    }
    Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material,bool solid)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=at;go.transform.localScale=size;
        if(!solid){var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",size);block.SetFloat("_GrainAxis",name.Contains("post")?1:0);
        block.SetFloat("_EdgeWidth",.012f);renderer.SetPropertyBlock(block);return go.transform;
    }
    void Margins()
    {
        var random=new System.Random(803);
        for(int group=0;group<12;group++)
        {
            float angle=group*Mathf.PI*2/12;
            var center=Center+new Vector3(Mathf.Cos(angle)*5.35f,0,Mathf.Sin(angle)*4.1f);
            // Leave both road mouths broad enough for walking and later character blocking.
            if(Mathf.Abs(center.z-CourtyardEastFieldStyle.PathZ(center.x))<3.2f)continue;
            for(int i=0;i<4;i++)
            {
                var at=Ground(center+new Vector3(((float)random.NextDouble()-.5f)*1.2f,0,((float)random.NextDouble()-.5f)*.8f));
                if(IsBareGround(at))continue;
                string kind=i==0?"Pebbles":i==1?"Mixed":"Grass";
                var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)continue;
                var obj=new GameObject("Forecourt low "+kind,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
                obj.transform.position=at+Vector3.up*.025f;
                obj.transform.localScale=Vector3.one*((kind=="Grass"?.32f:.22f)/sprite.bounds.size.y);
                var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=i%2==0;
                renderer.color=new Color(.94f,.76f,.39f);plants.Add(obj.transform);
            }
        }
    }
    void LateUpdate(){if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;}
    void OnDestroy()
    {
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        foreach(var material in materials)if(material!=null)Destroy(material);
    }
}
