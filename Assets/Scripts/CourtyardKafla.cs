using System.Collections;
using UnityEngine;

public sealed class CourtyardKafla : MonoBehaviour, IInteractableAsPlayer
{
    public const string Speaker = "Kafla";
    const float PaperScale = .78f;
    public string Label => "Mit Kafla sprechen";
    public Vector3 GetTransform() => transform.position;

    SphereCollider interaction;
    GameObject paper;
    SpriteRenderer body;
    PaperSwordAnimation sword;
    CourtyardTrainingYard trainingYard;
    Transform player;
    float nextPractice;
    float impactAt = float.PositiveInfinity;
    bool greeted;

    internal static void Install()
    {
        var prefab = Resources.Load<GameObject>("Kafla courtyard figure");
        if (prefab == null || FindFirstObjectByType<CourtyardKafla>() != null) return;
        var yard = CourtyardTrainingYard.Install();
        var spot = CourtyardGuard.Grounded(CourtyardTrainingYard.PracticeSpot);
        if (spot.y < -90) return;

        var root = new GameObject("Courtyard Kafla",typeof(CourtyardKafla),typeof(SphereCollider));
        root.transform.position = spot;
        var encounter = root.GetComponent<CourtyardKafla>();
        encounter.trainingYard = yard;
        encounter.interaction = root.GetComponent<SphereCollider>();
        encounter.interaction.center = Vector3.up*.45f;
        encounter.interaction.radius = .45f;
        encounter.interaction.isTrigger = true;
        encounter.interaction.enabled = false;

        encounter.paper = Instantiate(prefab,root.transform,false);
        encounter.paper.transform.localScale = Vector3.one * PaperScale;
        encounter.body = encounter.paper.GetComponent<SpriteRenderer>();
        encounter.sword = encounter.paper.GetComponent<PaperSwordAnimation>();
        encounter.paper.SetActive(false);
        PaperGroundShadow.Attach(root,encounter.body);
    }

    void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted += StopPractice;
        DialogueBoxController.OnDialogueEnded += StopPractice;
    }

    void OnDisable()
    {
        DialogueBoxController.OnDialogueStarted -= StopPractice;
        DialogueBoxController.OnDialogueEnded -= StopPractice;
        StopPractice();
    }

    void LateUpdate()
    {
        bool visible = !CourtyardStarterMeetings.RecalledForTrial && CourtyardStarterSummoning.Completed && (interaction.enabled || !DemoOpening.IsPresenting);
        if (paper.activeSelf != visible) paper.SetActive(visible);
        interaction.enabled = visible;
        var view = Camera.main;
        if (!visible) return;
        if (view != null) paper.transform.rotation = view.transform.rotation;
        if (trainingYard == null) return;

        if (Time.time >= impactAt)
        {
            impactAt = float.PositiveInfinity;
            if (sword.IsSwinging) trainingYard.Hit();
        }

        if (DialogueBoxController.instance?.IsOpen == true || DemoOpening.IsPresenting ||
            SceneLoadManager.IsTransitioning) return;

        var towardDummy = trainingYard.transform.position - transform.position;
        Face(towardDummy);
        if (Time.time < nextPractice) return;
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (player != null && (player.position - transform.position).sqrMagnitude < 64f)
            StartPractice();
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (!interaction.enabled || dialogue == null || dialogue.IsOpen || player == null ||
            DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning ||
            (player.transform.position-GetTransform()).sqrMagnitude > player.interacRange*player.interacRange) return;

        var towardPlayer = player.transform.position-transform.position;
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(-towardPlayer);
        var portrait = PaperUIStyle.Load()?.flacaPortrait;
        if (greeted || CourtyardStarterMeetings.HasMet(CourtyardStarterMeetings.Starter.Kafla))
        {
            Face(towardPlayer);
            dialogue.ShowConversation(new[] {new DialogueSection {
                dialogue = new[] { "Oh, hallo, Elian! Heute klappt der Hieb schon besser." },
                presentations = new[] { new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly } },
                endAfterDialogue = true
            }},Speaker,"",null);
            return;
        }

        dialogue.ShowConversation(new[] {new DialogueSection {
            dialogue = new[] {
                "Hallo! Übst du mit deinem Schwert?",
                "Ha! Oh, verzeiht. Kafla, zu Euren Diensten!",
                "Ich bin Elian. Du kannst ruhig weiterüben.",
                "Danke! Ich will die Puppe so treffen, dass sie bis zu den Stiefeln wackelt!"
            },
            presentations = new[] {
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly,
                    BeforePresentation = () => FinishPractice(towardPlayer) },
                new DialogueLinePresentation { speaker = IntroChapter.PlayerName },
                new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly }
            },
            endAfterDialogue = true
        }},Speaker,"",null);
    }

    IEnumerator FinishPractice(Vector3 towardPlayer)
    {
        if (trainingYard != null)
        {
            Face(trainingYard.transform.position - transform.position);
            yield return new WaitForSeconds(.48f);
            StartPractice();
            yield return new WaitWhile(() => sword.IsSwinging);
        }
        Face(towardPlayer);
        greeted = true;
        CourtyardStarterMeetings.Meet(CourtyardStarterMeetings.Starter.Kafla);
    }

    void Face(Vector3 direction)
    {
        var view = Camera.main;
        if (view == null) return;
        float horizontal = Vector3.Dot(direction,view.transform.right);
        // Mirror the whole rig: the body material does not support SpriteRenderer.flipX.
        if (Mathf.Abs(horizontal) > .05f)
            paper.transform.localScale = new Vector3(horizontal < 0 ? -PaperScale : PaperScale, PaperScale, PaperScale);
    }

    void StartPractice()
    {
        sword.BeginSwing(trainingYard.transform.position, false);
        impactAt = Time.time + PaperSwordAnimation.HitTime;
        nextPractice = Time.time + 2.9f;
    }

    void StopPractice()
    {
        impactAt = float.PositiveInfinity;
        nextPractice = Time.time + 1.25f;
        if (sword != null) sword.CancelSwing();
    }
}
