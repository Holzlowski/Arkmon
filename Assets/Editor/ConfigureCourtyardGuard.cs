using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ConfigureCourtyardGuard
{
    private const string Folder="Assets/Art/Intro/CharacterStyle/";
    [MenuItem("Arkmon/Import courtyard guard art and patrol")]
    public static void Run()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Guard"))AssetDatabase.CreateFolder("Assets/Resources","Guard");
        ImportSprites("Guard idle.png",1,1,"Idle");
        ImportRig();
        var texture=CourtyardCharacterArt.Texture(Folder+"Guard expressions.png");
        float w=texture.width/2f,h=texture.height/2f;
        string[] moods={"neutral","friendly","surprised","worried"};
        var portraits=new Sprite[4];
        for(int i=0;i<4;i++)
            portraits[i]=CourtyardCharacterArt.Sprite(texture,new Rect(i%2*w,(1-i/2)*h,w,h),new Vector2(.5f,.5f),100,
                "Assets/Resources/DialoguePortraits/Guard-"+moods[i]+".asset","Guard-"+moods[i]);
        const string portraitPath="Assets/Resources/DialoguePortraits/Guard Portrait Set.asset";
        var set=AssetDatabase.LoadAssetAtPath<DialoguePortraitSet>(portraitPath);
        if(set==null){set=ScriptableObject.CreateInstance<DialoguePortraitSet>();AssetDatabase.CreateAsset(set,portraitPath);}
        set.speaker=CourtyardGuard.Speaker;set.neutral=portraits[0];set.friendly=portraits[1];
        set.surprised=portraits[2];set.worried=portraits[3];EditorUtility.SetDirty(set);
        const string routePath="Assets/Resources/Guard/Patrol.asset";
        // Preserve hand-tuned patrol points on later art imports.
        if(AssetDatabase.LoadAssetAtPath<CourtyardGuardRoute>(routePath)==null)
        {
            var route=ScriptableObject.CreateInstance<CourtyardGuardRoute>();
            route.stops=new[]{
                Stop(14.1f,9.2f,4,true),Stop(23.8f,11.1f),Stop(24.8f,4.5f),Stop(24.8f,-1),
                Stop(26,-3.5f),Stop(31,-4),Stop(38,-2.6f),Stop(39.2f,-.3f,5,false)
            };
            AssetDatabase.CreateAsset(route,routePath);
        }
        AssetDatabase.SaveAssets();
    }
    private static CourtyardGuardRoute.Stop Stop(float x,float z,float wait=0,bool left=false)=>
        new CourtyardGuardRoute.Stop{position=new Vector3(x,0,z),wait=wait,lookLeft=left};
    private static void ImportRig()
    {
        string path=Folder+"Guard puppet.png";
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        int w=texture.width/3,h=texture.height/2;
        string[] parts={"Body","NearArm","FarArm","NearLeg","FarLeg","Sword"};
        float[] heights={1.68f,1.08f,1.04f,1.15f,1.15f,.91f};
        Vector2[] pivots={new Vector2(.5f,0),new Vector2(.65f,.90f),new Vector2(.35f,.90f),
            new Vector2(.43f,.94f),new Vector2(.40f,.94f),new Vector2(.5f,.80f)};
        for(int i=0;i<parts.Length;i++)
        {
            int minX=w,minY=h,maxX=0,maxY=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(pixels[((1-i/3)*h+y)*texture.width+i%3*w+x].a>190)
                {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(maxY<=minY)throw new System.InvalidOperationException("Empty guard puppet part "+parts[i]);
            // Tight source rectangles only; the generated pixels and alpha are never rewritten.
            var rect=new Rect(i%3*w+minX,(1-i/3)*h+minY,maxX-minX+1,maxY-minY+1);
            var sprite=CourtyardCharacterArt.Sprite(texture,rect,pivots[i],rect.height/heights[i],
                "Assets/Resources/Guard/"+parts[i]+".asset","Guard-"+parts[i]);
            if(i==3 || i==4)FitLegOutline(sprite,texture,pixels,rect);
        }
        importer.isReadable=false;importer.SaveAndReimport();
    }
    private static void FitLegOutline(Sprite sprite,Texture2D texture,Color32[] pixels,Rect rect)
    {
        // A convex alpha outline gives the runtime real boot contacts, not empty rectangle corners.
        var points=new List<Vector2>();
        for(int y=0;y<(int)rect.height;y+=2)
        {
            int left=(int)rect.width,right=-1;
            for(int x=0;x<(int)rect.width;x++)
                if(pixels[((int)rect.y+y)*texture.width+(int)rect.x+x].a>190)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);}
            if(right>=left){points.Add(new Vector2(Mathf.Max(0,left-1),y));points.Add(new Vector2(Mathf.Min(rect.width,right+1),y));}
        }
        points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
        var hull=new List<Vector2>();
        float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        foreach(var point in points)
        {while(hull.Count>=2 && Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=0)hull.RemoveAt(hull.Count-1);hull.Add(point);}
        int lower=hull.Count;
        for(int i=points.Count-2;i>=0;i--)
        {while(hull.Count>lower && Cross(hull[hull.Count-2],hull[hull.Count-1],points[i])<=0)hull.RemoveAt(hull.Count-1);hull.Add(points[i]);}
        hull.RemoveAt(hull.Count-1);
        var vertices=new Vector2[hull.Count];
        for(int i=0;i<vertices.Length;i++)vertices[i]=(hull[i]-sprite.pivot)/sprite.pixelsPerUnit;
        var triangles=new ushort[(vertices.Length-2)*3];
        for(int i=0;i<vertices.Length-2;i++){triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)(i+2);}
        sprite.OverrideGeometry(vertices,triangles);EditorUtility.SetDirty(sprite);
    }
    private static void ImportSprites(string file,int columns,int rows,string name)
    {
        string path=Folder+file;
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels=texture.GetPixels32();int w=texture.width/columns,h=texture.height/rows;
        var floors=new int[columns*rows];int maxHeight=1;
        for(int i=0;i<floors.Length;i++)
        {
            int min=h,max=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(pixels[((rows-1-i/columns)*h+y)*texture.width+i%columns*w+x].a>190)
                {min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
            if(max<=min)throw new System.InvalidOperationException("Empty guard sprite cell "+i);
            floors[i]=min;maxHeight=Mathf.Max(maxHeight,max-min+1);
        }
        for(int i=0;i<floors.Length;i++)
        {
            string pose=name+(floors.Length==1?"":i.ToString());
            CourtyardCharacterArt.Sprite(texture,new Rect(i%columns*w,(rows-1-i/columns)*h,w,h),
                new Vector2(.5f,floors[i]/(float)h),maxHeight/2.5f,"Assets/Resources/Guard/"+pose+".asset","Guard-"+pose);
        }
        importer.isReadable=false;importer.SaveAndReimport();
    }
}