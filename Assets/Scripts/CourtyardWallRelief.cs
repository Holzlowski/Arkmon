using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Replace only the visible shell. Authored wall collisions and camera cutaways stay on their objects.
public sealed class CourtyardWallRelief : MonoBehaviour
{
    sealed class Binding
    {
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Mesh original, relief;
        public float singleStone, meshUV;
    }
    readonly List<Binding> bindings=new List<Binding>();
    readonly List<MeshRenderer> hiddenArchFaces=new List<MeshRenderer>();
    static readonly int SingleStone=Shader.PropertyToID("_SingleStone");
    static readonly int MeshUV=Shader.PropertyToID("_StoneMeshUV");
    internal static void Install()
    {
        if(GameObject.Find("Courtyard wall stone relief")!=null)return;
        var root=GameObject.Find("Intro courtyard layout");if(root==null)return;
        var style=new GameObject("Courtyard wall stone relief").AddComponent<CourtyardWallRelief>();
        var names=new HashSet<string>{"North castle wall","West wall near","West wall far",
            "East wall south of gate","East wall north of gate","Cutaway near castle wall",
            "North battlement","Side battlement","Gate stone pier"};
        int seed=2401;
        foreach(Transform part in root.transform)
        {
            if(!part.gameObject.activeInHierarchy || !names.Contains(part.name))continue;
            var filter=part.GetComponent<MeshFilter>();var renderer=part.GetComponent<MeshRenderer>();
            if(filter==null || filter.sharedMesh==null || renderer==null || !renderer.enabled)continue;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            var binding=new Binding{filter=filter,renderer=renderer,original=filter.sharedMesh,
                singleStone=block.GetFloat(SingleStone),meshUV=block.GetFloat(MeshUV)};
            binding.relief=Build(part.lossyScale,seed++);filter.sharedMesh=binding.relief;
            block.SetFloat(SingleStone,1);block.SetFloat(MeshUV,1);renderer.SetPropertyBlock(block);
            style.bindings.Add(binding);
        }
        style.GateArch();
    }
    void GateArch()
    {
        var gate=GameObject.Find("Illustrated east crossing");if(gate==null)return;
        var faces=new List<MeshFilter>();
        foreach(var f in gate.GetComponentsInChildren<MeshFilter>())
            if(f.name=="East gate arch masonry")faces.Add(f);
        if(faces.Count==0)return;
        var shell=new Shell(Vector3.one,7811);
        foreach(var face in faces)shell.ArchCore(face.sharedMesh);
        shell.ArchStones();
        var mesh=shell.Mesh();
        var first=faces[0];var renderer=first.GetComponent<MeshRenderer>();
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        bindings.Add(new Binding{filter=first,renderer=renderer,original=first.sharedMesh,relief=mesh,
            singleStone=block.GetFloat(SingleStone),meshUV=block.GetFloat(MeshUV)});
        first.sharedMesh=mesh;block.SetFloat(SingleStone,1);block.SetFloat(MeshUV,1);renderer.SetPropertyBlock(block);
        for(int i=1;i<faces.Count;i++)
        {
            var r=faces[i].GetComponent<MeshRenderer>();
            if(r.enabled){hiddenArchFaces.Add(r);r.enabled=false;}
        }
    }
    static Mesh Build(Vector3 scale,int seed)
    {
        var size=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        var builder=new Shell(size,seed);
        builder.Face(Vector3.right,Vector3.back,Vector3.up,size.x,size.z,size.y,true);
        builder.Face(Vector3.left,Vector3.forward,Vector3.up,size.x,size.z,size.y,true);
        builder.Face(Vector3.forward,Vector3.right,Vector3.up,size.z,size.x,size.y,true);
        builder.Face(Vector3.back,Vector3.left,Vector3.up,size.z,size.x,size.y,true);
        builder.Face(Vector3.up,Vector3.right,Vector3.back,size.y,size.x,size.z,false);
        builder.Face(Vector3.down,Vector3.right,Vector3.forward,size.y,size.x,size.z,false);
        return builder.Mesh();
    }
    sealed class Shell
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<int> triangles=new List<int>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<Color> colors=new List<Color>();
        readonly Vector3 size;
        readonly System.Random random;
        public Shell(Vector3 size,int seed){this.size=size;random=new System.Random(seed);}
        public Mesh Mesh()
        {
            var mesh=new Mesh{name="Courtyard jointed stone relief",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.SetColors(colors);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void ArchCore(Mesh original)
        {
            int start=vertices.Count;
            foreach(var point in original.vertices)
            {vertices.Add(point);uv.Add(new Vector2(.67f,.43f));colors.Add(new Color(.25f,.25f,.23f,1));}
            foreach(int index in original.triangles)triangles.Add(start+index);
        }
        public void ArchStones()
        {
            // Match the curved gate opening instead of covering it with a rectangular wall.
            for(int side=0;side<2;side++)
            for(int row=0;row<11;row++)
            for(float z=-3.5f-(row%2)*.365f;z<1.5f;z+=.73f)
            {
                float lo=Mathf.Max(z,-3.5f)+.014f,hi=Mathf.Min(z+.73f,1.5f)-.014f;
                if(hi-lo<.07f)continue;
                float closest=Mathf.Clamp(-1,lo,hi)+1;
                float arch=3.25f+Mathf.Sqrt(Mathf.Max(0,2.23f*2.23f-closest*closest));
                float bottom=Mathf.Max(3.25f+row*.44f+.012f,arch+.015f);
                float top=Mathf.Min(3.25f+(row+1)*.44f-.012f,8);
                if(top-bottom<.08f)continue;
                float sign=side==0?1:-1;
                Stone(Vector3.right*sign,Vector3.back*sign,Vector3.up,side==0?44:-42,
                    -(lo+hi)*.5f*sign,(bottom+top)*.5f,(hi-lo)*.5f,(top-bottom)*.5f);
            }
            for(int side=0;side<2;side++)for(int col=0;col<3;col++)for(int row=0;row<11;row++)
            {
                float sign=side==0?1:-1;
                Stone(Vector3.forward*sign,Vector3.right*sign,Vector3.up,side==0?1.5f:3.5f,
                    (42+(col+.5f)*2/3f)*sign,3.25f+(row+.5f)*4.75f/11,
                    1/3f-.015f,2.375f/11-.012f);
            }
            for(int col=0;col<3;col++)for(int row=0;row<7;row++)
                Stone(Vector3.up,Vector3.right,Vector3.back,8,42+(col+.5f)*2/3f,
                    -(-3.5f+(row+.5f)*5/7f),1/3f-.015f,2.5f/7-.015f);
        }
        float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        Vector3 Local(Vector3 point)=>new Vector3(point.x/size.x,point.y/size.y,point.z/size.z);
        void Polygon(Vector3[] points,Vector2[] paint,Color color)
        {
            int start=vertices.Count;
            for(int i=0;i<points.Length;i++){vertices.Add(Local(points[i]));uv.Add(paint[i]);colors.Add(color);}
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        public void Face(Vector3 normal,Vector3 u,Vector3 v,float depth,float width,float height,bool vertical)
        {
            // Continuous recessed mortar closes the gaps; it never becomes another visible brick texture.
            var center=normal*(depth*.5f-.055f);
            Polygon(new[]{center-u*width*.5f-v*height*.5f,center+u*width*.5f-v*height*.5f,
                center+u*width*.5f+v*height*.5f,center-u*width*.5f+v*height*.5f},
                new[]{new Vector2(.67f,.43f),new Vector2(.67f,.43f),new Vector2(.67f,.43f),new Vector2(.67f,.43f)},
                new Color(.25f,.25f,.23f,1));
            float pitchY=vertical?.44f:.73f;
            int rows=Mathf.Max(1,Mathf.RoundToInt(height/pitchY));float stepY=height/rows;
            const float stepX=.73f;
            for(int row=0;row<rows;row++)
            {
                float shift=row%2==0?0:stepX*.5f;
                for(float left=-width*.5f-shift;left<width*.5f;)
                {
                    float span=stepX*Rand(.72f,1.28f);
                    float lo=Mathf.Max(left,-width*.5f)+.014f,hi=Mathf.Min(left+span,width*.5f)-.014f;
                    left+=span;
                    if(hi-lo<.07f)continue;
                    float bottom=-height*.5f+row*stepY+.012f,top=bottom+stepY-.024f;
                    Stone(normal,u,v,depth*.5f,(lo+hi)*.5f,(bottom+top)*.5f,(hi-lo)*.5f,(top-bottom)*.5f);
                }
            }
        }
        void Stone(Vector3 normal,Vector3 u,Vector3 v,float depth,float x,float y,float hw,float hh)
        {
            float chip=Mathf.Min(hw,hh)*Rand(.22f,.43f),tone=Rand(.92f,1.055f);
            var outline=new[]{new Vector2(-hw+chip,-hh),new Vector2(hw-chip,-hh),new Vector2(hw,-hh+chip),
                new Vector2(hw,hh-chip),new Vector2(hw-chip,hh),new Vector2(-hw+chip,hh),
                new Vector2(-hw,hh-chip),new Vector2(-hw,-hh+chip)};
            var inner=new Vector3[8];var outer=new Vector3[8];var paint=new Vector2[8];
            float relief=Rand(.025f,.065f),leanX=Rand(-.025f,.025f),leanY=Rand(-.015f,.015f);
            float inset=Rand(.80f,.89f);
            var center=u*x+v*y+normal*depth;
            bool flip=random.NextDouble()<.5;float crop=Rand(.78f,1),cropX=Rand(0,1-crop),cropY=Rand(0,1-crop);
            for(int i=0;i<8;i++)
            {
                var p=outline[i];p*=Rand(.96f,1);outline[i]=p;
                outer[i]=center+u*p.x+v*p.y-normal*.035f;
                inner[i]=center+(u*p.x+v*p.y)*inset+normal*(relief+p.x*leanX+p.y*leanY);
                float tx=p.x/(2*hw)+.5f,ty=p.y/(2*hh)+.5f;
                paint[i]=new Vector2(.63f+(cropX+(flip?1-tx:tx)*crop)*.16f,.405f+(cropY+ty*crop)*.065f);
            }
            Polygon(inner,paint,new Color(tone,tone,tone,1));
            for(int i=0;i<8;i++)
            {
                int j=(i+1)%8;
                Polygon(new[]{outer[i],outer[j],inner[j],inner[i]},new[]{paint[i],paint[j],paint[j],paint[i]},
                    new Color(tone*.84f,tone*.84f,tone*.84f,1));
            }
        }
    }
    void OnDestroy()
    {
        foreach(var face in hiddenArchFaces)if(face!=null)face.enabled=true;
        foreach(var b in bindings)
        {
            if(b.filter!=null && b.filter.sharedMesh==b.relief)b.filter.sharedMesh=b.original;
            if(b.renderer!=null)
            {
                var block=new MaterialPropertyBlock();b.renderer.GetPropertyBlock(block);
                block.SetFloat(SingleStone,b.singleStone);block.SetFloat(MeshUV,b.meshUV);b.renderer.SetPropertyBlock(block);
            }
            if(b.relief!=null)Destroy(b.relief);
        }
    }
}
