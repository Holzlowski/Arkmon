using System.Collections;
using UnityEngine;

public sealed class CourtyardRauk : MonoBehaviour, IInteractableAsPlayer
{
    public const string Speaker = "Rauk";
    public string Label => "Mit Rauk sprechen";
    public Vector3 GetTransform() => transform.position;
    internal Vector3 PromptPosition => bird != null
        ? bird.bounds.center + Vector3.up*(bird.bounds.extents.y-1.32f) : transform.position;
    public static bool IsPresenting => active != null;
    static CourtyardRauk active;

    static readonly Vector3 TreePosition = new Vector3(32f,0,2.65f);
    const float TreeHeight = 8.4f;
    static readonly Quaternion ShotAngle = Quaternion.Euler(25,47,0);
    const float ShotFov = 66f;

    SphereCollider interaction;
    Transform stance, perch;
    SpriteRenderer bird;
    Sprite seated, wings, standing, portrait;
    Material paper, oakPaper, branchPaper, shade;
    Mesh shadowMesh;
    CourtyardCamera follow;
    PaperCharacterVisual elian;
    Canvas objective;
    bool objectiveWasEnabled;
    Vector3 landing;
    bool onBranch = true;
    bool perchShadowVisible;

    internal static void Install()
    {
        if (FindFirstObjectByType<CourtyardRauk>() != null) return;
        var oak = Resources.Load<Sprite>("CourtyardStyle/Trees/Oak");
        var shader = Resources.Load<Shader>("AurenfeldScenicPaper");
        var origin = CourtyardGuard.Grounded(TreePosition);
        var ground = CourtyardGuard.Grounded(TreePosition + new Vector3(-1.65f,0,-.95f));
        var landing = CourtyardGuard.Grounded(TreePosition + new Vector3(-2.15f,0,-.60f));
        if (oak == null || shader == null || origin.y < -90 || ground.y < -90 || landing.y < -90) return;

        var seated = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Rauk perched",1.06f,true);
        var wings = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Rauk landing",1.13f,true);
        var standing = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/Rauk",1.38f);
        if (seated == null || wings == null || standing == null)
        {
            Destroy(seated); Destroy(wings); Destroy(standing);
            return;
        }

        var site = new GameObject("Rauk at the gate");
        var root = new GameObject("Courtyard Rauk",typeof(SphereCollider),typeof(CourtyardRauk));
        root.transform.SetParent(site.transform,false);
        root.transform.position = ground;
        var actor = root.GetComponent<CourtyardRauk>();
        actor.paper = new Material(shader);
        actor.seated = seated;
        actor.wings = wings;
        actor.standing = standing;
        var rect = standing.rect;
        actor.portrait = Sprite.Create(standing.texture,
            new Rect(rect.x+rect.width*.13f,rect.y+rect.height*.44f,rect.width*.84f,rect.height*.56f),
            new Vector2(.5f,.5f),standing.pixelsPerUnit);
        actor.landing = landing;
        actor.interaction = root.GetComponent<SphereCollider>();
        actor.interaction.center = Vector3.up*.48f;
        actor.interaction.radius = .50f;
        actor.interaction.isTrigger = true;
        actor.interaction.enabled = false;
        actor.BuildOak(site.transform,oak,origin);

        var figure = new GameObject("Rauk paper figure",typeof(SpriteRenderer));
        actor.stance = figure.transform;
        actor.stance.SetParent(root.transform,false);
        actor.bird = figure.GetComponent<SpriteRenderer>();
        actor.bird.sharedMaterial = actor.paper;
        actor.bird.sortingOrder = 1;
        actor.bird.enabled = false;
        actor.RestOnBranch();
    }

    void BuildOak(Transform site, Sprite oak, Vector3 origin)
    {
        var tree = new GameObject("Rauk's oak",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        tree.transform.SetParent(site,false);
        tree.sprite = oak;
        oakPaper = new Material(paper) { renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest };
        oakPaper.SetFloat("_ZWrite",1);
        oakPaper.SetFloat("_AlphaCutoff",.4f);
        oakPaper.SetFloat("_OakPerchShape",1);
        tree.sharedMaterial = oakPaper;
        float scale = TreeHeight/oak.bounds.size.y;
        tree.transform.SetPositionAndRotation(origin-Vector3.up*(oak.bounds.min.y*scale),Quaternion.Euler(0,12,0));
        // Face the courtyard approach without letting the crown spread back towards the gate.
        tree.transform.localScale = new Vector3(.56f,1,1)*scale;

        // Repaint the visible lip of the branch over the seated figure at the same depth.
        var branchFront = new GameObject("Oak branch foreground",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        branchFront.transform.SetParent(tree.transform,false);
        branchFront.sprite = oak;
        branchFront.sortingOrder = 2;
        branchPaper = new Material(paper);
        branchPaper.SetFloat("_OakPerchShape",1);
        branchPaper.SetFloat("_OakBranchForeground",1);
        branchFront.sharedMaterial = branchPaper;

        perch = new GameObject("Rauk branch mark").transform;
        perch.SetParent(tree.transform,false);
        // The shallow dip on the left branch has enough level bark for both feet.
        perch.localPosition = new Vector3(Mathf.Lerp(oak.bounds.min.x,oak.bounds.max.x,.291f),
            Mathf.Lerp(oak.bounds.min.y,oak.bounds.max.y,.320f),-.012f);

        var trunk = new GameObject("Oak trunk",typeof(CapsuleCollider)).GetComponent<CapsuleCollider>();
        trunk.transform.SetParent(site,false);
        trunk.transform.position = origin;
        trunk.radius = .49f;
        trunk.height = 2.1f;
        trunk.center = Vector3.up*1.05f;

        if (RenderSettings.sun == null) return;
        shade = CourtyardTreeShadows.CreateCasterMaterial(oak.texture);
        if (shade == null) return;
        var light = RenderSettings.sun.transform.forward;
        var across = Vector3.Cross(Vector3.up,new Vector3(light.x,0,light.z).normalized);
        CourtyardTreeShadows.CreateCaster(site,tree,across,shade,out shadowMesh);
    }

    void LateUpdate()
    {
        bool visible = !CourtyardStarterMeetings.RecalledForTrial && CourtyardStarterSummoning.Completed;
        interaction.enabled = visible && active != this;
        bird.enabled = visible;
        bool showPerchShadow = visible && onBranch;
        if (perchShadowVisible != showPerchShadow)
        {
            perchShadowVisible = showPerchShadow;
            var contact = BranchPosition + perch.up*.075f + perch.right*.035f;
            oakPaper.SetVector("_ContactShadow",new Vector4(contact.x,contact.y,contact.z,showPerchShadow ? 1f : 0f));
        }
    }

    // The perch and the talons share the tree's plane, so camera turns preserve contact.
    Vector3 BranchPosition => perch.position-perch.rotation*new Vector3(.035f,.14f,0);

    void RestOnBranch()
    {
        onBranch = true;
        bird.sprite = seated;
        stance.SetPositionAndRotation(BranchPosition,perch.rotation);
        stance.localScale = Vector3.one;
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (!interaction.enabled || dialogue == null || dialogue.IsOpen || player == null ||
            DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning ||
            (player.transform.position-GetTransform()).sqrMagnitude > player.interacRange*player.interacRange) return;
        follow = FindFirstObjectByType<CourtyardCamera>();
        if (follow == null || Camera.main == null) return;
        active = this;
        StartCoroutine(Meet(dialogue,player.transform));
    }

    IEnumerator Meet(DialogueBoxController dialogue, Transform player)
    {
        try
        {
            elian = player.GetComponent<PaperCharacterVisual>();
            elian?.SetTrialLookTarget(stance);
            objective = GameObject.Find("Demo objective")?.GetComponent<Canvas>();
            objectiveWasEnabled = objective != null && objective.enabled;
            if (objective != null) objective.enabled = false;

            var center = Vector3.Lerp(landing,player.position,.55f);
            center.y = landing.y+1.47f;
            // Cut to the lawn side of the parked carriage before Rauk leaves the branch.
            Vector3 shot = center-ShotAngle*Vector3.forward*4.35f;
            follow.SetScenicView(shot,ShotAngle,ShotFov);

            yield return new WaitForSeconds(.40f);
            yield return Fly(false);
            Face(player.position-stance.position,ShotAngle);
            yield return new WaitForSeconds(.18f);

            var conversationShot = shot-Vector3.up*.94f;
            follow.SetScenicView(conversationShot,ShotAngle,ShotFov);
            bool met = CourtyardStarterMeetings.HasMet(CourtyardStarterMeetings.Starter.Rauk);
            var lines = met ? new[] { "Schon wieder Ihr? Der Ast gehört trotzdem mir." } : new[]
            {
                "Ihr seid laut für jemanden, der mich überraschen wollte.",
                "Ich wollte nur Hallo sagen. Ich bin Elian.",
                "Rauk. Von da oben sieht man, wer wohin will. Die meisten wissen es selbst noch nicht.",
                "Wenn Ihr mich sucht, schaut nach oben."
            };
            var rauk = new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly };
            var faces = met ? new[] { rauk } : new[]
            {
                rauk, new DialogueLinePresentation { speaker = IntroChapter.PlayerName }, rauk, rauk
            };
            dialogue.ShowConversation(new[] { new DialogueSection {
                dialogue = lines, presentations = faces, endAfterDialogue = true
            } },Speaker,"",null);
            CourtyardStarterMeetings.Meet(CourtyardStarterMeetings.Starter.Rauk);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            follow.SetScenicView(shot,ShotAngle,ShotFov);
            yield return Fly(true);
        }
        finally { EndMeeting(); }
    }

    IEnumerator Fly(bool returning)
    {
        var branch = BranchPosition;
        var start = returning ? landing : branch;
        var end = returning ? branch : landing;
        var outward = ShotAngle*Vector3.right;
        if (!returning) CourtyardAmbientDetails.ShakeOakLeaves(perch.position,perch.rotation,false);
        onBranch = false;
        bird.sprite = wings;
        float duration = returning ? .95f : 1.04f;
        for (float time=0; time<duration; time+=Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0,1,time/duration);
            float arc = Mathf.Sin(Mathf.PI*t);
            stance.position = Vector3.Lerp(start,end,t)+outward*arc*.52f
                + Vector3.up*arc*(returning ? .28f : .35f);
            stance.rotation = ShotAngle*Quaternion.Euler(0,0,arc*(returning ? 9f : -7f));
            stance.localScale = new Vector3(returning ? -1 : 1,1,1);
            yield return null;
        }
        stance.SetPositionAndRotation(end,ShotAngle);
        if (returning)
        {
            RestOnBranch();
            CourtyardAmbientDetails.ShakeOakLeaves(perch.position,perch.rotation,true);
            const float settleDuration = .36f;
            for (float elapsed = Time.deltaTime; elapsed < settleDuration; elapsed += Time.deltaTime)
            {
                float progress = elapsed/settleDuration;
                float dip = .13f*Mathf.Sin(progress*Mathf.PI*2)*(1-progress);
                SetBranchDip(dip);
                yield return null;
            }
            SetBranchDip(0);
        }
        else bird.sprite = standing;
    }

    void SetBranchDip(float dip)
    {
        oakPaper.SetFloat("_OakBranchDip",dip/TreeHeight);
        branchPaper.SetFloat("_OakBranchDip",dip/TreeHeight);
        stance.position = BranchPosition-Vector3.up*dip;
        if (perchShadowVisible)
        {
            var contact = stance.position + perch.up*.075f + perch.right*.035f;
            oakPaper.SetVector("_ContactShadow",new Vector4(contact.x,contact.y,contact.z,1));
        }
    }

    void Face(Vector3 direction, Quaternion view)
    {
        stance.localScale = new Vector3(Vector3.Dot(direction,view*Vector3.right) < 0 ? -1 : 1,1,1);
    }

    void EndMeeting()
    {
        if (active != this) return;
        active = null;
        elian?.SetTrialLookTarget(null);
        if (objective != null) objective.enabled = objectiveWasEnabled;
        if (follow != null) follow.ClearScenicView();
        if (bird != null)
        {
            if (!onBranch) RestOnBranch();
            SetBranchDip(0);
        }
    }

    void OnDisable() { StopAllCoroutines(); EndMeeting(); }

    void OnDestroy()
    {
        Destroy(seated); Destroy(wings); Destroy(standing); Destroy(portrait);
        Destroy(paper); Destroy(oakPaper); Destroy(branchPaper); Destroy(shade); Destroy(shadowMesh);
    }
}
