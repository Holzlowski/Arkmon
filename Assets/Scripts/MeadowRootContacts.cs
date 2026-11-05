using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Rebuild-time sampler and batched root occlusion. No per-blade objects or frame updates.
internal sealed class MeadowRootContacts
{
    struct Triangle { public Vector3 a,b,c; }
    readonly Dictionary<Vector2Int,List<Triangle>> ground = new Dictionary<Vector2Int,List<Triangle>>();
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector2> uv = new List<Vector2>();
    readonly List<int> triangles = new List<int>();
    readonly List<Mesh> meshes = new List<Mesh>();
    static readonly Vector2[] Offsets = { Vector2.zero, new Vector2(-1,-1), new Vector2(-1,1), new Vector2(1,1), new Vector2(1,-1) };
    readonly Transform owner;
    readonly Material material;
    static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x/8),Mathf.FloorToInt(p.z/8));

    public MeadowRootContacts(Transform owner)
    {
        this.owner=owner;
        var shader=Resources.Load<Shader>("MeadowRootContact");
        if(shader!=null) material=new Material(shader){name="Soft grass root contact"};
        foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(filter.gameObject.scene!=owner.gameObject.scene)continue;
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="Arkmon/Meadow" || filter.sharedMesh==null)continue;
            var points=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
            for(int i=0;i<points.Length;i++)points[i]=filter.transform.TransformPoint(points[i]);
            for(int i=0;i<indices.Length;i+=3)
            {
                var t=new Triangle{a=points[indices[i]],b=points[indices[i+1]],c=points[indices[i+2]]};
                var min=Cell(Vector3.Min(t.a,Vector3.Min(t.b,t.c)));var max=Cell(Vector3.Max(t.a,Vector3.Max(t.b,t.c)));
                for(int x=min.x;x<=max.x;x++)for(int z=min.y;z<=max.y;z++)
                {
                    var cell=new Vector2Int(x,z);
                    if(!ground.TryGetValue(cell,out var list)){list=new List<Triangle>();ground.Add(cell,list);}list.Add(t);
                }
            }
        }
    }
    Vector3 Ground(Vector3 p)
    {
        bool found=false;float highest=float.NegativeInfinity;
        if(ground.TryGetValue(Cell(p),out var list))foreach(var t in list)
        {
            var a=t.b-t.a;var b=t.c-t.a;var d=p-t.a;
            float determinant=a.x*b.z-a.z*b.x;if(Mathf.Abs(determinant)<.00001f)continue;
            float u=(d.x*b.z-d.z*b.x)/determinant,v=(a.x*d.z-a.z*d.x)/determinant;
            if(u>=-.0001f && v>=-.0001f && u+v<=1.0001f){highest=Mathf.Max(highest,t.a.y+u*a.y+v*b.y);found=true;}
        }
        if(found)p.y=highest;
        return p;
    }
    public Vector3 Root(Vector3 local) => owner.InverseTransformPoint(Ground(owner.TransformPoint(local))-Vector3.up*.008f);
    public void Begin(){vertices.Clear();uv.Clear();triangles.Clear();}
    public void Add(Vector3 center,float radius)
    {
        if(material==null)return;
        int n=vertices.Count;
        foreach(var offset in Offsets)
        {
            var local=center+new Vector3(offset.x*radius,0,offset.y*radius);
            vertices.Add(owner.InverseTransformPoint(Ground(owner.TransformPoint(local))+Vector3.up*.012f));
            uv.Add(offset*.5f+Vector2.one*.5f);
        }
        for(int i=0;i<4;i++){triangles.Add(n);triangles.Add(n+1+i);triangles.Add(n+1+(i+1)%4);}
    }
    public MeshRenderer Build(Transform parent)
    {
        if(material==null || vertices.Count==0)return null;
        var mesh=new Mesh{name="Terrain-conforming grass root contacts",indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.UploadMeshData(true);meshes.Add(mesh);
        var go=new GameObject("Grass root contacts",typeof(MeshFilter),typeof(MeshRenderer)){hideFlags=HideFlags.DontSave};go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
    }
    public void FinishSampling(){ground.Clear();vertices.Clear();uv.Clear();triangles.Clear();}
    public void Dispose()
    {
        foreach(var mesh in meshes)if(mesh!=null)Object.Destroy(mesh);
        if(material!=null)Object.Destroy(material);
    }
}
