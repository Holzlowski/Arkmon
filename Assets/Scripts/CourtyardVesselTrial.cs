using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(9010)]
public sealed class CourtyardVesselTrial : MonoBehaviour
{
    const float ChargeDuration = 12f;
    const float DrainDuration = 22f;
    static readonly int VeilStrength = Shader.PropertyToID("_Strength");
    static readonly int VeilOrigin = Shader.PropertyToID("_Origin");
    static readonly Color ArcaneViolet = new Color(.65f,.32f,1);
    static readonly Color ArcaneTeal = new Color(.26f,1,.84f);
    const float DaylightReturn = 3.2f;
    static readonly Vector2 RaisedGem = new Vector2(.35f,1.46f);
    public static bool HasVessel { get; private set; }
    public static bool Completed { get; private set; }
    internal float Charge { get; private set; }
    int Effort => Charge < .36f ? 0 : Charge < .78f ? 1 : 2;

    readonly Sprite[] expressions = new Sprite[3];
    readonly List<(Transform actor, Vector3 home)> stagedResidents = new();
    Sprite proud;
    PaperCharacterVisual elian;
    SpriteRenderer mentor;
    Sprite mentorIdle, offer;
    CourtyardGuard guard;
    NPC witness;
    CourtyardGardenerVisual gardener;
    CourtyardStablekeeperVisual stablekeeper;
    CourtyardCarpenterVisual carpenter;
    Canvas prompt;
    TextMeshProUGUI hint;
    Image chargeFill;
    ParticleSystem sparks;
    Light glow;
    SpriteRenderer halo;
    Sprite haloSprite;
    Texture2D haloTexture;
    SpriteRenderer darkness;
    Sprite darknessSprite;
    Material veilMaterial;
    Transform chargeAudio;
    AudioSource resonance, overtones, arrivalTheme;
    float themeLevel, excitation, musicDuck;
    float pulse, nextSpark;
    float successBegan = -1;
    int highestEffort;
    bool accepting, cancelled;

    internal static void ResetProgress() => HasVessel = Completed = false;

    internal IEnumerator Play(DialogueBoxController dialogue, Transform player, SpriteRenderer teacher, Sprite portrait, AudioSource theme)
    {
        if (Completed || dialogue == null || player == null) yield break;
        elian = player.GetComponent<PaperCharacterVisual>();
        if (elian == null || !LoadPoses()) yield break;
        mentor = teacher;
        mentorIdle = mentor.sprite;
        Charge = 0;
        highestEffort = 0;
        cancelled = false;

        try
        {
            if (!HasVessel && offer != null) mentor.sprite = offer;
            dialogue.ShowConversation(new[] { new DialogueSection {
                dialogue = new[] { HasVessel ? ArkanmeisterinConversation.TrialRetry : ArkanmeisterinConversation.VesselOffer },
                presentations = new[] { new DialogueLinePresentation { portrait = portrait, emotion = DialogueEmotion.Friendly } },
                endAfterDialogue = true
            } }, ArkanmeisterinConversation.Speaker, "", null);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (dialogue == null) yield break;
            HasVessel = true;
            mentor.sprite = mentorIdle;

            elian.SetVesselTrialPose(expressions[0]);
            elian.FaceWorldDirection(transform.position-player.position);
            yield return GatherWitnesses(player);
            BuildEffects();
            BuildPrompt();
            StartChargeAudio(theme);
            SetHint(false);
            accepting = true;
            yield return new WaitUntil(() => cancelled || Charge >= 1);
            accepting = false;
            if (cancelled)
            {
                yield return new WaitForSecondsRealtime(.42f);
                yield break;
            }

            Completed = true;
            prompt.enabled = false;
            elian.SetVesselTrialPose(proud,RaisedGem);
            successBegan = Time.time;
            nextSpark = successBegan+.2f;
            pulse = 4.3f;
            Burst(58);
            SoundManager.instance?.PlayEffect("VesselTrial/resolution",.70f);
            yield return new WaitForSeconds(.65f);
            gardener?.ReactToVessel();
            yield return new WaitForSeconds(.23f);
            stablekeeper?.ReactToVessel();
            yield return new WaitForSeconds(.23f);
            carpenter?.ReactToVessel();
            yield return new WaitUntil(() => Time.time-successBegan >= DaylightReturn+.55f);
            ArkanmeisterinConversation.Congratulate(dialogue,portrait,witness != null,guard != null,
                gardener != null,stablekeeper != null,carpenter != null);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
        }
        finally { ClearPresentation(); }
    }

    private void Update()
    {
        if (!accepting) return;
        if (ExplorationInput.Pressed(Key.Escape)) { cancelled = true; return; }
        bool holding = ExplorationInput.Held(Key.Space);
        float duration = holding ? ChargeDuration : DrainDuration;
        int previousEffort = Effort;
        Charge = Mathf.MoveTowards(Charge,holding ? 1 : 0,Time.deltaTime/duration);
        if (Effort != previousEffort) elian.SetVesselTrialPose(expressions[Effort]);
        PaperUIFactory.SetHealthFill(chargeFill,Charge);
        chargeFill.color = Color.Lerp(ArcaneViolet,ArcaneTeal,Charge);
        SetHint(holding);

        if (Effort > highestEffort)
        {
            highestEffort = Effort;
            pulse = highestEffort*.64f;
            Burst(highestEffort*5);
        }
    }

    private void Burst(int count)
    {
        sparks.transform.position = elian.VesselTrialOrigin;
        sparks.Emit(count);
    }

    private void StartChargeAudio(AudioSource theme)
    {
        var low = Resources.Load<AudioClip>("Audio/VesselTrial/resonance");
        var bright = Resources.Load<AudioClip>("Audio/VesselTrial/overtones");
        if (low == null || bright == null) return;
        chargeAudio = new GameObject("Vessel charge audio").transform;
        chargeAudio.SetParent(transform,false);
        arrivalTheme = theme;
        themeLevel = theme != null ? theme.volume : 0;
        resonance = StartLoop("Vessel charge resonance",low);
        overtones = StartLoop("Vessel charge overtones",bright);
    }

    private AudioSource StartLoop(string name, AudioClip clip)
    {
        var source = new GameObject(name,typeof(AudioSource)).GetComponent<AudioSource>();
        source.transform.SetParent(chargeAudio,false);
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.loop = true;
        source.volume = 0;
        source.clip = clip;
        source.Play();
        return source;
    }

    private void MixChargeAudio()
    {
        if (chargeAudio == null) return;
        bool active = accepting && !cancelled;
        bool holding = active && ExplorationInput.Held(Key.Space);
        float effects = SoundManager.instance != null ? SoundManager.instance.effectsVolume : 0;
        float fade = Time.unscaledDeltaTime;
        excitation = Mathf.MoveTowards(excitation,active ? holding ? 1 : .38f : 0,fade*3);
        float body = Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,Charge));
        float brightness = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.16f,.90f,Charge));
        resonance.volume = .28f*effects*body*excitation;
        overtones.volume = .38f*effects*brightness*excitation;

        float duck = active && effects > .001f ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.42f,Charge)) : 0;
        musicDuck = Mathf.MoveTowards(musicDuck,duck,fade*2.5f);
        if (arrivalTheme != null) arrivalTheme.volume = themeLevel*Mathf.Lerp(1,.45f,musicDuck);
    }

    private void LateUpdate()
    {
        if (glow == null || elian == null) return;
        MixChargeAudio();
        var camera = Camera.main;
        glow.transform.position = elian.VesselTrialOrigin;
        pulse = Mathf.MoveTowards(pulse,0,Time.deltaTime*1.5f);
        bool succeeded = successBegan >= 0;
        float timeSinceSuccess = Time.time-successBegan;
        float dim = succeeded
            ? 1-Mathf.SmoothStep(0,1,timeSinceSuccess/DaylightReturn)
            : Mathf.SmoothStep(0,1,Charge);
        if (camera != null)
        {
            darkness.transform.SetPositionAndRotation(
                camera.transform.position+camera.transform.forward*(camera.nearClipPlane+.2f),camera.transform.rotation);
            darkness.color = new Color(.035f,.027f,.075f,dim*.79f);
            if (veilMaterial != null)
            {
                var origin = camera.WorldToViewportPoint(glow.transform.position);
                veilMaterial.SetVector(VeilOrigin,new Vector4(origin.x,origin.y,camera.aspect,0));
                veilMaterial.SetFloat(VeilStrength,dim);
            }
        }
        var arcane = Color.Lerp(ArcaneViolet,ArcaneTeal,.5f+.5f*Mathf.Sin(Time.time*.7f));
        glow.color = succeeded ? new Color(1,.76f,.4f) : arcane;
        float steady = succeeded ? 2.65f+Mathf.Sin(Time.time*3)*.22f : dim*.83f;
        glow.intensity = Mathf.Max(steady,pulse);
        if ((succeeded || Charge > .22f) && Time.time >= nextSpark)
        {
            Burst(succeeded && timeSinceSuccess < DaylightReturn ? 5 : Charge > .65f ? 2 : 1);
            nextSpark = Time.time+(succeeded
                ? timeSinceSuccess < DaylightReturn ? .16f : .38f
                : Mathf.Lerp(.8f,.19f,Charge));
        }
        if (camera != null) halo.transform.rotation = camera.transform.rotation;
        float light = Mathf.Clamp01(glow.intensity/2.8f);
        var haloColor = succeeded ? new Color(1,.83f,.45f) : arcane;
        haloColor.a = light*.94f;
        halo.color = haloColor;
        halo.transform.localScale = Vector3.one*Mathf.Lerp(.28f,1.24f,light);
    }

    private void BuildPrompt()
    {
        prompt = new GameObject("Vessel concentration",typeof(Canvas)).GetComponent<Canvas>();
        prompt.transform.SetParent(transform,false);
        prompt.renderMode = RenderMode.ScreenSpaceOverlay;
        prompt.sortingOrder = 25;
        PaperUIFactory.Scale(prompt);
        var anchor = new Vector2(.5f,.08f);
        var panel = PaperUIFactory.Image("Concentration paper",prompt.transform,PaperUIStyle.Load()?.panel,
            anchor,anchor,new Vector2(-236,-24),new Vector2(236,49));
        hint = PaperUIFactory.Text("Concentration instruction",panel.transform,"",23,
            new Vector2(0,.41f),Vector2.one,new Vector2(14,0),new Vector2(-14,-6),TextAlignmentOptions.Center);
        var track = PaperUIFactory.Image("Vessel charge track",panel.transform,null,
            Vector2.zero,Vector2.right,new Vector2(29,16),new Vector2(-29,29),false);
        track.color = new Color(.20f,.135f,.28f);
        chargeFill = PaperUIFactory.HealthFill("Vessel charge fill",track.transform);
        PaperUIFactory.SetHealthFill(chargeFill,0);
    }

    private void SetHint(bool holding)
    {
        string value = holding ? "Elian bündelt die Magie …" : "Leertaste halten · Konzentrieren";
        if (hint.text != value) hint.text = value;
    }

    private void BuildEffects()
    {
        // Grade the world behind the unlit gem and sparks, independently of the camera's post-processing profile.
        darkness = new GameObject("Vessel trial lighting",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        darkness.transform.SetParent(transform,false);
        darkness.transform.localScale = Vector3.one*8;
        var white = Texture2D.whiteTexture;
        darknessSprite = Sprite.Create(white,new Rect(0,0,white.width,white.height),Vector2.one*.5f,white.width);
        darkness.sprite = darknessSprite;
        darkness.sortingOrder = 16;
        darkness.color = Color.clear;
        var veilShader = Resources.Load<Shader>("Vessel arcane veil");
        if (veilShader != null)
        {
            veilMaterial = new Material(veilShader);
            var arcaneVeil = new GameObject("Arcane colors",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            arcaneVeil.transform.SetParent(darkness.transform,false);
            arcaneVeil.sprite = darknessSprite;
            arcaneVeil.sharedMaterial = veilMaterial;
            arcaneVeil.sortingOrder = 17;
        }

        sparks = new GameObject("Vessel magic sparks",typeof(ParticleSystem)).GetComponent<ParticleSystem>();
        sparks.transform.SetParent(transform,false);
        sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = sparks.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.45f,1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.65f,2.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f,.095f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.76f,.42f,1),new Color(1,.89f,.54f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 85;
        var emission = sparks.emission;
        emission.enabled = false;
        var shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .13f;
        var particleSize = sparks.sizeOverLifetime;
        particleSize.enabled = true;
        particleSize.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
        var renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Resources.Load<Material>("Magic Sparks");
        renderer.sortingOrder = 19;
        sparks.Play();

        glow = new GameObject("Vessel light",typeof(Light)).GetComponent<Light>();
        glow.transform.SetParent(transform,false);
        glow.type = LightType.Point;
        glow.color = new Color(.83f,.54f,1);
        glow.range = 2.8f;
        glow.intensity = 0;

        const int size = 48;
        haloTexture = new Texture2D(size,size,TextureFormat.RGBA32,false);
        haloTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size*size];
        for (int y=0; y<size; y++)
        for (int x=0; x<size; x++)
        {
            float r = new Vector2(x-(size-1)*.5f,y-(size-1)*.5f).magnitude/(size*.5f);
            byte alpha = (byte)Mathf.RoundToInt(255*Mathf.Pow(Mathf.Max(0,1-r),2));
            pixels[y*size+x] = new Color32(255,255,255,alpha);
        }
        haloTexture.SetPixels32(pixels);
        haloTexture.Apply();
        haloSprite = Sprite.Create(haloTexture,new Rect(0,0,size,size),Vector2.one*.5f,76);
        halo = new GameObject("Vessel gem halo",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        halo.transform.SetParent(glow.transform,false);
        halo.sprite = haloSprite;
        halo.sortingOrder = 18;
        halo.color = Color.clear;
    }

    private IEnumerator GatherWitnesses(Transform player)
    {
        guard = FindFirstObjectByType<CourtyardGuard>();
        if (guard != null)
        {
            var mark = transform.position+new Vector3(-3.05f,0,.65f);
            var entrance = mark+Vector3.left*2.9f;
            guard.BeginArrivalRun(entrance,mark);
            yield return new WaitWhile(() => guard != null && guard.IsArrivalRunning);
            if (guard != null) guard.FaceArrivalGuest(player.position);
        }
        foreach (var npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
            if (npc.npcName == "John") { witness = npc; break; }
        if (witness != null)
        {
            yield return StageResident(witness,new Vector3(.55f,0,2.58f),null);
            if (witness != null) witness.FacePlayer();
        }

        stablekeeper = FindFirstObjectByType<CourtyardStablekeeperVisual>();
        if (stablekeeper != null) stablekeeper.SetVesselTrialScale(true);
        yield return StageResident(stablekeeper,new Vector3(-1.62f,0,1.9f),new Vector3(-5.65f,0,1.95f));
        gardener = FindFirstObjectByType<CourtyardGardenerVisual>();
        yield return StageResident(gardener,new Vector3(3.85f,0,.88f),new Vector3(6.6f,0,-.65f));
        carpenter = FindFirstObjectByType<CourtyardCarpenterVisual>();
        yield return StageResident(carpenter,new Vector3(2.62f,0,2.25f),new Vector3(-7.8f,0,4.45f));
    }

    private IEnumerator StageResident(Component resident, Vector3 mark, Vector3? entrance)
    {
        if (resident == null) yield break;
        var actor = resident.transform;
        stagedResidents.Add((actor,actor.position));
        var entry = entrance.HasValue ? transform.position+entrance.Value : actor.position;
        yield return WalkWitness(actor,entry,transform.position+mark);
    }

    private static IEnumerator WalkWitness(Transform actor, Vector3 entry, Vector3 mark)
    {
        entry = CourtyardGuard.Grounded(entry);
        mark = CourtyardGuard.Grounded(mark);
        if (entry.y < -90 || mark.y < -90) yield break;
        actor.position = entry;
        while (actor != null && Vector3.Distance(actor.position,mark) > .025f)
        {
            var point = Vector3.MoveTowards(actor.position,mark,3.15f*Time.deltaTime);
            var ground = CourtyardGuard.Grounded(point);
            actor.position = ground.y > -90 ? ground : point;
            yield return null;
        }
        if (actor != null) actor.position = mark;
    }

    private bool LoadPoses()
    {
        if (expressions[0] != null) return true;
        var art = Resources.Load<Texture2D>("Elian vessel trial");
        var lifted = Resources.Load<Texture2D>("Elian vessel proud");
        var strain = Resources.Load<Texture2D>("Elian vessel strain");
        if (art == null || lifted == null || strain == null) return false;
        var concentrationPivot = new Vector2(232f/450,11f/887);
        expressions[0] = Sprite.Create(art,new Rect(80,0,450,887),new Vector2(233f/450,11f/887),440);
        expressions[1] = Sprite.Create(art,new Rect(650,0,450,887),concentrationPivot,440);
        expressions[2] = Sprite.Create(strain,new Rect(0,0,450,887),concentrationPivot,440);
        expressions[0].name = "Elian concentrating";
        expressions[1].name = "Elian straining";
        expressions[2].name = "Elian maximum effort";
        proud = Sprite.Create(lifted,new Rect(0,0,lifted.width,lifted.height),new Vector2(.498f,.060f),lifted.height/2.138f);
        proud.name = "Elian proud vessel lift";
        var offering = Resources.Load<Texture2D>("Arkanmeisterin vessel offer");
        if (offering != null) offer = CreateOfferSprite(offering);
        return true;
    }

    private static Sprite CreateOfferSprite(Texture2D art)
    {
        var sprite = Sprite.Create(art,new Rect(0,0,art.width,art.height),new Vector2(.50f,.025f),art.height/2.38f);
        sprite.name = "Vessel offered";
        return sprite;
    }

    internal void ClearPresentation()
    {
        accepting = false;
        cancelled = true;
        if (elian != null) elian.SetVesselTrialPose(null);
        if (mentor != null && mentorIdle != null) mentor.sprite = mentorIdle;
        foreach (var (actor,home) in stagedResidents)
            if (actor != null) actor.position = home;
        stagedResidents.Clear();
        witness = null;
        gardener = null;
        if (stablekeeper != null) stablekeeper.SetVesselTrialScale(false);
        stablekeeper = null;
        carpenter = null;
        guard?.EndArrivalRun();
        guard = null;
        if (prompt != null) { prompt.enabled = false; Destroy(prompt.gameObject); }
        if (sparks != null) { sparks.gameObject.SetActive(false); Destroy(sparks.gameObject); }
        if (glow != null) { glow.gameObject.SetActive(false); Destroy(glow.gameObject); }
        if (darkness != null) { darkness.gameObject.SetActive(false); Destroy(darkness.gameObject); }
        Destroy(veilMaterial);
        veilMaterial = null;
        Destroy(darknessSprite);
        darkness = null;
        darknessSprite = null;
        successBegan = -1;
        Destroy(haloSprite);
        Destroy(haloTexture);
        haloSprite = null;
        haloTexture = null;
        prompt = null;
        chargeFill = null;
        sparks = null;
        glow = null;
        pulse = 0;
        if (chargeAudio != null) { chargeAudio.gameObject.SetActive(false); Destroy(chargeAudio.gameObject); }
        if (arrivalTheme != null) arrivalTheme.volume = themeLevel;
        chargeAudio = null;
        resonance = null;
        overtones = null;
        arrivalTheme = null;
        excitation = musicDuck = 0;
    }

    private void OnDisable() => ClearPresentation();

    private void OnDestroy()
    {
        foreach (var sprite in expressions) Destroy(sprite);
        Destroy(offer);
        Destroy(proud);
    }
}
