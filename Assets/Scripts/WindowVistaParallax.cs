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
        [NonSerialized] public Vector3 origin;
    }
    [SerializeField] private Layer[] layers;
    private Camera view;
    private Vector3 cameraOrigin;
    private bool initialized;

    public void Configure(Transform background, Transform fields, Transform clouds)
    {
        layers = new[] {
            new Layer { target = background, cameraFollow = new Vector2(.1f, .015f) },
            new Layer { target = fields, cameraFollow = new Vector2(.04f, .035f) },
            new Layer { target = clouds, cameraFollow = new Vector2(.12f, .01f), driftAmplitude = .35f }
        };
        initialized = false;
    }
    private void LateUpdate()
    {
        if (layers == null || Camera.main == null) return;
        if (!initialized)
        {
            view = Camera.main; cameraOrigin = view.transform.position;
            foreach (Layer layer in layers) if (layer.target != null) layer.origin = layer.target.position;
            initialized = true;
        }
        Vector3 delta = view.transform.position - cameraOrigin;
        foreach (Layer layer in layers)
        {
            if (layer.target == null) continue;
            float x = Mathf.Clamp(delta.x * layer.cameraFollow.x, -.65f, .65f);
            float y = Mathf.Clamp(delta.y * layer.cameraFollow.y, -.18f, .18f);
            float drift = Mathf.Sin(Time.timeSinceLevelLoad * .035f) * layer.driftAmplitude;
            layer.target.position = layer.origin + new Vector3(x + drift, y, 0);
        }
    }
    private void OnDisable()
    {
        if (initialized && layers != null)
            foreach (Layer layer in layers) if (layer.target != null) layer.target.position = layer.origin;
        initialized = false;
    }
}
