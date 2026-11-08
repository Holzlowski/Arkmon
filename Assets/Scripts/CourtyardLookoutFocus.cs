using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Scenic lens and grading: a protected sharp band works with transparent paper art too.
[DisallowMultipleComponent]
public sealed class CourtyardLookoutFocus : MonoBehaviour
{
    Camera view;
    Transform target;
    Volume volume;
    VolumeProfile profile;
    AurenfeldTiltShiftPass tiltShift;
    float blend;
    public bool TiltShiftEnabled { get; set; } = true;
    public float ForegroundStrength { get; set; } = .55f;
    public float Strength => blend;

    public void Begin(Camera camera)
    {
        End();
        view=camera;
        target=GameObject.Find("Aurenfeld distant farm")?.transform;
        if(volume==null)
        {
            var root=new GameObject("Aurenfeld scenic focus",typeof(Volume));
            root.transform.SetParent(transform,false);
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name="Temporary Aurenfeld scenic finish";
            // Suppress physical DoF so it cannot blur the protected paper farm/mountain band.
            var depth=profile.Add<DepthOfField>();depth.mode.Override(DepthOfFieldMode.Off);
            var color=profile.Add<ColorAdjustments>();
            color.contrast.Override(10);color.saturation.Override(4);
            var tones=profile.Add<ShadowsMidtonesHighlights>();
            tones.shadows.Override(new Vector4(.96f,.985f,1.04f,0));
            tones.highlights.Override(new Vector4(1.055f,1.01f,.95f,0));
            var bloom=profile.Add<Bloom>();
            bloom.intensity.Override(.24f);bloom.threshold.Override(1.08f);
            bloom.scatter.Override(.65f);
            bloom.tint.Override(new Color(1,.94f,.83f));
            volume=root.GetComponent<Volume>();
            volume.isGlobal=true;volume.priority=1000;volume.sharedProfile=profile;
        }
        if(tiltShift==null)
        {
            var shader=Resources.Load<Shader>("AurenfeldTiltShift");
            if(shader!=null && shader.isSupported)tiltShift=new AurenfeldTiltShiftPass(shader);
            else Debug.LogError("Aurenfeld tilt-shift shader is missing or unsupported.",this);
        }
        volume.enabled=false;volume.weight=0;
        RenderPipelineManager.beginCameraRendering+=RenderScenicCamera;
    }
    public void Present(float progress)
    {
        if(view==null || volume==null || !isActiveAndEnabled)return;
        blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.92f,progress));
        volume.enabled=blend>.001f;volume.weight=blend;
    }
    void RenderScenicCamera(ScriptableRenderContext context,Camera camera)
    {
        if(camera!=view || camera.cameraType!=CameraType.Game || !TiltShiftEnabled
            || blend<=.001f || tiltShift==null || !isActiveAndEnabled)return;
        var point=target!=null?target.position+Vector3.up*3:new Vector3(-137,3,20);
        float center=Mathf.Clamp(camera.WorldToViewportPoint(point).y,.35f,.65f);
        tiltShift.Configure(center,blend,camera.aspect,Mathf.Clamp01(ForegroundStrength));
        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(tiltShift);
    }
    public void End()
    {
        RenderPipelineManager.beginCameraRendering-=RenderScenicCamera;
        blend=0;
        if(volume!=null){volume.enabled=false;volume.weight=0;}
    }
    void OnDisable()=>End();
    void OnDestroy()
    {
        End();tiltShift?.Dispose();
        if(volume!=null){volume.sharedProfile=null;Destroy(volume.gameObject);}
        if(profile!=null)
        {
            foreach(var component in profile.components)if(component!=null)Destroy(component);
            Destroy(profile);
        }
    }
}
