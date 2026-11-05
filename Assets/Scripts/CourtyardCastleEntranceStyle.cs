using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Dress only the throne-hall entrance; the existing inspection interaction remains authoritative.
public sealed class CourtyardCastleEntranceStyle : MonoBehaviour
{
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    Material oak,stone,iron,brass;
    internal static void Install()
    {
        var entrance=FindObjectsByType<CourtyardEntrance>(FindObjectsSortMode.None)
            .FirstOrDefault(e=>e.destination==CourtyardEntrance.Destination.ThroneHall);
        if(entrance==null || GameObject.Find("Illustrated castle entrance")!=null)return;
        var old=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.name=="Closed oak entrance").OrderBy(r=>(r.transform.position-entrance.transform.position).sqrMagnitude).FirstOrDefault();
        var oak=Resources.Load<Material>("CourtyardStyle/Stable oak");var stone=Resources.Load<Material>("CourtyardStyle/Stone");
        if(old==null || oak==null || stone==null)return;
        var root=new GameObject("Illustrated castle entrance");root.transform.SetParent(old.transform.parent,false);
        root.transform.position=new Vector3(old.transform.position.x,old.bounds.min.y,old.transform.position.z);
        var style=root.AddComponent<CourtyardCastleEntranceStyle>();style.oak=oak;
        var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        style.stone=new Material(plaster!=null?plaster:stone){name="Castle portal carved limestone"};
        style.stone.SetColor("_BaseColor",new Color(.76f,.73f,.66f));style.stone.SetFloat("_TextureScale",.45f);style.materials.Add(style.stone);
        style.iron=style.Tint("Castle forged iron",new Color(.13f,.115f,.09f),.2f);
        style.brass=style.Tint("Castle aged brass",new Color(.58f,.37f,.12f),.35f);
        old.enabled=false;
        foreach(var trim in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if((trim.name=="Doorway jamb" || trim.name=="Doorway lintel") && Mathf.Abs(trim.transform.position.x-root.transform.position.x)<2.1f && Mathf.Abs(trim.transform.position.z-root.transform.position.z)<.5f)trim.enabled=false;
        style.Build();
    }
    Material Tint(string name,Color color,float metal)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.22f);materials.Add(m);return m;
    }
    void Build()
    {
        const float radius=1.72f,spring=3.30f;
        // A continuous dark silhouette closes plank seams and keeps the arch grounded in the wall.
        var outline=new List<Vector2>{new Vector2(-radius-.05f,.02f),new Vector2(radius+.05f,.02f)};
        for(int i=0;i<=32;i++){float a=i*Mathf.PI/32;outline.Add(new Vector2(Mathf.Cos(a)*(radius+.05f),spring+Mathf.Sin(a)*(radius+.05f)));}
        Solid("Recessed arch shadow",outline,-.25f,.30f,iron);
        for(int i=0;i<12;i++)
        {
            float left=-radius+i*radius*2/12+.009f,right=-radius+(i+1)*radius*2/12-.009f;
            var p=new List<Vector2>{new Vector2(left,.10f),new Vector2(right,.10f)};
            for(int j=0;j<=4;j++){float x=Mathf.Lerp(right,left,j/4f);p.Add(new Vector2(x,spring+Mathf.Sqrt(Mathf.Max(0,radius*radius-x*x))));}
            Solid("Carved oak door plank",p,-.37f,.19f,oak);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Box("Portal stone foot",new Vector3(side*1.97f,.18f,-.31f),new Vector3(.72f,.36f,.78f),stone,true);
            for(int course=0;course<6;course++)Box("Portal stone jamb",new Vector3(side*1.95f,.35f+(course+.5f)*.48f,-.27f),new Vector3(.44f,.46f,.68f),stone,true);
            Box("Portal spring capital",new Vector3(side*1.96f,3.22f,-.30f),new Vector3(.64f,.20f,.80f),stone,true);
            foreach(float y in new[]{.72f,2.55f})
            {
                Box("Forged door strap",new Vector3(side*.87f,y,-.50f),new Vector3(1.48f,.14f,.07f),iron);
                foreach(float x in new[]{.26f,.78f,1.42f})Box("Brass strap stud",new Vector3(side*x,y,-.55f),new Vector3(.065f,.065f,.045f),brass);
            }
            Box("Door pull backplate",new Vector3(side*.26f,1.65f,-.50f),new Vector3(.20f,.31f,.07f),iron);
            Ring("Brass door pull",new Vector2(side*.26f,1.57f),.105f,.145f,-.59f,.045f,brass,20,0,Mathf.PI*2);
        }
        Ring("Carved arch voussoir",new Vector2(0,spring),1.78f,2.20f,-.65f,.72f,stone,17,0,Mathf.PI);
        Box("Portal keystone",new Vector3(0,5.35f,-.36f),new Vector3(.36f,.60f,.82f),stone);
        Box("Portal threshold",new Vector3(0,.045f,-.36f),new Vector3(3.5f,.09f,.90f),stone);
        // Solid closed door, matching the visible front instead of leaving a gap to the old collider.
        var blocker=new GameObject("Closed portal collision",typeof(BoxCollider));blocker.transform.SetParent(transform,false);
        var collider=blocker.GetComponent<BoxCollider>();collider.center=new Vector3(0,2.4f,-.26f);collider.size=new Vector3(3.44f,4.8f,.40f);
        var source=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name=="Aurenfeld banner crest");
        if(source!=null)
        {
            // Reuse only the approved image/material, never the banner's swaying behaviour.
            var crest=new GameObject("Aurenfeld portal crest",typeof(MeshFilter),typeof(MeshRenderer));crest.transform.SetParent(transform,false);
            crest.transform.localPosition=new Vector3(0,4.06f,-.52f);crest.transform.localScale=new Vector3(1.08f,1.20f,1);
            Vector2[] uv={new Vector2(.5f,.985f),new Vector2(.9f,.9f),new Vector2(.86f,.5f),new Vector2(.73f,.22f),new Vector2(.5f,.043f),new Vector2(.27f,.22f),new Vector2(.14f,.5f),new Vector2(.09f,.9f)};
            var verts=uv.Select(p=>new Vector3(p.x-.5f,p.y-.5f,0)).ToArray();var tris=new List<int>();
            for(int i=1;i<uv.Length-1;i++)tris.AddRange(new[]{0,i,i+1});
            var shield=new Mesh{name="Portal Aurenfeld shield",vertices=verts,uv=uv,triangles=tris.ToArray()};shield.RecalculateNormals();shield.RecalculateBounds();meshes.Add(shield);
            crest.GetComponent<MeshFilter>().sharedMesh=shield;crest.GetComponent<MeshRenderer>().sharedMaterial=source.sharedMaterial;
        }
    }
    void Box(string name,Vector3 position,Vector3 size,Material material,bool collision=false)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=size;
        if(!collision){var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}
        Finish(part.GetComponent<MeshRenderer>(),material,size);
    }
    void Ring(string name,Vector2 center,float inner,float outer,float z,float depth,Material material,int pieces,float from,float to)
    {
        for(int i=0;i<pieces;i++)
        {
            float a=Mathf.Lerp(from,to,(i+.015f)/pieces),b=Mathf.Lerp(from,to,(i+.985f)/pieces);
            Vector2 P(float radius,float angle)=>center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
            Solid(name,new List<Vector2>{P(inner,a),P(outer,a),P(outer,b),P(inner,b)},z,depth,material);
        }
    }
    void Solid(string name,List<Vector2> polygon,float front,float depth,Material material)
    {
        int n=polygon.Count;var vertices=new List<Vector3>();var triangles=new List<int>();
        // Separate face vertices preserve the sharp illustrated bevel edges.
        void Triangle(Vector3 a,Vector3 b,Vector3 c){int k=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.AddRange(new[]{k,k+1,k+2});}
        Vector3 P(int i,float z)=>new Vector3(polygon[i].x,polygon[i].y,z);
        for(int i=1;i<n-1;i++){Triangle(P(0,front),P(i+1,front),P(i,front));Triangle(P(0,front+depth),P(i,front+depth),P(i+1,front+depth));}
        for(int i=0;i<n;i++){int j=(i+1)%n;Triangle(P(i,front),P(j,front),P(j,front+depth));Triangle(P(i,front),P(j,front+depth),P(i,front+depth));}
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(transform,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;Finish(part.GetComponent<MeshRenderer>(),material,Vector3.one);
    }
    static void Finish(MeshRenderer renderer,Material material,Vector3 scale)
    {
        renderer.sharedMaterial=material;
        if(!material.HasProperty("_ObjectScale"))return;
        var block=new MaterialPropertyBlock();block.SetVector("_ObjectScale",scale);block.SetFloat("_GrainAxis",1);block.SetFloat("_EdgeWidth",renderer.GetComponent<MeshFilter>().sharedMesh.name=="Cube"?.012f:0);
        if(renderer.name=="Carved oak door plank")block.SetVector("_ObjectScale",new Vector3(1,.22f,1));
        renderer.SetPropertyBlock(block);
    }
    void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
}
