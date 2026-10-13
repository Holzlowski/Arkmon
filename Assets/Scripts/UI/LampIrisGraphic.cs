using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LampIrisGraphic : MaskableGraphic
{
    public float Openness { get; private set; } = 1f;
    private Material irisMaterial;
    protected override void Awake()
    {
        base.Awake();
        Material template=Resources.Load<Material>("Player Lamp Iris");
        irisMaterial=template!=null?new Material(template):new Material(Shader.Find("Arkmon/Player Lamp Iris"));
        material=irisMaterial;UpdateShape();
    }
    public void SetOpenness(float value) { Openness=Mathf.Clamp01(value);UpdateShape(); }
    private void UpdateShape()
    {
        if(irisMaterial==null)return;
        irisMaterial.SetFloat("_Openness",Openness);
        irisMaterial.SetFloat("_Aspect",rectTransform.rect.width/Mathf.Max(1,rectTransform.rect.height));
        SetMaterialDirty();
    }
    protected override void OnRectTransformDimensionsChange() { base.OnRectTransformDimensionsChange();UpdateShape(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();Rect r=rectTransform.rect;
        mesh.AddVert(new Vector3(r.xMin,r.yMin),color,new Vector2(0,0));
        mesh.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(0,1));
        mesh.AddVert(new Vector3(r.xMax,r.yMax),color,new Vector2(1,1));
        mesh.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(1,0));
        mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
    }
    protected override void OnDestroy() { if(irisMaterial!=null)Destroy(irisMaterial);base.OnDestroy(); }
}
