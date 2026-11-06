using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// One painted stone palette for castle, residence and enclosing walls; preserve authored solids and navigation.
public sealed class CourtyardCastleFacadeStyle : MonoBehaviour
{
    readonly List<Material> materials=new List<Material>();
    readonly List<Mesh> meshes=new List<Mesh>();
    Material masonry,trim,glass,lead,boundaryStone;
    internal static void Install()
    {
        var hall=GameObject.Find("Castle hall")?.transform;
        if(hall==null || hall.Find("Illustrated castle facade")!=null)return;
        var stone=Resources.Load<Material>("CourtyardStyle/Stone");var plaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        if(stone==null || plaster==null)return;
        var root=new GameObject("Illustrated castle facade");root.transform.SetParent(hall,false);
        var style=root.AddComponent<CourtyardCastleFacadeStyle>();
        style.masonry=style.Paint(stone,"Castle illustrated masonry",new Color(.73f,.77f,.78f),.44f);
        style.masonry.SetFloat("_FaceAlignedMasonry",1);
        style.masonry.SetFloat("_Contrast",1.06f);
        style.masonry.SetFloat("_AmbientFill",.45f);
        style.boundaryStone=style.Paint(stone,"Quieter enclosing wall stone",new Color(.66f,.71f,.73f),.44f);
        style.boundaryStone.SetFloat("_FaceAlignedMasonry",1);
        style.trim=style.Paint(plaster,"Castle dressed limestone",new Color(.76f,.73f,.66f),.45f);
        style.glass=style.Plain("Castle recessed blue glass",new Color(.085f,.14f,.16f));
        style.lead=style.Plain("Castle window bronze",new Color(.26f,.23f,.15f));
        foreach(string name in new[]{"Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower"})
        {
            var building=GameObject.Find(name)?.transform;if(building!=null)style.Building(building);
        }
        style.WestResidenceAndWalls();
        style.BannerShields();
    }
    Material Paint(Material source,string name,Color tint,float scale)
    {
        var m=new Material(source){name=name};m.SetColor("_BaseColor",tint);m.SetFloat("_TextureScale",scale);materials.Add(m);return m;
    }
    Material Plain(string name,Color tint)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",.18f);materials.Add(m);return m;
    }
    void Building(Transform building)
    {
        var stoneParts=new Geometry();var glassParts=new Geometry();var bars=new Geometry();var seams=new Geometry();
        foreach(var renderer in building.GetComponentsInChildren<MeshRenderer>().ToArray())
        {
            string n=renderer.name;
            if(n=="Window stone reveal" || n=="Window stone sill" || n=="Slender stone mullion") {renderer.enabled=false;continue;}
            if(n=="Deep castle window")
            {
                renderer.enabled=false;Window(renderer.transform.localPosition,stoneParts,glassParts,bars);continue;
            }
            bool wall=n.EndsWith(" structure") || n=="Hall battlement" || n=="Stone foundation course";
            bool dressed=n=="Projecting stone cornice" || n=="Castle corner buttress" || n=="Buttress foot";
            if(!wall && !dressed)continue;
            renderer.sharedMaterial=wall?masonry:trim;
            var s=renderer.transform.lossyScale;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            block.SetVector("_ObjectScale",new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z)));
            block.SetFloat("_EdgeWidth",dressed?.016f:0);renderer.SetPropertyBlock(block);
            if(n=="Projecting stone cornice")
            {
                var p=renderer.transform.localPosition;var scale=renderer.transform.localScale;
                seams.Box(p+Vector3.down*.15f,new Vector3(scale.x-.16f,.055f,scale.z-.16f));
            }
            if(n=="Hall battlement")
            {
                var p=renderer.transform.localPosition;var scale=renderer.transform.localScale;
                stoneParts.Box(p+Vector3.up*(scale.y*.5f+.055f),new Vector3(scale.x+.13f,.11f,scale.z+.12f));
            }
        }
        Emit(building,"Castle cornice shadow seams",seams,lead);
        Emit(building,"Painted castle window surrounds",stoneParts,trim);
        Emit(building,"Recessed castle arch windows",glassParts,glass);
        Emit(building,"Castle window glazing bars",bars,lead);
    }
    void WestResidenceAndWalls()
    {
        foreach(string name in new[]{"Player residence tower","Tower entrance","Castle west wing","Aurenfeld upper residence","Tower connecting wing","Aurenfeld raised approach"})
        {
            var root=GameObject.Find(name);if(root==null)continue;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                string source=renderer.sharedMaterial!=null?renderer.sharedMaterial.name:"";
                Material material=source=="Aurenfeld carved limestone" || source=="Courtyard painted limestone"?trim:
                    source=="Aurenfeld window recess"?glass:source=="Courtyard stone architecture"?masonry:null;
                if(material==masonry && (renderer.name.Contains("cornice") || renderer.name=="Tower foundation"))material=trim;
                if(material!=null)Surface(renderer,material);
            }
        }
        // Exact authored boundary pieces only: do not repaint cottages, the cellar or garden props.
        var wallNames=new HashSet<string>{"North castle wall","West wall near","West wall far","Broken west wall base",
            "Fallen limestone","East wall south of gate","East wall north of gate","East gate lintel","Cutaway near castle wall",
            "North battlement","Side battlement","Gate stone pier","Gate arch voussoir"};
        var courtyard=GameObject.Find("Intro courtyard layout")?.transform;if(courtyard==null)return;
        foreach(Transform part in courtyard)
        {
            if(!wallNames.Contains(part.name))continue;
            var renderer=part.GetComponent<MeshRenderer>();if(renderer==null)continue;
            Surface(renderer,part.name=="Gate arch voussoir" || part.name=="Fallen limestone"?trim:boundaryStone);
        }
    }
    static void Surface(MeshRenderer renderer,Material material)
    {
        renderer.sharedMaterial=material;
        if(!material.HasProperty("_ObjectScale"))return;
        var s=renderer.transform.lossyScale;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        block.SetVector("_ObjectScale",new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z)));
        var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
        block.SetFloat("_EdgeWidth",material.name=="Castle dressed limestone" && mesh!=null && mesh.name=="Cube"?.016f:0);
        renderer.SetPropertyBlock(block);
    }
    void Window(Vector3 center,Geometry stones,Geometry panes,Geometry bars)
    {
        // Original window center is just outside the wall; recess stays behind the projecting stone frame.
        var p=new List<Vector2>{new Vector2(-.41f,-.69f),new Vector2(.41f,-.69f)};
        for(int i=0;i<=14;i++){float a=i*Mathf.PI/14;p.Add(new Vector2(Mathf.Cos(a)*.41f,.30f+Mathf.Sin(a)*.41f));}
        panes.Extrude(p,center+Vector3.back*.018f,.035f);
        foreach(float side in new[]{-1f,1f})
            stones.Box(center+new Vector3(side*.49f,-.20f,-.15f),new Vector3(.17f,1.02f,.35f));
        stones.Box(center+new Vector3(0,-.77f,-.19f),new Vector3(1.23f,.16f,.48f));
        stones.Box(center+new Vector3(0,-.88f,-.12f),new Vector3(.98f,.08f,.30f));
        for(int i=0;i<9;i++)
        {
            float a=(i+.025f)*Mathf.PI/9,b=(i+.975f)*Mathf.PI/9;
            Vector2 P(float r,float angle)=>new Vector2(Mathf.Cos(angle)*r,.30f+Mathf.Sin(angle)*r);
            stones.Extrude(new List<Vector2>{P(.415f,a),P(.59f,a),P(.59f,b),P(.415f,b)},center+Vector3.back*.33f,.35f);
        }
        bars.Box(center+new Vector3(0,-.02f,-.065f),new Vector3(.045f,1.33f,.05f));
        bars.Box(center+new Vector3(0,-.10f,-.065f),new Vector3(.80f,.035f,.05f));
    }
    void Emit(Transform parent,string name,Geometry geometry,Material material)
    {
        if(geometry.vertices.Count==0)return;
        var mesh=new Mesh{name=name};mesh.SetVertices(geometry.vertices);mesh.SetTriangles(geometry.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    void BannerShields()
    {
        Vector2[] uv={new Vector2(.5f,.985f),new Vector2(.9f,.9f),new Vector2(.86f,.5f),new Vector2(.73f,.22f),new Vector2(.5f,.043f),new Vector2(.27f,.22f),new Vector2(.14f,.5f),new Vector2(.09f,.9f)};
        var triangles=new List<int>();for(int i=1;i<uv.Length-1;i++)triangles.AddRange(new[]{0,i,i+1});
        var mesh=new Mesh{name="Facade Aurenfeld shield",vertices=uv.Select(p=>new Vector3(p.x-.5f,p.y-.5f,0)).ToArray(),uv=uv,triangles=triangles.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        foreach(var filter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            if(filter.name=="Aurenfeld banner crest" || filter.name=="Aurenfeld residence crest")filter.sharedMesh=mesh;
    }
    void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var m in materials)if(m!=null)Destroy(m);}

    // Batch all windows of a building by material, avoiding dozens of renderers per arch.
    sealed class Geometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<int> triangles=new List<int>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c){int i=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{i,i+1,i+2});}
        public void Box(Vector3 center,Vector3 size)
        {
            var p=new List<Vector2>{new Vector2(-size.x/2,-size.y/2),new Vector2(size.x/2,-size.y/2),new Vector2(size.x/2,size.y/2),new Vector2(-size.x/2,size.y/2)};
            Extrude(p,center-Vector3.forward*size.z/2,size.z);
        }
        public void Extrude(List<Vector2> polygon,Vector3 origin,float depth)
        {
            Vector3 P(int i,float z)=>origin+new Vector3(polygon[i].x,polygon[i].y,z);
            int n=polygon.Count;
            for(int i=1;i<n-1;i++){Triangle(P(0,0),P(i+1,0),P(i,0));Triangle(P(0,depth),P(i,depth),P(i+1,depth));}
            for(int i=0;i<n;i++){int j=(i+1)%n;Triangle(P(i,0),P(j,0),P(j,depth));Triangle(P(i,0),P(j,depth),P(i,depth));}
        }
    }
}
