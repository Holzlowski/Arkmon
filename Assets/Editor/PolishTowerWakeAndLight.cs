using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PolishTowerWakeAndLight
{
    private const string Art="Assets/Art/Intro/";
    [MenuItem("Arkmon/Polish tower waking and cozy lighting")]
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ApplyBed();
        Lighting();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }

    public static void RestoreSleepingPresentation()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ApplyBed();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }

    private static void ApplyBed()
    {
        var bed=GameObject.Find("Canopy bed").GetComponent<PaintedCanopyBed>();
        bed.transform.localRotation=Quaternion.Euler(0,26,0);
        var data=new SerializedObject(bed);
        var sleeping=(SpriteRenderer)data.FindProperty("sleepingIllustration").objectReferenceValue;
        var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Prince-Sleeping.png");
        // Only the head and collar are exposed; the opaque quilt represents the covered body.
        // Reuse the source texture/UVs without painting or replacing the character artwork.
        Rect rect=source.rect;float fraction=.36f;
        rect.y+=rect.height*(1-fraction);rect.height*=fraction;
        var head=Sprite.Create(source.texture,rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        head.name="Elian sleeping head";
        string spritePath=Art+head.name+".asset";var savedHead=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if(savedHead==null){AssetDatabase.CreateAsset(head,spritePath);savedHead=head;}
        else{EditorUtility.CopySerialized(head,savedHead);Object.DestroyImmediate(head);EditorUtility.SetDirty(savedHead);}
        sleeping.sprite=savedHead;sleeping.transform.localRotation=Quaternion.Euler(54,0,0);
        sleeping.transform.localPosition=new Vector3(0,1.36f,.82f);
        sleeping.transform.localScale=Vector3.one*(2.5f/source.bounds.size.y);
        var previous=bed.transform.Find("Sleeping quilt volume");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var quilt=new GameObject("Sleeping quilt volume",typeof(MeshFilter),typeof(MeshRenderer));quilt.transform.SetParent(bed.transform,false);
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int cols=32,rows=32;
        for(int r=0;r<=rows;r++)for(int c=0;c<=cols;c++)
        {
            float u=c/(float)cols,w=r/(float)rows,x=(u-.5f)*2.12f,z=Mathf.Lerp(-1.47f,.66f,w);
            float body=Mathf.Exp(-x*x/ .19f)*Mathf.Sin(w*Mathf.PI*.82f)*.43f;
            float folds=Mathf.Sin(u*Mathf.PI*9+w*4)*.015f*Mathf.Sin(w*Mathf.PI);
            v.Add(new Vector3(x,1.03f+body+folds,z));uv.Add(new Vector2(u,w));
            if(r<rows&&c<cols){int a=r*(cols+1)+c,b=a+cols+1;t.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
        }
        var mesh=new Mesh{name="Sleeping quilt volume"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string meshPath=Art+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        quilt.GetComponent<MeshFilter>().sharedMesh=saved;
        quilt.GetComponent<Renderer>().sharedMaterial=bed.transform.Find("Painted bed geometry/Bed folded cover").GetComponent<Renderer>().sharedMaterial;
        data.FindProperty("sleepingQuilt").objectReferenceValue=quilt;data.ApplyModifiedPropertiesWithoutUndo();quilt.SetActive(false);
    }
    public static void RefreshAtmosphere()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        Lighting();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    internal static void CenterChandelier()
    {
        var root=GameObject.Find("Tower candle chandelier").transform;
        root.localPosition=new Vector3(0,3.53f,.75f);
        var template=root.Find("Suspension link 0");
        // The ceiling is cut away in the room view; continue the chain above the frame.
        const float ceilingHeight=8.1f, first=.23f, spacing=.065f;
        float top=root.InverseTransformPoint(new Vector3(root.position.x,ceilingHeight,root.position.z)).y;
        int count=Mathf.CeilToInt((top-first)/spacing)+1;
        for(int i=0;i<count;i++)
        {
            string name="Suspension link "+i;
            var link=root.Find(name);
            if(link==null){link=Object.Instantiate(template,root);link.name=name;}
            link.localPosition=Vector3.up*Mathf.Lerp(first,top,i/(float)(count-1));
            link.localRotation=Quaternion.Euler(0,(i%2)*90,0);
        }
    }

    private static void Lighting()
    {
        CenterChandelier();
        var camera=Camera.main.GetUniversalAdditionalCameraData();
        camera.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camera.antialiasingQuality=AntialiasingQuality.High;
        EditorUtility.SetDirty(camera);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.11f,.105f,.10f);
        RenderSettings.ambientEquatorColor=new Color(.09f,.075f,.065f);
        RenderSettings.ambientGroundColor=new Color(.055f,.04f,.03f);
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(light.type==LightType.Directional){light.color=new Color(.93f,.88f,.79f);light.intensity=.07f;}
        var sun=GameObject.Find("Window morning light").GetComponent<Light>();
        sun.color=new Color(1,.79f,.53f);sun.intensity=24;sun.spotAngle=54;sun.innerSpotAngle=27;
        sun.shadows=LightShadows.Soft;sun.shadowStrength=1f;
        var chandelier=GameObject.Find("Chandelier warm light").GetComponent<Light>();
        chandelier.color=new Color(1,.70f,.42f);chandelier.intensity=1.4f;chandelier.range=6.8f;
        var desk=GameObject.Find("Desk candle warm accent").GetComponent<Light>();
        desk.color=new Color(1,.67f,.34f);desk.intensity=.95f;desk.range=3;desk.shadows=LightShadows.Soft;desk.shadowStrength=.75f;
        var old=GameObject.Find("Warm bedside candle");if(old!=null)Object.DestroyImmediate(old);
        var table=GameObject.Find("Bedside water table");
        if(table!=null)
        {
            var candle=new GameObject("Warm bedside candle");candle.transform.SetParent(table.transform,false);candle.transform.localPosition=new Vector3(.2f,1.01f,.23f);
            var wax=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wax.name="Bedside candle wax";wax.transform.SetParent(candle.transform,false);
            wax.transform.localPosition=new Vector3(0,.1f,0);wax.transform.localScale=new Vector3(.07f,.1f,.07f);
            wax.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Tower light - Tower candle wax.mat");Object.DestroyImmediate(wax.GetComponent<Collider>());
            var style=Resources.Load<FireVisualStyle>("Paper Fire");
            if(style!=null&&style.frames.Length>0)
            {
                var flame=new GameObject("Bedside paper candle flame",typeof(SpriteRenderer));flame.transform.SetParent(candle.transform,false);flame.transform.localPosition=Vector3.up*.24f;
                var sprite=flame.GetComponent<SpriteRenderer>();sprite.sprite=style.frames[0];sprite.sharedMaterial=style.material;
                var light=new GameObject("Bedside amber pool",typeof(Light)).GetComponent<Light>();light.transform.SetParent(candle.transform,false);light.transform.localPosition=Vector3.up*.4f;
                light.type=LightType.Point;light.color=new Color(1,.70f,.4f);light.intensity=.55f;light.range=2.8f;
                var flicker=candle.AddComponent<TowerChandelierVisual>();var data=new SerializedObject(flicker);
                data.FindProperty("candleLight").objectReferenceValue=light;var flames=data.FindProperty("flames");flames.arraySize=1;flames.GetArrayElementAtIndex(0).objectReferenceValue=sprite;data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Art/SceneStyle/Tower morning Volume.asset");
        if(profile.TryGet<ColorAdjustments>(out var grade))
        {grade.postExposure.Override(-1.40f);grade.contrast.Override(14);grade.colorFilter.Override(new Color(1,.97f,.92f));EditorUtility.SetDirty(grade);}
        if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.26f);bloom.threshold.Override(1.1f);EditorUtility.SetDirty(bloom);}
        EditorUtility.SetDirty(profile);
    }
}
