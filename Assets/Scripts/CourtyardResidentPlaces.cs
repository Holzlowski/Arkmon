using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Small domestic enclosures, grounded on the existing walkable surface.
// Their openings are deliberately separate from the main street and work interactions.
public sealed class CourtyardResidentPlaces : MonoBehaviour
{
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Material> materials = new List<Material>();
    Material oak, earth;
    internal static void Install()
    {
        if (GameObject.Find("Resident working yards") != null) return;
        var root = new GameObject("Resident working yards").AddComponent<CourtyardResidentPlaces>();
        root.oak = Resources.Load<Material>("CourtyardStyle/Stable oak");
        var source = Resources.Load<Material>("CourtyardStyle/Stable earth");
        if (source == null || root.oak == null) { Destroy(root.gameObject); return; }
        root.earth = new Material(source) { name = "Resident yard worn earth" };
        root.earth.SetColor("_BaseColor", new Color(.72f,.66f,.50f,.48f));root.materials.Add(root.earth);
        root.Workshop();root.Stable();root.Garden();Physics.SyncTransforms();
    }
    void Workshop()
    {
        var house = GameObject.Find("Ida workshop")?.transform;if(house==null)return;
        var yard = Child("Ida working yard", house.position);
        Patch(yard,"Ida worn work apron",new Vector3(0,0,-4.5f),3.6f,1.65f,earth);
        Patch(yard,"Ida timber delivery ground",new Vector3(3.5f,0,-1.5f),1.05f,2.0f,earth);
        // Spare boards stay by the stack rather than filling the doorway or the tower square.
        for(int i=0;i<3;i++)
        {
            var board=Box(yard,"Stacked seasoned board",new Vector3(3.5f,.10f+i*.075f,.65f),new Vector3(.85f,.065f,1.65f),false);
            board.localRotation=Quaternion.Euler(0,5+i*3,0);
        }
        Rail(yard,new Vector3(-4,0,.9f),new Vector3(-4,0,-1.1f),.62f);
    }
    void Stable()
    {
        var yard = Child("Hannes small paddock", Vector3.zero);
        // The paddock extends west from the stable. The wide eastern opening faces the horses.
        Rail(yard,new Vector3(-23,0,-.2f),new Vector3(-16.5f,0,-.2f),.95f);
        Rail(yard,new Vector3(-23,0,-.2f),new Vector3(-23,0,6.1f),.95f);
        Rail(yard,new Vector3(-23,0,6.1f),new Vector3(-16.5f,0,6.1f),.95f);
        Rail(yard,new Vector3(-16.5f,0,-.2f),new Vector3(-16.5f,0,1.0f),.95f);
        Rail(yard,new Vector3(-16.5f,0,4.8f),new Vector3(-16.5f,0,6.1f),.95f);
        Patch(yard,"Paddock hoof worn ground",new Vector3(-19.4f,0,2.7f),2.7f,2.8f,earth);
        var straw=Resources.Load<Material>("CourtyardStyle/Stable straw earth");
        if(straw!=null)Patch(yard,"Paddock feeding straw",new Vector3(-21.5f,0,4.5f),.85f,.55f,straw);
        // A feeding rack and a low water trough explain the enclosure even when both horses are being cared for.
        Box(yard,"Paddock feeding rack",new Vector3(-21.5f,.55f,4.8f),new Vector3(1.5f,.22f,.65f),true);
        for(int i=0;i<6;i++)Box(yard,"Paddock rack slat",new Vector3(-22.1f+i*.24f,.88f,4.55f),new Vector3(.07f,.65f,.08f),false);
        var trough=Child("Paddock trough",new Vector3(-21.6f,0,.8f));
        Box(trough,"Trough base",new Vector3(0,.18f,0),new Vector3(1.5f,.32f,.7f),true);
        foreach(float z in new[]{-.34f,.34f})Box(trough,"Trough rim",new Vector3(0,.43f,z),new Vector3(1.6f,.22f,.10f),false);
        foreach(float x in new[]{-.75f,.75f})Box(trough,"Trough end",new Vector3(x,.43f,0),new Vector3(.10f,.22f,.7f),false);
        var waterSource=GameObject.Find("Recessed still water")?.GetComponent<Renderer>();
        if(waterSource!=null)Box(trough,"Paddock still water",new Vector3(0,.40f,0),new Vector3(1.4f,.025f,.6f),false).GetComponent<Renderer>().sharedMaterial=waterSource.sharedMaterial;
    }
    void Garden()
    {
        var yard=Child("Marta kitchen garden fence",Vector3.zero);
        Rail(yard,new Vector3(32.6f,0,-16.8f),new Vector3(32.6f,0,-14.4f),.64f);
        Rail(yard,new Vector3(32.6f,0,-11.5f),new Vector3(32.6f,0,-4.65f),.64f);
        Rail(yard,new Vector3(27.9f,0,-4.65f),new Vector3(32.6f,0,-4.65f),.64f);
        Rail(yard,new Vector3(20.1f,0,-16.45f),new Vector3(24.8f,0,-16.45f),.64f);
        Rail(yard,new Vector3(30.6f,0,-16.45f),new Vector3(32.6f,0,-16.45f),.64f);
        // The can and all three watering points remain inside a broad open entrance.
        Patch(yard,"Garden entrance worn ground",new Vector3(27.6f,0,-15.7f),2.6f,.75f,earth);
    }
    Transform Child(string name,Vector3 position)
    {
        var root=new GameObject(name).transform;root.SetParent(transform,false);root.position=position;return root;
    }
    static Vector3 Ground(Vector3 p)
    {
        float y=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.5f)y=Mathf.Max(y,hit.point.y);
        p.y=float.IsNegativeInfinity(y)?p.y:y;return p;
    }
    Transform Box(Transform parent,string name,Vector3 local,Vector3 size,bool solid)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        var p=Ground(parent.TransformPoint(new Vector3(local.x,0,local.z)))+Vector3.up*local.y;
        go.transform.position=p;go.transform.localScale=size;
        var collider=go.GetComponent<BoxCollider>();collider.enabled=solid;
        if(!solid)Destroy(collider);
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=oak;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",size);
        block.SetFloat("_GrainAxis",size.y>size.x && size.y>size.z?1:size.z>size.x?2:0);block.SetFloat("_EdgeWidth",.018f);renderer.SetPropertyBlock(block);
        return go.transform;
    }
    void Rail(Transform parent,Vector3 a,Vector3 b,float height)
    {
        int count=Mathf.CeilToInt(Vector3.Distance(a,b)/1.7f);
        for(int i=0;i<=count;i++)Box(parent,"Yard fence post",Vector3.Lerp(a,b,i/(float)count)+Vector3.up*(height*.5f),new Vector3(.13f,height,.13f),true);
        for(int i=0;i<count;i++)
        {
            var p=Vector3.Lerp(a,b,i/(float)count);var q=Vector3.Lerp(a,b,(i+1)/(float)count);
            foreach(float h in new[]{height*.35f,height*.78f})
            {
                var rail=Box(parent,"Yard fence rail",(p+q)*.5f+Vector3.up*h,new Vector3(.08f,.10f,Vector3.Distance(p,q)),true);
                rail.rotation=Quaternion.LookRotation(q-p);
            }
        }
    }
    void Patch(Transform parent,string name,Vector3 center,float rx,float rz,Material material)
    {
        const int sides=36,rings=6;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings,edge=1+.06f*Mathf.Sin(a*5)+.04f*Mathf.Cos(a*3);
            var p=Ground(parent.TransformPoint(center+new Vector3(Mathf.Cos(a)*rx*r*edge,0,Mathf.Sin(a)*rz*r*edge)))+Vector3.up*.058f;
            v.Add(parent.InverseTransformPoint(p));c.Add(new Color(r,0,0,1));if(ring==0)continue;
            int n=ring*sides+side,next=ring*sides+(side+1)%sides;t.AddRange(new[]{n-sides,next,n,n-sides,next-sides,next});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
}
