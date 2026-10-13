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
    private SpriteRenderer bed;
    private Sprite emptyBed;
    private Vector3 bedScale;
    private CinemachineFramingTransposer framing;
    private Vector3 focus;
    private float distance;
    private PaperCharacterVisual dressingVisual;
    private PaintedWardrobe dressingWardrobe;
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
        bed = bedObject != null ? bedObject.GetComponentInChildren<SpriteRenderer>() : null;
        Sprite occupied = Resources.Load<Sprite>("Prince-Sleeping-Bed");
        if (bed != null && occupied != null)
        {
            emptyBed = bed.sprite; bedScale = bed.transform.localScale;
            float width = emptyBed.bounds.size.x * bedScale.x;
            bed.sprite = occupied; bed.transform.localScale = Vector3.one * (width / occupied.bounds.size.x);
        }
        hidden = player != null ? player.GetComponentsInChildren<Renderer>() : new Renderer[0];
        enabledBefore = new bool[hidden.Length];
        for (int i = 0; i < hidden.Length; i++) { enabledBefore[i] = hidden[i].enabled; hidden[i].enabled = false; }
        var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
        framing = camera != null ? camera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
        if (framing != null && player != null && bedObject != null)
        {
            focus = framing.m_TrackedObjectOffset; distance = framing.m_CameraDistance;
            framing.m_TrackedObjectOffset = new Vector3(bedObject.transform.position.x - player.transform.position.x, .7f, 2.5f);
            framing.m_CameraDistance = distance * .92f;
        }
        try
        {
            yield return Fade(1, 0, .65f);
            for (float elapsed = 0; elapsed < 1.2f; elapsed += Time.unscaledDeltaTime)
            {
                if (bed != null) bed.transform.localScale *= 1 + Mathf.Sin(elapsed * 5) * .00015f;
                yield return null;
            }
            yield return Fade(0, 1, .18f);
            RestoreVisuals();
            yield return Fade(1, 0, .3f);
        }
        finally { RestoreVisuals(); if (fade != null) Destroy(fade.GetComponentInParent<Canvas>().gameObject); IsBusy = false; }
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
            if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(true);
            SoundManager.instance?.PlayEffect("wardrobe-open");
            // Let the doors finish opening, then give the hanging mantle a readable beat.
            yield return new WaitForSecondsRealtime(.95f);
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
            yield return new WaitForSecondsRealtime(.6f);
        }
        finally { if (visual != null) visual.SetOutfitChangeProgress(-1); if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; dressingVisual = null; IsBusy = false; }
        DialogueBoxController.instance.ShowMessage("Den grünen Mantel haben mir meine Eltern geschenkt. Ich trage ihn immer, wenn die Arkanmeisterin kommt. Jetzt bin ich bereit!", "Der Junge");
    }
    private void RestoreVisuals()
    {
        if (hidden != null) for (int i = 0; i < hidden.Length; i++) if (hidden[i] != null) hidden[i].enabled = enabledBefore[i];
        hidden = null;
        if (bed != null && emptyBed != null) { bed.sprite = emptyBed; bed.transform.localScale = bedScale; emptyBed = null; }
        if (framing != null) { framing.m_TrackedObjectOffset = focus; framing.m_CameraDistance = distance; framing = null; }
    }
    private void OnDisable() { StopAllCoroutines(); RestoreVisuals(); if (dressingVisual != null) dressingVisual.SetOutfitChangeProgress(-1); if (dressingWardrobe != null) dressingWardrobe.SetInteractionOpen(false); dressingWardrobe = null; IsBusy = false; if (fade != null) Destroy(fade.GetComponentInParent<Canvas>().gameObject); }
}
