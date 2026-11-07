using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Dress one connected west-courtyard section; retain the authored terrain and walkable solids.
public sealed class CourtyardWestGroundStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    readonly List<Renderer> hidden=new List<Renderer>();
    readonly List<Transform> plants=new List<Transform>();
    readonly List<(Vector3 at,float width)> paths=new List<(Vector3,float)>();
    MeshFilter oldGrass;Mesh originalGrass;
    IInteractableAsPlayer[] interactions;
    Transform workshop;
    Material earth,contact;

    internal static void Install()
    {
        if(GameObject.Find("Western courtyard ground details")!=null)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");
        var shader=Resources.Load<Shader>("CourtyardFarmContact");
        var workshop=GameObject.Find("Ida workshop")?.transform;
        if(source==null || shader==null || workshop==null)return;
        var style=new GameObject("Western courtyard ground details").AddComponent<CourtyardWestGroundStyle>();
        style.earth=new Material(source){name="West courtyard worn earth"};style.materials.Add(style.earth);
        style.earth.SetColor("_BaseColor",new Color(.74f,.71f,.58f,.57f));
        style.contact=new Material(shader){name="West foundation soft contact"};style.materials.Add(style.contact);
        style.workshop=workshop;
        style.interactions=FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();
        Physics.SyncTransforms();style.ReadPaths();style.RetireOldGrowth();style.Doorways(workshop);style.Foundations(workshop);style.Growth();
    }
    static bool InSection(Vector3 p)=>p.x>-42.5f && p.x< -20.5f && p.z> -12 && p.z<11;
    static Vector3 Ground(Vector3 p)
    {
        float y=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.5f)y=Mathf.Max(y,hit.point.y);
        p.y=float.IsNegativeInfinity(y)?0:y;return p;
    }
    void ReadPaths()
    {
        foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.name!="Curved courtyard path" || f.sharedMesh==null)continue;
            var v=f.sharedMesh.vertices;if(v.Length%17!=0)continue;
            for(int row=0;row<v.Length;row+=17)
            {
                var a=f.transform.TransformPoint(v[row]);var b=f.transform.TransformPoint(v[row+16]);
                paths.Add(((a+b)*.5f,Vector3.Distance(a,b)*.5f));
            }
        }
    }
    bool OnRoad(Vector3 point)
    {
        foreach(var path in paths)
        {
            var delta=point-path.at;delta.y=0;
            if(delta.sqrMagnitude<(path.width+.2f)*(path.width+.2f))return true;
        }
        return false;
    }
    void RetireOldGrowth()
    {
        // Retire legacy triangles on the west routes and the entire front lawn.
        oldGrass=GameObject.Find("Courtyard decorative grass")?.GetComponent<MeshFilter>();
        if(oldGrass!=null && oldGrass.sharedMesh!=null)
        {
            originalGrass=oldGrass.sharedMesh;var v=originalGrass.vertices;var source=originalGrass.triangles;var kept=new List<int>();
            for(int i=0;i<source.Length;i+=3)
            {
                var center=oldGrass.transform.TransformPoint((v[source[i]]+v[source[i+1]]+v[source[i+2]])/3);
                if(!InSection(center) && !CourtyardForeground.IsInnerVerge(center) && !OnRoad(center))kept.AddRange(new[]{source[i],source[i+1],source[i+2]});
            }
            var copy=Instantiate(originalGrass);copy.name="Courtyard grass outside polished routes";copy.SetTriangles(kept,0);copy.RecalculateBounds();
            meshes.Add(copy);oldGrass.sharedMesh=copy;
        }
        foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var p=r.transform.position;if(!r.enabled || (!InSection(p) && !CourtyardForeground.IsInnerVerge(p)))continue;
            bool groundLeaf=r.name=="Painted ivy leaf" && p.y<Ground(p).y+.65f;
            bool oldPatch=r.name=="Path edge grass" || r.name=="Windblown grass" || r.name=="Meadow color patch";
            if(groundLeaf || oldPatch){hidden.Add(r);r.enabled=false;}
        }
    }
    bool Clear(Vector3 p)
    {
        if(!InSection(p))return false;
        // Stair flight, its approach and the lookout corridor remain visually legible.
        if(p.x> -37.5f && p.x< -30.5f && p.z>3.7f)return false;
        var local=workshop.InverseTransformPoint(p);
        if(local.x> -3.2f && local.x<2.6f && local.z> -6.8f && local.z< -2.0f)return false;
        if(p.x< -39 && p.z>4.9f && p.z<8.7f)return false;
        foreach(var target in interactions)
        {var d=target.GetTransform()-p;d.y=0;if(d.sqrMagnitude<1.4f*1.4f)return false;}
        foreach(var section in paths)
        {var d=section.at-p;d.y=0;if(d.sqrMagnitude<section.width*section.width)return false;}
        return !Physics.OverlapSphere(p+Vector3.up*.25f,.28f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.CompareTag("Ground"));
    }
    void Doorways(Transform workshop)
    {
        Patch("Worn tower stair approach",new Vector3(-34,0,4.55f),2.65f,.85f,earth,1);
        Patch("Worn workshop entrance",workshop.TransformPoint(new Vector3(1.6f,0,-2.75f)),.85f,1.1f,earth,2);
        Patch("Worn carpenter standing place",workshop.TransformPoint(new Vector3(-2.0f,0,-4.65f)),1.4f,.85f,earth,3);
    }
    void Foundations(Transform workshop)
    {
        for(int i=0;i<2;i++)
        {
            float x=i==0?-37:-31;
            Patch("Tower stair foot contact",new Vector3(x,0,4.95f),.62f,.58f,contact,i);
            Patch("Terrace foot contact",new Vector3(i==0?-39.4f:-28.7f,0,7.56f),1.6f,.40f,contact,i);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Patch("Workshop plinth contact",workshop.TransformPoint(new Vector3(side*3.12f,0,-.2f)),.46f,1.9f,contact,side+5);
            Patch("Workshop porch post contact",workshop.TransformPoint(new Vector3(side*3.15f,0,-3.65f)),.42f,.42f,contact,side+7);
        }
        Patch("Workshop facade contact",workshop.TransformPoint(new Vector3(0,0,-1.90f)),3.3f,.4f,contact,9);
        foreach(string name in new[]{"Workshop spare timber","Workshop offcut basket"})
        {
            var prop=GameObject.Find(name);if(prop!=null)Patch(name+" contact",prop.transform.position,.65f,.42f,contact,10);
        }
    }
    void Growth()
    {
        // Clusters frame the route instead of filling every empty lawn patch.
        Vector4[] groups={new Vector4(-39.3f,3.6f,1.1f,.7f),new Vector4(-38.5f,6.3f,.65f,.8f),
            new Vector4(-29.6f,6.4f,.7f,.65f),new Vector4(-30.2f,3.0f,.9f,.55f),
            new Vector4(-29.8f,1.1f,.6f,.8f),new Vector4(-21.2f,3.4f,.55f,.8f),
            new Vector4(-35.9f,1.1f,1.3f,.7f),new Vector4(-31.8f,-2.2f,1.1f,.8f),
            new Vector4(-27.3f,-5.1f,.9f,.7f)};
        var random=new System.Random(619);
        for(int group=0;group<groups.Length;group++)for(int i=0;i<15;i++)
        {
            var g=groups[group];float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble());
            var p=Ground(new Vector3(g.x+Mathf.Cos(a)*g.z*r,0,g.y+Mathf.Sin(a)*g.w*r));
            if(!Clear(p))continue;
            string kind=i==0?"Pebbles":i==4 && group%3==0?"Flowers":i%4==0?"Grass":"Clover";
            float h=kind=="Grass"?.34f:kind=="Flowers"?.30f:.18f;
            var tint=group<2?new Color(.88f,.84f,.58f):new Color(.85f,.94f,.68f);
            Plant(kind,p,h+(float)random.NextDouble()*.10f,tint,i);
        }
    }
    void Plant(string kind,Vector3 at,float height,Color tint,int seed)
    {
        var sprite=Resources.Load<Sprite>("CourtyardStyle/"+kind);if(sprite==null)return;
        var go=new GameObject("West verge "+kind,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
        go.transform.position=at+Vector3.up*.035f;go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=tint;renderer.flipX=seed%2==0;plants.Add(go.transform);
    }
    void Patch(string name,Vector3 center,float rx,float rz,Material material,int seed)
    {
        const int sides=32,rings=7;var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)(rings-1),edge=1+.10f*Mathf.Sin(a*3+seed)+.04f*Mathf.Cos(a*7);
            var p=center+new Vector3(Mathf.Cos(a)*rx*r*edge,0,Mathf.Sin(a)*rz*r*edge);
            v.Add(Ground(p)+Vector3.up*.045f);colors.Add(new Color(r,0,0,1));
            if(ring==0)continue;int n=ring*sides+side,next=ring*sides+(side+1)%sides;
            t.AddRange(new[]{n-sides,next,n,n-sides,next-sides,next});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void LateUpdate(){if(Camera.main!=null)foreach(var plant in plants)plant.rotation=Camera.main.transform.rotation;}
    void OnDestroy()
    {
        if(oldGrass!=null)oldGrass.sharedMesh=originalGrass;
        foreach(var r in hidden)if(r!=null)r.enabled=true;
        foreach(var m in meshes)if(m!=null)Destroy(m);
        foreach(var m in materials)if(m!=null)Destroy(m);
    }
}
