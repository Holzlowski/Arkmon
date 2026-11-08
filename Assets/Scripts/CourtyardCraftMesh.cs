using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Small, deterministic surface details in metric object space. No collision geometry is changed.
internal static class CourtyardCraftMesh
{
    internal enum Finish { Masonry, Roof, Paving }
    sealed class PlaneFace
    {
        public Vector3 n,origin;
        public readonly List<Vector3> points=new List<Vector3>();
    }
    sealed class Builder
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int> triangles=new List<int>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<Color> colors=new List<Color>();
        readonly Vector3 scale;
        public Builder(Vector3 scale){this.scale=scale;}
        public void Polygon(IList<Vector3> points,IList<Vector2> paint,Color color)
        {
            int start=vertices.Count;
            for(int i=0;i<points.Count;i++)
            {vertices.Add(Divide(points[i],scale));uv.Add(paint==null?Vector2.zero:paint[i]);colors.Add(color);}
            for(int i=1;i<points.Count-1;i++)triangles.AddRange(new[]{start,start+i,start+i+1});
        }
        public Mesh Mesh(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    static Vector3 Divide(Vector3 a,Vector3 b)=>new Vector3(a.x/b.x,a.y/b.y,a.z/b.z);
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static List<Vector2> Hull(IEnumerable<Vector2> input)
    {
        var points=input.GroupBy(p=>(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000)))
            .Select(g=>g.First()).OrderBy(p=>p.x).ThenBy(p=>p.y).ToList();
        if(points.Count<3)return points;
        var result=new List<Vector2>();
        foreach(var p in points){while(result.Count>=2 && Cross(result[result.Count-1]-result[result.Count-2],p-result[result.Count-1])<=0)result.RemoveAt(result.Count-1);result.Add(p);}
        int lower=result.Count;
        for(int i=points.Count-2;i>=0;i--){var p=points[i];while(result.Count>lower && Cross(result[result.Count-1]-result[result.Count-2],p-result[result.Count-1])<=0)result.RemoveAt(result.Count-1);result.Add(p);}
        result.RemoveAt(result.Count-1);return result;
    }
    static List<Vector2> Clip(List<Vector2> shape,List<Vector2> boundary)
    {
        for(int edge=0;edge<boundary.Count && shape.Count>0;edge++)
        {
            var a=boundary[edge];var d=boundary[(edge+1)%boundary.Count]-a;
            var next=new List<Vector2>();var last=shape[shape.Count-1];float lastSide=Cross(d,last-a);
            foreach(var p in shape)
            {
                float side=Cross(d,p-a);
                if((side>=0)!=(lastSide>=0))next.Add(Vector2.Lerp(last,p,lastSide/(lastSide-side)));
                if(side>=0)next.Add(p);last=p;lastSide=side;
            }
            shape=next;
        }
        return shape;
    }
    internal static Mesh Surface(Mesh source,Vector3 scale,Finish finish,Vector4 patch,int seed,Func<Vector3,bool> reserved=null)
    {
        var builder=new Builder(scale);var groups=new Dictionary<string,PlaneFace>();
        var vertices=source.vertices.Select(p=>Vector3.Scale(p,scale)).ToArray();var triangles=source.triangles;
        for(int t=0;t<triangles.Length;t+=3)
        {
            var a=vertices[triangles[t]];var b=vertices[triangles[t+1]];var c=vertices[triangles[t+2]];
            var n=Vector3.Cross(b-a,c-a).normalized;if(n.sqrMagnitude<.5f)continue;
            if(finish==Finish.Masonry?Mathf.Abs(n.y)>.15f:n.y<.20f)continue;
            float distance=Vector3.Dot(n,a);
            string key=Mathf.RoundToInt(n.x*1000)+","+Mathf.RoundToInt(n.y*1000)+","+Mathf.RoundToInt(n.z*1000)+","+Mathf.RoundToInt(distance*1000);
            if(!groups.TryGetValue(key,out var group)){group=new PlaneFace{n=n,origin=n*distance};groups.Add(key,group);}
            group.points.AddRange(new[]{a,b,c});
        }
        var random=new System.Random(seed);
        foreach(var group in groups.Values)
        {
            Vector3 u=Mathf.Abs(group.n.y)>.99f?Vector3.right:Vector3.Cross(Vector3.up,group.n).normalized;
            Vector3 v=Vector3.Cross(group.n,u).normalized;
            var hull=Hull(group.points.Select(p=>new Vector2(Vector3.Dot(p-group.origin,u),Vector3.Dot(p-group.origin,v))));
            if(hull.Count<3)continue;
            float minX=hull.Min(p=>p.x),maxX=hull.Max(p=>p.x),minY=hull.Min(p=>p.y),maxY=hull.Max(p=>p.y);
            float nominalWidth=finish==Finish.Roof?.44f:finish==Finish.Paving?.68f:.73f;
            float nominalHeight=finish==Finish.Roof?.34f:finish==Finish.Paving?.57f:.44f;
            int row=0;
            float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            for(float y=minY;y<maxY;row++)
            {
                float height=nominalHeight*Rand(.91f,1.09f);
                for(float x=minX-(row%2)*nominalWidth*.5f;x<maxX;)
                {
                    float width=nominalWidth*(finish==Finish.Roof?Rand(.90f,1.10f):Rand(.72f,1.28f));
                    float left=x;x+=width; // Advance even when a tile is clipped or reserved.
                    float gap=finish==Finish.Roof?Rand(.004f,.010f):Rand(.009f,.015f);
                    float chip=(finish==Finish.Roof?.020f:.033f)*Rand(.65f,1.5f);
                    float lo=left+gap,hi=left+width-gap,bottom=y+gap,top=y+height-gap;
                    float cornerA=chip*Rand(.7f,1.3f),cornerB=chip*Rand(.7f,1.3f);
                    if(random.NextDouble()<.07)cornerA*=2.1f;
                    var shape=Clip(new List<Vector2>{new Vector2(lo+cornerA,bottom),new Vector2(hi-cornerB,bottom),new Vector2(hi,bottom+cornerB),
                        new Vector2(hi,top-chip),new Vector2(hi-chip,top),new Vector2(lo+chip,top),new Vector2(lo,top-chip),new Vector2(lo,bottom+cornerA)},hull);
                    if(shape.Count<3)continue;
                    var center=shape.Aggregate(Vector2.zero,(sum,p)=>sum+p)/shape.Count;
                    if(reserved!=null && (reserved(group.origin+u*center.x+v*center.y) || shape.Any(p=>reserved(group.origin+u*p.x+v*p.y))))continue;
                    float lift=finish==Finish.Roof?Rand(.030f,.044f):finish==Finish.Paving?Rand(.006f,.011f):Rand(.012f,.030f);
                    float lean=finish==Finish.Paving?0:Rand(-.008f,.008f);
                    var front=new Vector3[shape.Count];var back=new Vector3[shape.Count];var paint=new Vector2[shape.Count];
                    float tone=Rand(.92f,1.055f),warmth=Rand(-.018f,.018f);
                    bool flip=random.NextDouble()<.5;
                    float crop=Rand(.78f,1),cropX=Rand(0,1-crop),cropY=Rand(0,1-crop);
                    for(int i=0;i<shape.Count;i++)
                    {
                        var pt=shape[i];var inset=Vector2.Lerp(pt,center,finish==Finish.Roof?.035f:.08f);
                        float lap=finish==Finish.Roof?(.5f-(pt.y-y)/height)*.030f:0;
                        front[i]=group.origin+u*inset.x+v*inset.y+group.n*(lift+lap+(pt.x-center.x)*lean);
                        back[i]=group.origin+u*pt.x+v*pt.y+group.n*.0015f;
                        float tx=Mathf.Clamp01((pt.x-left)/width),ty=Mathf.Clamp01((pt.y-y)/height);
                        paint[i]=new Vector2(patch.z+(cropX+(flip?1-tx:tx)*crop)*patch.x,patch.w+(cropY+ty*crop)*patch.y);
                    }
                    var pigment=new Color(tone+warmth,tone,tone-warmth,1);
                    builder.Polygon(front,paint,pigment);
                    // Narrow bevels are quieter than a repeated dark outline around every stone.
                    for(int i=0;i<shape.Count;i++){int j=(i+1)%shape.Count;builder.Polygon(new[]{back[i],back[j],front[j],front[i]},new[]{paint[i],paint[j],paint[j],paint[i]},pigment*.84f);}
                }
                y+=height;
            }
        }
        return builder.Mesh("Crafted "+finish+" relief");
    }
    internal static Mesh BevelBox(Mesh source,Vector3 scale,float bevel)
    {
        var bounds=source.bounds;var center=Vector3.Scale(bounds.center,scale);var half=Vector3.Scale(bounds.extents,scale);
        bevel=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.35f);
        var corners=new Vector3[8];
        for(int i=0;i<8;i++)corners[i]=center+new Vector3((i%2==0?-1:1)*half.x,((i/2)%2==0?-1:1)*half.y,(i<4?-1:1)*half.z);
        int[][] faces={new[]{0,2,3,1},new[]{4,5,7,6},new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3}};
        var builder=new Builder(scale);var edgeFaces=new Dictionary<(int,int),List<Vector3>>();
        var cornersFaces=Enumerable.Range(0,8).Select(i=>new List<Vector3>()).ToArray();
        foreach(var face in faces)
        {
            var mid=face.Aggregate(Vector3.zero,(sum,i)=>sum+corners[i])*.25f;var inset=new Vector3[4];
            for(int j=0;j<4;j++)
            {
                var p=corners[face[j]];
                for(int k=0;k<3;k++)p[k]=Mathf.MoveTowards(p[k],mid[k],bevel);
                inset[j]=p;cornersFaces[face[j]].Add(p);
            }
            Emit(inset);
            for(int j=0;j<4;j++)
            {
                int a=face[j],b=face[(j+1)%4];var key=(Mathf.Min(a,b),Mathf.Max(a,b));
                if(!edgeFaces.TryGetValue(key,out var points)){points=new List<Vector3>();edgeFaces.Add(key,points);}
                points.Add(inset[j]);points.Add(inset[(j+1)%4]);
            }
        }
        foreach(var face in edgeFaces.Values)Emit(face.ToArray());foreach(var face in cornersFaces)Emit(face.ToArray());
        return builder.Mesh("Crafted softened edge");
        void Emit(Vector3[] points)
        {
            var mid=points.Aggregate(Vector3.zero,(sum,p)=>sum+p)/points.Length;var n=(mid-center).normalized;
            var axis=(points[0]-mid).normalized;var other=Vector3.Cross(n,axis);
            var order=points.OrderBy(p=>Mathf.Atan2(Vector3.Dot(p-mid,other),Vector3.Dot(p-mid,axis))).ToArray();
            if(Vector3.Dot(Vector3.Cross(order[1]-order[0],order[2]-order[0]),n)<0)Array.Reverse(order);
            builder.Polygon(order,null,Color.white);
        }
    }
}
