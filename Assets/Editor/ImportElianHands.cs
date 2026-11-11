using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportElianHands
{
    [System.Serializable] sealed class Geometry {public Vector2[] vertices;public int[] triangles;}
    sealed class Shape { public Rect rect;public List<Vector2> boundary; }
    public static void Run()
    {
        const string path="Assets/Art/Intro/CharacterStyle/Elian distinct hands-v2.png";
        AssetDatabase.Refresh();CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.npotScale=TextureImporterNPOTScale.None;
        importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        var shapes=new Shape[4];int cw=texture.width/2,ch=texture.height/2;
        for(int i=0;i<4;i++)shapes[i]=Largest(pixels,texture.width,new RectInt(i%2*cw,(1-i/2)*ch,cw,ch));
        float ppu=(shapes[0].rect.height+shapes[1].rect.height)*.5f/.19f;
        string folder="Assets/Resources/ElianHands";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","ElianHands");
        string[] names={"RightGrip","LeftGrip","RightSupport","LeftSupport"};
        Vector2[] pivots={new Vector2(.5f,.07f),new Vector2(.5f,.07f),new Vector2(.85f,.12f),new Vector2(.15f,.12f)};
        for(int i=0;i<4;i++)
        {
            var sprite=CourtyardCharacterArt.Sprite(texture,shapes[i].rect,pivots[i],ppu,folder+"/"+names[i]+".asset","Elian-"+names[i]);
            var hull=Hull(shapes[i].boundary);var center=hull.Aggregate(Vector2.zero,(a,b)=>a+b)/hull.Count;
            // Metadata geometry excludes isolated transparent-background flecks; pixels are untouched.
            var vertices=hull.Select(v=>(v+(v-center).normalized*3-shapes[i].rect.position-Vector2.Scale(pivots[i],shapes[i].rect.size))/ppu).ToArray();
            var triangles=new List<ushort>();for(int k=1;k<vertices.Length-1;k++){triangles.Add(0);triangles.Add((ushort)k);triangles.Add((ushort)(k+1));}
            System.IO.File.WriteAllText(folder+"/"+names[i]+"-outline.json",JsonUtility.ToJson(new Geometry{vertices=vertices,triangles=triangles.Select(t=>(int)t).ToArray()}));
            EditorUtility.SetDirty(sprite);
        }
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.Refresh();AssetDatabase.SaveAssets();
        AlignCanHandle();
        Debug.Log("ELIAN_HAND_ART_IMPORTED distinct left/right hands; source pixels unchanged");
    }
    static void AlignCanHandle()
    {
        const string spritePath="Assets/Resources/Garden/Can.asset";
        var original=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        string texturePath=AssetDatabase.GetAssetPath(original.texture);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);var pixels=texture.GetPixels32();var rect=original.rect;
        bool Handle(int x,int y)
        {
            var c=pixels[y*texture.width+x];return c.a>230 && !(c.r>215 && c.g>205 && c.b>175);
        }
        int top=-1;var xs=new List<int>();
        for(int y=(int)rect.yMax-1;y>rect.y+rect.height*.72f && top<0;y--)
            for(int x=(int)(rect.x+rect.width*.15f);x<rect.x+rect.width*.62f;x++)
                if(Handle(x,y)){top=y;xs.Add(x);}
        if(top<0)throw new System.InvalidOperationException("Missing visible can handle");
        xs.Sort();int centerX=xs[xs.Count/2],lower=top;
        while(lower>rect.y+rect.height*.70f && Handle(centerX,lower-1))lower--;
        var pivot=new Vector2((centerX+.5f-rect.x)/rect.width,((top+lower)*.5f+.5f-rect.y)/rect.height);
        CourtyardCharacterArt.Sprite(texture,rect,pivot,original.pixelsPerUnit,spritePath,original.name);
        importer.isReadable=readable;importer.SaveAndReimport();AssetDatabase.SaveAssets();
        Debug.Log("ELIAN_CAN_HANDLE_ALIGNED "+pivot+" actual painted bar, not sprite origin estimate");
    }
    static Shape Largest(Color32[] pixels,int width,RectInt cell)
    {
        var seen=new HashSet<int>();List<int> largest=null;
        for(int y=cell.yMin;y<cell.yMax;y++)for(int x=cell.xMin;x<cell.xMax;x++)
        {
            int start=y*width+x;if(pixels[start].a<230 || !seen.Add(start))continue;
            var queue=new List<int>{start};
            for(int i=0;i<queue.Count;i++)
            {
                int at=queue[i],ax=at%width,ay=at/width;
                foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    int nx=ax+d.x,ny=ay+d.y,n=ny*width+nx;
                    if(!cell.Contains(new Vector2Int(nx,ny)) || pixels[n].a<230 || !seen.Add(n))continue;queue.Add(n);
                }
            }
            if(largest==null || queue.Count>largest.Count)largest=queue;
        }
        if(largest==null || largest.Count<1000 || largest.Count>cell.width*cell.height*.9f)throw new System.InvalidOperationException("Invalid hand alpha in "+cell);
        var set=new HashSet<int>(largest);var boundary=new List<Vector2>();
        foreach(int at in largest)if(!set.Contains(at-1)||!set.Contains(at+1)||!set.Contains(at-width)||!set.Contains(at+width))boundary.Add(new Vector2(at%width,at/width));
        int x0=largest.Min(n=>n%width),x1=largest.Max(n=>n%width),y0=largest.Min(n=>n/width),y1=largest.Max(n=>n/width);
        return new Shape{rect=new Rect(x0-4,y0-4,x1-x0+9,y1-y0+9),boundary=boundary};
    }
    static List<Vector2> Hull(List<Vector2> p)
    {
        p=p.OrderBy(v=>v.x).ThenBy(v=>v.y).ToList();var h=new List<Vector2>();
        float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        foreach(var v in p){while(h.Count>=2 && Cross(h[h.Count-2],h[h.Count-1],v)<=0)h.RemoveAt(h.Count-1);h.Add(v);}
        int lower=h.Count;
        for(int i=p.Count-2;i>=0;i--){while(h.Count>lower && Cross(h[h.Count-2],h[h.Count-1],p[i])<=0)h.RemoveAt(h.Count-1);h.Add(p[i]);}
        h.RemoveAt(h.Count-1);return h;
    }
}
