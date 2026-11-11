using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class PaperIceIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        for (int i = -1; i <= 1; i++)
        {
            float offset = i * .24f;
            float lift = i == 0 ? .10f : -.04f;
            int start = mesh.currentVertCount;
            Point(offset - .11f, .12f + lift, new Color(.35f, .58f, .72f));
            Point(offset, .43f + lift, new Color(.86f, .96f, 1f));
            Point(offset + .11f, .12f + lift, new Color(.59f, .81f, .93f));
            Point(offset, -.43f + lift, new Color(.35f, .58f, .72f));
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        void Point(float x, float y, Color tint)
            => mesh.AddVert(rect.center + new Vector2(x * rect.width, y * rect.height), tint, Vector2.zero);
    }
}
