using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// A single architectural family for Elian's tower, residence and raised approach.
public static class CourtyardWestArchitecture
{
    const string Art="Assets/Art/Intro/";
    static Material masonry,trim,slate,timber,glass,iron,leaf,darkLeaf,gold;
    static Mesh leafMesh;

    public static void Build(Transform courtyard,Transform tower,Material stone,Material oak,Func<float,float,float> height)
    {
        masonry=stone;timber=oak;
        leafMesh=null;
        trim=Material("Aurenfeld carved limestone",new Color(.66f,.57f,.40f));
        slate=Material("Aurenfeld blue slate",new Color(.22f,.34f,.46f));
        slate.shader=Shader.Find("Arkmon/Tower Painted Light");
        slate.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Courtyard slate courses.asset"));
        slate.SetFloat("_TextureScale",.34f);slate.SetFloat("_DetailStrength",.55f);
        glass=Material("Aurenfeld window recess",new Color(.075f,.15f,.18f));
        iron=Material("Aurenfeld aged bronze",new Color(.27f,.21f,.12f));
        leaf=Material("Aurenfeld ivy light",new Color(.29f,.43f,.12f));
        darkLeaf=Material("Aurenfeld ivy shade",new Color(.12f,.25f,.085f));
        gold=Material("Aurenfeld flower gold",new Color(.92f,.63f,.20f));

        // Rebuilding must not accumulate details on the retained tower.
        foreach(Transform part in tower.Cast<Transform>().ToArray())
            if(part.name=="Aurenfeld tower details" || part.name.StartsWith("Tower window"))Object.DestroyImmediate(part.gameObject);
        foreach(var renderer in tower.GetComponentsInChildren<MeshRenderer>())
            if(renderer.name.ToLowerInvariant().Contains("roof"))renderer.sharedMaterial=slate;
        var details=Root(tower,"Aurenfeld tower details",Vector3.zero);
        foreach(float y in new[]{5.4f,8.7f})
            Window(details,new Vector3(0,y-1.25f,-2.64f),1.12f,2.5f);
        // The adjacent octagonal face carries a second visible window bay.
        var side=Root(details,"Tower side window bay",new Vector3(1.85f,0,-1.85f));
        side.localRotation=Quaternion.Euler(0,-45,0);
        Window(side,new Vector3(0,4.15f,-.02f),.95f,2.4f);
        Banner(details,new Vector3(-1.35f,6.8f,-2.53f),.65f,1.7f);

        Residence(courtyard,"Castle west wing",new Vector3(-13.8f,1.4f,15.6f),13.6f,7.6f,7,3.8f);
        Residence(courtyard,"Aurenfeld upper residence",new Vector3(-13.8f,0,22.9f),13.6f,13.3f,6.4f,4.4f);
        Residence(courtyard,"Tower connecting wing",new Vector3(-25.2f,1.4f,17.2f),13.4f,3.2f,2.6f,1.2f);
        Terrace(courtyard);
        Planting(courtyard,height);
        Lighting();
        foreach(var material in new[]{trim,slate,glass,iron,leaf,darkLeaf,gold})EditorUtility.SetDirty(material);
    }

    static Transform Root(Transform parent,string name,Vector3 at)
    {var obj=new GameObject(name).transform;obj.SetParent(parent,false);obj.localPosition=at;return obj;}

    static Material Material(string name,Color color)
    {
        string path=Art+name+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(result==null){result=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,path);}
        result.SetColor("_BaseColor",color);if(result.HasProperty("_Smoothness"))result.SetFloat("_Smoothness",.06f);
        return result;
    }

    static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
        obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }

    static void Residence(Transform parent,string name,Vector3 at,float width,float wallHeight,float depth,float rise)
    {
        var house=Root(parent,name,at);
        Box(house,name+" structure",Vector3.up*(wallHeight*.5f),new Vector3(width,wallHeight,depth),masonry,true);
        float front=-depth*.5f;
        bool lowGallery=wallHeight<5;
        Box(house,"Carved foundation course",new Vector3(0,.25f,0),new Vector3(width+.26f,.5f,depth+.26f),trim);
        Box(house,"Carved eaves course",new Vector3(0,wallHeight-.12f,0),new Vector3(width+.32f,.24f,depth+.32f),trim);
        Box(house,"Lower facade stringcourse",new Vector3(0,lowGallery?.50f:wallHeight*.43f,front-.12f),new Vector3(width,.16f,.30f),trim);
        Roof(house,"Residence slate roof",width+.55f,depth+.65f,wallHeight,rise);
        int bays=Mathf.Max(1,Mathf.FloorToInt(width/3.4f));
        for(int i=0;i<bays;i++)
        {
            // The east end of the gallery is behind the projecting west residence.
            float x=name=="Tower connecting wing"?Mathf.Lerp(-4.4f,2.8f,i/(float)Mathf.Max(1,bays-1)):-width*.5f+(i+.5f)*width/bays;
            Window(house,new Vector3(x,lowGallery?.80f:wallHeight*.50f,front-.055f),lowGallery?1.10f:1.35f,lowGallery?1.75f:wallHeight>9?3.4f:2.6f);
            if(width>8 && !lowGallery)Window(house,new Vector3(x,.85f,front-.055f),1.15f,2.1f);
        }
        for(int i=0;i<Mathf.CeilToInt(width/.85f);i++)
            Box(house,"Eaves carved corbel",new Vector3(-width/2+.35f+i*.85f,wallHeight-.43f,front-.16f),new Vector3(.19f,.47f,.36f),trim);
        foreach(float side in new[]{-1f,1f})
        {
            for(int i=0;i<Mathf.FloorToInt(wallHeight/.65f);i++)
                Box(house,"Corner dressed stone",new Vector3(side*(width/2-.16f),.35f+i*.65f,front-.07f),
                    new Vector3(i%2==0?.6f:.38f,.5f,.25f),trim);
        }
        if(width>8 && !lowGallery)
        {
            foreach(float x in new[]{-width*.28f,width*.28f})
            {
                var dormer=Root(house,"Slate roof dormer",new Vector3(x,wallHeight+.25f,front+.82f));
                Box(dormer,"Dormer stone face",new Vector3(0,.8f,0),new Vector3(1.6f,1.6f,1.9f),masonry);
                Roof(dormer,"Dormer roof",1.95f,2.2f,1.6f,.95f);
                Window(dormer,new Vector3(0,.17f,-.99f),.69f,1.16f);
            }
            Box(house,"Residence chimney",new Vector3(width*.35f,wallHeight+rise-.2f,1),new Vector3(.85f,2.6f,.85f),masonry);
            Box(house,"Chimney cap",new Vector3(width*.35f,wallHeight+rise+1.1f,1),new Vector3(1.12f,.24f,1.12f),trim);
            Banner(house,new Vector3(0,wallHeight*.70f,front-.29f),.95f,2.2f);
        }
    }

    static void Roof(Transform parent,string name,float width,float depth,float baseY,float rise)
    {
        float half=depth*.5f, angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
        foreach(float side in new[]{-1f,1f})
        {
            var slope=Box(parent,name,new Vector3(0,baseY+rise*.5f,side*half*.5f),new Vector3(width,.2f,Mathf.Sqrt(half*half+rise*rise)),slate);
            slope.localRotation=Quaternion.Euler(side*angle,0,0);
            Box(parent,"Slate eaves rim",new Vector3(0,baseY,side*half),new Vector3(width+.06f,.15f,.19f),iron);
        }
        Box(parent,"Slate ridge cap",new Vector3(0,baseY+rise,0),new Vector3(width+.10f,.2f,.23f),slate);
        var vertices=new[]{new Vector3(-width/2,baseY,-half),new Vector3(-width/2,baseY,half),new Vector3(-width/2,baseY+rise,0),
            new Vector3(width/2,baseY,-half),new Vector3(width/2,baseY,half),new Vector3(width/2,baseY+rise,0)};
        MeshPart(parent,name+" gables",vertices,new[]{0,1,2,3,5,4},masonry);
    }

    static void Window(Transform parent,Vector3 at,float width,float height)
    {
        var window=Root(parent,"Arched residence window",at);
        float r=width*.5f,spring=height-r;
        var outline=new List<Vector3>{new Vector3(-r,0,0),new Vector3(r,0,0)};
        for(int i=0;i<=12;i++)
        {float a=i*Mathf.PI/12;outline.Add(new Vector3(Mathf.Cos(a)*r,spring+Mathf.Sin(a)*r,0));}
        var triangles=new List<int>();for(int i=1;i<outline.Count-1;i++)triangles.AddRange(new[]{0,i+1,i});
        MeshPart(window,"Arched dark glazing",outline.ToArray(),triangles.ToArray(),glass);
        foreach(float side in new[]{-1f,1f})
            Box(window,"Carved window jamb",new Vector3(side*(r+.10f),spring*.5f,-.09f),new Vector3(.2f,spring+.08f,.26f),trim);
        for(int i=0;i<11;i++)
        {
            float a=(i+.5f)*Mathf.PI/11;
            var block=Box(window,"Arch wedge",new Vector3(Mathf.Cos(a)*(r+.10f),spring+Mathf.Sin(a)*(r+.10f),-.10f),new Vector3(.24f,(r+.10f)*Mathf.PI/11+.035f,.30f),trim);
            block.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
        }
        Box(window,"Window sill",new Vector3(0,-.06f,-.13f),new Vector3(width+.5f,.20f,.52f),trim);
        Box(window,"Window mullion",new Vector3(0,height*.48f,-.09f),new Vector3(.075f,height*.96f,.10f),trim);
        Box(window,"Window crossbar",new Vector3(0,spring*.6f,-.09f),new Vector3(width,.07f,.1f),iron);
    }

    static void Banner(Transform parent,Vector3 at,float width,float height)
    {
        var cloth=Material("Aurenfeld forest banner",new Color(.075f,.19f,.15f));EditorUtility.SetDirty(cloth);
        Box(parent,"Aurenfeld hanging banner",at,new Vector3(width,height,.055f),cloth);
        Box(parent,"Banner bronze rail",at+Vector3.up*(height*.5f+.04f),new Vector3(width+.3f,.07f,.12f),iron);
        var crest=AssetDatabase.LoadAssetAtPath<Material>(Art+"Aurenfeld painted crest.mat");
        if(crest==null)return;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Aurenfeld residence crest";
        quad.transform.SetParent(parent,false);quad.transform.localPosition=at+new Vector3(0,height*.12f,-.04f);
        quad.transform.localScale=new Vector3(width*.82f,width*.92f,1);quad.GetComponent<Renderer>().sharedMaterial=crest;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
    }

    static void Terrace(Transform parent)
    {
        var root=Root(parent,"Aurenfeld raised approach",Vector3.zero);
        Box(root,"Raised tower terrace",new Vector3(-34,.7f,12.4f),new Vector3(10,1.4f,9),masonry,true).tag="Ground";
        Box(root,"Residence terrace extension",new Vector3(-17.5f,.7f,12f),new Vector3(23,1.4f,8.2f),masonry,true).tag="Ground";
        // Pale coping makes the retaining wall legible against the darker planting below it.
        foreach(var span in new[]{new Vector2(-37.95f,2.1f),new Vector2(-18.55f,25.1f)})
        {
            Box(root,"Terrace coping",new Vector3(span.x,1.43f,7.84f),new Vector3(span.y,.16f,.55f),trim);
            Box(root,"Stone terrace handrail",new Vector3(span.x,2.25f,7.94f),new Vector3(span.y,.15f,.30f),trim,true);
            int count=Mathf.FloorToInt(span.y/.65f);
            for(int i=0;i<=count;i++)
                Box(root,"Terrace baluster",new Vector3(span.x-span.y/2+i*span.y/count,1.86f,7.94f),new Vector3(.17f,.7f,.20f),trim,true);
        }
        for(int i=0;i<7;i++)
        {
            float h=(7-i)*.2f;
            Box(root,"Tower stair tread",new Vector3(-34,h*.5f,7.65f-i*.42f),new Vector3(5.4f,h,.44f),masonry);
            Box(root,"Carved stair lip",new Vector3(-34,h+.014f,7.46f-i*.42f),new Vector3(5.45f,.045f,.10f),trim);
        }
        // The Rigidbody walker has no step-up motor: a continuous collision wedge
        // avoids snagging on the decorative 20 cm risers in either direction.
        string rampPath=Art+"Aurenfeld stair collision.asset";
        var ramp=AssetDatabase.LoadAssetAtPath<Mesh>(rampPath);
        if(ramp==null){ramp=new Mesh();AssetDatabase.CreateAsset(ramp,rampPath);}
        ramp.Clear();
        ramp.vertices=new[]{new Vector3(-2.7f,0,4.6f),new Vector3(2.7f,0,4.6f),new Vector3(-2.7f,1.4f,7.92f),new Vector3(2.7f,1.4f,7.92f),new Vector3(-2.7f,0,7.92f),new Vector3(2.7f,0,7.92f)};
        ramp.triangles=new[]{0,2,1,1,2,3,0,4,2,1,3,5,2,4,3,3,4,5,0,1,4,1,5,4};
        ramp.RecalculateNormals();ramp.RecalculateBounds();EditorUtility.SetDirty(ramp);
        var rampObject=Root(root,"Tower stairs walkable slope",new Vector3(-34,0,0));
        rampObject.gameObject.tag="Ground";
        var rampCollider=rampObject.gameObject.AddComponent<MeshCollider>();rampCollider.sharedMesh=ramp;rampCollider.convex=true;
        foreach(float x in new[]{-37f,-31f})
        {
            for(int i=0;i<7;i++)
            {
                float h=(7-i)*.2f;
                Box(root,"Stair cheek masonry",new Vector3(x,(h+.7f)*.5f,7.65f-i*.42f),new Vector3(.42f,h+.7f,.44f),masonry,true);
                Box(root,"Stair cheek cap",new Vector3(x,h+.74f,7.65f-i*.42f),new Vector3(.54f,.12f,.47f),trim);
            }
            Box(root,"Stair foot pillar",new Vector3(x,.60f,4.95f),new Vector3(.65f,1.2f,.65f),masonry,true);
            Box(root,"Stair foot pillar cap",new Vector3(x,1.23f,4.95f),new Vector3(.83f,.16f,.83f),trim);
        }
        // Keep the existing doorway's single carved arch and interaction trigger.
    }

    static void Planting(Transform parent,Func<float,float,float> height)
    {
        var root=Root(parent,"Aurenfeld terrace planting",Vector3.zero);
        // Diamond leaves form branching ivy silhouettes instead of spherical green dots.
        for(int stem=0;stem<9;stem++)
        {
            float x=-38.6f+stem*2.1f;if(x>-37 && x<-31)continue;
            for(int i=0;i<18;i++)
            {
                float y=.25f+i*.12f,z=7.60f;
                Leaf(root,new Vector3(x+Mathf.Sin(i*.7f+stem)*.18f,y,z),.17f,i%2==0?leaf:darkLeaf,i*31);
            }
        }
        for(int stem=0;stem<4;stem++)
        for(int i=0;i<35;i++)
        {
            float y=1.5f+i*.16f;
            float x=-19.9f+stem*3.8f+Mathf.Sin(i*.32f+stem)*.23f;
            Leaf(root,new Vector3(x,y,11.98f),.18f,i%3==0?leaf:darkLeaf,stem*31+i*47);
        }
        var random=new System.Random(402);
        for(int i=0;i<140;i++)
        {
            float x=-41+(float)random.NextDouble()*20,z=3.3f+(float)random.NextDouble()*4.2f;
            if(x>-37.7f && x<-30.2f || x>-30.2f && z<5.5f)continue;
            float y=height(x,z);
            for(int blade=0;blade<3;blade++)
                Leaf(root,new Vector3(x+blade*.08f,y+.12f+blade*.06f,z),.14f+blade*.04f,blade==0?darkLeaf:leaf,i*23+blade*70);
            if(i%5==0)Leaf(root,new Vector3(x,y+.38f,z-.06f),.085f,gold,i*16);
        }
    }

    static void Leaf(Transform parent,Vector3 at,float size,Material material,float angle)
    {
        const string path=Art+"Aurenfeld folded leaf.asset";
        if(leafMesh==null)
        {
            leafMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(leafMesh==null){leafMesh=new Mesh();AssetDatabase.CreateAsset(leafMesh,path);}
            leafMesh.Clear();leafMesh.name="Folded pointed ivy leaf";
            var vertices=new[]{new Vector3(0,1,0),new Vector3(-.6f,.15f,0),new Vector3(0,-1,0),new Vector3(.6f,.15f,0),new Vector3(0,.1f,-.18f)};
            // Separate back vertices preserve opposing normals on both leaf faces.
            leafMesh.vertices=vertices.Concat(vertices).ToArray();
            leafMesh.triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0,6,9,5,7,9,6,8,9,7,5,9,8};
            leafMesh.RecalculateNormals();leafMesh.RecalculateBounds();EditorUtility.SetDirty(leafMesh);
        }
        var obj=new GameObject("Painted ivy leaf",typeof(MeshFilter),typeof(MeshRenderer));
        obj.transform.SetParent(parent,false);obj.transform.localPosition=at;
        obj.transform.localScale=Vector3.one*size;
        obj.transform.localRotation=Quaternion.Euler(15,Mathf.Sin(angle)*25,angle);
        obj.GetComponent<MeshFilter>().sharedMesh=leafMesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
    }

    static void MeshPart(Transform parent,string name,Vector3[] vertices,int[] triangles,Material material)
    {
        string asset=Art+"West "+name+" "+parent.name+".asset";
        // Dimensions disambiguate windows/dormers that share their human-readable names.
        asset=asset.Replace(".asset"," "+vertices.Length+" "+vertices.Max(v=>v.y).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" "+vertices.Max(v=>v.x).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".asset");
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,asset);}
        mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
    }

    static void Lighting()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.66f,.72f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.50f,.43f,.31f);
        RenderSettings.ambientGroundColor=new Color(.32f,.28f,.14f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=180;RenderSettings.fogEndDistance=1600;RenderSettings.fogColor=new Color(.79f,.74f,.64f);
        var morningSky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Courtyard morning sky.mat");
        if(morningSky!=null)RenderSettings.skybox=morningSky;
        var sun=GameObject.Find("Directional Light")?.GetComponent<Light>();
        if(sun==null)return;
        sun.transform.rotation=Quaternion.Euler(22,65,0);
        sun.color=new Color(1,.80f,.48f);sun.intensity=1.65f;sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
    }
}
