using UnityEngine;

// Preserve the foreground wall collision while revealing Elian close to the garden edge.
[DefaultExecutionOrder(9508)]
public sealed class CourtyardNearView : MonoBehaviour
{
    static readonly int Cutaway=Shader.PropertyToID("_ViewCutaway");
    Renderer wall;
    Transform player;
    MaterialPropertyBlock block;
    Vector4 original;
    float radius;
    internal static void Install()
    {
        var wall=GameObject.Find("Cutaway near castle wall");
        if(wall==null || wall.GetComponent<CourtyardNearView>()!=null)return;
        var renderer=wall.GetComponent<Renderer>();
        if(renderer==null || renderer.sharedMaterial==null || !renderer.sharedMaterial.HasProperty(Cutaway))return;
        var style=wall.AddComponent<CourtyardNearView>();style.wall=renderer;
        style.player=GameObject.FindWithTag("Player")?.transform;
        style.block=new MaterialPropertyBlock();renderer.GetPropertyBlock(style.block);
        style.original=style.block.HasVector(Cutaway)?style.block.GetVector(Cutaway):Vector4.zero;
    }
    void LateUpdate()
    {
        if(player==null || wall==null)return;
        float near=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,6,Mathf.Abs(player.position.z-wall.bounds.center.z)));
        float target=CourtyardLookout.IsPresenting || SceneLoadManager.IsTransitioning?0:near*1.4f;
        radius=Mathf.MoveTowards(radius,target,Time.unscaledDeltaTime*3);
        var at=player.position;
        wall.GetPropertyBlock(block);block.SetVector(Cutaway,new Vector4(at.x,at.y,at.z,radius));wall.SetPropertyBlock(block);
    }
    void OnDisable()
    {
        radius=0;if(wall==null || block==null)return;
        wall.GetPropertyBlock(block);block.SetVector(Cutaway,original);wall.SetPropertyBlock(block);
    }
}
