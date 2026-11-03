using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureGameIntro
{
    const string Art = "Assets/Art/Intro/";
    [MenuItem("Arkmon/Configure game intro room")]
    public static void Run()
    {
        ImportFamilySprites();
        var crestImporter = (TextureImporter)AssetImporter.GetAtPath(Art+"Aurenfeld-Crest.png");
        crestImporter.textureCompression = TextureImporterCompression.Uncompressed;
        crestImporter.maxTextureSize = 2048; crestImporter.anisoLevel = 8; crestImporter.SaveAndReimport();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var opening = Object.FindFirstObjectByType<DemoOpening>();
        if(opening.GetComponent<IntroChapter>() == null) opening.gameObject.AddComponent<IntroChapter>();
        if(opening.GetComponent<TowerRoomCamera>() == null) opening.gameObject.AddComponent<TowerRoomCamera>();
        var cameraData = new SerializedObject(opening.GetComponent<TowerRoomCamera>());
        cameraData.FindProperty("focus").vector3Value = new Vector3(0,1,2);
        cameraData.FindProperty("angles").vector3Value = new Vector3(25,0,0);
        cameraData.FindProperty("distance").floatValue = 12;
        cameraData.FindProperty("fieldOfView").floatValue = 40;
        cameraData.ApplyModifiedPropertiesWithoutUndo();
        var lamp = GameObject.Find("Player lamp pickup"); if(lamp != null) Object.DestroyImmediate(lamp);
        // Show the room as a cutaway: roof geometry must not occlude the fixed view.
        foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(renderer.name == "Tower ceiling" || renderer.transform.GetComponentsInParent<Transform>().Any(t=>t.name == "Tower ceiling decoration" || t.name == "Round tower roof timbers"))
                renderer.enabled = renderer.GetComponentInParent<TowerChandelierVisual>() != null;
        var wardrobe = Object.FindFirstObjectByType<PaintedWardrobe>();
        var data = new SerializedObject(wardrobe); var garment = (GameObject)data.FindProperty("mantle").objectReferenceValue;
        if(garment != null)
        {
            garment.name = "Aurenfeld family outfit on hanger";
            var cream = Material("Family linen",new Color(.82f,.73f,.55f));
            var brown = Material("Family brown cloth",new Color(.24f,.13f,.065f));
            foreach(var r in garment.GetComponentsInChildren<MeshRenderer>())r.sharedMaterial = cream;
            var filter = garment.GetComponent<MeshFilter>();
            var vertices = new[]{new Vector3(-.4f,.8f,0),new Vector3(-.35f,1.85f,0),new Vector3(-.64f,1.55f,0),new Vector3(-.8f,1.84f,0),new Vector3(-.3f,2.12f,0),new Vector3(.3f,2.12f,0),new Vector3(.8f,1.84f,0),new Vector3(.64f,1.55f,0),new Vector3(.35f,1.85f,0),new Vector3(.4f,.8f,0)};
            var mesh = new Mesh { name = "Hanging family tunic" }; mesh.vertices = vertices;
            var triangles = new System.Collections.Generic.List<int>();for(int i=1;i<vertices.Length-1;i++)triangles.AddRange(new[]{0,i+1,i});
            mesh.triangles = triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path = Art+"Hanging family tunic.asset";var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved == null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
            filter.sharedMesh = saved;
            var old = garment.transform.Find("Family brown front");if(old != null)Object.DestroyImmediate(old.gameObject);
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);front.name="Family brown front";front.transform.SetParent(garment.transform,false);
            front.transform.localPosition = new Vector3(0,1.46f,-.015f);front.transform.localScale = new Vector3(.67f,1.12f,1);front.GetComponent<Renderer>().sharedMaterial = brown;Object.DestroyImmediate(front.GetComponent<Collider>());
        }
        AddCrest();
        foreach(var item in Object.FindObjectsByType<DemoInteractable>(FindObjectsSortMode.None))
            if(item.kind == DemoInteractable.Kind.Wardrobe)item.name="Take Aurenfeld family outfit";
        EditorSceneManager.SaveScene(scene);
        scene = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        opening = Object.FindFirstObjectByType<DemoOpening>();
        if(opening.GetComponent<IntroChapter>() == null) opening.gameObject.AddComponent<IntroChapter>();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static Material Material(string name,Color color)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.03f);EditorUtility.SetDirty(mat);return mat;
    }
    static void AddCrest()
    {
        var old = GameObject.Find("Aurenfeld family crest");if(old != null)Object.DestroyImmediate(old);
        var crest=GameObject.CreatePrimitive(PrimitiveType.Quad);crest.name="Aurenfeld family crest";
        crest.transform.position=new Vector3(-2.15f,3.1f,5.35f);crest.transform.localScale=new Vector3(1.15f,1.28f,1);
        Object.DestroyImmediate(crest.GetComponent<Collider>());
        var mat=Material("Aurenfeld painted crest",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Aurenfeld-Crest.png"));crest.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static void ImportFamilySprites()
    {
        const string path="Assets/Resources/Elian-Family-Star-Sheet.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);float cellWidth=width/3f,cellHeight=height/3f;
        importer.spritePixelsPerUnit=cellHeight/2f;
        var frames=new SpriteMetaData[9];
        for(int i=0;i<9;i++)frames[i]=new SpriteMetaData{name=i==0?"Family-Idle":"Family-Walk-"+(i-1).ToString("D2"),rect=new Rect(i%3*cellWidth,(2-i/3)*cellHeight,cellWidth,cellHeight),alignment=(int)SpriteAlignment.BottomCenter,pivot=new Vector2(.5f,0)};
        #pragma warning disable CS0618
        importer.spritesheet=frames;
        #pragma warning restore CS0618
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        SaveSprite("Prince-Family",Sprite.Create(texture,frames[0].rect,new Vector2(.5f,0),cellHeight/2f,0,SpriteMeshType.FullRect));
        var rect=frames[0].rect;rect.y+=cellHeight*.32f;rect.height=cellHeight*.68f;
        SaveSprite("Prince-Family-Portrait",Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect));
    }
    static void SaveSprite(string name,Sprite sprite)
    {
        sprite.name=name;string path="Assets/Resources/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(existing==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);}
    }
}
