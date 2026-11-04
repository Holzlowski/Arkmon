using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CourtyardStable
{
    public static readonly Vector3 Position=new Vector3(-11.5f,0,5);
    const string Art="Assets/Art/Intro/";
    static Material wood,stone,slate,plaster,hay,leather,water,iron;
    public static void Build(Transform root,Material masonry)
    {
        foreach(Transform child in root.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        root.position=Position;root.rotation=Quaternion.identity;root.localScale=Vector3.one;
        var oldSprite=root.GetComponent<SpriteRenderer>();if(oldSprite!=null)Object.DestroyImmediate(oldSprite);
        wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"Marta painted oak.mat");stone=masonry;
        slate=Paint("Hannes slate shingles",new Color(.25f,.34f,.32f),"Courtyard slate courses.asset",.75f);
        plaster=Paint("Hannes warm plaster",new Color(.77f,.72f,.55f),"Courtyard earth grain.asset",.25f);
        hay=Paint("Hannes golden straw",new Color(.66f,.49f,.19f),"Courtyard earth grain.asset",.6f);
        leather=Paint("Hannes saddle leather",new Color(.29f,.15f,.08f),null,0);
        iron=Paint("Hannes dark iron",new Color(.16f,.18f,.17f),null,0);
        water=Paint("Hannes trough water",new Color(.25f,.40f,.43f),null,0);
        var collider=root.GetComponent<BoxCollider>()??root.gameObject.AddComponent<BoxCollider>();
        collider.center=new Vector3(0,1.65f,.15f);collider.size=new Vector3(7,3.3f,3.7f);
        Box(root,"Stable stone footing",new Vector3(0,.17f,0),new Vector3(7.15f,.34f,4),stone).gameObject.AddComponent<BoxCollider>();
        Box(root,"Stable limewashed rear",new Vector3(0,1.8f,1.8f),new Vector3(7,3.25f,.20f),plaster);
        foreach(float x in new[]{-3.4f,3.4f})
        {
            Box(root,"Stable limewashed side",new Vector3(x,1.8f,0),new Vector3(.20f,3.25f,3.7f),plaster);
            foreach(float z in new[]{-1.9f,1.9f})Box(root,"Stable oak corner post",new Vector3(x,1.8f,z),new Vector3(.24f,3.6f,.24f),wood);
        }
        Box(root,"Stable front oak lintel",new Vector3(0,3.5f,-1.9f),new Vector3(7.2f,.28f,.30f),wood);
        Box(root,"Stable central post",new Vector3(.2f,1.7f,-1.9f),new Vector3(.24f,3.4f,.24f),wood);
        foreach(float x in new[]{-3.4f,.2f,3.4f})
            Beam(root,"Stable corner brace",new Vector3(x,2.75f,-1.9f),new Vector3(x+(x<1?.65f:-.65f),3.45f,-1.9f),.16f,wood);
        // Two low gates reveal both horses from the gameplay camera.
        foreach(float center in new[]{-1.65f,1.8f})
        {
            for(int i=0;i<2;i++)Box(root,"Low horse stall gate",new Vector3(center,.48f+i*.30f,-2.17f),new Vector3(2.9f,.14f,.15f),wood);
            foreach(float side in new[]{-1f,1f})
                Box(root,"Stall gate upright",new Vector3(center+side*1.34f,.68f,-2.17f),new Vector3(.12f,1.05f,.18f),wood);
            Box(root,"Stall gate latch",new Vector3(center+1.20f,.85f,-2.28f),new Vector3(.22f,.07f,.07f),iron);
        }
        Box(root,"Stable stall divider",new Vector3(.2f,.72f,.05f),new Vector3(.16f,.95f,3.4f),wood);
        var chestnut=Anchor(root,"Chestnut horse",new Vector3(-1.65f,.35f,-2.03f));
        chestnut.gameObject.AddComponent<CourtyardHorse>().Configure(Resources.Load<Sprite>("StableHorses/Chestnut"),true,.7f);
        var dun=Anchor(root,"Dun horse",new Vector3(1.72f,.35f,-2.03f));
        dun.gameObject.AddComponent<CourtyardHorse>().Configure(Resources.Load<Sprite>("StableHorses/Dun"),false,4.1f);
        // A small rack sits beneath the chestnut's lowered muzzle.
        var rack=Anchor(root,"Chestnut hay rack",new Vector3(-.55f,.85f,-2.1f));
        Box(rack,"Rack straw",Vector3.zero,new Vector3(.64f,.16f,.32f),hay);
        foreach(float x in new[]{-.28f,-.14f,0,.14f,.28f})
        {
            Beam(rack,"Hay rack upright",new Vector3(x,-.24f,-.20f),new Vector3(x,.08f,-.20f),.035f,wood);
            var strand=Box(rack,"Feeding hay strands",new Vector3(x,.11f,0),new Vector3(.025f,.025f,.36f),hay);
            strand.localRotation=Quaternion.Euler(0,x*90,0);
        }
        Box(rack,"Hay rack lower rail",new Vector3(0,-.18f,-.20f),new Vector3(.70f,.06f,.07f),wood);
        const float half=2.15f,rise=1.45f;
        float angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
        foreach(float sign in new[]{-1f,1f})
        {
            var slope=Box(root,"Stable slate roof",new Vector3(0,4.325f,sign*half/2),new Vector3(7.8f,.19f,Mathf.Sqrt(half*half+rise*rise)),slate);
            slope.localRotation=Quaternion.Euler(sign*angle,0,0);
            Box(root,"Stable eaves fascia",new Vector3(0,3.6f,sign*half),new Vector3(7.85f,.22f,.18f),wood);
            foreach(float x in new[]{-3.8f,3.8f})
                Beam(root,"Stable gable fascia",new Vector3(x,3.6f,sign*half),new Vector3(x,5.05f,0),.17f,wood);
        }
        Box(root,"Stable roof ridge",new Vector3(0,5.08f,0),new Vector3(7.9f,.18f,.22f),wood);
        Gables(root);
        // Hay is layered with narrow stalks, not a single flat-colored cube.
        for(int bale=0;bale<3;bale++)
        {
            var at=new Vector3(.8f+bale*.8f,.38f,.8f);
            Box(root,"Bound hay bale",at,new Vector3(.72f,.55f,.85f),hay);
            for(int strand=0;strand<8;strand++)
                Box(root,"Loose straw stalk",at+new Vector3(-.3f+strand*.085f,.29f,0),new Vector3(.035f,.04f,.88f),hay);
            foreach(float z in new[]{-.25f,.25f})Box(root,"Hay binding",at+new Vector3(0,.30f,z),new Vector3(.73f,.025f,.04f),leather);
        }
        // The trough sits outside the bay and has a hollow rim with recessed water.
        var trough=Anchor(root,"Stable water trough",new Vector3(3.8f,0,-.8f));
        Box(trough,"Trough base",new Vector3(0,.28f,0),new Vector3(.72f,.4f,1.65f),wood);
        foreach(float x in new[]{-.34f,.34f})Box(trough,"Trough long rim",new Vector3(x,.6f,0),new Vector3(.10f,.40f,1.75f),wood);
        foreach(float z in new[]{-.83f,.83f})Box(trough,"Trough end rim",new Vector3(0,.6f,z),new Vector3(.72f,.40f,.10f),wood);
        Box(trough,"Recessed still water",new Vector3(0,.65f,0),new Vector3(.60f,.02f,1.55f),water);
        var tc=trough.gameObject.AddComponent<BoxCollider>();tc.center=new Vector3(0,.4f,0);tc.size=new Vector3(.82f,.8f,1.85f);
        SaddleStand(root);
        Anchor(root,"Hannes work spot",new Vector3(.5f,0,-3.5f));
        Anchor(root,"Stable sign spot",new Vector3(-3.6f,0,-3.1f));
        Anchor(root,"Stable approach",new Vector3(.5f,0,-4.8f));
        for(int i=0;i<22;i++)
        {
            var chip=Box(root,"Forecourt scattered straw",new Vector3(-.5f+(i%6)*.55f,.02f,-2.8f-(i/6)*.3f),new Vector3(.18f,.018f,.025f),hay);
            chip.localRotation=Quaternion.Euler(0,i*47,0);
        }
    }
    static void SaddleStand(Transform root)
    {
        // The saddle is beside Hannes, beneath his extended brushing hand.
        var rack=Anchor(root,"Stable tack rail",new Vector3(1.6f,0,-3.0f));
        rack.localScale=new Vector3(1,.87f,1); // Match the brush sole in the shared character poses.
        foreach(float x in new[]{-.48f,.48f})
        {
            foreach(float z in new[]{-.28f,.28f})
                Beam(rack,"Splayed saddle stand leg",new Vector3(x,.04f,z),new Vector3(x,.84f,0),.10f,wood);
            Box(rack,"Stand foot",new Vector3(x,.05f,0),new Vector3(.17f,.10f,.72f),wood);
        }
        Box(rack,"Saddle stand spine",new Vector3(0,.85f,0),new Vector3(1.2f,.14f,.18f),wood);
        Box(rack,"Stand lower brace",new Vector3(0,.35f,0),new Vector3(1.1f,.09f,.12f),wood);
        // Curved leather seat: raised pommel/cantle, depressed centre, rounded edges.
        var vertices=new System.Collections.Generic.List<Vector3>();
        var triangles=new System.Collections.Generic.List<int>();
        const int along=16,across=10;
        for(int i=0;i<=along;i++)
        for(int j=0;j<=across;j++)
        {
            float u=i/(float)along*2-1,v=j/(float)across*2-1;
            float y=1.05f+.19f*Mathf.Pow(Mathf.Abs(u),4)-.07f*v*v;
            vertices.Add(new Vector3(u*.65f,y,v*.36f*(1-.16f*Mathf.Abs(u))));
            if(i==0||j==0)continue;
            int n=vertices.Count-1,a=n-across-2,b=n-across-1,c=n-1;
            triangles.AddRange(new[]{a,b,n,a,n,c});
        }
        Mesh(rack,"Hannes curved saddle seat",vertices.ToArray(),triangles.ToArray(),leather);
        foreach(float sign in new[]{-1f,1f})
        {
            // Rounded hanging skirt/flap, inset panel and visible stitch edging.
            Vector3[] outline={new Vector3(-.48f,1.01f,sign*.32f),new Vector3(.48f,1.01f,sign*.32f),
                new Vector3(.43f,.72f,sign*.39f),new Vector3(.22f,.55f,sign*.41f),
                new Vector3(-.20f,.55f,sign*.41f),new Vector3(-.43f,.73f,sign*.39f)};
            Mesh(rack,"Hannes saddle leather flap "+sign,outline,new[]{0,1,2,0,2,3,0,3,4,0,4,5,2,1,0,3,2,0,4,3,0,5,4,0},leather);
            for(int i=0;i<outline.Length;i++)
                Beam(rack,"Saddle stitched edge",outline[i]+Vector3.forward*sign*.006f,outline[(i+1)%outline.Length]+Vector3.forward*sign*.006f,.018f,hay);
            Box(rack,"Stirrup leather",new Vector3(.15f,.66f,sign*.44f),new Vector3(.045f,.45f,.035f),leather);
            Beam(rack,"Stirrup left iron",new Vector3(.07f,.48f,sign*.45f),new Vector3(.015f,.32f,sign*.45f),.035f,iron);
            Beam(rack,"Stirrup right iron",new Vector3(.23f,.48f,sign*.45f),new Vector3(.285f,.32f,sign*.45f),.035f,iron);
            Box(rack,"Stirrup tread",new Vector3(.15f,.31f,sign*.45f),new Vector3(.30f,.04f,.09f),iron);
        }
        foreach(float side in new[]{-1f,1f})
        for(int segment=0;segment<10;segment++)
        {
            float a=segment/10f*Mathf.PI,b=(segment+1)/10f*Mathf.PI;
            Vector3 Rim(float t)=>new Vector3(side*.65f,1.16f+.12f*Mathf.Sin(t),.30f*Mathf.Cos(t));
            Beam(rack,side<0?"Saddle raised pommel":"Saddle raised cantle",Rim(a),Rim(b),.055f,leather);
        }
        var collider=rack.gameObject.AddComponent<BoxCollider>();
        collider.center=new Vector3(0,.65f,0);collider.size=new Vector3(1.4f,1.3f,1);
    }
    static void Mesh(Transform root,string name,Vector3[] vertices,int[] triangles,Material material)
    {
        string path=Art+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var part=Anchor(root,name,Vector3.zero);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static Transform Anchor(Transform root,string name,Vector3 at)
    {var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=at;return t;}
    static Transform Box(Transform root,string name,Vector3 at,Vector3 size,Material mat)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);
        obj.transform.localPosition=at;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
    }
    static void Beam(Transform root,string name,Vector3 a,Vector3 b,float width,Material mat)
    {var t=Box(root,name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static Material Paint(string name,Color color,string texture,float detail)
    {
        string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Arkmon/Tower Painted Light"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_TextureScale",.65f);m.SetFloat("_DetailStrength",detail);
        m.SetTexture("_BaseMap",texture==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));
        EditorUtility.SetDirty(m);return m;
    }
    static void Gables(Transform root)
    {
        string path=Art+"Hannes solid gables.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.vertices=new[]{new Vector3(-3.4f,3.5f,-1.9f),new Vector3(-3.4f,3.5f,1.9f),new Vector3(-3.4f,4.95f,0),
            new Vector3(3.4f,3.5f,-1.9f),new Vector3(3.4f,3.5f,1.9f),new Vector3(3.4f,4.95f,0)};
        mesh.triangles=new[]{0,1,2,3,5,4};mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var g=Anchor(root,"Stable solid gables",Vector3.zero);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=plaster;
    }
}