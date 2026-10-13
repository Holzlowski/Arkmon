using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ConfigureOpeningDetails
{
    const string Art="Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Finish tower opening details")]
    public static void Run()
    {
        ImportArt();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ConfigurePaintedBed.Apply(); Apply();ConfigureArchedWindow.Apply();ConfigureRoundTowerRoom.Apply();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    public static void ImportArt()
    {
        TextureImporter importer;
        foreach(string pose in new[]{"Prince-Sitting.png","Prince-Sleeping.png"})
        {
        importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+pose);
        if(importer!=null)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=pose=="Prince-Sitting.png"?(int)SpriteAlignment.BottomCenter:(int)SpriteAlignment.Center;settings.spritePivot=pose=="Prince-Sitting.png"?new Vector2(.5f,0):new Vector2(.5f,.5f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        }
        foreach(string texture in new[]{"Bed-Velvet-Painted.png","Bed-Linen-Painted.png"})
        {
        importer=(TextureImporter)AssetImporter.GetAtPath(Art+texture);
        if(importer!=null){importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        }
    }
    static Material Mat(string name,Color color,string texture=null)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.06f);
        mat.SetTexture("_BaseMap",texture!=null?AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture):null);EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Part(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
    }
    public static void Apply()
    {
        Transform room=GameObject.Find("Tower room interior").transform;
        Desk();
        ConfigurePreparationEquipment.ApplyTowerLamp();
        var old=room.Find("Tower morning atmosphere");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Tower morning atmosphere").transform;root.SetParent(room,false);root.gameObject.AddComponent<TowerMorningAtmosphere>();
        var light=new GameObject("Window morning light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);light.transform.localPosition=new Vector3(0,3.4f,5.35f);
        light.transform.LookAt(new Vector3(-1.8f,.1f,1.1f));light.type=LightType.Spot;light.color=new Color(1,.84f,.55f);light.intensity=24f;light.range=11;light.spotAngle=52;light.innerSpotAngle=28;light.shadows=LightShadows.Soft;
        string path=Art+"Window morning beam.mat";var beamMat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(beamMat==null){beamMat=new Material(Shader.Find("Arkmon/Morning Light Beam"));AssetDatabase.CreateAsset(beamMat,path);}
        beamMat.SetColor("_BaseColor",new Color(1.5f,1.12f,.61f,.24f));EditorUtility.SetDirty(beamMat);
        var mesh=new Mesh{name="Window morning beam"};mesh.vertices=new[]{new Vector3(-1.1f,3.2f,5.35f),new Vector3(1.1f,3.2f,5.35f),new Vector3(.2f,.04f,.7f),new Vector3(-3.8f,.04f,.7f)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();mesh.RecalculateNormals();
        path=Art+"Window morning beam.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        var beam=new GameObject("Soft painted light shaft",typeof(MeshFilter),typeof(MeshRenderer));beam.transform.SetParent(root,false);beam.GetComponent<MeshFilter>().sharedMesh=saved;beam.GetComponent<Renderer>().sharedMaterial=beamMat;beam.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        // Several intersecting soft sheets give the light a volume from different room angles.
        foreach(float angle in new[]{-9f,9f})
        {
            var ray=Object.Instantiate(beam,root);ray.name="Layered morning haze "+angle;
            ray.transform.localRotation=Quaternion.Euler(0,angle,0);
        }
        path=Art+"Window morning floor glow.mat";var glowMat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(glowMat==null){glowMat=new Material(Shader.Find("Arkmon/Morning Light Beam"));AssetDatabase.CreateAsset(glowMat,path);}
        glowMat.SetColor("_BaseColor",new Color(1.35f,.89f,.39f,.42f));EditorUtility.SetDirty(glowMat);
        var glowMesh=new Mesh{name="Window morning floor glow"};glowMesh.vertices=new[]{new Vector3(-2.6f,.022f,2.8f),new Vector3(.4f,.022f,2.8f),new Vector3(-.7f,.022f,-.5f),new Vector3(-4,.022f,-.5f)};
        glowMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};glowMesh.triangles=new[]{0,1,2,0,2,3};glowMesh.RecalculateNormals();glowMesh.RecalculateBounds();
        path=Art+"Window morning floor glow.asset";var glowSaved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(glowSaved==null){AssetDatabase.CreateAsset(glowMesh,path);glowSaved=glowMesh;}else{EditorUtility.CopySerialized(glowMesh,glowSaved);Object.DestroyImmediate(glowMesh);EditorUtility.SetDirty(glowSaved);}
        var glow=new GameObject("Golden morning patch",typeof(MeshFilter),typeof(MeshRenderer));glow.transform.SetParent(root,false);glow.GetComponent<MeshFilter>().sharedMesh=glowSaved;glow.GetComponent<Renderer>().sharedMaterial=glowMat;glow.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        var dust=new GameObject("Sunlit dust",typeof(ParticleSystem));dust.transform.SetParent(root,false);dust.transform.localPosition=new Vector3(-.8f,2.2f,3.2f);
        var ps=dust.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=219;
        var main=ps.main;main.duration=12;main.loop=true;main.prewarm=true;main.startLifetime=new ParticleSystem.MinMaxCurve(7,12);main.startSpeed=.035f;main.startSize=new ParticleSystem.MinMaxCurve(.018f,.055f);main.startColor=new Color(1,.91f,.71f,.65f);main.maxParticles=180;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=ps.emission;emission.rateOverTime=13;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(3.1f,2.3f,3.5f);
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.x=.018f;velocity.y=.009f;velocity.z=-.013f;
        var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.3f,.2f),new GradientAlphaKey(.3f,.8f),new GradientAlphaKey(0,1)});color.color=gradient;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Magic Sparks.mat");
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art+"Tower morning Volume.asset");
        if(profile!=null)
        {
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.32f);bloom.threshold.Override(.95f);bloom.scatter.Override(.65f);
            if(!profile.TryGet<ColorAdjustments>(out var grade)){grade=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grade,profile);}
            grade.contrast.Override(12);grade.saturation.Override(5);grade.postExposure.Override(.12f);
            grade.colorFilter.Override(new Color(1,.97f,.90f));EditorUtility.SetDirty(profile);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(grade);
        }
    }
    static void Desk()
    {
        var desk=GameObject.Find("Writing desk");if(desk==null)return;
        var paper=desk.GetComponent<PaperPropVisual>();if(paper!=null)Object.DestroyImmediate(paper);
        foreach(var sr in desk.GetComponentsInChildren<SpriteRenderer>(true))
        {if(sr.gameObject==desk)Object.DestroyImmediate(sr);else Object.DestroyImmediate(sr.gameObject);}
        var old=desk.transform.Find("Painted desk geometry");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Painted desk geometry").transform;root.SetParent(desk.transform,false);
        var oak=Mat("Desk painted oak",new Color(.73f,.55f,.34f),"Tower-Oak-Painted.png");
        var gold=Mat("Desk aged brass",new Color(.65f,.44f,.18f));var cream=Mat("Desk parchment",new Color(.92f,.85f,.68f));var red=Mat("Desk crimson book",new Color(.40f,.15f,.12f));var green=Mat("Desk green book",new Color(.16f,.28f,.20f));var ink=Mat("Desk ink",new Color(.13f,.12f,.12f));
        Part(root,"Oak desktop",new Vector3(0,1.12f,0),new Vector3(2.55f,.14f,1.05f),oak);
        Part(root,"Desk drawer rail",new Vector3(0,.94f,0),new Vector3(2.35f,.24f,.85f),oak);
        foreach(float x in new[]{-.57f,.57f})
        {
            Part(root,"Drawer front",new Vector3(x,.94f,-.44f),new Vector3(1.02f,.20f,.06f),oak);
            Part(root,"Brass drawer pull",new Vector3(x,.94f,-.50f),new Vector3(.09f,.07f,.06f),gold,PrimitiveType.Sphere);
        }
        foreach(float x in new[]{-1.08f,1.08f})foreach(float z in new[]{-.35f,.35f})
        {
            Part(root,"Turned desk leg",new Vector3(x,.47f,z),new Vector3(.12f,.46f,.12f),oak,PrimitiveType.Cylinder);
            Part(root,"Carved leg collar",new Vector3(x,.66f,z),new Vector3(.19f,.11f,.19f),oak,PrimitiveType.Sphere);
            Part(root,"Oak desk foot",new Vector3(x,.07f,z),new Vector3(.19f,.13f,.19f),oak,PrimitiveType.Sphere);
        }
        Part(root,"Desk rear stretcher",new Vector3(0,.3f,.35f),new Vector3(2.2f,.11f,.1f),oak);
        var page=Part(root,"Letter on desk",new Vector3(.30f,1.201f,-.04f),new Vector3(.68f,.012f,.45f),cream);page.localRotation=Quaternion.Euler(0,-12,0);
        for(int i=0;i<5;i++)Part(page,"Ink line",new Vector3(0,.55f,-.28f+i*.115f),new Vector3(.65f,.05f,.015f),ink);
        for(int i=0;i<3;i++)
        {
            float height=1.25f+i*.11f;
            var book=new GameObject("Painted book").transform;book.SetParent(root,false);book.localPosition=new Vector3(-.79f,height,.12f);book.localRotation=Quaternion.Euler(0,i*9-8,0);
            Part(book,"Book page edges",Vector3.zero,new Vector3(.45f,.08f,.50f),cream);
            foreach(float y in new[]{-.047f,.047f})Part(book,"Book cover",new Vector3(0,y,0),new Vector3(.49f,.015f,.54f),i%2==0?green:red);
            Part(book,"Book spine",new Vector3(-.235f,0,0),new Vector3(.03f,.11f,.54f),i%2==0?green:red);
        }
        Part(root,"Ink pot",new Vector3(.85f,1.27f,-.10f),new Vector3(.16f,.15f,.16f),ink,PrimitiveType.Sphere);
        var quill=Part(root,"Ivory quill",new Vector3(.91f,1.47f,-.10f),new Vector3(.055f,.4f,.025f),cream,PrimitiveType.Sphere);quill.localRotation=Quaternion.Euler(0,0,-27);
        Part(root,"Candle holder",new Vector3(.76f,1.21f,.30f),new Vector3(.21f,.04f,.21f),gold,PrimitiveType.Cylinder);
        Part(root,"Desk candle",new Vector3(.76f,1.41f,.30f),new Vector3(.075f,.18f,.075f),cream,PrimitiveType.Cylinder);
        var flame=new GameObject("Desk candle flame",typeof(SpriteRenderer));flame.transform.SetParent(root,false);flame.transform.localPosition=new Vector3(.76f,1.6f,.30f);
        var flicker=flame.AddComponent<TowerChandelierVisual>();var data=new SerializedObject(flicker);data.FindProperty("flames").arraySize=1;data.FindProperty("flames").GetArrayElementAtIndex(0).objectReferenceValue=flame.GetComponent<SpriteRenderer>();data.ApplyModifiedPropertiesWithoutUndo();
        var chair=new GameObject("Painted desk chair").transform;chair.SetParent(root,false);chair.localPosition=new Vector3(-.25f,0,-.93f);
        Part(chair,"Chair seat",new Vector3(0,.61f,0),new Vector3(.74f,.12f,.65f),oak);
        Part(chair,"Seat cushion",new Vector3(0,.70f,0),new Vector3(.66f,.11f,.58f),red,PrimitiveType.Sphere);
        Part(chair,"Chair back",new Vector3(0,1.07f,-.28f),new Vector3(.72f,.78f,.09f),oak);
        foreach(float x in new[]{-.29f,.29f})foreach(float z in new[]{-.24f,.24f})Part(chair,"Chair leg",new Vector3(x,.30f,z),new Vector3(.09f,.3f,.09f),oak,PrimitiveType.Cylinder);
        var collider=desk.GetComponent<BoxCollider>()??desk.AddComponent<BoxCollider>();collider.center=new Vector3(0,.6f,0);collider.size=new Vector3(2.6f,1.2f,1.12f);
    }
}
