using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureDemoRefinements
{
    private const string Art="Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Refine wardrobe, gate and player lamp")]
    public static void Run()
    {
        ConfigureRoomPortrait();
        const string irisPath="Assets/Resources/Player Lamp Iris.mat";
        Material iris=AssetDatabase.LoadAssetAtPath<Material>(irisPath);
        if(iris==null){iris=new Material(Shader.Find("Arkmon/Player Lamp Iris"));AssetDatabase.CreateAsset(iris,irisPath);}
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ApplyWardrobe();EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        ApplyGate();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Demo refinements configured: room portrait, safe gate forecourt, painted 3D wardrobe and player lamp iris.");
    }
    public static void ConfigureRoomPortrait()
    {
        var style=AssetDatabase.LoadAssetAtPath<PaperUIStyle>("Assets/Resources/Paper UI.asset");
        Texture2D texture=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Prince-Room.png").OfType<Texture2D>().First();
        Sprite sprite=Sprite.Create(texture,new Rect(texture.width*.22f,texture.height*.60f,texture.width*.55f,texture.height*.39f),new Vector2(.5f,.5f),100);
        sprite.name="Player room portrait";const string path="Assets/Art/UI/Player room portrait.asset";
        Sprite existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(existing==null){AssetDatabase.CreateAsset(sprite,path);style.playerRoomPortrait=sprite;}
        else{EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);EditorUtility.SetDirty(existing);style.playerRoomPortrait=existing;}
        EditorUtility.SetDirty(style);
    }
    private static Material Material(string name,Color color,Texture2D texture=null)
    {
        string path=Art+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(material);return material;
    }
    private static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
    }
    public static void ApplyGate()
    {
        Transform parent=GameObject.Find("Demo castle courtyard").transform;
        Transform old=parent.Find("Gate forecourt");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Gate forecourt").transform;root.SetParent(parent,false);
        Material floor=GameObject.Find("Courtyard stone floor").GetComponent<Renderer>().sharedMaterial;
        Material wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"Tower painted oak.mat");
        Transform paving=Box(root,"Gate approach ground",new Vector3(27,-.25f,.75f),new Vector3(7,.5f,4.5f),floor,true);paving.tag="Ground";
        foreach(float z in new[]{-1.5f,3f})
        {
            Box(root,"Oak roadside rail",new Vector3(27,.85f,z),new Vector3(6.8f,.12f,.13f),wood);
            foreach(float x in new[]{24.5f,27,29.8f})Box(root,"Oak roadside post",new Vector3(x,.55f,z),new Vector3(.14f,1.1f,.14f),wood);
            Transform wall=Box(root,"Forecourt side boundary",new Vector3(27,6,z),new Vector3(7,12,.25f),wood,true);wall.GetComponent<Renderer>().enabled=false;
        }
        Box(root,"Outer oak barrier",new Vector3(30,.75f,.75f),new Vector3(.15f,.16f,4.5f),wood);
        Transform end=Box(root,"Demo exit boundary",new Vector3(30,6,.75f),new Vector3(.3f,12,4.75f),wood,true);end.GetComponent<Renderer>().enabled=false;
    }
    private static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Art+name+".asset";Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);mesh.name=name;
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
    private static Transform Surface(Transform parent,string name,Mesh mesh,Material material)
    {
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;
    }
    private static Mesh Door(bool mirror)
    {
        var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(0,2.55f,0),new Vector3(.94f,2.55f,0),new Vector3(.94f,0,0)};
        mesh.uv=mirror?new[]{new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),new Vector2(0,0)}:new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(mirror?"Wardrobe right door face":"Wardrobe left door face",mesh);
    }
    private static Mesh Crown()
    {
        const int count=48;
        var vertices=new System.Collections.Generic.List<Vector3>();
        var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        for(int i=0;i<=count;i++)
        {
            float u=i/(float)count,x=Mathf.Lerp(-1.13f,1.13f,u),top=3.20f+.34f*Mathf.Sin(u*Mathf.PI);
            vertices.AddRange(new[]{new Vector3(x,3.14f,-.54f),new Vector3(x,top,-.54f),new Vector3(x,3.14f,.5f),new Vector3(x,top,.5f)});
            uv.AddRange(new[]{new Vector2(u,0),new Vector2(u,1),new Vector2(u,0),new Vector2(u,1)});
            if(i==count)continue;int a=i*4,b=a+4;
            triangles.AddRange(new[]{a,a+1,b+1,a,b+1,b, a+2,b+2,b+3,a+2,b+3,a+3, a+1,a+3,b+3,a+1,b+3,b+1, a,b,b+2,a,b+2,a+2});
        }
        triangles.AddRange(new[]{0,2,3,0,3,1,count*4,count*4+1,count*4+3,count*4,count*4+3,count*4+2});
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return SaveMesh("Wardrobe curved crown",mesh);
    }
    private static Mesh SideUV()
    {
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);Mesh mesh=Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(temporary);
        var uv=new Vector2[mesh.vertexCount];Vector3[] vertices=mesh.vertices,normals=mesh.normals;
        for(int i=0;i<uv.Length;i++)
        {
            Vector3 p=vertices[i]+Vector3.one*.5f;
            uv[i]=Mathf.Abs(normals[i].x)>.5f?new Vector2(p.y*2.1f,p.z*1.2f):new Vector2(p.y*2.1f,p.x*.3f);
        }
        mesh.uv=uv;return SaveMesh("Wardrobe side grain box",mesh);
    }
    public static void ApplyWardrobe()
    {
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Wardrobe-Door-Painted.png")==null)return;
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"Wardrobe-Door-Painted.png");
        importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.SaveAndReimport();
        Material oak=Material("Wardrobe painted oak",new Color(.74f,.66f,.57f),AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Tower-Oak-Painted.png"));
        Material doors=Material("Wardrobe illustrated doors",Color.white,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Wardrobe-Door-Painted.png"));
        doors.SetFloat("_Cull",0);EditorUtility.SetDirty(doors);
        Material inner=Material("Wardrobe dark interior",new Color(.22f,.12f,.06f));
        Material gold=Material("Wardrobe muted gold",new Color(.7f,.47f,.18f));
        Material green=Material("Wardrobe green mantle",new Color(.20f,.29f,.10f));
        GameObject cabinet=GameObject.Find("Wardrobe");
        var billboard=cabinet.GetComponent<PaperPropVisual>();if(billboard!=null)Object.DestroyImmediate(billboard);
        foreach(Transform child in cabinet.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        var oldSprite=cabinet.GetComponent<SpriteRenderer>();if(oldSprite!=null)Object.DestroyImmediate(oldSprite);
        var animation=cabinet.GetComponent<PaintedWardrobe>()??cabinet.AddComponent<PaintedWardrobe>();
        Transform root=cabinet.transform;root.rotation=Quaternion.identity;root.localScale=Vector3.one;
        Box(root,"Dark inner back",new Vector3(0,1.75f,.43f),new Vector3(1.98f,2.85f,.08f),inner);
        Mesh side=SideUV();
        foreach(float x in new[]{-1.02f,1.02f})Box(root,"Oak cabinet side",new Vector3(x,1.75f,0),new Vector3(.13f,2.9f,.95f),oak).GetComponent<MeshFilter>().sharedMesh=side;
        Box(root,"Cabinet base",new Vector3(0,.32f,0),new Vector3(2.2f,.24f,1.08f),oak);
        Box(root,"Upper cornice",new Vector3(0,3.12f,0),new Vector3(2.25f,.16f,1.08f),oak);
        foreach(float x in new[]{-.93f,.93f})foreach(float z in new[]{-.36f,.36f})Box(root,"Carved foot",new Vector3(x,.14f,z),new Vector3(.19f,.28f,.21f),oak);
        Material crown=Material("Wardrobe carved crown",Color.white,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Wardrobe-Door-Painted.png"));
        crown.SetTextureScale("_BaseMap",new Vector2(1,.16f));crown.SetTextureOffset("_BaseMap",new Vector2(0,.84f));EditorUtility.SetDirty(crown);
        Surface(root,"Curved painted crown",Crown(),crown);
        var left=new GameObject("Left door hinge").transform;left.SetParent(root,false);left.localPosition=new Vector3(-.94f,.45f,-.52f);
        var right=new GameObject("Right door hinge").transform;right.SetParent(root,false);right.localPosition=new Vector3(.94f,.45f,-.52f);
        Surface(left,"Illustrated left door",Door(false),doors);
        Transform rightFace=Surface(right,"Illustrated right door",Door(true),doors);rightFace.localPosition=Vector3.left*.94f;
        Box(left,"Solid door thickness",new Vector3(.47f,1.275f,.045f),new Vector3(.94f,2.55f,.07f),oak);
        Box(right,"Solid door thickness",new Vector3(-.47f,1.275f,.045f),new Vector3(.94f,2.55f,.07f),oak);
        Mesh cloak=new Mesh();cloak.vertices=new[]{new Vector3(-.23f,2.55f,.18f),new Vector3(.23f,2.55f,.18f),new Vector3(.55f,.7f,.15f),new Vector3(0,.65f,.05f),new Vector3(-.55f,.7f,.15f)};
        cloak.triangles=new[]{0,1,3,1,2,3,0,3,4};cloak.RecalculateNormals();cloak.RecalculateBounds();
        GameObject mantle=Surface(root,"Green mantle on hanger",SaveMesh("Wardrobe hanging mantle",cloak),green).gameObject;
        Box(mantle.transform,"Mantle golden hem",new Vector3(0,.72f,.12f),new Vector3(1.03f,.025f,.025f),gold);
        var data=new SerializedObject(animation);data.FindProperty("leftDoor").objectReferenceValue=left;
        data.FindProperty("rightDoor").objectReferenceValue=right;data.FindProperty("mantle").objectReferenceValue=mantle;data.ApplyModifiedPropertiesWithoutUndo();
        var collider=cabinet.GetComponent<BoxCollider>()??cabinet.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.8f,0);collider.size=new Vector3(2.25f,3.6f,1.12f);
    }
}
