using System;
using UnityEngine;

// Only the distant illustrations move. The room, window and interaction targets stay fixed.
[DefaultExecutionOrder(10000)]
public class WindowVistaParallax : MonoBehaviour
{
    [Serializable]
    private class Layer
    {
        public Transform target;
        public Vector2 cameraFollow;
        public float driftAmplitude;
        public float closeUpLift;
        public int closeUpOrder;
        [NonSerialized] public Renderer renderer;
        [NonSerialized] public Material originalMaterial;
        [NonSerialized] public Material closeUpMaterial;
        [NonSerialized] public Vector3 origin;
        [NonSerialized] public Vector3 velocity;
    }
    [SerializeField] private Layer[] layers;
    [SerializeField] private Vector3 windowCentre = new Vector3(0, 3.2f, 6);
    [SerializeField] private Vector3 referenceView = new Vector3(0, 2.4f, -4);
    [SerializeField] private bool layeredVista;
    private Camera view;
    private TowerRoomCamera roomCamera;
    private Vector3 cameraOrigin;
    private bool initialized;

    public void Configure(Transform background, Transform fields, Transform clouds)
    {
        RestoreLayers();
        layers = new[] {
            new Layer { target = background, cameraFollow = new Vector2(.55f, .08f) },
            new Layer { target = fields, cameraFollow = new Vector2(.15f, .04f) },
            new Layer { target = clouds, cameraFollow = new Vector2(.65f, .1f), driftAmplitude = .55f }
        };
        initialized = false;
    }
    public void ConfigureDepthLayers(Transform sky, Transform mountains, Transform fields, Transform hills, Transform clouds)
    {
        RestoreLayers();
        layers = new[] {
            new Layer { target = sky, cameraFollow = new Vector2(.92f, .65f), closeUpOrder = 1 },
            new Layer { target = mountains, cameraFollow = new Vector2(.84f, .50f), closeUpLift = .46f, closeUpOrder = 3 },
            new Layer { target = fields, cameraFollow = new Vector2(.58f, .32f), closeUpLift = .36f, closeUpOrder = 4 },
            new Layer { target = hills, cameraFollow = new Vector2(.28f, .12f), closeUpLift = .14f, closeUpOrder = 5 },
            new Layer { target = clouds, cameraFollow = new Vector2(.9f, .58f), driftAmplitude = .35f, closeUpOrder = 2 }
        };
        layeredVista = true; initialized = false;
    }
    private void LateUpdate()
    {
        if (layers == null || Camera.main == null) return;
        if (!initialized || view != Camera.main)
        {
            RestoreLayers();
            view = Camera.main; cameraOrigin = layeredVista ? referenceView : view.transform.position;
            foreach (Layer layer in layers)
            {
                if (layer.target == null) continue;
                layer.origin = layer.target.position;
                layer.velocity = Vector3.zero;
                layer.renderer = layer.target.GetComponent<Renderer>();
                if (layer.renderer != null && layer.closeUpOrder != 0 && layer.renderer.sharedMaterial != null)
                {
                    layer.originalMaterial = layer.renderer.sharedMaterial;
                    layer.closeUpMaterial = new Material(layer.originalMaterial)
                    {
                        renderQueue = 3000 + layer.closeUpOrder
                    };
                }
            }
            roomCamera = FindFirstObjectByType<TowerRoomCamera>();
            initialized = true;
        }
        // Project the viewer onto a fixed window plane. Approaching the window changes
        // the viewing angle too; using a fixed reference avoids jumps after the wake-up shot.
        Vector3 delta = view.transform.position - cameraOrigin;
        if (layeredVista)
        {
            float distance = Mathf.Max(2, windowCentre.z - view.transform.position.z);
            float referenceDistance = windowCentre.z - referenceView.z;
            delta.x = (view.transform.position.x-windowCentre.x) * referenceDistance/distance;
            delta.y = (view.transform.position.y-windowCentre.y) * referenceDistance/distance
                - (referenceView.y-windowCentre.y);
        }
        float closeUp = roomCamera != null ? roomCamera.WindowViewBlend : 0f;
        foreach (Layer layer in layers)
        {
            if (layer.target == null) continue;
            // Transparent layers otherwise sort by camera distance, which can swap during the dolly.
            if (layer.renderer != null && layer.closeUpMaterial != null)
            {
                Material material = closeUp > 0f ? layer.closeUpMaterial : layer.originalMaterial;
                if (layer.renderer.sharedMaterial != material) layer.renderer.sharedMaterial = material;
            }
            float x = Mathf.Clamp(delta.x * layer.cameraFollow.x, layeredVista ? -3.3f : -1.25f, layeredVista ? 3.3f : 1.25f);
            float y = Mathf.Clamp(delta.y * layer.cameraFollow.y, layeredVista ? -.8f : -.3f, layeredVista ? .8f : .3f);
            float drift = Mathf.Sin(Time.timeSinceLevelLoad * .035f) * layer.driftAmplitude;
            layer.target.position = Vector3.SmoothDamp(layer.target.position,
                layer.origin + new Vector3(x + drift, y + layer.closeUpLift * closeUp, 0), ref layer.velocity, .16f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
    private void OnDisable() => RestoreLayers();

    private void RestoreLayers()
    {
        if (initialized && layers != null)
            foreach (Layer layer in layers)
            {
                if (layer.target != null) layer.target.position = layer.origin;
                if (layer.closeUpMaterial != null)
                {
                    if (layer.renderer != null) layer.renderer.sharedMaterial = layer.originalMaterial;
                    Destroy(layer.closeUpMaterial);
                    layer.closeUpMaterial = null;
                }
            }
        initialized = false;
    }
}
