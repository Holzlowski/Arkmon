using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// Continuous cut-paper gait. Both legs share a stable hip and move half a cycle apart.
public sealed class PaperPlayerWalkRig
{
    private readonly GameObject root;
    private readonly SpriteRenderer torso, nearLeg, farLeg;
    private readonly SpriteRenderer nearArm, farArm;
    private readonly MeshRenderer bodyMesh, nearArmMesh, farArmMesh;
    private readonly Material limbMaterial;
    private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private readonly Sprite family, room;
    private readonly SpriteRenderer original;
    private SpriteRenderer arrivalMouth;
    public bool Visible => root!=null && root.activeInHierarchy;
    public bool Available => family != null && room != null && nearLeg.sprite != null && farLeg.sprite != null;

    public PaperPlayerWalkRig(Transform pivot, SpriteRenderer original)
    {
        this.original = original;
        root = new GameObject("Elian paper walking rig");
        root.transform.SetParent(pivot, false);
        var group=root.AddComponent<SortingGroup>();
        group.sortingLayerID=original.sortingLayerID;group.sortingOrder=original.sortingOrder;
        family = Resources.Load<Sprite>("Elian-Walk-Family");
        room = Resources.Load<Sprite>("Elian-Walk-Room");
        farLeg = Part("Far leg", "Elian-Walk-FarLeg", 1);
        nearLeg = Part("Near leg", "Elian-Walk-NearLeg", 2);
        torso = Part("Stable upper body", "Elian-Walk-Family", 3);
        nearArm = Part("Near arm", "Elian-Walk-Family", 4);
        farArm = Part("Far arm", "Elian-Walk-Family", 0);
        limbMaterial = CreatePaperMaterial();
        bodyMesh = MeshPart(torso); nearArmMesh = MeshPart(nearArm); farArmMesh = MeshPart(farArm);
        root.SetActive(false);
    }

    private MeshRenderer MeshPart(SpriteRenderer source)
    {
        var obj = new GameObject(source.name + " painted surface", typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(source.transform, false);
        var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = limbMaterial;
        renderer.sortingLayerID = source.sortingLayerID; renderer.sortingOrder = source.sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
    }

    private void PresentMesh(MeshRenderer renderer, Sprite sprite)
    {
        string part = renderer == bodyMesh ? "Body" : renderer == nearArmMesh ? "NearArm" : "FarArm";
        string key = sprite.name + part;
        if (!meshes.TryGetValue(key, out var mesh))
        {
            mesh = CreatePaintedMesh(sprite, part);
            meshes.Add(key, mesh);
        }
        renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
        properties.SetTexture("_BaseMap", sprite.texture); properties.SetColor("_BaseColor", original.color);
        renderer.SetPropertyBlock(properties);
    }

    public static Material CreatePaperMaterial()
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
        material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
        return material;
    }
    public static Mesh CreatePaintedMesh(Sprite sprite,string part)
    {
            var mesh = new Mesh { name = sprite.name + part + " overlapping paper mesh" };
            float[] y = { 0, .1f, .3f, .44f, .5f, 1 };
            float[] left = { .25f, .29f, .36f, .30f, 0, 0 };
            float[] right = { .82f, .78f, .70f, .74f, 1, 1 };
            Vector2 pivot = part == "Body" ? new Vector2(.5f, 0) : part == "NearArm" ? new Vector2(.34f, .44f) : new Vector2(.72f, .44f);
            int bands = part == "Body" ? 6 : 5;
            var vertices = new Vector3[bands * 2]; var uv = new Vector2[bands * 2]; var triangles = new int[(bands - 1) * 6];
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            for (int i = 0; i < bands; i++)
            {
                float overlap=i>=2 && i<=3 ? .045f : 0;
                float a = part == "NearArm" ? 0 : part == "FarArm" ? Mathf.Max(0,right[i]-overlap) : Mathf.Max(0,left[i]-overlap);
                float b = part == "NearArm" ? Mathf.Min(1,left[i]+overlap) : part == "FarArm" ? 1 : Mathf.Min(1,right[i]+overlap);
                for (int side = 0; side < 2; side++)
                {
                    Vector2 point = new Vector2(side == 0 ? a : b, y[i]);
                    vertices[i * 2 + side] = Vector2.Scale(point - pivot, size);
                    Vector2 pixel = Vector2.Scale(point, sprite.rect.size) + sprite.rect.position;
                    uv[i * 2 + side] = new Vector2(pixel.x / sprite.texture.width, pixel.y / sprite.texture.height);
                }
                if (i == bands - 1) continue;
                int k = i * 2, t = i * 6;
                triangles[t] = k; triangles[t+1] = k+2; triangles[t+2] = k+3;
                triangles[t+3] = k; triangles[t+4] = k+3; triangles[t+5] = k+1;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds();
        return mesh;
    }

    private SpriteRenderer Part(string name, string resource, int order)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(root.transform, false);
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>(resource);
        renderer.sharedMaterial = original.sharedMaterial;
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder + order;
        return renderer;
    }

    public void Present(bool visible, bool familyOutfit, bool flip, float phase, bool walking, float weight = -1, bool arrivalReaction = false)
    {
        visible &= Available;
        root.SetActive(visible);
        original.forceRenderingOff = visible;
        if (!visible) return;
        PresentArrivalMouth(familyOutfit && arrivalReaction);
        // Both intro outfits share the same body scale; transparent sprite margins must not resize Elian.
        float size = 2f / 2.07f;
        root.transform.localScale = new Vector3(flip ? -size : size, size, 1);
        torso.sprite = familyOutfit ? family : room;
        var body = familyOutfit ? family : room;
        var front = body; var back = body;
        bool armsAvailable = body != null && front != null && back != null;
        nearArm.gameObject.SetActive(armsAvailable); farArm.gameObject.SetActive(armsAvailable);
        torso.enabled = !armsAvailable; nearArm.enabled = farArm.enabled = false;
        bodyMesh.enabled = nearArmMesh.enabled = farArmMesh.enabled = armsAvailable;
        if (armsAvailable)
        {
            torso.sprite = body; nearArm.sprite = front; farArm.sprite = back;
            PresentMesh(bodyMesh, body); PresentMesh(nearArmMesh, front); PresentMesh(farArmMesh, back);
        }
        if(weight < 0) weight = walking ? 1 : 0;
        float bob = .006f * weight * (1 - Mathf.Cos(phase * 2));
        torso.transform.localPosition = new Vector3(0, .65f + bob, -.015f);
        if (armsAvailable)
        {
            // Shoulder positions use the full source rectangle, even though the body mesh is clipped.
            var source = familyOutfit ? family : room;
            Vector2 extent = source.rect.size / source.pixelsPerUnit;
            nearArm.transform.localPosition = torso.transform.localPosition + new Vector3(-.16f * extent.x, .44f * extent.y, -.01f);
            farArm.transform.localPosition = torso.transform.localPosition + new Vector3(.22f * extent.x, .44f * extent.y, .025f);
            nearArm.transform.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(phase) * 12 * weight);
            farArm.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 12 * weight);
            nearArm.color = farArm.color = original.color;
        }
        root.transform.localPosition = Vector3.zero;
        PaperPlayerLegPose.Present(nearLeg, farLeg, root.transform, phase, weight, 0);
        torso.color = nearLeg.color = original.color;
        farLeg.color = original.color * new Color(.85f, .85f, .85f, 1);
    }

    private void PresentArrivalMouth(bool visible)
    {
        if (arrivalMouth == null && visible)
        {
            var texture = Resources.Load<Texture2D>("Elian-Arrival-Reaction");
            if (texture == null) return;
            // Registered 82 × 49 crop at (369,305) on the 1254-pixel square source.
            const float sourceSize = 1254;
            float scale = family.texture.width/sourceSize;
            var center = new Vector2(410,sourceSize-329.5f)*scale;
            var mouth = new GameObject("Elian arrival expression",typeof(SpriteRenderer));
            mouth.transform.SetParent(torso.transform,false);
            Vector2 local = (center-family.rect.position-family.pivot)/family.pixelsPerUnit;
            mouth.transform.localPosition = new Vector3(local.x,local.y,-.02f);
            arrivalMouth = mouth.GetComponent<SpriteRenderer>();
            arrivalMouth.sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),
                new Vector2(.5f,.5f),family.pixelsPerUnit/scale);
            arrivalMouth.sharedMaterial = original.sharedMaterial;
            arrivalMouth.sortingLayerID = torso.sortingLayerID;
            arrivalMouth.sortingOrder = torso.sortingOrder+1;
        }
        if (arrivalMouth == null) return;
        arrivalMouth.enabled = visible;
        arrivalMouth.color = original.color;
    }

    public void Hide() { if (root != null) root.SetActive(false); if (original != null) original.forceRenderingOff = false; }
    public void Dispose()
    {
        Hide(); if (root != null) Object.Destroy(root);
        foreach (var mesh in meshes.Values) Object.Destroy(mesh);
        if (arrivalMouth != null) Object.Destroy(arrivalMouth.sprite);
        Object.Destroy(limbMaterial);
    }
}

// The leg drawings keep their size. Their tops stay hidden inside the tunic overlap;
// a short vertical foot correction cannot pull the cut edge below the garment.
public static class PaperPlayerLegPose
{
    public static void Present(SpriteRenderer near, SpriteRenderer far, Transform root, float phase, float weight, float bend)
    {
        Leg(near,root,phase,weight,bend,.07f,0,-.01f);
        Leg(far,root,phase+Mathf.PI,weight,bend,-.07f,-16,.005f);
    }
    static void Leg(SpriteRenderer leg,Transform root,float phase,float weight,float bend,float hip,float restAngle,float depth)
    {
        float stride=Mathf.Sin(phase);
        leg.transform.localPosition=new Vector3(hip,.80f-.12f*bend,depth);
        leg.transform.localRotation=Quaternion.Euler(0,0,
            Mathf.Lerp(restAngle,-8+stride*24,weight)+(hip>0?-12:10)*bend);
        leg.transform.localScale=Vector3.one;
        float lift=.055f*Mathf.Max(0,stride)*weight;
        float target=root.position.y+.012f+lift*root.lossyScale.y;
        float up=root.TransformVector(Vector3.up).y;
        if(Mathf.Abs(up)>.001f)leg.transform.localPosition+=Vector3.up*((target-leg.bounds.min.y)/up);
    }
}
