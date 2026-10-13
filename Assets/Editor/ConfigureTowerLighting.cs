using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ConfigureTowerLighting
{
    const string Art = "Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Refine tower lighting")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var copies = new Dictionary<Material, Material>();
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var material = renderer.sharedMaterial;
            if (material == null || material.shader == null) continue;
            if (material.name.StartsWith("Tower light - ")) continue;
            if (material.shader.name != "Arkmon/Painted castle" && material.shader.name != "Universal Render Pipeline/Lit") continue;
            if (!copies.TryGetValue(material, out var copy))
            {
                string path = Art + "Tower light - " + material.name + ".mat";
                copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (copy == null) { copy = new Material(material); AssetDatabase.CreateAsset(copy, path); }
                if (material.shader.name == "Arkmon/Painted castle") copy.shader = Shader.Find("Arkmon/Tower Painted Light");
                else { copy.SetFloat("_Smoothness", material.name.Contains("brass") || material.name.Contains("gold") ? .3f : .035f); }
                EditorUtility.SetDirty(copy); copies.Add(material, copy);
            }
            renderer.sharedMaterial = copy; renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.18f,.24f,.35f);
        RenderSettings.ambientEquatorColor = new Color(.13f,.16f,.21f);
        RenderSettings.ambientGroundColor = new Color(.07f,.055f,.04f);
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { light.color = new Color(.76f,.84f,1); light.intensity = .25f; light.shadows = LightShadows.None; }
        var sun = GameObject.Find("Window morning light").GetComponent<Light>();
        sun.transform.position = new Vector3(.35f,3.8f,6.9f); sun.transform.LookAt(new Vector3(-2.4f,.1f,.4f));
        sun.color = new Color(1,.85f,.64f); sun.intensity = 32; sun.range = 15; sun.spotAngle = 48; sun.innerSpotAngle = 30;
        sun.shadows = LightShadows.Soft; sun.shadowBias = .025f; sun.shadowNormalBias = .08f; sun.shadowCustomResolution = 2048;
        var sunData = new SerializedObject(sun.GetUniversalAdditionalLightData());
        sunData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 2;sunData.ApplyModifiedPropertiesWithoutUndo();
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.name.StartsWith("Layered morning haze")) renderer.enabled = false;
            if (renderer.name == "Golden morning patch") renderer.enabled = false;
        }
        var beam = AssetDatabase.LoadAssetAtPath<Material>(Art + "Window morning beam.mat");
        beam.SetColor("_BaseColor",new Color(1.15f,.95f,.71f,.065f)); EditorUtility.SetDirty(beam);
        var dust = GameObject.Find("Sunlit dust").GetComponent<ParticleSystem>(); dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        // A narrow, tilted volume follows the window beam instead of filling the room.
        dust.transform.position = new Vector3(-1.05f,2.1f,3.1f);
        dust.transform.rotation = Quaternion.LookRotation(new Vector3(-2.75f,-3.7f,-6.5f));
        var main = dust.main; main.maxParticles = 180; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(12,18);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.012f,.035f);
        main.startSize = new ParticleSystem.MinMaxCurve(.016f,.04f);
        main.startColor = new Color(1,.92f,.76f,.65f);
        var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.9f,1.3f,4.5f);
        var noise = dust.noise; noise.enabled = true; noise.strength = .09f; noise.frequency = .35f; noise.scrollSpeed = .08f;
        var color = dust.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.85f,.2f),new GradientAlphaKey(.85f,.75f),new GradientAlphaKey(0,1)});
        color.color = gradient;
        string dustPath = Art + "Sunlit dust motes.mat";
        var dustMaterial = AssetDatabase.LoadAssetAtPath<Material>(dustPath);
        if(dustMaterial == null){dustMaterial = new Material(Shader.Find("Arkmon/Sunlit Dust"));AssetDatabase.CreateAsset(dustMaterial,dustPath);}
        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = dustMaterial;
        var emission = dust.emission; emission.rateOverTime = 9; dust.Play();
        var chandelier = GameObject.Find("Chandelier warm light").GetComponent<Light>(); chandelier.intensity = .55f; chandelier.range = 5; chandelier.color = new Color(1,.69f,.38f);
        var desk = GameObject.Find("Writing desk");
        var old = desk.transform.Find("Desk candle warm accent"); if(old != null)Object.DestroyImmediate(old.gameObject);
        var candle = new GameObject("Desk candle warm accent",typeof(Light)).GetComponent<Light>();candle.transform.SetParent(desk.transform,false);
        candle.transform.localPosition = new Vector3(.76f,1.65f,.3f); candle.type = LightType.Point; candle.range = 2.2f;candle.intensity = .42f;candle.color = new Color(1,.65f,.32f);
        var flicker = desk.GetComponentInChildren<TowerChandelierVisual>();
        var flickerData = new SerializedObject(flicker); flickerData.FindProperty("candleLight").objectReferenceValue = candle;flickerData.ApplyModifiedPropertiesWithoutUndo();
        ContactShadows(); Mullions();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art + "Tower morning Volume.asset");
        if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.22f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.55f);EditorUtility.SetDirty(bloom);}
        if(profile.TryGet<ColorAdjustments>(out var grade)){grade.contrast.Override(15);grade.saturation.Override(-3);grade.postExposure.Override(.1f);grade.colorFilter.Override(Color.white);EditorUtility.SetDirty(grade);}
        EditorUtility.SetDirty(profile);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static void ContactShadows()
    {
        var old = GameObject.Find("Furniture contact shadows");if(old != null)Object.DestroyImmediate(old);
        var root = new GameObject("Furniture contact shadows");
        string path = Art + "Tower furniture contact shadow.mat";var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material == null){material = new Material(Shader.Find("Arkmon/Tower Contact Shadow"));AssetDatabase.CreateAsset(material,path);}
        foreach(string name in new[]{"Canopy bed","Wardrobe","Writing desk"})
        {
            var furniture = GameObject.Find(name);var bounds = new Bounds(furniture.transform.position,Vector3.zero);
            foreach(var renderer in furniture.GetComponentsInChildren<MeshRenderer>())bounds.Encapsulate(renderer.bounds);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);plane.name = name+" contact shade";plane.transform.SetParent(root.transform);
            plane.transform.position = new Vector3(bounds.center.x,.028f,bounds.center.z);plane.transform.rotation = Quaternion.Euler(90,0,0);
            plane.transform.localScale = new Vector3(bounds.size.x*1.12f,bounds.size.z*1.15f,1);Object.DestroyImmediate(plane.GetComponent<Collider>());
            var mesh = plane.GetComponent<MeshRenderer>();mesh.sharedMaterial = material;mesh.shadowCastingMode = ShadowCastingMode.Off;mesh.receiveShadows = false;
        }
    }
    static void Mullions()
    {
        var old = GameObject.Find("Window slender mullions");if(old != null)Object.DestroyImmediate(old);
        var root = new GameObject("Window slender mullions");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art+"Tower light - Bed painted oak.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>(Art+"Tower warm wood.mat");
        foreach(var item in new[]{(new Vector3(0,3.1f,6.05f),new Vector3(.055f,2.0f,.06f)),(new Vector3(0,3.1f,6.05f),new Vector3(2.35f,.055f,.06f))})
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);bar.name = "Window wood mullion";bar.transform.SetParent(root.transform);bar.transform.position = item.Item1;bar.transform.localScale = item.Item2;
            Object.DestroyImmediate(bar.GetComponent<Collider>());bar.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
