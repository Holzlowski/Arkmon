using System.Collections;
using UnityEngine;

// Short authored steps only. Restore the normal physics settings even if a scene unloads.
public static class ExplorationPresentationMotion
{
    public static IEnumerator Walk(GameObject player, Vector3 destination, float duration)
    {
        if (player == null) yield break;
        var body = player.GetComponent<Rigidbody>();
        var controller = player.GetComponent<PlayerController>();
        bool gravity = body != null && body.useGravity;
        bool collisions = body != null && body.detectCollisions;
        var interpolation = body != null ? body.interpolation : RigidbodyInterpolation.None;
        Vector3 start = player.transform.position;
        if (controller != null) controller.SetScriptedMovement(true);
        if (body != null) { body.linearVelocity = Vector3.zero; body.useGravity = false; body.detectCollisions = false; body.interpolation = RigidbodyInterpolation.None; }
        try
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                if (player == null) yield break;
                float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));
                Place(player, body, Vector3.Lerp(start, destination, u));
                yield return null;
            }
            if (player != null) Place(player, body, destination);
        }
        finally
        {
            if (controller != null) controller.SetScriptedMovement(false);
            if (body != null) { body.useGravity = gravity; body.detectCollisions = collisions; body.interpolation = interpolation; body.linearVelocity = Vector3.zero; }
        }
    }
    public static void Place(GameObject player, Rigidbody body, Vector3 position)
    {
        player.transform.position = position;
        if (body != null) { body.position = position; body.linearVelocity = Vector3.zero; }
        Physics.SyncTransforms();
    }
}
