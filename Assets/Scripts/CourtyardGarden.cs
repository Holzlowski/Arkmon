using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class CourtyardGarden : MonoBehaviour, IInteractableAsPlayer
{
    enum GardenAction { None, PickUp, PutDown, Pour }
    static bool hasCan, requested, thanksPending;
    static int watered;
    static CourtyardGarden canStation;
    bool departureReminded;
    public static bool InsideToolArea(Vector3 p) => p.x >= 19 && p.x <= 35 && p.z >= -16.4f && p.z <= -3.4f;
    // Stop only outward movement at the garden boundary; returning remains possible.
    public static Vector3 ConstrainToolMovement(Vector3 position, Vector3 movement, float dt)
    {
        if (!hasCan || canStation == null || dt <= 0) return movement;
        var next = position + movement * dt;
        if ((next.x < 19 && movement.x < 0) || (next.x > 35 && movement.x > 0)) movement.x = 0;
        if ((next.z < -16.4f && movement.z < 0) || (next.z > -3.4f && movement.z > 0)) movement.z = 0;
        return movement;
    }
    void CheckBorrowedTool()
    {
        if (!hasCan || player == null || IsBusy) { departureReminded = false; return; }
        var p = player.transform.position;
        bool nearExit = p.x < 19.8f || p.x > 34.2f || p.z < -15.6f || p.z > -4.2f;
        if (!nearExit)
        {
            if (p.x > 20.6f && p.x < 33.4f && p.z > -14.8f && p.z < -5.0f) departureReminded = false;
            return;
        }
        var dialogue = DialogueBoxController.instance;
        if (departureReminded || dialogue == null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning) return;
        departureReminded = true;
        dialogue.ShowMessage("Prinz Elian, bitte lasst die Gießkanne hier. Die brauche ich noch.", "Marta");
    }
    static CourtyardGarden activeAction;
    const int AllBeds = 7;
    int bedIndex = -1;
    Renderer soil;
    GameObject canModel, dropsRoot;
    SpriteRenderer canSprite;
    AudioSource pourAudio;
    Coroutine routine;
    GardenAction action;
    CourtyardGarden station;
    GameObject player;
    PaperCharacterVisual visual;
    Rigidbody movingBody;
    PlayerController movingController;
    bool restoringMovement, savedGravity, savedCollisions;
    RigidbodyInterpolation savedInterpolation;

    public static bool Completed => watered == AllBeds;
    public static bool HasCan => hasCan;
    public static bool IsBusy => activeAction != null;
    public static bool IsWatering => activeAction != null && activeAction.action == GardenAction.Pour;
    public static bool ThanksReceived { get; private set; }
    public static int WateredCount => (watered & 1) + ((watered >> 1) & 1) + ((watered >> 2) & 1);
    public static event Action OnCompleted;
    public static string ObjectiveText
    {
        get
        {
            if (IsBusy) return IsWatering ? "Garten · Gießen … [Esc] Abbrechen"
                : activeAction.action == GardenAction.PickUp ? "Garten · Kanne aufnehmen … [Esc] Abbrechen"
                : "Garten · Kanne abstellen … [Esc] Abbrechen";
            if (Completed) return !ThanksReceived ? "Garten · Alle Beete gegossen! Sprich mit Marta"
                : hasCan ? "Garten · Stelle die Gießkanne zurück" : null;
            if (!requested) return null;
            return hasCan ? $"Marta helfen · Gieße die Gemüsebeete ({WateredCount}/3)"
                : "Marta helfen · Nimm die Gießkanne am seitlichen Gartenweg";
        }
    }
    public static void ResetProgress()
    {
        if (activeAction != null) activeAction.CancelWatering();
        hasCan = requested = thanksPending = ThanksReceived = false; watered = 0;
    }
    public static void OfferHelp() => requested = true;
    public static void BeginThanks() => thanksPending = true;
    public string Label => bedIndex < 0 ? hasCan ? "Gießkanne zurückstellen" : "Gießkanne nehmen"
        : IsWatered ? "Beet ansehen" : hasCan ? "Beet gießen" : "Trockenes Beet ansehen";
    internal static bool IsBedWatered(int index) => index >= 0 && index < 3 && (watered & (1 << index)) != 0;
    bool IsWatered => IsBedWatered(bedIndex);
    public Vector3 GetTransform() => transform.position;

    internal static void Install()
    {
        var beds = Array.FindAll(FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None), r => r.name == "Garden soil bed");
        if (beds.Length != 3) return;
        Array.Sort(beds, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        for (int i = 0; i < beds.Length; i++)
        {
            var bed = Spawn("Garden watering point", beds[i].transform.position + new Vector3(-beds[i].bounds.extents.x - 1.0f, 0, 0));
            bed.bedIndex = i; bed.soil = beds[i]; bed.RefreshAppearance();
        }
        var station = Spawn("Courtyard watering can", new Vector3(27.6f, beds[0].transform.position.y, -12.1f));
        station.canModel = new GameObject("Watering can model", typeof(SpriteRenderer));
        station.canModel.transform.SetParent(station.transform, false);
        station.canSprite = station.canModel.GetComponent<SpriteRenderer>();
        station.canSprite.sprite = Resources.Load<Sprite>("Garden/Can");
        station.FindPlayer();
        var original = station.player != null ? station.player.GetComponentInChildren<SpriteRenderer>() : null;
        if (original != null) station.canSprite.sharedMaterial = original.sharedMaterial;
        canStation = station;
        station.PlaceCanOnGround();
    }
    static CourtyardGarden Spawn(string name, Vector3 position)
    {
        var obj = new GameObject(name, typeof(SphereCollider), typeof(CourtyardGarden)); obj.transform.position = position;
        var trigger = obj.GetComponent<SphereCollider>(); trigger.radius = .45f; trigger.isTrigger = true;
        return obj.GetComponent<CourtyardGarden>();
    }
    void FindPlayer()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (visual == null && player != null) visual = player.GetComponent<PaperCharacterVisual>();
    }
    void OnEnable()
    {
        DialogueBoxController.OnLinePresented += OnLine;
        DialogueBoxController.OnDialogueEnded += OnDialogueEnded;
    }
    void OnLine(string speaker, DialogueEmotion emotion)
    {
        if (canModel != null && thanksPending && Completed && speaker == "Marta" && emotion == DialogueEmotion.Friendly)
        { ThanksReceived = true; thanksPending = false; }
    }
    void OnDialogueEnded() { if (canModel != null) thanksPending = false; }
    void OnDisable()
    {
        DialogueBoxController.OnLinePresented -= OnLine;
        DialogueBoxController.OnDialogueEnded -= OnDialogueEnded;
        CancelWatering();
        if (canModel != null) { thanksPending = false; if (visual != null) visual.SetGardenPose(null, false); }
    }
    void Update()
    {
        if (activeAction == this && (ExplorationInput.Pressed(UnityEngine.InputSystem.Key.Escape) || SceneLoadManager.IsTransitioning)) CancelWatering();
    }
    void LateUpdate()
    {
        if (canModel == null || IsBusy) return;
        FindPlayer();
        CheckBorrowedTool();
        if (hasCan && visual != null) visual.SetGardenPose(canSprite, true);
        else { if (visual != null) visual.SetGardenPose(null, false); PlaceCanOnGround(); }
    }
    void OnDestroy() { if (canStation == this) canStation = null; }
    void PlaceCanOnGround()
    {
        if (canSprite == null || canSprite.sprite == null) return;
        var rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
        canSprite.transform.SetPositionAndRotation(transform.position + rotation * Vector3.up * .6016f, rotation);
        canSprite.transform.localScale = Vector3.one;
    }
    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        if (dialogue == null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning) return;
        if (bedIndex >= 0 && IsWatered) { dialogue.ShowMessage("Dieses Beet hat schon genug Wasser.", IntroChapter.PlayerName); return; }
        if (bedIndex >= 0 && !hasCan)
        { requested = true; dialogue.ShowMessage("Die Erde ist trocken. Die Gießkanne steht am seitlichen Gartenweg.", IntroChapter.PlayerName); return; }
        FindPlayer();
        station = canModel != null ? this : Array.Find(FindObjectsByType<CourtyardGarden>(FindObjectsSortMode.None), p => p.canModel != null);
        if (player == null || visual == null || station == null || station.canSprite.sprite == null) return;
        Vector3 right = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        right.y = 0; right.Normalize();
        float side = bedIndex >= 0 ? 1 : Vector3.Dot(transform.position - player.transform.position, right) < 0 ? -1 : 1;
        Vector3 stand = bedIndex < 0 ? transform.position - right * (.65f * side) : transform.position;
        stand.y = player.transform.position.y;
        if (!CanApproach(stand))
        { dialogue.ShowMessage("Ich gehe erst näher heran. Hier ist etwas im Weg.", IntroChapter.PlayerName); return; }
        action = bedIndex < 0 ? hasCan ? GardenAction.PutDown : GardenAction.PickUp : GardenAction.Pour;
        requested = true; activeAction = this; routine = StartCoroutine(Perform(stand, right * side));
    }
    bool CanApproach(Vector3 goal)
    {
        Vector3 delta = goal - player.transform.position;
        if (delta.magnitude > 3.2f) return false;
        bool Obstacle(Collider c) => !c.transform.IsChildOf(player.transform) && c.transform != player.transform && !c.CompareTag("Ground");
        var body = player.GetComponent<Rigidbody>();
        Physics.SyncTransforms();
        if (delta.magnitude > .01f)
            foreach (var hit in body.SweepTestAll(delta.normalized, delta.magnitude, QueryTriggerInteraction.Ignore))
                if (Obstacle(hit.collider) && hit.normal.y < .7f) return false;
        // Test the actual enabled body colliders, including the paper character's width.
        foreach (var shape in player.GetComponentsInChildren<Collider>())
        {
            if (!shape.enabled || shape.isTrigger || shape.attachedRigidbody != body) continue;
            var bounds = shape.bounds;
            foreach (var c in Physics.OverlapBox(bounds.center + delta, bounds.extents * .98f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (Obstacle(c)) return false;
        }
        return true;
    }
    IEnumerator Perform(Vector3 stand, Vector3 facing)
    {
        try
        {
            yield return Approach(stand);
            visual.FaceWorldDirection(facing);
            if (action == GardenAction.Pour) yield return Pour();
            else
            {
                bool pickup = action == GardenAction.PickUp;
                for (float t = 0; t < .55f; t += Time.deltaTime)
                {
                    float u = Mathf.SmoothStep(0, 1, t / .55f);
                    visual.SetGardenPose(station.canSprite, !pickup, u, 0, u);
                    yield return null;
                }
                // The hand reaches the handle before ownership changes; cancellation rolls it back.
                visual.SetGardenPose(station.canSprite, pickup, 1, 0, 1);
                if (!pickup) station.PlaceCanOnGround();
                yield return null;
                for (float t = 0; t < .55f; t += Time.deltaTime)
                {
                    float u = 1 - Mathf.SmoothStep(0, 1, t / .55f);
                    visual.SetGardenPose(station.canSprite, pickup, u, 0, u);
                    yield return null;
                }
                hasCan = pickup;
            }
        }
        finally { FinishAction(); }
    }
    IEnumerator Approach(Vector3 goal)
    {
        Vector3 start = player.transform.position;
        float duration = Vector3.Distance(start, goal) / 3;
        if (duration < .015f) yield break;
        movingBody = player.GetComponent<Rigidbody>(); movingController = player.GetComponent<PlayerController>();
        savedGravity = movingBody.useGravity; savedCollisions = movingBody.detectCollisions; savedInterpolation = movingBody.interpolation;
        restoringMovement = true; movingBody.useGravity = false; movingBody.detectCollisions = false; movingBody.interpolation = RigidbodyInterpolation.None;
        if (!movingBody.isKinematic) movingBody.linearVelocity = Vector3.zero;
        movingController.SetScriptedMovement(true);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            Vector3 p = Vector3.Lerp(start, goal, Mathf.SmoothStep(0, 1, t / duration));
            player.transform.position = p; movingBody.position = p; yield return null;
        }
        player.transform.position = goal; movingBody.position = goal; Physics.SyncTransforms();
        RestoreMovement();
    }
    void RestoreMovement()
    {
        if (!restoringMovement) return;
        if (movingController != null) movingController.SetScriptedMovement(false);
        if (movingBody != null)
        { movingBody.useGravity = savedGravity; movingBody.detectCollisions = savedCollisions; movingBody.interpolation = savedInterpolation; }
        restoringMovement = false;
    }
    IEnumerator Pour()
    {
        dropsRoot = new GameObject("Garden watering drops"); dropsRoot.transform.SetParent(transform, false);
        var spray = dropsRoot.AddComponent<GardenWaterEffect>();
        pourAudio = gameObject.AddComponent<AudioSource>(); pourAudio.playOnAwake = false;
        pourAudio.clip = Resources.Load<AudioClip>("Audio/IntroFoley/garden-water");
        pourAudio.volume = (SoundManager.instance != null ? SoundManager.instance.effectsVolume : .55f) * .55f;
        if (!Application.isBatchMode && pourAudio.clip != null) pourAudio.Play();
        Vector3 target = soil.bounds.center + new Vector3(-soil.bounds.extents.x * .45f, .42f, 0);
        for (float t = 0; t < 1.9f; t += Time.deltaTime)
        {
            float tilt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Min(t, 1.9f - t) / .3f));
            visual.SetGardenPose(station.canSprite, true, .10f * tilt, 38 * tilt);
            Vector3 spout = station.canSprite.transform.TransformPoint(new Vector3(.41f, -.19f, 0));
            Vector3 sweep = target + Vector3.forward * (Mathf.Sin(t * 2.1f) * .32f);
            spray.Present(spout, sweep, t, tilt);
            SetWetness(Mathf.Clamp01((t - .2f) / 1.4f)); yield return null;
        }
        watered |= 1 << bedIndex; RefreshAppearance();
        if (Completed) OnCompleted?.Invoke();
    }
    public void CancelWatering()
    {
        if (activeAction != this) return;
        if (routine != null) StopCoroutine(routine);
        FinishAction();
    }
    void FinishAction()
    {
        RestoreMovement();
        if (dropsRoot != null) Destroy(dropsRoot); dropsRoot = null;
        if (pourAudio != null) { pourAudio.Stop(); Destroy(pourAudio); } pourAudio = null;
        if (visual != null) visual.SetGardenPose(hasCan && station != null ? station.canSprite : null, hasCan);
        if (!hasCan && station != null) station.PlaceCanOnGround();
        routine = null; action = GardenAction.None; if (activeAction == this) activeAction = null;
        RefreshAppearance();
    }
    void SetWetness(float amount)
    {
        if (soil == null) return;
        var properties = new MaterialPropertyBlock(); soil.GetPropertyBlock(properties);
        var original = soil.sharedMaterial.GetColor("_BaseColor");
        properties.SetColor("_BaseColor", Color.Lerp(original, original * .56f, amount)); soil.SetPropertyBlock(properties);
        soil.GetComponent<CourtyardVegetableBed>()?.SetWetness(amount);
    }
    void RefreshAppearance() => SetWetness(IsWatered ? 1 : 0);
}