using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureTowerCeiling
{
    private const string Art="Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Decorate tower ceiling")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        Apply(GameObject.Find("Tower room interior").transform);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Tower ceiling decorated with painted plaster, oak beams and a six-candle chandelier.");
    }
    public static void Apply(Transform room)
    {
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Tower-Oak-Painted.png")==null)return;
        Texture2D oak=Import("Tower-Oak-Painted.png"),plaster=Import("Tower-Plaster-Painted.png");
        Material wood=Material("Tower painted oak",new Color(.76f,.69f,.60f),oak);
        Material ceiling=Material("Tower painted plaster",Color.white,plaster);
        GameObject roof=GameObject.Find("Tower ceiling");
        roof.GetComponent<MeshRenderer>().sharedMaterial=ceiling;
        roof.GetComponent<MeshFilter>().sharedMesh=BoxUV("Tower plaster box",roof.transform.localScale,2.5f);
        foreach(Transform child in room)
            if(child.name.StartsWith("Ceiling beam "))
            {
                child.GetComponent<MeshRenderer>().sharedMaterial=wood;
                child.GetComponent<MeshFilter>().sharedMesh=BoxUV("Tower oak beam box",child.localScale,1.8f);
            }
        Remove(room,"Tower ceiling decoration");
        var decor=new GameObject("Tower ceiling decoration").transform;decor.SetParent(room,false);
        Mesh cross=BoxUV("Tower oak crossbeam box",new Vector3(15.8f,.22f,.18f),1.8f);
        foreach(float z in new[]{-4.7f,4.7f})
            Part(decor,"Oak crossbeam",cross,wood,new Vector3(0,4.64f,z),new Vector3(15.8f,.22f,.18f));
        BuildChandelier(decor);
    }
    private static void Remove(Transform parent,string name)
    {
        Transform found=parent.Find(name);if(found!=null)UnityEngine.Object.DestroyImmediate(found.gameObject);
    }
    private static Texture2D Import(string name)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name);
        importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name);
    }
    private static Material Material(string name,Color tint,Texture2D texture=null)
    {
        string path=Art+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",tint);material.SetTexture("_BaseMap",texture);
        material.SetFloat("_Smoothness",.08f);material.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(material);return material;
    }
    private static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Art+name+".asset";mesh.name=name;
        Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
    private static Mesh BoxUV(string name,Vector3 size,float repeatLength)
    {
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh=UnityEngine.Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
        UnityEngine.Object.DestroyImmediate(temporary);
        Vector3[] vertices=mesh.vertices,normals=mesh.normals;var uv=new Vector2[vertices.Length];
        bool alongX=size.x>size.z;
        for(int i=0;i<uv.Length;i++)
        {
            Vector3 v=vertices[i]+Vector3.one*.5f;
            if(Mathf.Abs(normals[i].y)>.5f)
                uv[i]=alongX?new Vector2(v.x*size.x/repeatLength,v.z*size.z/.35f):new Vector2(v.z*size.z/repeatLength,v.x*size.x/.35f);
            else if(Mathf.Abs(normals[i].x)>.5f)uv[i]=new Vector2(v.z*size.z/repeatLength,v.y*size.y/.35f);
            else uv[i]=new Vector2(v.x*size.x/repeatLength,v.y*size.y/.35f);
            // Plaster has equal proportions on both axes; beam grain follows its length.
            if(name.Contains("plaster"))
                uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(v.x*size.x/repeatLength,v.z*size.z/repeatLength):uv[i];
        }
        mesh.uv=uv;return SaveMesh(name,mesh);
    }
    private static Transform Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale)
    {
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;return obj.transform;
    }
    private static Mesh Primitive(PrimitiveType type)
    {
        var obj=GameObject.CreatePrimitive(type);Mesh mesh=obj.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(obj);return mesh;
    }
    private static Mesh Tube(string name,Func<float,Vector3> curve,int segments,float radius)
    {
        const int sides=8;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int i=0;i<=segments;i++)
        {
            float t=i/(float)segments;Vector3 centre=curve(t);
            Vector3 tangent=(curve(Mathf.Min(1,t+.001f))-curve(Mathf.Max(0,t-.001f))).normalized;
            Vector3 normal=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 binormal=Vector3.Cross(tangent,normal);
            for(int j=0;j<sides;j++)
            {
                float angle=j*Mathf.PI*2/sides;
                vertices.Add(centre+radius*(normal*Mathf.Cos(angle)+binormal*Mathf.Sin(angle)));uv.Add(new Vector2(t,j/(float)sides));
                if(i==segments)continue;
                int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return SaveMesh(name,mesh);
    }
    private static void BuildChandelier(Transform parent)
    {
        var root=new GameObject("Tower candle chandelier").transform;root.SetParent(parent,false);root.localPosition=new Vector3(2,3.9f,-1.6f);
        Material brass=Material("Tower aged brass",new Color(.32f,.20f,.09f));
        Material wax=Material("Tower candle wax",new Color(.94f,.85f,.63f));
        Material wick=Material("Tower candle wick",new Color(.15f,.09f,.045f));
        Mesh cylinder=Primitive(PrimitiveType.Cylinder),sphere=Primitive(PrimitiveType.Sphere);
        Part(root,"Central stem",cylinder,brass,new Vector3(0,-.02f,0),new Vector3(.065f,.23f,.065f));
        Part(root,"Pendant finial",sphere,brass,new Vector3(0,-.30f,0),Vector3.one*.13f);
        Mesh ring=Tube("Chandelier support ring",t=>new Vector3(Mathf.Cos(t*Mathf.PI*2)*.43f,-.12f,Mathf.Sin(t*Mathf.PI*2)*.43f),48,.02f);
        Part(root,"Forged support ring",ring,brass,Vector3.zero,Vector3.one);
        Mesh link=Tube("Chandelier chain link",t=>new Vector3(Mathf.Cos(t*Mathf.PI*2)*.035f,Mathf.Sin(t*Mathf.PI*2)*.045f,0),24,.009f);
        for(int i=0;i<7;i++)
        {
            Transform chain=Part(root,"Suspension link "+i,link,brass,new Vector3(0,.23f+i*.065f,0),Vector3.one);
            chain.localRotation=Quaternion.Euler(0,i%2*90,0);
        }
        Mesh arm=Tube("Chandelier curved arm",t=>
        {
            Vector3 a=new Vector3(0,-.03f,0),b=new Vector3(.15f,-.40f,0),c=new Vector3(.58f,-.26f,0),d=new Vector3(.60f,.05f,0);
            float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        },24,.028f);
        FireVisualStyle fire=Resources.Load<FireVisualStyle>("Paper Fire");var flames=new List<SpriteRenderer>();
        for(int i=0;i<6;i++)
        {
            var spoke=new GameObject("Candle arm "+i).transform;spoke.SetParent(root,false);spoke.localRotation=Quaternion.Euler(0,i*60,0);
            Part(spoke,"Curved brass arm",arm,brass,Vector3.zero,Vector3.one);
            Part(spoke,"Candle cup",cylinder,brass,new Vector3(.60f,.05f,0),new Vector3(.15f,.025f,.15f));
            float height=.22f+(i%3)*.025f;
            Part(spoke,"Ivory candle",cylinder,wax,new Vector3(.60f,.075f+height*.5f,0),new Vector3(.075f,height*.5f,.075f));
            Part(spoke,"Black wick",cylinder,wick,new Vector3(.60f,.085f+height,0),new Vector3(.008f,.025f,.008f));
            if(fire==null || fire.frames.Length==0)continue;
            var obj=new GameObject("Paper candle flame",typeof(SpriteRenderer));obj.transform.SetParent(spoke,false);
            obj.transform.localPosition=new Vector3(.60f,.09f+height,0);var flame=obj.GetComponent<SpriteRenderer>();
            flame.sprite=fire.frames[i%fire.frames.Length];flame.sharedMaterial=fire.material;
            obj.transform.localScale=Vector3.one*.13f/flame.sprite.bounds.size.y;flames.Add(flame);
        }
        var lightObject=new GameObject("Chandelier warm light",typeof(Light));lightObject.transform.SetParent(root,false);
        lightObject.transform.localPosition=Vector3.up*.15f;
        var light=lightObject.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.76f,.44f);light.range=6;light.intensity=1.5f;light.shadows=LightShadows.None;
        var animation=root.gameObject.AddComponent<TowerChandelierVisual>();var data=new SerializedObject(animation);
        var list=data.FindProperty("flames");list.arraySize=flames.Count;for(int i=0;i<flames.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=flames[i];
        data.FindProperty("candleLight").objectReferenceValue=light;data.ApplyModifiedPropertiesWithoutUndo();
    }
}
