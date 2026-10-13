using UnityEngine;
using UnityEngine.UI;

// Native UI geometry keeps the shield sharp at any canvas resolution.
[RequireComponent(typeof(CanvasRenderer))]
public class PaperGuardIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); Rect r = rectTransform.rect;
        Draw(mesh, r, 1, new Color(.22f,.14f,.09f));
        Draw(mesh, r, .84f, new Color(.76f,.57f,.30f));
        Draw(mesh, r, .63f, new Color(.37f,.44f,.27f));
    }
    private static void Draw(VertexHelper mesh, Rect r, float scale, Color tint)
    {
        int start = mesh.currentVertCount;
        Vector2[] points = {new Vector2(0,0),new Vector2(-.4f,.43f),new Vector2(.4f,.43f),
            new Vector2(.36f,-.1f),new Vector2(0,-.48f),new Vector2(-.36f,-.1f)};
        foreach(var p in points) mesh.AddVert(new Vector3(r.center.x+p.x*r.width*scale,r.center.y+p.y*r.height*scale),tint,Vector2.zero);
        for(int i=1;i<=5;i++)mesh.AddTriangle(start,start+i,start+(i==5?1:i+1));
    }
}
