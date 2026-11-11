using System.Collections;
using UnityEngine;

public sealed class CourtyardPanfrie : MonoBehaviour, IInteractableAsPlayer
{
    public const string Speaker = "Panfrie";
    public string Label => IsHiding ? "Panfrie rufen" : "Mit Panfrie sprechen";
    internal bool IsHiding => !hasEmerged;
    internal Vector3 PromptPosition => IsHiding ? hidingSpot : transform.position;
    public Vector3 GetTransform() => transform.position;

    const float PoseScale = .66f;
    const float AnswerPause = .46f;

    SphereCollider interaction;
    SpriteRenderer paper;
    Sprite cover, peek, portrait;
    Transform stance;
    Vector3 hidingSpot, corner;
    bool hasEmerged;

    internal static void Install()
    {
        var well = FindFirstObjectByType<CourtyardWell>();
        var cover = Resources.Load<Sprite>("Panfrie/Panfrie Deckung");
        var peek = Resources.Load<Sprite>("Panfrie/Panfrie Hervorlugen");
        if (well == null || cover == null || peek == null || FindFirstObjectByType<CourtyardPanfrie>() != null) return;

        var spot = CourtyardGuard.Grounded(well.transform.position + new Vector3(-2.65f,0,-.35f));
        var hidingSpot = CourtyardGuard.Grounded(well.transform.position + new Vector3(-1.05f,0,1.52f));
        var corner = CourtyardGuard.Grounded(well.transform.position + new Vector3(-2.42f,0,1.34f));
        if (spot.y < -90 || hidingSpot.y < -90 || corner.y < -90) return;
        var root = new GameObject("Courtyard Panfrie", typeof(CourtyardPanfrie), typeof(SphereCollider));
        root.transform.position = spot;
        var encounter = root.GetComponent<CourtyardPanfrie>();
        encounter.cover = cover;
        encounter.peek = peek;
        encounter.portrait = Sprite.Create(peek.texture,new Rect(520,590,600,600),new Vector2(.5f,.5f),100);
        encounter.interaction = root.GetComponent<SphereCollider>();
        encounter.interaction.center = Vector3.up*.45f;
        encounter.interaction.radius = .45f;
        encounter.interaction.isTrigger = true;
        encounter.interaction.enabled = false;

        encounter.hidingSpot = hidingSpot;
        encounter.corner = corner;
        encounter.stance = new GameObject("Panfrie stance").transform;
        encounter.stance.SetParent(root.transform,false);
        encounter.stance.position = hidingSpot;

        var visual = new GameObject("Panfrie paper figure",typeof(SpriteRenderer));
        visual.transform.SetParent(encounter.stance,false);
        visual.transform.localScale = Vector3.one*PoseScale;
        encounter.paper = visual.GetComponent<SpriteRenderer>();
        encounter.paper.sprite = cover;
        encounter.paper.enabled = false;
        encounter.paper.flipX = true;
        PaperGroundShadow.Attach(encounter.stance.gameObject,encounter.paper);
    }

    void OnEnable() => DialogueBoxController.OnDialogueEnded += ReturnToCover;

    void OnDisable()
    {
        DialogueBoxController.OnDialogueEnded -= ReturnToCover;
        ReturnToCover();
    }

    void OnDestroy()
    {
        if (portrait != null) Destroy(portrait);
    }

    void LateUpdate()
    {
        // Remain hidden until the trial and its presentation have finished.
        bool visible = !CourtyardStarterMeetings.RecalledForTrial && CourtyardStarterSummoning.Completed && (interaction.enabled || !DemoOpening.IsPresenting);
        interaction.enabled = paper.enabled = visible;
        if (!visible) return;
        var view = Camera.main;
        if (view != null) paper.transform.rotation = view.transform.rotation;
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (!interaction.enabled || dialogue == null || dialogue.IsOpen || player == null ||
            DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning ||
            (player.transform.position-GetTransform()).sqrMagnitude > player.interacRange*player.interacRange) return;

        Vector3 towardPanfrie = transform.position-player.transform.position;
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(towardPanfrie);
        var view = Camera.main;
        if (view != null)
        {
            float horizontal = Vector3.Dot(towardPanfrie,view.transform.right);
            if (Mathf.Abs(horizontal) > .05f) paper.flipX = horizontal > 0;
        }
        ReturnToCover();
        dialogue.ShowConversation(new[] {new DialogueSection {
            dialogue = new[] {
                hasEmerged ? "Hallo, Panfrie." : "Hallo? Ist da jemand hinter dem Brunnen?",
                "Ähm … ja. Ich bin Panfrie.",
                "Ich bin Elian. Ist dein Schild schwer?",
                "Ein bisschen. Aber dahinter kann ich erst mal in Ruhe gucken."
            },
            presentations = new[] {
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName },
                new DialogueLinePresentation { portrait = portrait, BeforePresentation = PeekBeforeReply },
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly }
            },
            endAfterDialogue = true
        }},Speaker,"",null);
    }

    IEnumerator PeekBeforeReply()
    {
        yield return new WaitForSecondsRealtime(AnswerPause);
        if (!hasEmerged)
        {
            yield return HopTo(corner);
            yield return new WaitForSecondsRealtime(.10f);
            yield return HopTo(transform.position);
            hasEmerged = true;
        }
        paper.sprite = peek;
        yield return new WaitForSecondsRealtime(.18f);
        CourtyardStarterMeetings.Meet(CourtyardStarterMeetings.Starter.Panfrie);
    }

    IEnumerator HopTo(Vector3 target)
    {
        const float duration = .38f;
        const float height = .18f;
        Vector3 start = stance.position;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed/duration;
            stance.position = Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,t))
                + Vector3.up*(Mathf.Sin(t*Mathf.PI)*height);
            yield return null;
        }
        stance.position = target;
    }

    void ReturnToCover()
    {
        if (paper == null) return;
        paper.sprite = cover;
        if (!hasEmerged) stance.position = hidingSpot;
    }
}
