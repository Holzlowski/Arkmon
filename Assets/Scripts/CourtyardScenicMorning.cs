using UnityEngine;
using UnityEngine.SceneManagement;

// Own the morning setup for StartScene; shared sky assets and other scenes stay untouched.
public sealed class CourtyardScenicMorning : MonoBehaviour
{
    Light sun;
    Quaternion rotation;
    Color sunColor, skyColor, equatorColor, groundColor;
    float intensity;
    Material originalSky, morningSky;
    bool ready;
    [SerializeField, Range(0,1)] float cloudShadowStrength=.96f;
    float weatherTime;

    internal static void Install()
    {
        if (GameObject.Find("Aurenfeld scenic morning") != null) return;
        var style = new GameObject("Aurenfeld scenic morning").AddComponent<CourtyardScenicMorning>();
        style.sun = RenderSettings.sun;
        if (style.sun != null)
        {
            style.rotation = style.sun.transform.rotation;
            style.sunColor = style.sun.color;
            style.intensity = style.sun.intensity;
        }
        style.skyColor = RenderSettings.ambientSkyColor;
        style.equatorColor = RenderSettings.ambientEquatorColor;
        style.groundColor = RenderSettings.ambientGroundColor;
        style.originalSky = RenderSettings.skybox;
        if (style.originalSky != null)
            style.morningSky = new Material(style.originalSky) { name = "Aurenfeld warm horizon runtime" };
        style.ready = true;
        style.Apply();
    }
    void Apply()
    {
        if (!ready) return;
        Shader.SetGlobalFloat("_AurenfeldMorning", 1);
        UpdateClouds();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(18, 65, 0);
            sun.color = new Color(1, .84f, .58f);
            sun.intensity = 1.72f;
        }
        RenderSettings.ambientSkyColor = new Color(.46f,.54f,.66f);
        RenderSettings.ambientEquatorColor = new Color(.39f,.37f,.36f);
        RenderSettings.ambientGroundColor = new Color(.25f,.23f,.19f);
        if (morningSky != null)
        {
            morningSky.SetColor("_Horizon", new Color(.93f,.71f,.45f));
            morningSky.SetColor("_Zenith", new Color(.12f,.31f,.59f));
            if (sun != null) morningSky.SetVector("_SunDirection", -sun.transform.forward);
            RenderSettings.skybox = morningSky;
        }
    }
    void Update()
    {
        weatherTime+=Time.deltaTime;
        UpdateClouds();
    }
    void UpdateClouds()
    {
        Shader.SetGlobalVector("_AurenfeldClouds",new Vector4(weatherTime*.65625f,weatherTime*1.015625f,cloudShadowStrength,0));
    }
    void OnEnable() => Apply();
    void OnDisable()
    {
        Shader.SetGlobalFloat("_AurenfeldMorning", 0);
        Shader.SetGlobalVector("_AurenfeldClouds",Vector4.zero);
        if (!ready) return;
        if (sun != null)
        {
            sun.transform.rotation = rotation; sun.color = sunColor; sun.intensity = intensity;
        }
        // During scene unload, never replace the incoming scene's ambient/sky settings.
        if (SceneManager.GetActiveScene() == gameObject.scene)
        {
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = equatorColor;
            RenderSettings.ambientGroundColor = groundColor;
            if (RenderSettings.skybox == morningSky) RenderSettings.skybox = originalSky;
        }
    }
    void OnDestroy() { if (morningSky != null) Destroy(morningSky); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetGlobals()
    {
        Shader.SetGlobalFloat("_AurenfeldMorning",0);
        Shader.SetGlobalVector("_AurenfeldClouds",Vector4.zero);
    }
}
