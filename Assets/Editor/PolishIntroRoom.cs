using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class PolishIntroRoom
{
    const string Art="Assets/Art/Intro/";
    [MenuItem("Arkmon/Polish intro room")]
    public static void Run()
    {
        ConfigureGameIntro.Run();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var data=new SerializedObject(Object.FindFirstObjectByType<TowerRoomCamera>());
        data.FindProperty("angles").vector3Value=new Vector3(28,-7,0);
        data.FindProperty("focus").vector3Value=new Vector3(.2f,.8f,2.6f);
        data.FindProperty("distance").floatValue=12.4f;data.FindProperty("fieldOfView").floatValue=43;
        data.FindProperty("trackingLimits").vector2Value=new Vector2(.7f,.35f);data.ApplyModifiedPropertiesWithoutUndo();
        var chandelier=GameObject.Find("Tower candle chandelier");
        if(chandelier!=null){chandelier.transform.position=new Vector3(-1.6f,4.45f,3.3f);chandelier.transform.localScale=Vector3.one*.8f;}
        var bed=GameObject.Find("Canopy bed");bed.transform.rotation=Quaternion.Euler(0,7,0);
        var rug=GameObject.Find("Heirloom rug");
        if(rug!=null){rug.transform.localScale=Vector3.one*1.15f;rug.transform.position=new Vector3(-.2f,.035f,.3f);}
        BedDetails(bed.transform);
        var sun=GameObject.Find("Window morning light").GetComponent<Light>();
        sun.intensity=38;sun.color=new Color(1,.87f,.69f);sun.transform.LookAt(new Vector3(-2,.3f,.4f));
        sun.shadowBias=.02f;sun.shadowNormalBias=.05f;
        RenderSettings.ambientSkyColor=new Color(.23f,.29f,.39f);RenderSettings.ambientEquatorColor=new Color(.17f,.18f,.22f);
        var floor=GameObject.Find("Stone floor").GetComponent<Renderer>();
        var floorMaterial=Clone(floor.sharedMaterial,"Intro warm limestone floor");floorMaterial.SetColor("_BaseColor",new Color(1.08f,1.02f,.91f));floorMaterial.SetFloat("_TextureScale",.22f);floor.sharedMaterial=floorMaterial;
        foreach(var renderer in bed.GetComponentsInChildren<MeshRenderer>())
            if(renderer.sharedMaterial!=null && renderer.sharedMaterial.name.Contains("oak"))renderer.sharedMaterial=Clone(renderer.sharedMaterial,"Intro carved bed oak",new Color(.88f,.71f,.53f));
        var old=GameObject.Find("Intro room finishing");if(old!=null)Object.DestroyImmediate(old);
        var root=new GameObject("Intro room finishing").transform;
        var wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"Intro carved bed oak.mat");
        var chest=Part(root,"Bedside chest",new Vector3(-5.55f,.43f,.25f),new Vector3(1.35f,.75f,.75f),wood);
        chest.gameObject.AddComponent<BoxCollider>();
        Part(root,"Chest lid rim",new Vector3(-5.55f,.85f,.25f),new Vector3(1.42f,.12f,.82f),wood);
        var brass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Desk aged brass.mat");
        foreach(float x in new[]{-5.95f,-5.15f})Part(root,"Chest brass strap",new Vector3(x,.46f,-.14f),new Vector3(.07f,.72f,.025f),brass);
        WindowTrim(root,wood);
        CrestShape();
        var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Art/SceneStyle/Tower morning Volume.asset");
        if(profile.TryGet<ColorAdjustments>(out var grade)){grade.contrast.Override(12);grade.postExposure.Override(.18f);EditorUtility.SetDirty(grade);}
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ConfigureKaflaRefinement.Run();
    }
    static Material Clone(Material source,string name,Color? tint=null)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
        if(tint.HasValue)mat.SetColor("_BaseColor",tint.Value);EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Part(Transform root,string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
    }
    static void BedDetails(Transform bed)
    {
        var old=bed.Find("Carved canopy details");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Carved canopy details").transform;root.SetParent(bed,false);
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Bed painted oak.mat");
        var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Bed embroidered gold.mat");
        var green=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Bed painted green velvet.mat");
        foreach(float x in new[]{-1.14f,1.14f})foreach(float z in new[]{-1.65f,1.65f})
        {
            Part(root,"Turned post base",new Vector3(x,.16f,z),new Vector3(.26f,.16f,.26f),wood,PrimitiveType.Cylinder);
            Part(root,"Rounded post finial",new Vector3(x,3.48f,z),Vector3.one*.25f,wood,PrimitiveType.Sphere);
            foreach(float y in new[]{.4f,1.4f,2.9f})Part(root,"Post carved collar",new Vector3(x,y,z),new Vector3(.22f,.035f,.22f),gold,PrimitiveType.Cylinder);
        }
        Part(root,"Headboard inset",new Vector3(0,1.4f,1.44f),new Vector3(1.78f,.8f,.03f),wood);
        Part(root,"Headboard crown molding",new Vector3(0,1.95f,1.55f),new Vector3(2.38f,.12f,.22f),wood);
        Part(root,"Footboard crown molding",new Vector3(0,1.2f,-1.6f),new Vector3(2.38f,.1f,.22f),wood);
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        const int count=40;
        for(int i=0;i<=count;i++)
        {
            float u=i/(float)count,x=(u-.5f)*2.38f,bottom=2.87f+.05f*Mathf.Cos(u*Mathf.PI*6);
            vertices.Add(new Vector3(x,3.28f,-1.76f));vertices.Add(new Vector3(x,bottom,-1.76f-.025f*Mathf.Sin(u*Mathf.PI*8)));
            uv.Add(new Vector2(u,1));uv.Add(new Vector2(u,0));if(i<count){int k=i*2;triangles.AddRange(new[]{k,k+1,k+3,k,k+3,k+2});}
        }
        var mesh=new Mesh{name="Scalloped canopy valance"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh=SaveMesh(mesh);var obj=new GameObject("Scalloped canopy valance",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=green;
    }
    static void WindowTrim(Transform root,Material wood)
    {
        var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Window painted limestone.mat");
        for(int i=0;i<=12;i++)
        {
            float angle=i*Mathf.PI/12;var part=Part(root,"Window dressed arch stone",new Vector3(Mathf.Cos(angle)*1.3f,3.65f+Mathf.Sin(angle)*1.3f,5.86f),new Vector3(.27f,.21f,.17f),stone);
            part.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
        }
        Part(root,"Window oak sill molding",new Vector3(0,2.15f,5.72f),new Vector3(2.7f,.09f,.25f),wood);
    }
    static Mesh SaveMesh(Mesh mesh)
    {
        string path=Art+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;
    }
    static void CrestShape()
    {
        var crest=GameObject.Find("Aurenfeld family crest");if(crest==null)return;
        Vector2[] uv={new Vector2(.5f,.985f),new Vector2(.9f,.9f),new Vector2(.86f,.5f),new Vector2(.73f,.22f),new Vector2(.5f,.043f),new Vector2(.27f,.22f),new Vector2(.14f,.5f),new Vector2(.09f,.9f)};
        var vertices=new Vector3[uv.Length];for(int i=0;i<uv.Length;i++)vertices[i]=new Vector3(uv[i].x-.5f,uv[i].y-.5f,0);
        var triangles=new List<int>();for(int i=1;i<uv.Length-1;i++)triangles.AddRange(new[]{0,i,i+1});
        var mesh=new Mesh{name="Aurenfeld shield silhouette",vertices=vertices,uv=uv,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        crest.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
        var mat=crest.GetComponent<Renderer>().sharedMaterial;mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);
    }
}
