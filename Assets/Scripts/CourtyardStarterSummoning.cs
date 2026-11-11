using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CourtyardStarterSummoning : MonoBehaviour
{
    internal const string CuePath = "Audio/VesselTrial/courtyard-summoning";
    const float Duration = 8f;
    const float FirstLaunch = 1.6f;
    const float LaunchSpacing = .86f;
    const float FlightTime = 1.05f;

    static readonly Vector3[] Marks =
    {
        new Vector3(-2.25f,0,.75f), new Vector3(-1.5f,0,-1.48f),
        new Vector3(1.25f,0,-1.58f), new Vector3(2.38f,0,.62f)
    };
    static readonly Vector3[] Exits =
    {
        new Vector3(-7.2f,0,3.0f), new Vector3(-5.4f,0,-4.9f),
        new Vector3(6.9f,0,-4.7f), new Vector3(8.2f,0,1.9f)
    };
    static readonly string[] Names = { "Kafla", "Panfrie", "Liora", "Rauk" };
    static readonly Color[] Tints =
    {
        new Color(1,.42f,.13f), new Color(.31f,.78f,1),
        new Color(.52f,1,.45f), new Color(.77f,.48f,1)
    };

    sealed class Starter
    {
        public Transform root;
        public SpriteRenderer[] figure;
        public Vector3 mark, exit;
        public Vector3 scale;
        public SpriteRenderer halo, core, burst;
        public readonly SpriteRenderer[] trail = new SpriteRenderer[6];
        public readonly SpriteRenderer[] motes = new SpriteRenderer[10];

        public void Fade(float opacity)
        {
            var tint = new Color(1,1,1,opacity);
            foreach (var part in figure)
            {
                part.enabled = opacity > .002f;
                part.color = tint;
            }
        }
    }

    public static bool Completed { get; private set; }
    internal static void ResetProgress() => Completed = false;

    readonly List<Sprite> sprites = new();
    GameObject stage;
    SpriteRenderer mentor, vesselLight;
    Sprite idle, lowered, lifting, raised, haloSprite;
    Material idleMaterial, paper, hair;
    MaterialPropertyBlock originalProperties, wind;
    Texture2D haloTexture;
    PaperCharacterVisual elian;
    PlayerController controller;
    Transform eyeTarget;
    AudioSource cue, theme;
    float themeLevel;

    internal IEnumerator Play(DialogueBoxController dialogue, Transform player, SpriteRenderer figure, Sprite portrait,
        CourtyardCamera follow, AudioSource music)
    {
        if (Completed || dialogue == null || player == null || figure == null || follow == null || Camera.main == null)
            yield break;

        try
        {
            mentor = figure;
            idle = mentor.sprite;
            idleMaterial = mentor.sharedMaterial;
            originalProperties = new MaterialPropertyBlock();
            mentor.GetPropertyBlock(originalProperties);
            wind = new MaterialPropertyBlock();
            elian = player.GetComponent<PaperCharacterVisual>();
            controller = player.GetComponent<PlayerController>();
            theme = music;
            themeLevel = theme != null ? theme.volume : 0;

            ArkanmeisterinConversation.ExplainArkmon(dialogue,portrait);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (dialogue == null) yield break;

            var center = transform.position;
            var view = Quaternion.Euler(20,0,0);
            yield return FrameSummoning(player,follow,center,view);
            var starters = BuildStage(center,view);
            if (starters == null) yield break;

            const float height = 2.48f;
            Vector3 vessel = center + view * new Vector3(.54f,height*.778f,-.025f);
            for (float elapsed = 0; elapsed < Duration; elapsed += Time.deltaTime)
            {
                if (elapsed == 0 && cue != null) cue.Play();
                PresentSpell(elapsed,view,vessel,starters);
                yield return null;
            }
            PresentSpell(Duration,view,vessel,starters);
            RestoreMentor();
            elian?.SetTrialLookTarget(transform);

            // Leave the figures above the standard dialogue panel.
            var beforeDialogue = Camera.main.transform.position;
            for (float elapsed=0; elapsed<.55f; elapsed+=Time.deltaTime)
            {
                follow.SetScenicView(beforeDialogue - Vector3.up*(1.48f*Ease(0,.55f,elapsed)),view,48);
                yield return null;
            }
            follow.SetScenicView(beforeDialogue - Vector3.up*1.48f,view,48);
            ArkanmeisterinConversation.SendStartersToExplore(dialogue,portrait);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (dialogue == null) yield break;
            yield return Depart(starters,view);

            ArkanmeisterinConversation.InviteStarterMeetings(dialogue,portrait);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (dialogue != null) Completed = true;
        }
        finally { ClearStage(); }
    }

    IEnumerator FrameSummoning(Transform player, CourtyardCamera follow, Vector3 center, Quaternion view)
    {
        var camera = Camera.main;
        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        float oldFov = camera.fieldOfView;
        var start = player.position;
        var body = player.GetComponent<Rigidbody>();
        float footOffset = Mathf.Clamp(start.y-CourtyardGuard.Grounded(start).y,0,1.5f);
        var end = CourtyardGuard.Grounded(center + new Vector3(3.46f,0,-.16f)) + Vector3.up*footOffset;
        var position = center + new Vector3(.26f,1.37f,.05f) - view*Vector3.forward*7.2f;
        if (body != null) controller?.SetScriptedMovement(true);
        elian?.SetTrialLookTarget(transform);
        for (float elapsed = 0; elapsed < .97f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0,1,elapsed/.97f);
            follow.SetScenicView(Vector3.Lerp(oldPosition,position,t),Quaternion.Slerp(oldRotation,view,t),
                Mathf.Lerp(oldFov,48,t));
            if (body != null)
            {
                var mark = CourtyardGuard.Grounded(Vector3.Lerp(start,end,t)) + Vector3.up*footOffset;
                player.position = body.position = mark;
            }
            yield return null;
        }
        if (body != null) player.position = body.position = end;
        controller?.SetScriptedMovement(false);
        follow.SetScenicView(position,view,48);
    }

    Starter[] BuildStage(Vector3 center, Quaternion view)
    {
        var scenicShader = Resources.Load<Shader>("AurenfeldScenicPaper");
        var hairShader = Resources.Load<Shader>("StarterSummoningPaper");
        var prefab = Resources.Load<GameObject>("Kafla courtyard figure");
        var panfrie = Resources.Load<Sprite>("Panfrie/Panfrie Hervorlugen");
        var liora = LoadPose("Liora",1.35f);
        var rauk = LoadPose("Rauk",1.38f);
        lowered = LoadPose("Vessel low",2.48f,true);
        lifting = LoadPose("Vessel lift",2.48f,true);
        raised = LoadPose("Vessel raised",2.48f,true);
        if (scenicShader == null || hairShader == null || prefab == null || panfrie == null ||
            liora == null || rauk == null || lowered == null || lifting == null || raised == null)
        {
            Debug.LogError("The courtyard summoning stage is missing an asset.");
            return null;
        }

        stage = new GameObject("Courtyard starter summoning");
        paper = new Material(scenicShader);
        hair = new Material(hairShader);
        eyeTarget = new GameObject("Elian summoning gaze").transform;
        eyeTarget.SetParent(stage.transform,false);
        eyeTarget.position = center;
        elian?.SetTrialLookTarget(eyeTarget);

        haloTexture = new Texture2D(48,48,TextureFormat.RGBA32,false);
        var pixels = new Color[48*48];
        for (int y=0; y<48; y++)
        for (int x=0; x<48; x++)
        {
            var d = new Vector2(x-23.5f,y-23.5f)/24f;
            pixels[y*48+x] = new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d.sqrMagnitude),2.5f));
        }
        haloTexture.SetPixels(pixels);
        haloTexture.Apply();
        haloSprite = Sprite.Create(haloTexture,new Rect(0,0,48,48),new Vector2(.5f,.5f),48);
        vesselLight = Glow("Charged vessel",view);

        var starters = new Starter[Names.Length];
        var art = new[] { null, panfrie, liora, rauk };
        for (int i=0; i<starters.Length; i++)
        {
            var actor = i == 0 ? Instantiate(prefab,stage.transform) : new GameObject(Names[i],typeof(SpriteRenderer));
            actor.name = $"Summoned {Names[i]}";
            actor.transform.SetParent(stage.transform,false);
            var mark = CourtyardGuard.Grounded(center+Marks[i]);
            actor.transform.SetPositionAndRotation(mark,view);
            actor.transform.localScale = i == 0 ? Vector3.one*.78f
                : i == 1 ? Vector3.one*.66f : new Vector3(-1,1,1);
            if (i != 0) actor.GetComponent<SpriteRenderer>().sprite = art[i];
            var starter = new Starter
            {
                root = actor.transform, figure = actor.GetComponentsInChildren<SpriteRenderer>(),
                mark = mark, exit = center+Exits[i], scale = actor.transform.localScale,
                halo = Glow($"{Names[i]} orb halo",view), core = Glow($"{Names[i]} orb core",view),
                burst = Glow($"{Names[i]} arrival glow",view)
            };
            foreach (var part in starter.figure) part.sharedMaterial = paper;
            starter.Fade(0);
            PaperGroundShadow.Attach(actor,starter.figure[0]);
            for (int j=0; j<starter.trail.Length; j++) starter.trail[j] = Glow("Orb trail",view);
            for (int j=0; j<starter.motes.Length; j++) starter.motes[j] = Glow("Arrival spark",view);
            starters[i] = starter;
        }

        var clip = Resources.Load<AudioClip>(CuePath);
        if (clip != null)
        {
            cue = stage.AddComponent<AudioSource>();
            cue.playOnAwake = false;
            cue.clip = clip;
            cue.spatialBlend = 0;
            cue.volume = SoundManager.instance != null ? SoundManager.instance.effectsVolume : .55f;
        }
        return starters;
    }

    Sprite LoadPose(string name, float height, bool registerOnCanvas = false)
    {
        var sprite = LoadArtwork("StarterSummoning/"+name,height,registerOnCanvas);
        if (sprite != null) sprites.Add(sprite);
        return sprite;
    }

    internal static Sprite LoadArtwork(string resource, float height, bool registerOnCanvas = false)
    {
        var texture = Resources.Load<Texture2D>(resource);
        if (texture == null || !texture.isReadable) return null;
        var pixels = texture.GetPixels32();
        int left=texture.width, right=-1, bottom=texture.height, top=-1;
        for (int y=0; y<texture.height; y++)
        for (int x=0; x<texture.width; x++)
        {
            if (pixels[y*texture.width+x].a <= 150) continue;
            left=Mathf.Min(left,x); right=Mathf.Max(right,x);
            bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y);
        }
        if (right < left || top < bottom) return null;
        var rect = new Rect(left,bottom,right-left+1,top-bottom+1);
        float xPivot = registerOnCanvas ? (texture.width*.5f-left)/rect.width : .5f;
        var sprite = Sprite.Create(texture,rect,new Vector2(xPivot,0),rect.height/height);
        sprite.name = texture.name;
        return sprite;
    }

    SpriteRenderer Glow(string name, Quaternion view)
    {
        var effect = new GameObject(name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        effect.transform.SetParent(stage.transform,false);
        effect.transform.rotation = view;
        effect.sprite = haloSprite;
        effect.sharedMaterial = paper;
        effect.sortingOrder = 2;
        effect.enabled = false;
        return effect;
    }

    static void GlowAt(SpriteRenderer effect, Vector3 position, float size, Color tint, float alpha)
    {
        effect.enabled = alpha > .002f;
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one*size;
        tint.a = alpha;
        effect.color = tint;
    }

    void PresentSpell(float time, Quaternion view, Vector3 vessel, Starter[] starters)
    {
        float charge = Ease(.73f,1.58f,time);
        float release = 1-Ease(4.4f,5.55f,time);
        if (time < .70f || time >= 7.65f) mentor.sprite = lowered;
        else if (time < .86f || time >= 7.45f) mentor.sprite = lifting;
        else mentor.sprite = raised;
        if (theme != null)
            theme.volume = themeLevel*(1-.45f*Ease(0,1,time)*(1-Ease(6.9f,Duration,time)));
        mentor.sharedMaterial = hair;
        wind.SetFloat("_WindTime",time);
        wind.SetFloat("_Wind",charge*release);
        mentor.SetPropertyBlock(wind);
        var bias = view*Vector3.back*.07f;
        GlowAt(vesselLight,vessel+bias,.60f+charge*.34f,new Color(.77f,.52f,1),
            charge*release*(.83f+.17f*Mathf.Sin(time*11f))*.55f);
        eyeTarget.position = vessel;

        for (int i=0; i<starters.Length; i++)
        {
            var starter = starters[i];
            float since = time-FirstLaunch-i*LaunchSpacing;
            float arrival = since-FlightTime;
            var point = OrbPath(i,Mathf.Clamp01(since/FlightTime),vessel,starter.mark);
            float light = Ease(0,.16f,since)*(1-Ease(0,.64f,arrival));
            if (since >= 0 && arrival < .40f) eyeTarget.position = point;
            var tint = Tints[i];
            GlowAt(starter.halo,point+bias,.93f+.29f*Mathf.Clamp01(arrival/.34f),tint,light*.88f);
            GlowAt(starter.core,point+bias,.46f,Color.Lerp(tint,Color.white,.78f),light);
            for (int j=0; j<starter.trail.Length; j++)
            {
                float tail = since-(j+1)*.064f;
                float alpha = tail > 0 && tail < FlightTime && arrival < .18f ? (.49f-j*.062f)*light : 0;
                GlowAt(starter.trail[j],OrbPath(i,Mathf.Clamp01(tail/FlightTime),vessel,starter.mark)+bias,
                    .28f-j*.022f,tint,alpha);
            }
            starter.Fade(Ease(.12f,.83f,arrival));
            float burst = arrival < 0 ? 0 : Mathf.Clamp01(1-arrival/.68f);
            GlowAt(starter.burst,starter.mark+Vector3.up*.42f+bias,.7f+1.13f*(1-burst),tint,burst*.7f);
            for (int j=0; j<starter.motes.Length; j++)
            {
                float delay = j*.012f;
                float age = Mathf.Clamp01((arrival-delay)/.76f);
                float angle = j*2.39996f;
                var offset = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*age*(.38f+j*.048f);
                offset.y = .28f+Mathf.Sin(age*Mathf.PI)*(.22f+j*.059f);
                float alpha = arrival > delay ? Mathf.Sin(age*Mathf.PI) : 0;
                GlowAt(starter.motes[j],starter.mark+offset+bias,.095f+.04f*(j%2),
                    Color.Lerp(tint,Color.white,.42f),alpha);
            }
        }
    }

    static Vector3 OrbPath(int index, float t, Vector3 vessel, Vector3 mark)
    {
        float arc = Mathf.Sin(Mathf.PI*t);
        return Vector3.Lerp(vessel,mark+Vector3.up*.22f,t) + Vector3.up*arc*(1.05f+index*.07f)
            + Vector3.forward*arc*(index == 0 || index == 3 ? .46f : -.32f);
    }

    static float Ease(float from, float to, float time) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,time));

    IEnumerator Depart(Starter[] starters, Quaternion view)
    {
        for (float elapsed=0; elapsed<3.1f; elapsed+=Time.deltaTime)
        {
            for (int i=0; i<starters.Length; i++)
            {
                var starter = starters[i];
                float time = elapsed-i*.24f;
                if (time <= 0) continue;
                float progress = Mathf.Clamp01(time/2.3f);
                var point = CourtyardGuard.Grounded(Vector3.Lerp(starter.mark,starter.exit,progress));
                float step = Mathf.Abs(Mathf.Sin(time*11.2f))*.055f;
                starter.root.position = point + Vector3.up*step;
                var scale = starter.scale;
                scale.x = Mathf.Abs(scale.x) * (Vector3.Dot(starter.exit-starter.mark,view*Vector3.right) < 0 ? -1 : 1);
                starter.root.localScale = scale;
                starter.Fade(1-Ease(.74f,1,progress));
            }
            yield return null;
        }
        foreach (var starter in starters) starter.Fade(0);
    }

    void RestoreMentor()
    {
        if (mentor == null) return;
        mentor.sprite = idle;
        mentor.sharedMaterial = idleMaterial;
        mentor.SetPropertyBlock(originalProperties);
    }

    internal void ClearStage()
    {
        RestoreMentor();
        mentor = null;
        elian?.SetTrialLookTarget(null);
        controller?.SetScriptedMovement(false);
        if (theme != null) theme.volume = themeLevel;
        theme = null;
        if (stage != null)
        {
            stage.SetActive(false);
            Destroy(stage);
            stage = null;
        }
        foreach (var sprite in sprites) Destroy(sprite);
        sprites.Clear();
        Destroy(haloSprite);
        Destroy(haloTexture);
        Destroy(paper);
        Destroy(hair);
        haloSprite = null;
        haloTexture = null;
        paper = hair = null;
        cue = null;
    }

    void OnDisable() => ClearStage();
}
