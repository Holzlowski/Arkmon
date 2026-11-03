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
    public bool Available => family != null && room != null && nearLeg.sprite != null && farLeg.sprite != null;

    public PaperPlayerWalkRig(Transform pivot, SpriteRenderer original)
    {
        this.original = original;
        root = new GameObject("Elian paper walking rig");
        root.transform.SetParent(pivot, false);
        family = Resources.Load<Sprite>("Elian-Walk-Family");
        room = Resources.Load<Sprite>("Elian-Walk-Room");
        farLeg = Part("Far leg", "Elian-Walk-FarLeg", 1);
        nearLeg = Part("Near leg", "Elian-Walk-NearLeg", 2);
        torso = Part("Stable upper body", "Elian-Walk-Family", 3);
        nearArm = Part("Near arm", "Elian-Walk-Family", 4);
        farArm = Part("Far arm", "Elian-Walk-Family", 0);
        limbMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        limbMaterial.SetFloat("_Surface", 1); limbMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        limbMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); limbMaterial.SetFloat("_ZWrite", 0);
        limbMaterial.SetFloat("_Cull", 0); limbMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        limbMaterial.renderQueue = 3000;
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
            mesh = new Mesh { name = key + " clipped walking mesh" };
            float[] y = { 0, .1f, .3f, .44f, .5f, 1 };
            float[] left = { .25f, .29f, .36f, .30f, 0, 0 };
            float[] right = { .82f, .78f, .70f, .74f, 1, 1 };
            Vector2 pivot = part == "Body" ? new Vector2(.5f, 0) : part == "NearArm" ? new Vector2(.30f, .44f) : new Vector2(.74f, .44f);
            int bands = part == "Body" ? 6 : 5;
            var vertices = new Vector3[bands * 2]; var uv = new Vector2[bands * 2]; var triangles = new int[(bands - 1) * 6];
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            for (int i = 0; i < bands; i++)
            {
                float a = part == "NearArm" ? 0 : part == "FarArm" ? right[i] : left[i];
                float b = part == "NearArm" ? left[i] : part == "FarArm" ? 1 : right[i];
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
            meshes.Add(key, mesh);
        }
        renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
        properties.SetTexture("_BaseMap", sprite.texture); properties.SetColor("_BaseColor", original.color);
        renderer.SetPropertyBlock(properties);
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

    public void Present(bool visible, bool familyOutfit, bool flip, float phase)
    {
        visible &= Available;
        root.SetActive(visible);
        original.forceRenderingOff = visible;
        if (!visible) return;
        float size = original.sprite != null ? original.sprite.bounds.size.y / 2.07f : 1;
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
        float bob = .012f * (1 - Mathf.Cos(phase * 2));
        torso.transform.localPosition = new Vector3(0, .65f + bob, -.015f);
        if (armsAvailable)
        {
            // Shoulder positions use the full source rectangle, even though the body mesh is clipped.
            var source = familyOutfit ? family : room;
            Vector2 extent = source.rect.size / source.pixelsPerUnit;
            nearArm.transform.localPosition = torso.transform.localPosition + new Vector3(-.20f * extent.x, .44f * extent.y, -.01f);
            farArm.transform.localPosition = torso.transform.localPosition + new Vector3(.24f * extent.x, .44f * extent.y, .025f);
            nearArm.transform.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(phase) * 9);
            farArm.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 9);
            nearArm.color = farArm.color = original.color;
        }
        AnimateLeg(nearLeg, phase, .055f, -.01f);
        AnimateLeg(farLeg, phase + Mathf.PI, -.055f, .005f);
        // Rotating a wide boot can put its toe below the hip-derived baseline.
        KeepAboveFootPlane(nearLeg);
        KeepAboveFootPlane(farLeg);
        torso.color = nearLeg.color = original.color;
        farLeg.color = original.color * new Color(.85f, .85f, .85f, 1);
    }

    private void KeepAboveFootPlane(SpriteRenderer leg)
    {
        float lift = root.transform.position.y + .035f - leg.bounds.min.y;
        if (lift > 0) leg.transform.position += Vector3.up * lift;
    }

    private static void AnimateLeg(SpriteRenderer leg, float phase, float hipX, float depth)
    {
        float stride = Mathf.Sin(phase);
        leg.transform.localPosition = new Vector3(hipX, .75f + Mathf.Abs(stride) * .085f + Mathf.Max(0, Mathf.Cos(phase)) * .035f, depth);
        leg.transform.localRotation = Quaternion.Euler(0, 0, stride * (stride < 0 ? 40 : 27));
    }

    public void Hide() { if (root != null) root.SetActive(false); if (original != null) original.forceRenderingOff = false; }
    public void Dispose()
    {
        Hide(); if (root != null) Object.Destroy(root);
        foreach (var mesh in meshes.Values) Object.Destroy(mesh);
        Object.Destroy(limbMaterial);
    }
}
