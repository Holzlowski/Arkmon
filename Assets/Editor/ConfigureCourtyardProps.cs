using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Scoped replacement of the courtyard props; scene systems and horizon stay intact.
public static class ConfigureCourtyardProps
{
    const string Art="Assets/Art/SceneStyle/";
    static Material oak,stone,iron,straw,roof,water;
    [MenuItem("Arkmon/Build painted courtyard props")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");Apply();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static Material Mat(string name,Color tint,string texture=null)
    {
        string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",.04f);
        if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));EditorUtility.SetDirty(m);return m;
    }
    static Transform Prepare(GameObject obj)
    {
        if(obj==null)return null;
        foreach(var paper in obj.GetComponents<PaperPropVisual>())Object.DestroyImmediate(paper);
        foreach(Transform child in obj.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        foreach(var renderer in obj.GetComponents<Renderer>())Object.DestroyImmediate(renderer);
        foreach(var filter in obj.GetComponents<MeshFilter>())Object.DestroyImmediate(filter);
        foreach(var collider in obj.GetComponents<Collider>())Object.DestroyImmediate(collider);
        obj.transform.rotation=Quaternion.identity;obj.transform.localScale=Vector3.one;return obj.transform;
    }
    static Transform Part(Transform root,string name,Vector3 pos,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=pos;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
    }
    static void Collider(Transform root,Vector3 centre,Vector3 size)
    {var c=root.gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;}
    public static void Apply()
    {
        oak=Mat("Courtyard painted oak",new Color(.75f,.58f,.37f),"Tower-Oak-Painted.png");
        stone=Mat("Courtyard painted limestone",new Color(.92f,.87f,.74f),"DemoStone.png");
        iron=Mat("Courtyard aged iron",new Color(.23f,.23f,.19f));straw=Mat("Courtyard golden straw",new Color(.84f,.63f,.24f),"Bed-Linen-Painted.png");
        roof=Mat("Courtyard painted roof",new Color(.40f,.22f,.14f),"Tower-Oak-Painted.png");
        water=Mat("Courtyard fountain water",new Color(.31f,.62f,.66f));
        Stable();Wheelbarrow();Workbench();Fountain();Bench();Entrance();
        foreach(var obj in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Paper supply crate").Select(t=>t.gameObject).ToArray())Crate(Prepare(obj));
        foreach(var bed in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Herb bed ")).ToArray())Garden(bed);
    }
    static void Crate(Transform root)
    {
        Part(root,"Crate body",new Vector3(0,.5f,0),new Vector3(1.08f,1,1.05f),oak);
        for(int i=0;i<5;i++)Part(root,"Front plank",new Vector3(-.44f+i*.22f,.5f,-.54f),new Vector3(.19f,.91f,.05f),oak);
        foreach(float y in new[]{.1f,.9f})
        {
            Part(root,"Crate face rail",new Vector3(0,y,-.58f),new Vector3(1.15f,.12f,.07f),oak);
            Part(root,"Crate rear rail",new Vector3(0,y,.56f),new Vector3(1.15f,.12f,.07f),oak);
        }
        var brace=Part(root,"Crate diagonal brace",new Vector3(0,.5f,-.62f),new Vector3(1.1f,.1f,.06f),oak);brace.localRotation=Quaternion.Euler(0,0,38);
        Collider(root,new Vector3(0,.5f,0),new Vector3(1.16f,1,1.2f));
    }
    static void Wheelbarrow()
    {
        var root=Prepare(GameObject.Find("Wheelbarrow"));if(root==null)return;
        Part(root,"Barrow bed",new Vector3(0,.68f,0),new Vector3(1.1f,.12f,1.55f),oak);
        foreach(float x in new[]{-.56f,.56f})
        {
            var side=Part(root,"Sloping trough side",new Vector3(x,.9f,0),new Vector3(.10f,.48f,1.6f),oak);side.localRotation=Quaternion.Euler(0,0,x<0?14:-14);
            Part(root,"Barrow handle",new Vector3(x,.64f,-.75f),new Vector3(.12f,.12f,2.55f),oak);
            Part(root,"Barrow support leg",new Vector3(x,.34f,-.4f),new Vector3(.12f,.62f,.12f),oak);
        }
        foreach(float z in new[]{-.78f,.78f})Part(root,"Trough end",new Vector3(0,.9f,z),new Vector3(1.14f,.48f,.12f),oak);
        var wheel=Part(root,"Wooden wheel",new Vector3(0,.34f,.97f),new Vector3(.67f,.13f,.67f),oak,PrimitiveType.Cylinder);wheel.localRotation=Quaternion.Euler(0,0,90);
        var axle=Part(root,"Iron axle",new Vector3(0,.34f,.97f),new Vector3(.16f,.43f,.16f),iron,PrimitiveType.Cylinder);axle.localRotation=Quaternion.Euler(0,0,90);
        Part(root,"Barrow hay",new Vector3(0,.97f,0),new Vector3(.93f,.37f,1.28f),straw);
        Collider(root,new Vector3(0,.65f,-.15f),new Vector3(1.4f,1.3f,2.7f));
    }
    static void Workbench()
    {
        var root=Prepare(GameObject.Find("Workbench"));if(root==null)return;
        Part(root,"Workbench top",new Vector3(0,1.02f,0),new Vector3(3,.16f,1.15f),oak);
        foreach(float x in new[]{-1.2f,1.2f})foreach(float z in new[]{-.4f,.4f})Part(root,"Workbench leg",new Vector3(x,.47f,z),new Vector3(.17f,.94f,.17f),oak);
        Part(root,"Workbench lower shelf",new Vector3(0,.28f,0),new Vector3(2.6f,.12f,.95f),oak);
        Part(root,"Mallet handle",new Vector3(.4f,1.16f,-.1f),new Vector3(.7f,.09f,.1f),oak);
        Part(root,"Mallet head",new Vector3(.72f,1.2f,-.1f),new Vector3(.25f,.23f,.4f),oak);
        Part(root,"Pot on workbench",new Vector3(-.85f,1.24f,.12f),new Vector3(.4f,.22f,.4f),roof,PrimitiveType.Cylinder);
        Collider(root,new Vector3(0,.58f,0),new Vector3(3.1f,1.16f,1.2f));
    }
    static void Stable()
    {
        var root=Prepare(GameObject.Find("Stable"));if(root==null)return;
        Part(root,"Stable rear wall",new Vector3(0,1.45f,1.4f),new Vector3(5.5f,2.9f,.18f),oak);
        foreach(float x in new[]{-2.65f,2.65f})
        {
            Part(root,"Stable side wall",new Vector3(x,1.45f,0),new Vector3(.18f,2.9f,2.8f),oak);
            foreach(float z in new[]{-1.4f,1.4f})Part(root,"Stable upright",new Vector3(x,1.5f,z),new Vector3(.23f,3,.23f),oak);
        }
        Part(root,"Stable front beam",new Vector3(0,2.9f,-1.4f),new Vector3(5.6f,.25f,.24f),oak);
        Part(root,"Stable central upright",new Vector3(.35f,1.45f,-1.4f),new Vector3(.22f,2.9f,.22f),oak);
        for(int i=0;i<7;i++)Part(root,"Stable door board",new Vector3(-2.37f+i*.37f,1.26f,-1.45f),new Vector3(.34f,2.45f,.1f),oak);
        foreach(float y in new[]{.55f,1.9f})Part(root,"Stable door crossbar",new Vector3(-1.24f,y,-1.55f),new Vector3(2.65f,.15f,.10f),oak);
        for(int i=0;i<3;i++)Part(root,"Stable stall rail",new Vector3(1.45f,.55f+i*.3f,-1.48f),new Vector3(2.03f,.13f,.13f),oak);
        foreach(float z in new[]{-.8f,.8f})
        {
            var slope=Part(root,"Stable sloped roof",new Vector3(0,3.35f,z),new Vector3(5.9f,.2f,1.93f),roof);slope.localRotation=Quaternion.Euler(z<0?-32:32,0,0);
            for(int i=0;i<9;i++){var slat=Part(root,"Roof tile seam",new Vector3(-2.72f+i*.68f,3.49f,z),new Vector3(.055f,.065f,1.95f),oak);slat.localRotation=slope.localRotation;}
        }
        Part(root,"Stable roof ridge",new Vector3(0,3.9f,0),new Vector3(5.95f,.15f,.18f),oak);
        Part(root,"Hay in stall",new Vector3(1.4f,.35f,.6f),new Vector3(1.7f,.7f,1.2f),straw);
        var bale=Part(root,"Straw bale",new Vector3(-1,.45f,.1f),new Vector3(.9f,.9f,1.3f),straw,PrimitiveType.Cylinder);bale.localRotation=Quaternion.Euler(0,0,90);
        Collider(root,new Vector3(0,1.5f,0),new Vector3(5.6f,3,3));
    }
    static void Ring(Transform root,string name,float inner,float outer,float y,float height,Material material)
    {
        const int count=24;float mid=(inner+outer)*.5f;
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            var segment=Part(root,name,new Vector3(Mathf.Sin(angle)*mid,y,Mathf.Cos(angle)*mid),
                new Vector3(2*outer*Mathf.Tan(Mathf.PI/count),height,outer-inner),material);
            segment.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
        }
    }
    static void Fountain()
    {
        var root=Prepare(GameObject.Find("Fountain basin"));if(root==null)return;
        Part(root,"Fountain stepped base",new Vector3(0,.12f,0),new Vector3(3.1f,.12f,3.1f),stone,PrimitiveType.Cylinder);
        Ring(root,"Limestone basin",1.05f,1.42f,.43f,.52f,stone);
        Ring(root,"Rounded basin lip",1.0f,1.48f,.73f,.15f,stone);
        Part(root,"Basin water",new Vector3(0,.59f,0),new Vector3(2.11f,.025f,2.11f),water,PrimitiveType.Cylinder);
        Part(root,"Fountain column",new Vector3(0,.96f,0),new Vector3(.42f,.57f,.42f),stone,PrimitiveType.Cylinder);
        Part(root,"Upper bowl floor",new Vector3(0,1.52f,0),new Vector3(1.38f,.07f,1.38f),stone,PrimitiveType.Cylinder);
        Ring(root,"Upper carved bowl",.55f,.72f,1.61f,.18f,stone);
        Part(root,"Upper water",new Vector3(0,1.66f,0),new Vector3(1.1f,.017f,1.1f),water,PrimitiveType.Cylinder);
        Part(root,"Fountain finial",new Vector3(0,1.85f,0),new Vector3(.2f,.2f,.2f),stone,PrimitiveType.Sphere);
        Collider(root,new Vector3(0,.65f,0),new Vector3(3,1.3f,3));
    }
    static void Bench()
    {
        foreach(var leg in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Bench leg").ToArray())Object.DestroyImmediate(leg.gameObject);
        var root=Prepare(GameObject.Find("Bench seat"));if(root==null)return;root.position=new Vector3(root.position.x,0,root.position.z);
        for(int i=0;i<3;i++)Part(root,"Bench seat board",new Vector3(0,.57f,-.25f+i*.25f),new Vector3(2.8f,.13f,.22f),oak);
        foreach(float x in new[]{-1.1f,1.1f})
        {Part(root,"Bench support",new Vector3(x,.28f,0),new Vector3(.18f,.56f,.75f),oak);Part(root,"Bench back upright",new Vector3(x,.95f,.39f),new Vector3(.12f,1,.14f),oak);}
        foreach(float y in new[]{.95f,1.25f})Part(root,"Bench back board",new Vector3(0,y,.4f),new Vector3(2.8f,.23f,.12f),oak);
        Collider(root,new Vector3(0,.65f,.05f),new Vector3(2.9f,1.3f,1));
    }
    static void Garden(Transform bed)
    {
        var old=bed.Find("Painted planter border");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Painted planter border").transform;root.SetParent(bed.parent,false);root.position=bed.position;root.localScale=Vector3.one;
        // World-sized raised stone edging, distinct from the scaled soil cube.
        foreach(float x in new[]{-.75f,.75f})Part(root,"Planter side",new Vector3(x,.1f,0),new Vector3(.12f,.28f,1.65f),stone);
        foreach(float z in new[]{-.78f,.78f})Part(root,"Planter end",new Vector3(0,.1f,z),new Vector3(1.6f,.28f,.12f),stone);
        root.SetParent(bed,true);
    }
    static void Entrance()
    {
        var root=Prepare(GameObject.Find("Tower entrance"));if(root==null)return;
        Part(root,"Arched oak door top",new Vector3(0,2.0f,0),new Vector3(2.32f,2.32f,.2f),oak,PrimitiveType.Sphere);
        Part(root,"Oak door body",new Vector3(0,1.0f,-.015f),new Vector3(2.3f,2,.2f),oak);
        foreach(float x in new[]{-1.3f,1.3f})Part(root,"Door stone jamb",new Vector3(x,1.03f,0),new Vector3(.25f,2.1f,.38f),stone);
        for(int i=0;i<18;i++)
        {
            float angle=(i+.5f)*Mathf.PI/18;
            var piece=Part(root,"Door carved arch stone",new Vector3(Mathf.Cos(angle)*1.3f,2+Mathf.Sin(angle)*1.3f,0),new Vector3(.25f,.23f,.38f),stone);
            piece.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg+90);
        }
        for(int i=0;i<8;i++)Part(root,"Door lower plank",new Vector3(-1.0f+i*.285f,1,-.14f),new Vector3(.26f,1.94f,.035f),oak);
        foreach(float y in new[]{.55f,1.55f})Part(root,"Door iron strap",new Vector3(0,y,-.19f),new Vector3(2.04f,.085f,.04f),iron);
        Part(root,"Door brass handle",new Vector3(.75f,1,-.24f),new Vector3(.12f,.16f,.08f),straw,PrimitiveType.Sphere);
        Collider(root,new Vector3(0,1.65f,0),new Vector3(2.8f,3.3f,.4f));
    }
}
