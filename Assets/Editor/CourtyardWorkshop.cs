using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CourtyardWorkshop
{
    const string Art="Assets/Art/Intro/";
    public static readonly Vector3 Position=new Vector3(-37.5f,0,-5f);
    public static Vector2[] Approach => new[]{new Vector2(-25,-3.2f),new Vector2(-31,-7.5f),
        new Vector2(Position.x+4.5f,Position.z-5.8f),new Vector2(Position.x+1.6f,Position.z-5.2f),
        new Vector2(Position.x+1.6f,Position.z-3.8f)};
    static Material wood, plaster, roof, stone, dark, iron, cutWood;
    public static void Build(Transform courtyard,Transform bench,Material masonry)
    {
        stone=masonry;
        wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"Marta painted oak.mat");
        plaster=Paint("Ida warm plaster",new Color(.80f,.74f,.58f),"Courtyard earth grain.asset",.28f);
        roof=Paint("Ida brown shingles",new Color(.35f,.28f,.20f),"Courtyard slate courses.asset",.90f);
        dark=Paint("Ida window recess",new Color(.12f,.15f,.14f),null,1);
        iron=Paint("Ida forged iron",new Color(.22f,.23f,.20f),null,1);
        cutWood=Paint("Ida fresh cut wood",new Color(.70f,.49f,.25f),"Courtyard earth grain.asset",.45f);

        var root=Root(courtyard,"Ida workshop",Position);
        Box(root,"Workshop plaster structure",new Vector3(0,1.8f,0),new Vector3(6.2f,3.6f,3.6f),plaster,true);
        Box(root,"Workshop stone plinth",new Vector3(0,.22f,0),new Vector3(6.3f,.44f,3.7f),stone);
        foreach(float x in new[]{-3f,0,3f})
            Box(root,"Workshop facade post",new Vector3(x,1.9f,-1.90f),new Vector3(.20f,3.4f,.24f),wood);
        foreach(float y in new[]{.55f,3.5f})
            Box(root,"Workshop facade crossbeam",new Vector3(0,y,-1.91f),new Vector3(6.2f,.20f,.24f),wood);
        foreach(float side in new[]{-1f,1f})
            Beam(root,"Workshop diagonal brace",new Vector3(side*3,2.9f,-1.93f),new Vector3(side*2.05f,1.9f,-1.93f),.13f,wood);

        const float run=3.5f,rise=1.8f;
        float angle=Mathf.Atan2(rise,run)*Mathf.Rad2Deg;
        foreach(float side in new[]{-1f,1f})
        {
            var slope=Box(root,"Workshop shingled roof",new Vector3(side*run*.5f,4.5f,0),
                new Vector3(Mathf.Sqrt(run*run+rise*rise),.19f,4.55f),roof);
            slope.localRotation=Quaternion.Euler(0,0,-side*angle);
            Box(root,"Workshop eaves fascia",new Vector3(side*run,3.56f,0),new Vector3(.15f,.21f,4.65f),wood);
            Beam(root,"Workshop gable fascia",new Vector3(side*run,3.6f,-2.3f),new Vector3(0,5.4f,-2.3f),.17f,wood);
        }
        Box(root,"Workshop ridge cap",new Vector3(0,5.43f,0),new Vector3(.25f,.17f,4.7f),wood);
        Gables(root);
        Box(root,"Workshop gable king post",new Vector3(0,4.4f,-1.88f),new Vector3(.16f,1.65f,.19f),wood);
        foreach(float x in new[]{-.45f,-.15f,.15f,.45f})
            Box(root,"Attic ventilation slat",new Vector3(x,4.1f,-1.94f),new Vector3(.11f,.50f,.12f),dark);

        // The entrance occupies the right half; the work station stays to its left.
        Box(root,"Workshop door recess",new Vector3(1.6f,1.35f,-1.86f),new Vector3(1.45f,2.5f,.12f),dark);
        for(int i=0;i<7;i++)
            Box(root,"Workshop door plank",new Vector3(.98f+i*.20f,1.31f,-1.96f),new Vector3(.19f,2.36f,.11f),wood);
        foreach(float x in new[]{.82f,2.38f})
            Box(root,"Workshop door jamb",new Vector3(x,1.35f,-2.03f),new Vector3(.16f,2.6f,.22f),wood);
        Box(root,"Workshop door lintel",new Vector3(1.6f,2.64f,-2.02f),new Vector3(1.78f,.18f,.24f),wood);
        foreach(float y in new[]{.65f,2.05f})
            Box(root,"Workshop door iron strap",new Vector3(1.6f,y,-2.04f),new Vector3(1.35f,.08f,.06f),iron);
        Box(root,"Workshop latch",new Vector3(2.10f,1.3f,-2.1f),new Vector3(.06f,.23f,.11f),iron);
        Box(root,"Workshop worn threshold",new Vector3(1.6f,.03f,-2.17f),new Vector3(1.80f,.06f,.50f),stone);
        Window(root);

        var canopy=Box(root,"Workshop shelter roof",new Vector3(0,3.1f,-2.85f),new Vector3(6.8f,.18f,2.1f),roof);
        canopy.localRotation=Quaternion.Euler(-10,0,0);
        Box(root,"Workshop porch fascia",new Vector3(0,2.91f,-3.87f),new Vector3(6.9f,.20f,.20f),wood);
        foreach(float x in new[]{-3.15f,3.15f})
        {
            Box(root,"Workshop shelter post",new Vector3(x,1.45f,-3.65f),new Vector3(.22f,2.9f,.22f),wood,true);
            Box(root,"Workshop post stone foot",new Vector3(x,.18f,-3.65f),new Vector3(.37f,.36f,.37f),stone);
            Beam(root,"Workshop shelter brace",new Vector3(x,2.30f,-3.65f),new Vector3(x-Mathf.Sign(x)*.65f,2.92f,-3.65f),.14f,wood);
        }

        bench.SetParent(root,false);bench.localPosition=new Vector3(-.8f,0,-3.5f);
        bench.localRotation=Quaternion.identity;bench.localScale=new Vector3(.8f,.82f,.85f);
        foreach(var renderer in bench.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=wood;
        // Existing bench props are retained; add a clear planing blank on top.
        Box(root,"Ida planing blank",new Vector3(-1.65f,.91f,-3.95f),new Vector3(2f,.075f,.22f),cutWood);
        Root(root,"Ida work spot",new Vector3(-2.4f,0,-4.4f));
        Root(root,"Workshop door approach",new Vector3(1.6f,0,-3.8f));
        Root(root,"Workshop sign spot",new Vector3(3.9f,0,-3.9f));
        ToolBoard(root);
        Yard(root);
    }
    static Transform Root(Transform parent,string name,Vector3 pos)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
    static Material Paint(string name,Color color,string texture,float detail)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Arkmon/Tower Painted Light"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_TextureScale",.65f);mat.SetFloat("_DetailStrength",detail);
        mat.SetTexture("_BaseMap",texture==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));
        EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }
    static void Beam(Transform root,string name,Vector3 a,Vector3 b,float width,Material material)
    {
        var beam=Box(root,name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),material);
        beam.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Gables(Transform root)
    {
        string path=Art+"Ida solid gables.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();
        mesh.vertices=new[]{new Vector3(-3.1f,3.6f,-1.8f),new Vector3(3.1f,3.6f,-1.8f),new Vector3(0,5.4f,-1.8f),
            new Vector3(-3.1f,3.6f,1.8f),new Vector3(3.1f,3.6f,1.8f),new Vector3(0,5.4f,1.8f)};
        mesh.triangles=new[]{0,2,1,3,4,5};mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var part=Root(root,"Workshop solid gables",Vector3.zero);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=plaster;
    }
    static void Window(Transform root)
    {
        var win=Root(root,"Workshop wide window",new Vector3(-1.55f,2.05f,-1.91f));
        Box(win,"Workshop dark glass",Vector3.zero,new Vector3(1.65f,1.06f,.08f),dark);
        foreach(float x in new[]{-.87f,0,.87f})
            Box(win,"Workshop window mullion",new Vector3(x,0,-.08f),new Vector3(.08f,1.20f,.15f),wood);
        foreach(float y in new[]{-.57f,0,.57f})
            Box(win,"Workshop window crossrail",new Vector3(0,y,-.08f),new Vector3(1.82f,.08f,.15f),wood);
        Box(win,"Workshop window sill",new Vector3(0,-.65f,-.15f),new Vector3(2.0f,.14f,.40f),cutWood);
        foreach(float x in new[]{-1.12f,1.12f})
            Box(win,"Workshop open shutter",new Vector3(x,0,-.04f),new Vector3(.32f,1.13f,.12f),wood);
    }
    static void ToolBoard(Transform root)
    {
        var board=Root(root,"Workshop tool rack",new Vector3(-1.55f,1.12f,-1.99f));
        Box(board,"Tool peg rail",Vector3.zero,new Vector3(1.85f,.14f,.16f),wood);
        for(int i=0;i<3;i++)
        {
            var at=new Vector3(-.62f+i*.60f,-.20f,-.09f);
            Box(board,"Hanging tool handle",at,new Vector3(.06f,.37f,.07f),cutWood);
            Box(board,i==1?"Chisel blade":"Mallet head",at+Vector3.up*.19f,new Vector3(i==1?.08f:.23f,.12f,.12f),i==1?iron:wood);
        }
        var sign=Root(root,"Carpenter hanging sign",new Vector3(-2.50f,2.67f,-3.72f));
        Box(sign,"Painted wooden trade sign",Vector3.zero,new Vector3(.68f,.46f,.08f),cutWood);
        Box(sign,"Plane emblem base",new Vector3(0,-.07f,-.06f),new Vector3(.46f,.09f,.035f),wood);
        Beam(sign,"Plane emblem handle",new Vector3(-.10f,.05f,-.06f),new Vector3(.10f,.13f,-.06f),.06f,iron);
    }
    static void Yard(Transform root)
    {
        var rack=Root(root,"Workshop timber rack",new Vector3(3.65f,0,-.1f));
        foreach(float z in new[]{-1.1f,1.1f})
            Box(rack,"Timber rack foot",new Vector3(0,.14f,z),new Vector3(.95f,.28f,.22f),wood,true);
        for(int row=0;row<5;row++)
        for(int plank=0;plank<3;plank++)
            Box(rack,"Seasoned timber plank",new Vector3(-.32f+plank*.32f,.31f+row*.14f,0),
                new Vector3(.27f,.11f,2.7f-(row%2)*.18f),row%2==0?wood:cutWood);
        // One collider for the whole pile keeps the visual slats from trapping feet.
        var collider=rack.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.5f,0);collider.size=new Vector3(.95f,1,2.75f);
        var barrel=Root(root,"Workshop offcut bin",new Vector3(-3.65f,0,-.6f));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            var stave=Box(barrel,"Offcut bin stave",new Vector3(Mathf.Cos(a)*.32f,.38f,Mathf.Sin(a)*.32f),new Vector3(.25f,.76f,.075f),wood);
            stave.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);
        }
        for(int i=0;i<4;i++)
        {
            var plank=Box(barrel,"Loose wood offcut",new Vector3(-.19f+i*.12f,.66f,0),new Vector3(.09f,.85f,.15f),cutWood);
            plank.localRotation=Quaternion.Euler(0,0,(i-2)*8);
        }
        var random=new System.Random(16);
        for(int i=0;i<16;i++)
        {
            float x=-2.7f+(float)random.NextDouble()*1.9f,z=-3.6f-(float)random.NextDouble()*.75f;
            var chip=Box(root,"Curled wood shaving",new Vector3(x,.018f,z),new Vector3(.12f,.025f,.035f),cutWood);
            chip.localRotation=Quaternion.Euler(0,i*47,0);
        }
        for(int i=0;i<4;i++)
        {
            var paver=Box(root,"Workshop approach stone",new Vector3(1.6f,.02f,-2.25f-i*.62f),new Vector3(1.35f,.04f,.49f),stone);
            paver.localRotation=Quaternion.Euler(0,i%2==0?3:-4,0);
        }
    }
}