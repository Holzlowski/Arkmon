using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// Small, interruptible paper-sprite sequences; gameplay progress stays in DemoOpening.
public class DemoOpeningPresentation : MonoBehaviour
{
    public bool IsBusy { get; private set; }
    private Image fade;
    private Renderer[] hidden;
    private bool[] enabledBefore;
    private CinemachineFramingTransposer framing;
    private Vector3 focus;
    private float distance;
    private PaperCharacterVisual dressingVisual;
    private PaintedWardrobe dressingWardrobe;
    private PaperCharacterVisual wakingVisual;
    private PaintedCanopyBed paintedBed;
    private CinemachineVirtualCamera openingCamera;
    private Quaternion cameraRotation;
    private void CreateFade()
    {
        var canvas = new GameObject("Wake-up fade", typeof(Canvas)); canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 31000;
        fade = PaperUIFactory.Image("Morning black fade", canvas.transform, null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
        fade.color = Color.black; fade.raycastTarget = true;
    }
    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        { fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, elapsed / duration)); yield return null; }
        fade.color = new Color(0, 0, 0, to);
    }
    public IEnumerator WakeUp(GameObject player)
    {
        IsBusy = true; CreateFade();
        var bedObject = GameObject.Find("Canopy bed");
        paintedBed = bedObject != null ? bedObject.GetComponent<PaintedCanopyBed>() : null;
        if (paintedBed != null)
        {
            paintedBed.SetSleeping(true);
            if (player != null) player.transform.position = paintedBed.BedsidePosition;
        }
        hidden = player != null ? player.GetComponentsInChildren<Renderer>() : new Renderer[0];
        enabledBefore = new bool[hidden.Length];
        for (int i = 0; i < hidden.Length; i++) { enabledBefore[i] = hidden[i].enabled; hidden[i].enabled = false; }
        var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
        framing = camera != null ? camera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
        if (framing != null && player != null && bedObject != null)
        {
            focus = framing.m_TrackedObjectOffset; distance = framing.m_CameraDistance;
            openingCamera = camera; cameraRotation = camera.transform.rotation; camera.transform.rotation = Quaternion.Euler(25, -12, 0);
            framing.m_TrackedObjectOffset = new Vector3(bedObject.transform.position.x - player.transform.position.x, 1.05f, bedObject.transform.position.z - player.transform.position.z);
            framing.m_CameraDistance = Mathf.Min(distance * .92f, 6.4f);
        }
        try
        {
            yield return Fade(1, 0, .65f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Fade(0, 1, .18f);
            RestoreVisuals(false, false);
            if (paintedBed != null)
            {
                paintedBed.SetSeated(true);
                yield return Fade(1, 0, .12f);
                yield return new WaitForSecondsRealtime(.65f);
                yield return Fade(0, 1, .08f);
            }
            RestoreVisuals(false);
            wakingVisual = player != null ? player.GetComponent<PaperCharacterVisual>() : null;
            if (wakingVisual != null) wakingVisual.SetWakeUpProgress(0);
            SoundManager.instance?.PlayEffect("mantle-rustle");
            Vector3 bedsideFocus = framing != null ? framing.m_TrackedObjectOffset : Vector3.zero;
            float bedsideDistance = framing != null ? framing.m_CameraDistance : 0;
            Quaternion bedsideRotation = openingCamera != null ? openingCamera.transform.rotation : Quaternion.identity;
            // Reveal the bedside pose while easing back to the exploration camera.
            for (float elapsed = 0; elapsed < .9f; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / .9f);
                if (wakingVisual != null) wakingVisual.SetWakeUpProgress(progress);
                fade.color = new Color(0, 0, 0, 1 - Mathf.Clamp01(elapsed / .25f));
                if (framing != null)
                {
                    float ease = Mathf.SmoothStep(0, 1, progress);
                    framing.m_TrackedObjectOffset = Vector3.Lerp(bedsideFocus, focus, ease);
                    framing.m_CameraDistance = Mathf.Lerp(bedsideDistance, distance, ease);
                    if (openingCamera != null) openingCamera.transform.rotation = Quaternion.Slerp(bedsideRotation, cameraRotation, ease);
                }
                yield return null;
            }
        }
        finally { if (wakingVisual != null) wakingVisual.SetWakeUpProgress(-1); wakingVisual = null; RestoreVisuals(); if (fade != null) Destroy(fade.GetComponentInParent<Canvas>().gameObject); IsBusy = false; }
    }
    public IEnumerator Dress(GameObject player)
    {
        if (IsBusy) yield break;
        IsBusy = true;
        PaperCharacterVisual visual = player != null ? player.GetComponent<PaperCharacterVisual>() : null;
        dressingVisual = visual;
        dressingWardrobe = GameObject.Find("Wardrobe")?.GetComponent<PaintedWardrobe>();
        try
        {
            var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
            framing = camera != null ? camera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
            if (framing != null) { focus = framing.m_TrackedObjectOffset; distance = framing.m_CameraDistance; }
            if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(true);
            SoundManager.instance?.PlayEffect("wardrobe-open");
            // Let the doors finish opening, then give the hanging mantle a readable beat.
            if (framing != null && player != null && dressingWardrobe != null)
            {
                Vector3 target = dressingWardrobe.transform.position - player.transform.position; target.y = 1;
                yield return EaseCamera(target, distance * .9f, .45f);
                yield return new WaitForSecondsRealtime(.5f);
            }
            else yield return new WaitForSecondsRealtime(.95f);
            SoundManager.instance?.PlayEffect("mantle-rustle");
            if (visual != null) visual.SetOutfitChangeProgress(0);
            for (float elapsed = 0; elapsed < .55f; elapsed += Time.unscaledDeltaTime)
            {
                float progress = elapsed / .55f;
                if (visual != null) visual.SetOutfitChangeProgress(progress);
                if (progress >= .5f) DemoOpening.FinishTakingMantle();
                yield return null;
            }
            DemoOpening.FinishTakingMantle();
            yield return new WaitForSecondsRealtime(.25f);
            if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false);
            SoundManager.instance?.PlayEffect("wardrobe-close");
            if (framing != null) yield return EaseCamera(focus, distance, .6f);
            else yield return new WaitForSecondsRealtime(.6f);
        }
        finally { if (visual != null) visual.SetOutfitChangeProgress(-1); if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; dressingVisual = null; RestoreVisuals(); IsBusy = false; }
        DialogueBoxController.instance.ShowMessage("Den grünen Mantel haben mir meine Eltern geschenkt. Ich trage ihn immer, wenn die Arkanmeisterin kommt. "
            + (DemoOpening.HasLamp ? "Jetzt bin ich bereit für John!" : "Jetzt noch meine Lampe vom Tisch holen."), "Der Junge");
    }
    private IEnumerator EaseCamera(Vector3 target, float targetDistance, float duration)
    {
        if (framing == null) yield break;
        Vector3 start = framing.m_TrackedObjectOffset; float startDistance = framing.m_CameraDistance;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (framing == null) yield break;
            float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
            framing.m_TrackedObjectOffset = Vector3.Lerp(start, target, progress);
            framing.m_CameraDistance = Mathf.Lerp(startDistance, targetDistance, progress);
            yield return null;
        }
        if (framing != null) { framing.m_TrackedObjectOffset = target; framing.m_CameraDistance = targetDistance; }
    }
    private void RestoreVisuals(bool restoreCamera = true, bool restorePlayer = true)
    {
        if (restorePlayer && hidden != null) for (int i = 0; i < hidden.Length; i++) if (hidden[i] != null) hidden[i].enabled = enabledBefore[i];
        if (restorePlayer) hidden = null;
        if (paintedBed != null) { paintedBed.SetSleeping(false); if (restorePlayer) paintedBed.SetSeated(false); }
        if (restoreCamera && openingCamera != null) { openingCamera.transform.rotation = cameraRotation; openingCamera = null; }
        if (restoreCamera && framing != null) { framing.m_TrackedObjectOffset = focus; framing.m_CameraDistance = distance; framing = null; }
    }
    private void OnDisable()
    {
        StopAllCoroutines(); if (wakingVisual != null) wakingVisual.SetWakeUpProgress(-1); wakingVisual = null;
        RestoreVisuals(); if (dressingVisual != null) dressingVisual.SetOutfitChangeProgress(-1);
        if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; IsBusy = false;
        var canvas = fade != null ? fade.GetComponentInParent<Canvas>() : null;
        if (canvas != null) Destroy(canvas.gameObject);
        fade = null;
    }
}
