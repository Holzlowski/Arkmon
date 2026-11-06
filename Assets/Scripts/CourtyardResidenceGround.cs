using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Ground the existing homes and work areas; never add solids to their interaction routes.
public sealed class CourtyardResidenceGround : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Renderer> hidden=new List<Renderer>();
    Material contact,earth;
    internal static void Install()
    {
        if(GameObject.Find("Resident doorstep details")!=null)return;
        var source=Resources.Load<Material>("CourtyardStyle/Stable earth");
        var shader=Resources.Load<Shader>("CourtyardFarmContact");if(source==null || shader==null)return;
        var style=new GameObject("Resident doorstep details").AddComponent<CourtyardResidenceGround>();
        style.contact=new Material(shader){name="Resident foundation contact"};
        style.earth=new Material(source){name="Resident work ground"};
        style.earth.SetColor("_BaseColor",new Color(.70f,.67f,.53f,.55f));
        foreach(string name in new[]{"Garden cottage","East cottage"})
        {
            var home=GameObject.Find(name)?.transform;if(home==null)continue;
            float width=name=="Garden cottage"?2.65f:2.77f;
            style.Patch(name+" facade contact",home.TransformPoint(new Vector3(0,0,-2.5f)),width,.35f,true);
            for(int side=-1;side<=1;side+=2)
                style.Patch(name+" side foundation contact",home.TransformPoint(new Vector3(side*width,0,0)),.35f,2.5f,true);
        }
        foreach(string name in new[]{"Garden potting table","Garden bench seat","Workshop offcut basket","Stable grooming caddy","Stable water bucket","Aurenfeld exterior waymarker","Forecourt verge stone"})
            foreach(var part in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(part.name==name)style.Patch(name+" settled contact",part.position,name.Contains("table")?1.0f:.45f,.42f,true);
        style.WorkPlace("Ida workshop",new Vector3(-1.85f,0,-4.4f),1.45f,.72f);
        style.WorkPlace("Stable",new Vector3(1.05f,0,-4.1f),1.6f,.8f);
        var table=GameObject.Find("Garden potting table");
        if(table!=null)style.Patch("Marta potting work ground",table.transform.position+Vector3.back*.65f,1.15f,.8f,false);
    }
    void WorkPlace(string name,Vector3 local,float width,float depth)
    {
        var root=GameObject.Find(name)?.transform;if(root==null)return;
        var at=root.TransformPoint(local);Patch(name+" worn work footprint",at,width,depth,false);
        // Keep tall decorative foliage out of the worker silhouette; tools and characters are untouched.
        foreach(var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string n=r.name.ToLowerInvariant();
            if(!n.Contains("verge") && !n.Contains("plant") && !n.Contains("clover"))continue;
            var d=r.transform.position-at;d.y=0;
            if(d.sqrMagnitude<1.35f*1.35f && r.enabled){hidden.Add(r);r.enabled=false;}
        }
    }
    static Vector3 Ground(Vector3 p)
    {
        float y=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.5f)y=Mathf.Max(y,hit.point.y);
        p.y=float.IsNegativeInfinity(y)?p.y:y;return p;
    }
    void Patch(string name,Vector3 at,float rx,float rz,bool shadow)
    {
        const int rings=5,sides=32;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int ring=0;ring<=rings;ring++)for(int side=0;side<sides;side++)
        {
            float a=side*Mathf.PI*2/sides,r=ring/(float)rings,edge=1+.06f*Mathf.Sin(a*5);
            v.Add(Ground(at+new Vector3(Mathf.Cos(a)*rx*r*edge,0,Mathf.Sin(a)*rz*r*edge))+Vector3.up*(shadow?.065f:.06f));
            c.Add(new Color(r,0,0,1));if(ring==0)continue;
            int n=ring*sides+side,next=ring*sides+(side+1)%sides;
            t.AddRange(new[]{n-sides,next,n,n-sides,next-sides,next});
        }
        var mesh=new Mesh{name=name};meshes.Add(mesh);mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=shadow?contact:earth;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void OnDestroy(){foreach(var r in hidden)if(r!=null)r.enabled=true;foreach(var m in meshes)if(m!=null)Destroy(m);if(contact!=null)Destroy(contact);if(earth!=null)Destroy(earth);}
}
