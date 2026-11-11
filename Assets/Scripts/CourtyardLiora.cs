using UnityEngine;

public sealed class CourtyardLiora : MonoBehaviour, IInteractableAsPlayer
{
    public const string Speaker = "Liora";
    public string Label => "Mit Liora sprechen";
    public Vector3 GetTransform() => transform.position;

    SphereCollider interaction;
    SpriteRenderer paper;
    Sprite body, portrait;
    Material material;
    bool facesLeft;

    internal static void Install()
    {
        if (FindFirstObjectByType<CourtyardLiora>() != null) return;
        var spot = CourtyardGuard.Grounded(new Vector3(34.0f,0,-9.8f));
        var shader = Resources.Load<Shader>("AurenfeldScenicPaper");
        if (spot.y < -90 || shader == null) return;
        var pose = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Liora",1.35f);
        if (pose == null) return;

        var root = new GameObject("Courtyard Liora",typeof(CourtyardLiora),typeof(SphereCollider));
        root.transform.position = spot;
        var actor = root.GetComponent<CourtyardLiora>();
        actor.body = pose;
        var rect = pose.rect;
        actor.portrait = Sprite.Create(pose.texture,
            new Rect(rect.x+rect.width*.08f,rect.y+rect.height*.37f,rect.width*.90f,rect.height*.62f),
            new Vector2(.5f,.5f),pose.pixelsPerUnit);
        actor.interaction = root.GetComponent<SphereCollider>();
        actor.interaction.center = Vector3.up*.48f;
        actor.interaction.radius = .46f;
        actor.interaction.isTrigger = true;
        actor.interaction.enabled = false;
        var figure = new GameObject("Liora paper figure",typeof(SpriteRenderer));
        figure.transform.SetParent(root.transform,false);
        actor.paper = figure.GetComponent<SpriteRenderer>();
        actor.paper.sprite = pose;
        actor.material = new Material(shader);
        actor.paper.sharedMaterial = actor.material;
        actor.paper.enabled = false;
        PaperGroundShadow.Attach(root,actor.paper);
    }

    void LateUpdate()
    {
        bool visible = !CourtyardStarterMeetings.RecalledForTrial && CourtyardStarterSummoning.Completed;
        interaction.enabled = paper.enabled = visible;
        if (!visible || Camera.main == null) return;
        paper.transform.rotation = Camera.main.transform.rotation;
        paper.transform.localScale = new Vector3(facesLeft ? -1 : 1,1,1);
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (!interaction.enabled || dialogue == null || dialogue.IsOpen || player == null ||
            DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning ||
            (player.transform.position-GetTransform()).sqrMagnitude > player.interacRange*player.interacRange) return;

        var direction = player.transform.position-transform.position;
        if (Camera.main != null) facesLeft = Vector3.Dot(direction,Camera.main.transform.right)<0;
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(-direction);
        bool met = CourtyardStarterMeetings.HasMet(CourtyardStarterMeetings.Starter.Liora);
        var liora = new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly };
        var lines = met ? new[] { "Schon zurück? Die Bohnen sind in der Zwischenzeit kein Stück gewachsen. Ich hab nachgesehen." } : new[]
        {
            "Marta redet mit den Bohnen. Und wenn sie glaubt, dass niemand hinsieht, singt sie ihnen sogar vor.",
            "Meinst du, sie wachsen dann besser?",
            "Das hoffe ich doch. Sonst müsste ich ihr sagen, dass ich den Refrain schon auswendig kann.",
            "Ich bin übrigens Liora. Ihr dürft mir gern erzählen, was die Menschen hier sonst noch so tun."
        };
        var faces = met ? new[] { liora } : new[]
        {
            liora, new DialogueLinePresentation { speaker = IntroChapter.PlayerName }, liora, liora
        };
        dialogue.ShowConversation(new[] { new DialogueSection {
            dialogue = lines, presentations = faces, endAfterDialogue = true
        } },Speaker,"",null);
        CourtyardStarterMeetings.Meet(CourtyardStarterMeetings.Starter.Liora);
    }

    void OnDestroy() { Destroy(body); Destroy(portrait); Destroy(material); }
}
