using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DetailTowerFurniture
{
    private const string Style = "Assets/Art/SceneStyle/Tower light - ";

    [MenuItem("Arkmon/Detail tower furniture and framing")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var gold = AssetDatabase.LoadAssetAtPath<Material>(Style + "Desk aged brass.mat");
        var oak = AssetDatabase.LoadAssetAtPath<Material>(Style + "Desk painted oak.mat");
        var cloth = AssetDatabase.LoadAssetAtPath<Material>(Style + "Bed painted green velvet.mat");
        var bed = Details(GameObject.Find("Canopy bed").transform);
        Frame(bed, new Vector3(0,.84f,-1.68f), 2.1f,.48f, oak, gold);
        foreach(float x in new[]{-.80f,.80f}) Jewel(bed,new Vector3(x,.84f,-1.715f),.11f,gold);
        var canopy = Details(GameObject.Find("Painted bed geometry").transform);
        Bar(canopy,new Vector3(0,3.15f,-1.85f),new Vector3(2.42f,.028f,.028f),gold);

        var wardrobe=GameObject.Find("Wardrobe").transform;
        var cabinet=Details(wardrobe);
        foreach(float y in new[]{.33f,3.15f})
            Bar(cabinet,new Vector3(0,y,-.552f),new Vector3(2.19f,.04f,.04f),gold);
        foreach(var name in new[]{"Left door hinge","Right door hinge"})
        {
            var door=Details(wardrobe.Find(name));
            float x=name.StartsWith("Left")?.76f:-.76f;
            Jewel(door,new Vector3(x,1.25f,-.04f),.12f,gold);
            Bar(door,new Vector3(x,1.18f,-.035f),new Vector3(.032f,.065f,.025f),gold);
        }

        var desk=Details(GameObject.Find("Painted desk geometry").transform);
        foreach(float x in new[]{-.56f,.56f})
            Frame(desk,new Vector3(x,.94f,-.478f),.94f,.15f,oak,gold);
        var chair=Details(GameObject.Find("Painted desk chair").transform);
        Bar(chair,new Vector3(0,1.07f,-.334f),new Vector3(.55f,.59f,.035f),cloth);
        Frame(chair,new Vector3(0,1.07f,-.356f),.57f,.62f,oak,gold);
        Jewel(chair,new Vector3(0,1.07f,-.38f),.12f,gold);

        var camera=new SerializedObject(Object.FindFirstObjectByType<TowerRoomCamera>());
        camera.FindProperty("focus").vector3Value=new Vector3(.85f,1.55f,1.70f);
        camera.FindProperty("distance").floatValue=10.8f;
        camera.FindProperty("fieldOfView").floatValue=46.5f;
        camera.FindProperty("trackingLimits").vector2Value=new Vector2(2.8f,.38f);
        camera.FindProperty("trackingWeight").floatValue=.54f;
        camera.FindProperty("smoothing").floatValue=.12f;camera.ApplyModifiedPropertiesWithoutUndo();
        PolishTowerWakeAndLight.CenterChandelier();
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform Details(Transform parent)
    {
        var previous=parent.Find("Aurenfeld furniture details");
        if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var group=new GameObject("Aurenfeld furniture details").transform;
        group.SetParent(parent,false);return group;
    }
    private static void Frame(Transform parent,Vector3 center,float width,float height,Material wood,Material brass)
    {
        foreach(float sign in new[]{-1f,1f})
        {
            Bar(parent,center+Vector3.up*(height*.5f*sign),new Vector3(width,.035f,.035f),wood);
            Bar(parent,center+Vector3.right*(width*.5f*sign),new Vector3(.035f,height,.035f),wood);
            Bar(parent,center+new Vector3(0,sign*(height*.5f-.026f),-.018f),new Vector3(width-.06f,.012f,.015f),brass);
        }
    }
    private static void Bar(Transform parent,Vector3 position,Vector3 size,Material material)
        => Part(parent,"Furniture molding",position,size,material,PrimitiveType.Cube);
    private static void Jewel(Transform parent,Vector3 position,float size,Material material)
        => Part(parent,"Brass rosette",position,new Vector3(size,size,.04f),material,PrimitiveType.Sphere);
    private static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,PrimitiveType primitive)
    {
        var obj=GameObject.CreatePrimitive(primitive);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
}
