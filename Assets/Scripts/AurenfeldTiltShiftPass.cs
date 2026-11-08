using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Enqueued only by the active lookout camera. No shared renderer asset is modified.
public sealed class AurenfeldTiltShiftPass : ScriptableRenderPass
{
    readonly Material material;
    public AurenfeldTiltShiftPass(Shader shader)
    {
        material=CoreUtils.CreateEngineMaterial(shader);
        renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture=true;
    }
    public void Configure(float center,float strength,float aspect,float foreground)
    {
        material.SetVector("_FocusBand",new Vector4(.08f,.32f,center,strength*.85f));
        const float radius=3f/1080;
        material.SetVector("_Blur",new Vector4(radius/Mathf.Max(.1f,aspect),radius,foreground,0));
    }
    public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
    {
        var resources=frame.Get<UniversalResourceData>();
        if(resources.isActiveTargetBackBuffer || material==null)return;
        var source=resources.activeColorTexture;
        var desc=graph.GetTextureDesc(source);
        desc.name="Aurenfeld tilt-shift color";desc.clearBuffer=false;
        var destination=graph.CreateTexture(desc);
        graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source,destination,material,0),passName:"Aurenfeld gentle tilt-shift");
        resources.cameraColor=destination;
    }
    public void Dispose() => CoreUtils.Destroy(material);
}
