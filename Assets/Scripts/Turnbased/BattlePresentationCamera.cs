using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

// Own the real camera only during summoning; restore normal controls afterward.
public class BattlePresentationCamera : MonoBehaviour
{
    private Camera view;
    private CinemachineBrain brain;
    private CameraController controls;
    private bool active, brainEnabled;
    private Quaternion overviewRotation;
    private Vector3 overviewOffset, horizontalForward, focus;
    private Renderer[] scenery;
    private readonly Dictionary<SpriteRenderer, Color> fadedSprites = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> fadedMeshes = new Dictionary<Renderer, MaterialPropertyBlock>();
    public bool IsPresenting => active;

    private void LateUpdate()
    {
        if (active || Camera.main == null) return;
        view = Camera.main;
        if (scenery == null) scenery = CacheScenery();
        RestoreOccluders();
        if (UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning)
        {
            foreach (Unit unit in UnitManager.Instance.GetUnitList())
            {
                if (unit == null || unit.IsDead) continue;
                PaperCharacterVisual paper = unit.GetComponent<PaperCharacterVisual>();
                FadeOccluders(paper != null ? paper.GetRecallOrigin() : unit.transform.position + Vector3.up * .5f);
            }
        }
        else if (BattleDeployment.Instance != null && !BattleDeployment.Instance.IsReady
            && BattleDeployment.Instance.Trainer != null)
            FadeOccluders(BattleDeployment.Instance.Trainer.position + Vector3.up * .9f);
    }

    public IEnumerator FocusTrainer(Transform trainer)
    {
        if (trainer == null || Camera.main == null) yield break;
        if (!active)
        {
            view = Camera.main; brain = view.GetComponent<CinemachineBrain>();
            controls = FindFirstObjectByType<CameraController>();
            overviewRotation = view.transform.rotation;
            overviewOffset = view.transform.position - (controls != null ? controls.transform.position : trainer.position);
            horizontalForward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            if (horizontalForward.sqrMagnitude < .01f) horizontalForward = Vector3.forward;
            brainEnabled = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
            if (controls != null) controls.IsPresentationLocked = true;
            scenery = CacheScenery();
            active = true;
        }
        RestoreOccluders();
        focus = trainer.position + Vector3.up * .9f;
        yield return MoveTo(focus - horizontalForward * 4.8f + Vector3.up * 2.6f,
            Quaternion.LookRotation(horizontalForward * 4.8f - Vector3.up * 2.6f), .3f);
        FadeOccluders(focus);
        yield return new WaitForSeconds(.12f);
    }

    public void FollowPulse(Vector3 position)
    {
        if (!active || view == null) return;
        focus = Vector3.Lerp(focus, position, 1 - Mathf.Exp(-Time.deltaTime * 12));
        view.transform.position = Vector3.Lerp(view.transform.position,
            focus - horizontalForward * 4.8f + Vector3.up * 2.6f, 1 - Mathf.Exp(-Time.deltaTime * 10));
        view.transform.rotation = Quaternion.LookRotation(focus - view.transform.position);
        FadeOccluders(focus);
    }

    public IEnumerator ReturnToOverview(Vector3 center)
    {
        if (!active || view == null) yield break;
        RestoreOccluders();
        if (controls != null)
        {
            CameraState state = controls.PrepareBattleOverview(center);
            overviewOffset = state.FinalPosition - center; overviewRotation = state.FinalOrientation;
            view.fieldOfView = state.Lens.FieldOfView;
        }
        yield return MoveTo(center + overviewOffset, overviewRotation, .4f);
        Release();
    }

    private IEnumerator MoveTo(Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 from = view.transform.position; Quaternion facing = view.transform.rotation;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            view.transform.SetPositionAndRotation(Vector3.Lerp(from, position, t), Quaternion.Slerp(facing, rotation, t));
            yield return null;
        }
        view.transform.SetPositionAndRotation(position, rotation);
    }

    private void FadeOccluders(Vector3 target)
    {
        Ray ray = new Ray(view.transform.position, target - view.transform.position);
        float distance = Vector3.Distance(view.transform.position, target);
        foreach (Renderer renderer in scenery)
        {
            if (renderer == null) continue;
            Material material = renderer.sharedMaterial;
            if (!renderer.enabled || material == null || !material.name.Contains("-Paper")
                || renderer.GetComponentInParent<Unit>() != null || renderer.GetComponentInParent<NPC>() != null
                || renderer.GetComponentInParent<PlayerController>() != null) continue;
            if (!renderer.bounds.IntersectRay(ray, out float hit) || hit >= distance - .35f) continue;
            if (renderer is SpriteRenderer sprite)
            {
                if (!fadedSprites.ContainsKey(sprite)) fadedSprites.Add(sprite, sprite.color);
                Color tint = fadedSprites[sprite]; tint.a *= .18f; sprite.color = tint;
            }
            else if (!fadedMeshes.ContainsKey(renderer))
            {
                var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original);
                fadedMeshes.Add(renderer, original);
                var faded = new MaterialPropertyBlock(); renderer.GetPropertyBlock(faded);
                Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                tint.a *= .18f; faded.SetColor("_BaseColor", tint); renderer.SetPropertyBlock(faded);
            }
        }
    }

    private static Renderer[] CacheScenery()
    {
        var result = new List<Renderer>();
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Material material = renderer.sharedMaterial;
            if (material != null && material.name.Contains("-Paper")
                && renderer.GetComponentInParent<Unit>() == null && renderer.GetComponentInParent<NPC>() == null
                && renderer.GetComponentInParent<PlayerController>() == null) result.Add(renderer);
        }
        return result.ToArray();
    }

    private void RestoreOccluders()
    {
        foreach (var entry in fadedSprites) if (entry.Key != null) entry.Key.color = entry.Value;
        foreach (var entry in fadedMeshes) if (entry.Key != null) entry.Key.SetPropertyBlock(entry.Value);
        fadedSprites.Clear(); fadedMeshes.Clear();
    }
    public void Release()
    {
        RestoreOccluders();
        if (brain != null) brain.enabled = brainEnabled;
        if (controls != null) controls.IsPresentationLocked = false;
        active = false;
    }
    private void OnDisable() => Release();
}
