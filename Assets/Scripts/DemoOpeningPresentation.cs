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
    private Rigidbody wakingBody;
    private bool wakingGravity, wakingCollisions, wakePhysicsSaved;
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
        var playerBody = player != null ? player.GetComponent<Rigidbody>() : null;
        bool gravityBefore = playerBody != null && playerBody.useGravity;
        bool collisionsBefore = playerBody != null && playerBody.detectCollisions;
        wakingBody=playerBody;wakingGravity=gravityBefore;wakingCollisions=collisionsBefore;wakePhysicsSaved=playerBody!=null;
        if (playerBody != null) { playerBody.useGravity = false; playerBody.detectCollisions = false; playerBody.linearVelocity = Vector3.zero; }
        var bedObject = GameObject.Find("Canopy bed");
        paintedBed = bedObject != null ? bedObject.GetComponent<PaintedCanopyBed>() : null;
        if (paintedBed != null)
        {
            paintedBed.SetSleeping(true);
            if (player != null) ExplorationPresentationMotion.Place(player, player.GetComponent<Rigidbody>(), paintedBed.BedsidePosition);
        }
        hidden = player != null ? player.GetComponentsInChildren<Renderer>() : new Renderer[0];
        player?.GetComponent<PaperCharacterVisual>()?.SetPresentationHidden(true);
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
            if (paintedBed != null)
                SoundManager.instance?.PlayEffect("IntroFoley/bed-cloth");
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
            if (wakingVisual != null)
            {
                wakingVisual.SetWakeUpProgress(0);
                if (paintedBed != null)
                    wakingVisual.FaceWorldDirection(paintedBed.StandingPosition - paintedBed.BedsidePosition);
            }
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
            if (wakingVisual != null) wakingVisual.SetWakeUpProgress(-1);
            if (paintedBed != null && player != null)
                yield return ExplorationPresentationMotion.Walk(player, paintedBed.StandingPosition, .45f);
        }
        finally { if (wakingVisual != null) wakingVisual.SetWakeUpProgress(-1); wakingVisual = null; RestoreVisuals(); RestoreWakePhysics(); if (fade != null) Destroy(fade.GetComponentInParent<Canvas>().gameObject); IsBusy = false; }
    }
    public IEnumerator LeaveThroughDoor(GameObject player, TowerRoomDoor door)
    {
        if (IsBusy || player == null || door == null) yield break;
        IsBusy = true;
        try
        {
            yield return ExplorationPresentationMotion.Walk(player, door.OpeningStandPosition, .45f);
            SoundManager.instance?.PlayEffect("wardrobe-open");
            for (float t = 0; t < .4f; t += Time.unscaledDeltaTime)
            { door.SetOpenness(Mathf.SmoothStep(0, 1, t / .4f)); yield return null; }
            door.SetOpenness(1);
            SceneLoadManager.instance.LoadExplorationScene("StartScene");
            yield return ExplorationPresentationMotion.Walk(player, door.PassagePosition, .45f);
        }
        finally { IsBusy = false; }
    }

    public IEnumerator ArriveInCourtyard(GameObject player, Transform entrance, bool firstVisit)
    {
        if (player == null || entrance == null) yield break;
        IsBusy = true;
        try
        {
            Vector3 start = entrance.position + new Vector3(0, 1, -.6f);
            ExplorationPresentationMotion.Place(player, player.GetComponent<Rigidbody>(), start);
            while (SceneLoadManager.IsTransitioning) yield return null;
            yield return ExplorationPresentationMotion.Walk(player, start + Vector3.back * 1.15f, .65f);
        }
        finally { IsBusy = false; }
        if (firstVisit)
            DialogueBoxController.instance?.ShowMessage("Ein neuer Morgen im Hof von Aurenfeld. Ich sollte die Leute begrüßen. Die Arkanmeisterin wird am Burgtor erwartet.", IntroChapter.PlayerName);
    }
    public IEnumerator Dress(GameObject player)
    {
        if (IsBusy) yield break;
        IsBusy = true;
        PaperCharacterVisual visual = player != null ? player.GetComponent<PaperCharacterVisual>() : null;
        dressingVisual = visual;
        dressingWardrobe = GameObject.Find("Wardrobe")?.GetComponent<PaintedWardrobe>();
        var roomView = FindFirstObjectByType<TowerRoomCamera>();
        if(roomView != null && dressingWardrobe != null) roomView.SetPresentationFocus(dressingWardrobe.transform.position,.5f);
        try
        {
            var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
            framing = camera != null ? camera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
            if (framing != null) { focus = framing.m_TrackedObjectOffset; distance = framing.m_CameraDistance; }
            if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(true);
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
            if (framing != null) yield return EaseCamera(focus, distance, .6f);
            else yield return new WaitForSecondsRealtime(.6f);
        }
        finally { if(roomView != null)roomView.SetPresentationFocus(Vector3.zero,0); if (visual != null) visual.SetOutfitChangeProgress(-1); if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; dressingVisual = null; RestoreVisuals(); IsBusy = false; }
        if(IntroChapter.Active) { DialogueBoxController.instance.ShowMessage("Der Stern von Aurenfeld. Jetzt bin ich bereit, die Arkanmeisterin zu empfangen. Mal sehen, wer schon im Hof unterwegs ist.",IntroChapter.PlayerName); yield break; }
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
        if (restorePlayer) { hidden = null; GetComponent<PaperCharacterVisual>()?.SetPresentationHidden(false); GameObject.FindWithTag("Player")?.GetComponent<PaperCharacterVisual>()?.SetPresentationHidden(false); }
        if (paintedBed != null) { paintedBed.SetSleeping(false); if (restorePlayer) paintedBed.SetSeated(false); }
        if (restoreCamera && openingCamera != null) { openingCamera.transform.rotation = cameraRotation; openingCamera = null; }
        if (restoreCamera && framing != null) { framing.m_TrackedObjectOffset = focus; framing.m_CameraDistance = distance; framing = null; }
    }
    private void OnDisable()
    {
        StopAllCoroutines(); if (wakingVisual != null) wakingVisual.SetWakeUpProgress(-1); wakingVisual = null;
        RestoreWakePhysics();
        var roomView=FindFirstObjectByType<TowerRoomCamera>();if(roomView!=null)roomView.SetPresentationFocus(Vector3.zero,0);
        RestoreVisuals(); if (dressingVisual != null) dressingVisual.SetOutfitChangeProgress(-1);
        if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; IsBusy = false;
        var canvas = fade != null ? fade.GetComponentInParent<Canvas>() : null;
        if (canvas != null) Destroy(canvas.gameObject);
        fade = null;
    }
    private void RestoreWakePhysics()
    {
        if(wakePhysicsSaved && wakingBody!=null){wakingBody.useGravity=wakingGravity;wakingBody.detectCollisions=wakingCollisions;}
        wakePhysicsSaved=false;wakingBody=null;
    }
}
