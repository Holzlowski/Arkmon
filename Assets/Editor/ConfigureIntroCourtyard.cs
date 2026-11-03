using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureIntroCourtyard
{
    const string Art="Assets/Art/Intro/";
    static Material stone,oak,roof,grass,path,soil;
    [MenuItem("Arkmon/Build intro courtyard layout")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        var root=new GameObject("Intro courtyard layout pending").transform;
        string[] keep={"Player residence tower","Tower entrance","Stable","Workbench","Wheelbarrow","Bench seat","Fountain basin","Outside gate"};
        var retained=new Dictionary<string,Transform>();foreach(string name in keep){var obj=GameObject.Find(name);if(obj!=null){obj.transform.SetParent(root,true);retained[name]=obj.transform;}}
        var old=GameObject.Find("Intro courtyard layout");if(old!=null)Object.DestroyImmediate(old);
        var legacy=GameObject.Find("Demo castle courtyard");
        foreach(Transform child in legacy.transform.Cast<Transform>().ToArray())
            if(child.GetComponentInChildren<PlayerController>()==null && child.GetComponentInChildren<NPC>()==null)Object.DestroyImmediate(child.gameObject);
        root.name="Intro courtyard layout";
        var opening=legacy.GetComponent<DemoOpening>();if(opening.GetComponent<CourtyardCamera>()==null)opening.gameObject.AddComponent<CourtyardCamera>();
        stone=Clone("Courtyard stone architecture","Assets/Art/SceneStyle/Courtyard painted limestone.mat");
        oak=Clone("Courtyard oak architecture","Assets/Art/SceneStyle/Courtyard painted oak.mat");
        roof=Clone("Courtyard roof architecture","Assets/Art/SceneStyle/Courtyard painted roof.mat");
        grass=Mat("Courtyard grass floor",new Color(.32f,.43f,.16f));
        path=Clone("Courtyard crossing path","Assets/Art/SceneStyle/Tower light - Tower soft paving.mat");path.SetColor("_BaseColor",new Color(1.1f,1.06f,.91f));
        soil=Mat("Courtyard garden soil",new Color(.25f,.15f,.07f));
        Box(root,"Courtyard walkable ground",new Vector3(0,-.15f,1),new Vector3(88,.3f,40),grass,true).gameObject.tag="Ground";
        Box(root,"East west crossing path",new Vector3(0,.012f,-1),new Vector3(82,.025f,4.2f),path);
        foreach(float x in new[]{-34,-21,12,25,37})Box(root,"Connecting path",new Vector3(x,.014f,6.5f),new Vector3(2.8f,.028f,15),path);
        Walls(root);
        Move(retained,"Player residence tower",new Vector3(-34,0,13),1.3f);
        Move(retained,"Tower entrance",new Vector3(-34,0,9.4f),1.1f);
        Move(retained,"Stable",new Vector3(-9,0,14),1.45f);
        Move(retained,"Workbench",new Vector3(-21,0,8.8f),1);
        Move(retained,"Wheelbarrow",new Vector3(17,0,5.5f),1);
        Move(retained,"Bench seat",new Vector3(0,0,8),1);
        Move(retained,"Fountain basin",new Vector3(1.3f,0,13.3f),1.15f);
        Move(retained,"Outside gate",new Vector3(37,0,17.6f),1.3f);
        House(root,"Workshop",new Vector3(-21,0,14),7,3.6f,4.5f);
        House(root,"Castle hall",new Vector3(12,0,16),13,9,5.5f,true);
        House(root,"Cellar storehouse",new Vector3(25,0,15.5f),7,4.2f,4.5f);
        Entrance(root,retained["Tower entrance"].position+new Vector3(0,0,-.45f),CourtyardEntrance.Destination.Tower);
        Door(root,new Vector3(12,0,12.95f),3.5f,4.5f);Entrance(root,new Vector3(12,0,12.2f),CourtyardEntrance.Destination.ThroneHall);
        Door(root,new Vector3(25,0,13),2.3f,2.9f);Entrance(root,new Vector3(25,0,12.2f),CourtyardEntrance.Destination.Cellar);
        Door(root,new Vector3(37,0,18),5,6);Entrance(root,new Vector3(37,0,16.2f),CourtyardEntrance.Destination.OuterGate);
        Garden(root);GrassTufts(root);Horizon();
        foreach(var npc in Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))if(npc.npcName=="John")npc.transform.position=new Vector3(7,1,1.7f);
        var player=GameObject.FindWithTag("Player");player.transform.position=new Vector3(-34,1,8);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static Material Clone(string name,string source){string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(source));AssetDatabase.CreateAsset(mat,path);}EditorUtility.SetDirty(mat);return mat;}
    static Material Mat(string name,Color tint){string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",tint);mat.SetFloat("_Smoothness",.02f);EditorUtility.SetDirty(mat);return mat;}
    static Transform Box(Transform root,string name,Vector3 position,Vector3 size,Material material,bool collision=false)
    {var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;}
    static void Move(Dictionary<string,Transform> objects,string name,Vector3 position,float scale){if(objects.TryGetValue(name,out var obj)){obj.position=position;obj.localScale=Vector3.one*scale;}}
    static void Walls(Transform root)
    {
        Box(root,"North castle wall",new Vector3(0,4,19),new Vector3(86,8,1.2f),stone,true);
        foreach(float x in new[]{-43,43})Box(root,"Side castle wall",new Vector3(x,4,1),new Vector3(1.2f,8,37),stone,true);
        // The near wall is visually cut down, but its full-height collision encloses the yard.
        var front=Box(root,"Cutaway near castle wall",new Vector3(0,.55f,-17),new Vector3(86,1.1f,1.1f),stone);
        var blocker=front.gameObject.AddComponent<BoxCollider>();blocker.center=new Vector3(0,3,0);blocker.size=new Vector3(1,7,1);
        for(int i=0;i<29;i++)Box(root,"North battlement",new Vector3(-42+i*3,8.65f,19),new Vector3(1.3f,1.3f,1.3f),stone);
        foreach(float x in new[]{-43,43})for(int i=0;i<12;i++)Box(root,"Side battlement",new Vector3(x,8.65f,-15+i*3),new Vector3(1.3f,1.3f,1.3f),stone);
    }
    static void House(Transform root,string name,Vector3 at,float width,float height,float depth,bool castle=false)
    {
        var house=new GameObject(name).transform;house.SetParent(root,false);house.localPosition=at;
        Box(house,name+" structure",new Vector3(0,height/2,0),new Vector3(width,height,depth),stone,true);
        if(castle)
        {
            for(int i=0;i<7;i++)Box(house,"Hall battlement",new Vector3(-width/2+.7f+i*1.95f,height+.6f,-depth/2),new Vector3(.9f,1.2f,.9f),stone);
        }
        else foreach(float z in new[]{-depth*.25f,depth*.25f})
        {
            var slope=Box(house,"Painted roof slope",new Vector3(0,height+.55f,z),new Vector3(width+.6f,.2f,depth*.65f),roof);slope.localRotation=Quaternion.Euler(z<0?-26:26,0,0);
        }
        foreach(float x in new[]{-width/2+.15f,width/2-.15f})Box(house,"Facade oak beam",new Vector3(x,height/2,-depth/2-.08f),new Vector3(.2f,height,.2f),oak);
        Box(house,"Facade cross beam",new Vector3(0,height*.68f,-depth/2-.08f),new Vector3(width,.2f,.2f),oak);
    }
    static void Door(Transform root,Vector3 at,float width,float height)
    {
        Box(root,"Closed oak entrance",at+Vector3.up*(height/2),new Vector3(width,height,.2f),oak,true);
        foreach(float x in new[]{-width/2,width/2})Box(root,"Doorway jamb",at+new Vector3(x,height/2,-.05f),new Vector3(.3f,height+.3f,.35f),stone);
        Box(root,"Doorway lintel",at+new Vector3(0,height+.1f,0),new Vector3(width+.5f,.35f,.35f),stone);
    }
    static void Entrance(Transform root,Vector3 position,CourtyardEntrance.Destination destination)
    {
        var obj=new GameObject(destination+" courtyard entrance",typeof(SphereCollider),typeof(CourtyardEntrance));obj.transform.SetParent(root,false);obj.transform.position=position;
        var trigger=obj.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.5f;obj.GetComponent<CourtyardEntrance>().destination=destination;
    }
    static void Garden(Transform root)
    {
        for(int i=0;i<3;i++)
        {
            Vector3 at=new Vector3(21+i*5.3f,.1f,7);Box(root,"Garden soil bed",at,new Vector3(4.3f,.15f,3.3f),soil);
            foreach(float x in new[]{-2.2f,2.2f})Box(root,"Garden border",at+new Vector3(x,.1f,0),new Vector3(.13f,.3f,3.5f),oak,true);
            foreach(float z in new[]{-1.7f,1.7f})Box(root,"Garden border",at+new Vector3(0,.1f,z),new Vector3(4.5f,.3f,.13f),oak,true);
            for(int j=0;j<12;j++){var plant=GameObject.CreatePrimitive(PrimitiveType.Sphere);plant.name="Vegetable plant";plant.transform.SetParent(root,false);plant.transform.position=at+new Vector3(-1.6f+j%4*1.05f,.25f,-1+j/4);plant.transform.localScale=new Vector3(.55f,.45f,.55f);plant.GetComponent<Renderer>().sharedMaterial=grass;Object.DestroyImmediate(plant.GetComponent<Collider>());}
        }
    }
    static void GrassTufts(Transform root)
    {
        var v=new List<Vector3>();var t=new List<int>();var rng=new System.Random(913);
        for(int i=0;i<850;i++)
        {
            float x=(float)rng.NextDouble()*82-41,z=(float)rng.NextDouble()*32-15;
            if(z>-3.5f&&z<1.5f || z>4.5f || new[]{-34f,-21,12,25,37}.Any(p=>Mathf.Abs(x-p)<1.7f))continue;
            float h=.18f+(float)rng.NextDouble()*.28f;int n=v.Count;v.AddRange(new[]{new Vector3(x-.1f,.015f,z),new Vector3(x,h,z+.06f),new Vector3(x+.1f,.015f,z),new Vector3(x,.015f,z-.1f),new Vector3(x+.06f,h,z),new Vector3(x,.015f,z+.1f)});t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
        }
        var mesh=new Mesh{name="Courtyard grass tufts"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string file=Art+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(file);if(old==null){AssetDatabase.CreateAsset(mesh,file);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
        var obj=new GameObject("Courtyard decorative grass",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=old;obj.GetComponent<Renderer>().sharedMaterial=grass;
    }
    static void Horizon()
    {
        var root=GameObject.Find("Castle horizon and golden fields");if(root==null)return;
        var material=Clone("Courtyard outer meadow cutout","Assets/Art/SceneStyle/Courtyard meadow cutout.mat");material.SetVector("_ClearArea",new Vector4(-45,-19,45,22));
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.name=="Arkmon/Meadow")renderer.sharedMaterial=material;
        foreach(var vegetation in root.GetComponentsInChildren<MeadowVegetation>())vegetation.SetClearArea(new Rect(-45,-19,90,41));
        foreach(var item in root.GetComponentsInChildren<Transform>().ToArray())if(item!=null && (item.name=="Meadow tree"||item.name=="Meadow wildflowers"||item.name=="Meadow thistles") && new Rect(-45,-19,90,41).Contains(new Vector2(item.position.x,item.position.z)))Object.DestroyImmediate(item.gameObject);
    }
}
