using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ArkanmeisterinArrival : MonoBehaviour, IInteractableAsPlayer
{
    internal enum Shot { None, Rise, Road, Wheel, Side, Gate, Coachman, Guard, Step, Landing, Reveal, Reaction, Approach, Greeting, VesselTrial, StarterSummoning, FieldWalk }
    internal Shot CurrentShot { get; private set; }

    const float ApproachX = 76.5f;
    static readonly Vector3 RoadViewPosition = new Vector3(37.6f,15.2f,-25.5f);
    static readonly Vector3 RoadViewTarget = new Vector3(54.8f,1.95f,-.20f);
    static readonly Vector3 Parked = new Vector3(36.15f,.035f,-1);
    static readonly Vector3 GreetingSpot = new Vector3(37.74f,0,-3.38f);
    static readonly Vector3 TrialSpot = new Vector3(13.2f,0,-1.8f);
    static Vector3 FieldStop => CourtyardGateForecourt.Center + new Vector3(.9f, 0, -.45f);
    static readonly Vector3[] FieldWaypoints = {
        new Vector3(20, 0, 2.1f), new Vector3(35, 0, 2.1f),
        new Vector3(40, 0, -.85f), new Vector3(47, 0, -.85f), FieldStop
    };
    static readonly Vector3 TrialPlayerOffset = new Vector3(1.52f,0,-.20f);
    static ArkanmeisterinArrival instance;
    public static bool Arrived { get; private set; }
    public static bool Greeted { get; private set; }
    public static bool IsPresenting => instance != null && instance.presenting;
    public static string ObjectiveText => IsPresenting ? "Burgtor · Die Kutsche fährt vor"
        : Arrived && !Greeted ? "Ankunft · Begrüße die Arkanmeisterin an der Kutsche" : null;

    public string Label => !Greeted ? "Arkanmeisterin begrüßen"
        : !CourtyardVesselTrial.Completed ? "Gefäßprobe beginnen" : "Mit der Arkanmeisterin sprechen";
    public Vector3 GetTransform() => transform.position + Vector3.up*.8f;

    bool presenting;
    Coroutine carriageDeparture;
    PlayerController steppingPlayer;
    Transform player;
    ArrivalCarriage carriage;
    CourtyardCamera follow;
    CourtyardGuard guard;
    SphereCollider greeting;
    SpriteRenderer figure, passenger;
    Canvas objective, arrivalTitle;
    Sprite arrivalTitleShade;
    const string RevealSound = "IntroFoley/arkanmeisterin-reveal";
    const float GreetingMusicGain = .42f;
    // Reach, hold the gold rim, then follow the cloth back to the collar.
    static readonly float[] HoodFrameDurations =
        { .12f, .13f, .13f, .13f, .37f, .17f, .22f, .25f, .23f, .23f };
    bool objectiveWasEnabled;
    PaperCharacterVisual elianVisual;
    SoundManager arrivalAudio;
    CourtyardVesselTrial vesselTrial;
    CourtyardStarterSummoning starterSummoning;
    Coroutine returnMusic;
    AudioSource arrivalTheme;
    float previousMusicDucking;
    float travelled;
    Sprite hoodUp, hoodDown, hoodedPortrait, unveiledPortrait;
    Sprite[] hoodFrames;
    int greetingLinesRemaining;
    bool greetingActive, hoodShotActive, walkingToTrial;
    float walkProgress;
    Transform sweat;
    Mesh drop;
    Material sweatInk;

    internal static void Install(Transform parent)
    {
        if (instance != null) return;
        var root = new GameObject(ArkanmeisterinConversation.Speaker,typeof(SphereCollider));
        root.transform.SetParent(parent,true);
        root.transform.position = CourtyardGuard.Grounded(GreetingSpot);
        instance = root.AddComponent<ArkanmeisterinArrival>();
        instance.greeting = root.GetComponent<SphereCollider>();
        instance.greeting.isTrigger = true;
        instance.greeting.center = Vector3.up*.85f;
        instance.greeting.radius = .48f;
        instance.greeting.enabled = false;
        if (Arrived && instance.BuildStage())
        {
            instance.ParkAndReveal();
            if (Greeted) instance.carriage.ParkAtStable(instance.StableParkingSpot());
        }
    }

    internal static void ResetProgress()
    {
        Arrived = Greeted = false;
        CourtyardVesselTrial.ResetProgress();
        CourtyardStarterSummoning.ResetProgress();
        CourtyardStarterMeetings.ResetProgress();
    }

    internal static bool AtGate(DialogueBoxController dialogue)
    {
        if (instance == null || !instance.isActiveAndEnabled) return false;
        if (Arrived)
        {
            dialogue.ShowMessage(Greeted ? "Der Kutscher wartet mit der Kutsche bei Hannes am Stall."
                : "Da steht ihre Kutsche. Ich sollte die Arkanmeisterin begrüßen.",IntroChapter.PlayerName);
            return true;
        }
        if (instance.presenting) return true;
        instance.player = GameObject.FindWithTag("Player")?.transform;
        instance.follow = FindFirstObjectByType<CourtyardCamera>();
        instance.guard = FindFirstObjectByType<CourtyardGuard>();
        if (instance.follow == null || Camera.main == null || instance.guard == null || !instance.BuildStage()) return false;
        instance.presenting = true;
        instance.StartCoroutine(instance.PlayArrival());
        return true;
    }

    private bool BuildStage()
    {
        if (carriage != null) return true;
        var art = Resources.Load<Texture2D>("Arkanmeisterin poses");
        if (art == null) return false;
        float half = art.width*.5f;
        hoodUp = CreateFigureSprite(art,true);
        hoodDown = CreateFigureSprite(art,false);
        hoodFrames = CreateHoodFrames();
        hoodedPortrait = CreatePortrait(art,0);
        unveiledPortrait = CreatePortrait(art,half);
        var paper = new GameObject("Arkanmeisterin paper figure",typeof(SpriteRenderer));
        paper.transform.SetParent(transform,false);
        figure = paper.GetComponent<SpriteRenderer>();
        PresentHoodPose(true);
        figure.enabled = false;
        PaperGroundShadow.Attach(gameObject,figure);
        var coach = new GameObject("Arkanmeisterin carriage");
        coach.transform.SetParent(transform.parent,true);
        coach.transform.position = RoadPosition(ApproachX);
        carriage = coach.AddComponent<ArrivalCarriage>();
        carriage.Build();
        carriage.PresentTravel(0,false);
        var window = new GameObject("Hood behind carriage window",typeof(SpriteRenderer));
        window.transform.SetParent(coach.transform,false);
        window.transform.localPosition = new Vector3(.08f,2.09f,-.52f);
        window.transform.localScale = Vector3.one * (.90f*hoodedPortrait.pixelsPerUnit/hoodUp.pixelsPerUnit);
        passenger = window.GetComponent<SpriteRenderer>();
        passenger.sprite = hoodedPortrait;
        passenger.color = new Color(.69f,.65f,.74f);
        return true;
    }

    internal static Sprite CreateFigureSprite(Texture2D art, bool hoodRaised)
    {
        float half = art.width*.5f;
        var cell = new Rect(hoodRaised ? 0 : half,0,half,art.height);
        var pivot = new Vector2(hoodRaised ? .539f : .491f,.014f);
        return Sprite.Create(art,cell,pivot,430);
    }

    private Sprite[] CreateHoodFrames()
    {
        const int columns = 4, rows = 3;
        var atlas = Resources.Load<Texture2D>("Arkanmeisterin hood animation");
        if (atlas == null) return System.Array.Empty<Sprite>();

        int width = atlas.width / columns;
        int height = atlas.height / rows;
        float pixelsPerUnit = hoodUp.pixelsPerUnit * width / hoodUp.rect.width;
        var pivot = new Vector2(hoodUp.pivot.x / hoodUp.rect.width, hoodUp.pivot.y / hoodUp.rect.height);
        var frames = new Sprite[HoodFrameDurations.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            var cell = new Rect((i % columns) * width, (rows - 1 - i / columns) * height, width, height);
            frames[i] = Sprite.Create(atlas,cell,pivot,pixelsPerUnit);
            frames[i].name = $"Hood pull {i + 1:00}";
        }
        return frames;
    }

    private static Sprite CreatePortrait(Texture2D art, float cellStart)
    {
        float half = art.width*.5f;
        var crop = new Rect(cellStart+half*.17f,art.height*.65f,half*.59f,art.height*.35f);
        return Sprite.Create(art,crop,new Vector2(.5f,.5f));
    }

    private IEnumerator PlayArrival()
    {
        travelled = 0;
        carriage.transform.SetPositionAndRotation(RoadPosition(ApproachX),Quaternion.identity);
        carriage.BeginApproach();
        carriage.FaceAlongRoad = true;
        figure.enabled = false;
        passenger.enabled = true;
        elianVisual = player != null ? player.GetComponent<PaperCharacterVisual>() : null;
        TakeMusicMix();
        if (arrivalAudio != null) StartCoroutine(FadeCourtyardMusic());
        yield return StepAside();

        HideObjective();

        CurrentShot = Shot.Rise;
        yield return GlideTo(RoadViewPosition,Quaternion.LookRotation(RoadViewTarget-RoadViewPosition),47,1.5f);
        yield return TravelTo(64.5f,2.4f,Shot.Road);
        // Change the paper figures' orientation at the cut, outside the wheel framing.
        carriage.FaceAlongRoad = false;
        yield return TravelTo(57.5f,1.4f,Shot.Wheel);
        yield return TravelTo(47.5f,2.0f,Shot.Side);
        yield return TravelTo(38.65f,1.77f,Shot.Gate);

        yield return TravelTo(Parked.x,1.0f,Shot.Coachman);
        carriage.PresentTravel(travelled,false);
        carriage.Park();
        yield return new WaitForSeconds(.65f);

        CurrentShot = Shot.Guard;
        guard.BeginArrivalRun(new Vector3(30.8f,0,-2.65f),new Vector3(36.49f,0,-2.61f));
        CreateSweat();
        SetShot(new Vector3(35.25f,1.38f,-1.45f),Quaternion.Euler(12,8,0),10.4f,43);
        yield return new WaitUntil(() => guard == null || !guard.IsArrivalRunning);
        yield return new WaitForSeconds(.24f);
        var dialogue = DialogueBoxController.instance;
        if (dialogue != null)
        {
            dialogue.ShowMessage(ArkanmeisterinConversation.GuardArrival,CourtyardGuard.Speaker);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
        }
        guard.BeginArrivalRun(guard.transform.position,new Vector3(35.03f,0,-3.70f));
        StartArrivalTheme();
        if (arrivalTheme != null) StartCoroutine(FadeInArrivalTheme());
        for (float t = 0; t < .70f; t += Time.deltaTime)
        {
            carriage.PresentDoor(Mathf.SmoothStep(0,1,t/.70f));
            yield return null;
        }
        carriage.PresentDoor(1);
        yield return new WaitUntil(() => guard == null || !guard.IsArrivalRunning);
        if (guard != null) guard.FaceArrivalGuest(transform.position);
        yield return new WaitForSeconds(.32f);

        // Cut between two planted marks and keep the held figure behind the shoe framing.
        sweat.gameObject.SetActive(false);
        passenger.enabled = false;
        CurrentShot = Shot.Step;
        var step = carriage.transform.TransformPoint(new Vector3(.30f,.53f,-1.23f));
        figure.transform.position = step;
        figure.enabled = true;
        SetShot(step+Vector3.up*.17f,Quaternion.Euler(7,-13,0),2.45f,33);
        SoundManager.instance?.PlayEffect("IntroFoley/footstep_wood_001",.42f);
        yield return new WaitForSeconds(1.15f);

        CurrentShot = Shot.Landing;
        elianVisual?.SetArrivalReaction(transform);
        figure.transform.localPosition = Vector3.zero;
        SetShot(transform.position+Vector3.up*.38f,Quaternion.Euler(6,-10,0),2.45f,34);
        SoundManager.instance?.PlayEffect("IntroFoley/footstep_grass_001",.44f);
        yield return new WaitForSeconds(1.05f);

        CurrentShot = Shot.Reveal;
        ParkAndReveal();
        yield return PresentArrivalTitle();
        yield return new WaitForSeconds(.45f);

        if (player != null)
        {
            CurrentShot = Shot.Reaction;
            SetGreetingShot();
            yield return new WaitForSeconds(1.55f);
        }
        yield return FadeToGreetingMix();
        elianVisual?.SetArrivalReaction(null);
        yield return ApproachForGreeting();

        if (dialogue != null && !dialogue.IsOpen)
        {
            CurrentShot = Shot.Greeting;
            BeginGreeting(dialogue);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (Greeted && dialogue != null) yield return PlayVesselTrial(dialogue);
        }
        FinishPresentation();
    }

    private void HideObjective()
    {
        if (objective != null) return;
        objective = GameObject.Find("Demo objective")?.GetComponent<Canvas>();
        if (objective != null) { objectiveWasEnabled = objective.enabled; objective.enabled = false; }
    }

    private void RestoreExplorationView()
    {
        if (objective != null)
        {
            objective.enabled = objectiveWasEnabled;
            objective = null;
        }
        follow?.ClearScenicView();
    }

    private IEnumerator PresentArrivalTitle()
    {
        var angle = Quaternion.Euler(6,-10,0);
        var titleFocus = transform.position+Vector3.up*1.23f+angle*Vector3.right*.60f;
        SetShot(titleFocus,angle,5.15f,27);
        yield return new WaitForSeconds(1.65f);

        var title = BuildArrivalTitle();
        SoundManager.instance?.PlayEffect(RevealSound,.77f);
        const float fadeIn = .80f, fadeOut = .26f;
        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            title.alpha = Mathf.SmoothStep(0,1,t/fadeIn);
            yield return null;
        }
        title.alpha = 1;
        yield return new WaitForSeconds(3.95f);
        for (float t = 0; t < fadeOut; t += Time.deltaTime)
        {
            title.alpha = Mathf.SmoothStep(1,0,t/fadeOut);
            yield return null;
        }
        arrivalTitle.enabled = false;
    }

    private void TakeMusicMix()
    {
        if (arrivalAudio != null) return;
        arrivalAudio = SoundManager.instance;
        if (arrivalAudio != null) previousMusicDucking = arrivalAudio.MusicDucking;
    }

    private IEnumerator FadeCourtyardMusic()
    {
        const float duration = 4.2f;
        for (float t = 0; t < duration && arrivalAudio != null; t += Time.deltaTime)
        {
            arrivalAudio.SetMusicDucking(previousMusicDucking*(1-Mathf.SmoothStep(0,1,t/duration)));
            yield return null;
        }
        if (arrivalAudio != null) arrivalAudio.SetMusicDucking(0);
    }

    private void StartArrivalTheme()
    {
        var track = Resources.Load<AudioClip>("Audio/arkanmeisterin-theme");
        if (track == null) return;
        TakeMusicMix();
        if (arrivalAudio == null) return;
        arrivalTheme = gameObject.AddComponent<AudioSource>();
        arrivalTheme.playOnAwake = false;
        arrivalTheme.spatialBlend = 0;
        arrivalTheme.loop = true;
        arrivalTheme.clip = track;
        arrivalTheme.volume = 0;
        arrivalTheme.Play();
    }

    private IEnumerator FadeInArrivalTheme()
    {
        const float duration = 3.2f;
        for (float t = 0; t < duration && arrivalAudio != null && arrivalTheme != null; t += Time.deltaTime)
        {
            arrivalTheme.volume = arrivalAudio.musicVolume*Mathf.SmoothStep(0,1,t/duration);
            yield return null;
        }
        if (arrivalAudio != null && arrivalTheme != null) arrivalTheme.volume = arrivalAudio.musicVolume;
    }

    private IEnumerator FadeToGreetingMix()
    {
        const float duration = 1.05f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            PresentGreetingMix(Mathf.SmoothStep(0,1,t/duration));
            yield return null;
        }
        PresentGreetingMix(1);
    }

    private void PresentGreetingMix(float blend)
    {
        if (arrivalAudio == null) return;
        if (arrivalTheme == null)
        {
            if (blend >= 1) StopArrivalMusic();
            return;
        }
        arrivalAudio.SetMusicDucking(0);
        arrivalTheme.volume = arrivalAudio.musicVolume*Mathf.Lerp(1,GreetingMusicGain,blend);
    }

    private IEnumerator FadeBackToCourtyard()
    {
        const float duration = 1.2f;
        for (float t = 0; t < duration && arrivalAudio != null; t += Time.unscaledDeltaTime)
        {
            float blend = Mathf.SmoothStep(0,1,t/duration);
            arrivalAudio.SetMusicDucking(previousMusicDucking*blend);
            if (arrivalTheme != null) arrivalTheme.volume = arrivalAudio.musicVolume*GreetingMusicGain*(1-blend);
            yield return null;
        }
        StopArrivalMusic();
        returnMusic = null;
    }

    private void StopArrivalMusic()
    {
        if (arrivalAudio != null) arrivalAudio.SetMusicDucking(previousMusicDucking);
        arrivalAudio = null;
        if (arrivalTheme != null)
        {
            arrivalTheme.Stop();
            Destroy(arrivalTheme);
            arrivalTheme = null;
        }
    }

    private Vector3 StableParkingSpot()
    {
        var stable = GameObject.Find("Stable")?.transform;
        return stable != null ? stable.position + new Vector3(-2.1f, 0, -7.8f) : new Vector3(-13.6f, 0, -2.8f);
    }

    private IEnumerator SendCarriageToStable()
    {
        yield return new WaitForSeconds(1.2f);
        var route = new[] {
            carriage.transform.position, new Vector3(30f, 0, -4.15f), new Vector3(20f, 0, -4.0f),
            new Vector3(8f, 0, -3.9f), new Vector3(-4f, 0, -6.15f), new Vector3(-9.5f, 0, -5.5f),
            StableParkingSpot()
        };
        yield return carriage.DriveToStable(route);
        carriageDeparture = null;
    }

    private static Vector3 RoadPosition(float x) => new Vector3(x,Parked.y,CourtyardEastFieldStyle.PathZ(x));

    private IEnumerator TravelTo(float x,float duration,Shot shot)
    {
        CurrentShot = shot;
        float start = carriage.transform.position.x;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = t/duration;
            PlaceCarriage(Mathf.Lerp(start,x,shot == Shot.Coachman ? 1-(1-u)*(1-u) : u));
            FrameTravel(shot,u);
            yield return null;
        }
        PlaceCarriage(x);
        FrameTravel(shot,1);
    }

    private void PlaceCarriage(float x)
    {
        var point = RoadPosition(x);
        travelled += Vector3.Distance(carriage.transform.position,point);
        float yaw = -Mathf.Atan2(RoadPosition(x+.25f).z-RoadPosition(x-.25f).z,.50f)*Mathf.Rad2Deg;
        carriage.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
        carriage.PresentTravel(travelled,true);
    }

    private void FrameTravel(Shot shot,float progress)
    {
        var coach = carriage.transform;
        switch (shot)
        {
            case Shot.Wheel:
                SetShot(coach.TransformPoint(new Vector3(1.18f,.62f,-1.03f)),Quaternion.Euler(3,-7,0),2.65f,39);
                break;
            case Shot.Side:
                SetShot(coach.TransformPoint(new Vector3(-1.45f,1.47f,-.48f)),Quaternion.Euler(5,5,0),
                    Mathf.Lerp(7.55f,7.10f,progress),43);
                break;
            case Shot.Gate:
                SetShot(new Vector3(42.2f,1.45f,-.75f),Quaternion.Euler(18,26,0),13.4f);
                break;
            case Shot.Coachman:
                SetShot(coach.TransformPoint(new Vector3(-1.90f,2.19f,-.55f)),Quaternion.Euler(4,-9,0),3.95f,36);
                break;
        }
    }

    private IEnumerator GlideTo(Vector3 position,Quaternion rotation,float fov,float duration)
    {
        var camera = Camera.main;
        var start = camera.transform.position;
        var angle = camera.transform.rotation;
        float lens = camera.fieldOfView;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = Mathf.SmoothStep(0,1,t/duration);
            follow.SetScenicView(Vector3.Lerp(start,position,u),Quaternion.Slerp(angle,rotation,u),Mathf.Lerp(lens,fov,u));
            yield return null;
        }
        follow.SetScenicView(position,rotation,fov);
    }

    private CanvasGroup BuildArrivalTitle()
    {
        arrivalTitle = CreateCinematicCanvas("Arkanmeisterin arrival title");
        var center = new Vector2(.65f,.56f);
        var title = PaperUIFactory.Rect("Arrival title lettering",arrivalTitle.transform,center,center,
            new Vector2(-234,-67),new Vector2(234,67));
        arrivalTitleShade = CreateTitleShade();
        PaperUIFactory.Image("Arrival title soft shadow",title,arrivalTitleShade,
            Vector2.zero,Vector2.one,new Vector2(-110,-58),new Vector2(110,58),false);
        var group = title.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        var name = PaperUIFactory.Text("Arrival title name",title,"Arkanmeisterin",52,
            Vector2.zero,Vector2.one,new Vector2(0,61),new Vector2(0,-7),TextAlignmentOptions.Center);
        name.fontStyle = FontStyles.SmallCaps;
        name.characterSpacing = 3;
        name.color = new Color(1,.85f,.53f);
        name.outlineColor = new Color32(44,23,57,255);
        name.outlineWidth = .21f;

        var mid = new Vector2(.5f,.5f);
        var rule = PaperUIFactory.Image("Arrival title gold rule",title,null,
            mid,mid,new Vector2(-137,-3),new Vector2(137,-1),false);
        rule.color = new Color(.96f,.76f,.41f,.94f);
        var jewel = PaperUIFactory.Image("Arrival title diamond",title,null,
            mid,mid,new Vector2(-4,-6),new Vector2(4,2),false);
        jewel.color = new Color(1,.88f,.60f);
        jewel.rectTransform.localRotation = Quaternion.Euler(0,0,45);

        var purpose = PaperUIFactory.Text("Arrival title purpose",title,"Gekommen, um Elian zu prüfen.",26,
            Vector2.zero,Vector2.one,new Vector2(0,9),new Vector2(0,-73),TextAlignmentOptions.Center);
        purpose.color = new Color(1,.94f,.81f);
        purpose.outlineColor = new Color32(42,23,47,255);
        purpose.outlineWidth = .25f;
        return group;
    }

    private static Sprite CreateTitleShade()
    {
        const int width = 80, height = 40;
        var texture = new Texture2D(width,height,TextureFormat.RGBA32,false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[width*height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float across = (2f*x/(width-1))-1;
            float vertical = (2f*y/(height-1))-1;
            float edge = Mathf.Sqrt(across*across+vertical*vertical);
            byte alpha = (byte)Mathf.RoundToInt(179*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,edge))));
            pixels[y*width+x] = new Color32(25,16,36,alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f));
    }

    private Canvas CreateCinematicCanvas(string name)
    {
        var canvas = new GameObject(name,typeof(Canvas)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform,false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        PaperUIFactory.Scale(canvas);
        return canvas;
    }

    private IEnumerator StepAside()
    {
        if (player == null || Mathf.Abs(player.position.z - Parked.z) > 1.55f) yield break;
        var body = player.GetComponent<Rigidbody>();
        steppingPlayer = player.GetComponent<PlayerController>();
        if (body == null || steppingPlayer == null) yield break;
        Vector3 inside = body.position;
        inside.x = Mathf.Min(inside.x,41.1f);
        steppingPlayer.SetScriptedMovement(true);
        yield return WalkToMark(body,inside);
        yield return WalkToMark(body,new Vector3(inside.x,inside.y,-3.22f));
        steppingPlayer.SetScriptedMovement(false);
        steppingPlayer = null;
    }

    private IEnumerator ApproachForGreeting()
    {
        if (player == null) yield break;
        var body = player.GetComponent<Rigidbody>();
        steppingPlayer = player.GetComponent<PlayerController>();
        if (body == null || steppingPlayer == null) yield break;

        CurrentShot = Shot.Approach;
        steppingPlayer.SetScriptedMovement(true);
        if (body.position.z > -2.65f)
        {
            // Pass the east end of the carriage before crossing to its courtyard side.
            var corner = new Vector3(41.1f,0,body.position.z);
            yield return WalkToMark(body,corner,2.5f);
            corner.z = -3.22f;
            yield return WalkToMark(body,corner,2.5f);
        }
        yield return WalkToMark(body,transform.position+new Vector3(1.55f,0,-.15f),2.5f);
        steppingPlayer.SetScriptedMovement(false);
        steppingPlayer = null;
        SetGreetingShot();
        elianVisual?.FaceWorldDirection(transform.position-player.position);
    }

    private static IEnumerator WalkToMark(Rigidbody body, Vector3 mark, float speed=3.5f)
    {
        while (body != null)
        {
            var position = body.position;
            mark.y = position.y;
            if ((mark-position).sqrMagnitude <= .0004f) yield break;
            Vector3 next = Vector3.MoveTowards(position,mark,speed*Time.deltaTime);
            body.position = next;
            body.transform.position = next;
            yield return null;
        }
    }

    private void ParkAndReveal()
    {
        carriage.transform.SetPositionAndRotation(Parked,Quaternion.identity);
        carriage.PresentDoor(1);
        passenger.enabled = false;
        figure.transform.localPosition = Vector3.zero;
        carriage.Park();
        PresentHoodPose(!Greeted);
        figure.enabled = true;
        greeting.enabled = true;
        Arrived = true;
        if (CourtyardStarterMeetings.AtTrialField) transform.position = CourtyardGuard.Grounded(FieldStop);
        else if (CourtyardVesselTrial.HasVessel) transform.position = CourtyardGuard.Grounded(TrialSpot);
    }

    private void SetShot(Vector3 point, Quaternion rotation, float distance, float fov=48)
        => follow.SetScenicView(point-rotation*Vector3.forward*distance,rotation,fov);

    // The dialogue switches between two held drawings on the same world mark.
    private void PresentHoodPose(bool raised) => figure.sprite = raised ? hoodUp : hoodDown;

    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null) return;
        if (CurrentShot == Shot.Approach) SetGreetingShot();
        if (passenger != null && passenger.enabled) passenger.transform.rotation = camera.transform.rotation;
        if (figure != null)
        {
            figure.transform.rotation = camera.transform.rotation;
            if (player == null) player = GameObject.FindWithTag("Player")?.transform;
            if (player != null)
                figure.flipX = walkingToTrial || Vector3.Dot(player.position-transform.position,camera.transform.right) < 0;
        }
        if (sweat != null && sweat.gameObject.activeSelf && guard != null)
        {
            sweat.rotation = camera.transform.rotation;
            float phase = Mathf.Repeat(Time.time*2.7f,1);
            var offset = Vector3.up*(2.17f-.14f*phase) + camera.transform.right*(guard.FacingLeft?.37f:-.37f);
            sweat.position = guard.transform.position + offset*CourtyardGuardVisual.FigureScale - camera.transform.forward*.15f;
        }
    }

    private void CreateSweat()
    {
        sweat = new GameObject("Guard sweat drops").transform;
        sweat.SetParent(transform,false);
        drop = new Mesh { name = "Sweat drop outline" };
        drop.vertices = new[]{new Vector3(0,.16f),new Vector3(-.062f,.025f),new Vector3(-.053f,-.027f),
            new Vector3(0,-.054f),new Vector3(.053f,-.027f),new Vector3(.062f,.025f)};
        drop.triangles = new[]{0,1,2,0,2,3,0,3,4,0,4,5};
        drop.RecalculateBounds();
        sweatInk = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        sweatInk.SetColor("_BaseColor",new Color(.56f,.87f,1));
        sweatInk.SetFloat("_Cull",0);
        foreach (float offset in new[]{0f,.16f})
        {
            var shape = new GameObject("Sweat bead",typeof(MeshFilter),typeof(MeshRenderer));
            shape.transform.SetParent(sweat,false);
            shape.transform.localPosition = new Vector3(offset,offset*.55f,0);
            shape.transform.localScale = Vector3.one * (offset>0?.70f:1);
            shape.GetComponent<MeshFilter>().sharedMesh = drop;
            shape.GetComponent<MeshRenderer>().sharedMaterial = sweatInk;
        }
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        if (!Arrived || IsPresenting || SceneLoadManager.IsTransitioning || dialogue == null || dialogue.IsOpen) return;
        if (!Greeted) { BeginGreeting(dialogue); return; }
        if (CourtyardVesselTrial.Completed)
        {
            if (CourtyardStarterSummoning.Completed)
            {
                if (CourtyardStarterMeetings.PartnerBound && CourtyardStarterMeetings.AtTrialField)
                {
                    player = GameObject.FindWithTag("Player")?.transform;
                    follow = FindFirstObjectByType<CourtyardCamera>();
                    if (player == null || follow == null || Camera.main == null) return;
                    presenting = true;
                    HideObjective();
                    StartCoroutine(CompleteReturnToCourtyard(dialogue));
                    return;
                }
                if (BeginPartnerChoice(false)) return;
                if (CourtyardStarterMeetings.ReadyForBriefing && !CourtyardStarterMeetings.FieldTrialWon
                    && !CourtyardStarterMeetings.AtTrialField)
                {
                    player = GameObject.FindWithTag("Player")?.transform;
                    follow = FindFirstObjectByType<CourtyardCamera>();
                    if (player == null || follow == null || Camera.main == null) return;
                    presenting = true;
                    CurrentShot = Shot.FieldWalk;
                    HideObjective();
                    StartCoroutine(LeadToField(dialogue));
                }
                else ArkanmeisterinConversation.SpeakAgain(dialogue,unveiledPortrait);
                return;
            }
            player = GameObject.FindWithTag("Player")?.transform;
            follow = FindFirstObjectByType<CourtyardCamera>();
            if (player == null || follow == null) return;
            presenting = true;
            HideObjective();
            StartCoroutine(ResumeStarterSummoning(dialogue));
            return;
        }
        presenting = true;
        HideObjective();
        SetGreetingShot();
        StartCoroutine(ResumeVesselTrial(dialogue));
    }

    internal bool BeginPartnerChoice(bool congratulate)
    {
        var dialogue = DialogueBoxController.instance;
        if (presenting || !CourtyardStarterMeetings.FieldTrialWon || CourtyardStarterMeetings.PartnerBound
            || dialogue == null || dialogue.IsOpen) return false;
        player = GameObject.FindWithTag("Player")?.transform;
        follow = FindFirstObjectByType<CourtyardCamera>();
        if (player == null || follow == null || Camera.main == null) return false;
        presenting = true;
        HideObjective();
        StartCoroutine(ChoosePartner(dialogue, congratulate));
        return true;
    }

    private IEnumerator ChoosePartner(DialogueBoxController dialogue, bool congratulate)
    {
        var choice = gameObject.AddComponent<CourtyardPartnerChoice>();
        try
        {
            yield return choice.Play(player, follow, dialogue, congratulate, figure, unveiledPortrait);
            if (CourtyardStarterMeetings.PartnerBound && CourtyardStarterMeetings.AtTrialField)
                yield return ReturnToCourtyard(dialogue);
        }
        finally
        {
            if (choice != null) Destroy(choice);
            FinishPresentation();
        }
    }

    private IEnumerator LeadToField(DialogueBoxController dialogue)
    {
        try
        {
            if (!CourtyardStarterMeetings.RecalledForTrial)
            {
                ArkanmeisterinConversation.RecallBeforeField(dialogue, unveiledPortrait);
                yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
                if (dialogue == null) yield break;
                yield return RecallCourtyardStarters();
            }

            ArkanmeisterinConversation.WalkToField(dialogue, unveiledPortrait,
                () => new WaitUntil(() => transform.position.x >= 47));
            yield return WalkFieldRoute(false);
            CourtyardStarterMeetings.AtTrialField = true;
            elianVisual?.FaceWorldDirection(transform.position - player.position);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            if (dialogue == null) yield break;
            ArkanmeisterinConversation.SpeakAgain(dialogue, unveiledPortrait);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
        }
        finally { FinishPresentation(); }
    }

    private IEnumerator CompleteReturnToCourtyard(DialogueBoxController dialogue)
    {
        try { yield return ReturnToCourtyard(dialogue); }
        finally { FinishPresentation(); }
    }

    private IEnumerator ReturnToCourtyard(DialogueBoxController dialogue)
    {
        CurrentShot = Shot.FieldWalk;
        follow.ClearScenicView();
        ArkanmeisterinConversation.WalkBackToCourtyard(dialogue, unveiledPortrait,
            CourtyardStarterMeetings.ChosenPartner.Value,
            () => new WaitUntil(() => transform.position.x <= 51),
            () => new WaitUntil(() => transform.position.x <= 35));
        yield return WalkFieldRoute(true);
        CourtyardStarterMeetings.AtTrialField = false;
        elianVisual?.FaceWorldDirection(transform.position - player.position);
        yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
    }

    private IEnumerator WalkFieldRoute(bool returning)
    {
        var body = player.GetComponent<Rigidbody>();
        if (body == null) yield break;
        float footOffset = Mathf.Clamp(body.position.y - CourtyardGuard.Grounded(body.position).y, 0, 1.5f);
        var camera = Camera.main;
        var cameraStart = camera.transform.position;
        var angleStart = camera.transform.rotation;
        float lens = camera.fieldOfView;
        var followerOffset = body.position - transform.position;
        followerOffset.y = 0;
        var besideOffset = new Vector3(returning ? 1.10f : -1.10f, 0, -.48f);
        steppingPlayer = player.GetComponent<PlayerController>();
        elianVisual = player.GetComponent<PaperCharacterVisual>();
        steppingPlayer?.SetScriptedMovement(true);
        walkingToTrial = true;
        float time = 0;
        int first = returning ? FieldWaypoints.Length - 2 : 0;
        int step = returning ? -1 : 1;
        for (int i = first; i >= 0 && i < FieldWaypoints.Length; i += step)
        {
            var mark = FieldWaypoints[i];
            if (returning && i == 1) mark.z = .85f;
            var start = transform.position;
            start.y = 0;
            float duration = Vector3.Distance(start, mark) / 2.65f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                time += Time.deltaTime;
                Vector3 point = Vector3.Lerp(start, mark, elapsed / duration);
                float settle = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / 1.1f));
                transform.position = CourtyardGuard.Grounded(point);
                Vector3 beside = point + Vector3.Lerp(followerOffset, besideOffset, settle);
                body.position = player.position = CourtyardGuard.Grounded(beside) + Vector3.up * footOffset;
                elianVisual?.FaceWorldDirection(mark - point);
                figure.transform.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(time * 7f)) * .032f;
                if (!returning) FrameFieldWalk(point, cameraStart, angleStart, lens, settle);
                yield return null;
            }
        }
        var destination = returning ? FieldWaypoints[0] : FieldStop;
        transform.position = CourtyardGuard.Grounded(destination);
        body.position = player.position = CourtyardGuard.Grounded(destination + besideOffset) + Vector3.up * footOffset;
        body.linearVelocity = Vector3.zero;
        StopTrialWalk();
    }

    private void FrameFieldWalk(Vector3 point, Vector3 cameraStart, Quaternion angleStart, float lens, float blend)
    {
        // Cut inside after the bridge shot so the camera stays clear of the wall.
        float passage = point.x < 39f ? 0f : 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(49, 54, point.x));
        Vector3 side = new Vector3(-2.8f, 3f, -6.6f);
        Vector3 throughGate = new Vector3(6.5f, 2.7f, -.20f);
        Vector3 position = transform.position + Vector3.Lerp(side, throughGate, passage);
        Vector3 focus = (transform.position + player.position) * .5f + Vector3.up * .9f;
        var angle = Quaternion.LookRotation(focus - position);
        follow.SetScenicView(Vector3.Lerp(cameraStart, position, blend), Quaternion.Slerp(angleStart, angle, blend),
            Mathf.Lerp(lens, 43, blend));
    }

    private IEnumerator RecallCourtyardStarters()
    {
        var effect = gameObject.GetComponent<MonsterSummonEffect>() ?? gameObject.AddComponent<MonsterSummonEffect>();
        var actors = new Component[] { FindFirstObjectByType<CourtyardKafla>(), FindFirstObjectByType<CourtyardPanfrie>(),
            FindFirstObjectByType<CourtyardLiora>(), FindFirstObjectByType<CourtyardRauk>() };
        var colors = new[] { new Color(1,.48f,.14f), new Color(.40f,.82f,1),
            new Color(.94f,.48f,.56f), new Color(.59f,.41f,.91f) };
        Vector3 vessel = transform.position + Camera.main.transform.right * .44f + Vector3.up * 1.16f;
        int pending = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i] == null) continue;
            var renderer = actors[i].GetComponentInChildren<SpriteRenderer>();
            var origin = renderer != null ? renderer.bounds.center : actors[i].transform.position + Vector3.up * .55f;
            pending++;
            StartCoroutine(Recall(origin, colors[i]));
        }
        while (pending > 0) yield return null;
        CourtyardStarterMeetings.RecalledForTrial = true;

        IEnumerator Recall(Vector3 origin, Color color)
        {
            yield return effect.PlayRecall(origin, vessel, color, null, null);
            pending--;
        }
    }

    private IEnumerator ResumeVesselTrial(DialogueBoxController dialogue)
    {
        yield return PlayVesselTrial(dialogue);
        FinishPresentation();
    }

    private IEnumerator PlayVesselTrial(DialogueBoxController dialogue)
    {
        if (returnMusic != null)
        {
            StopCoroutine(returnMusic);
            returnMusic = null;
        }
        if (arrivalTheme == null) StartArrivalTheme();
        PresentGreetingMix(1);
        CurrentShot = Shot.VesselTrial;
        if (player == null || follow == null) yield break;
        yield return WalkToTrial(dialogue);
        if (dialogue == null) yield break;
        if (vesselTrial == null) vesselTrial = gameObject.AddComponent<CourtyardVesselTrial>();
        yield return vesselTrial.Play(dialogue,player,figure,unveiledPortrait,arrivalTheme);
        if (CourtyardVesselTrial.Completed && dialogue != null) yield return PlayStarterSummoning(dialogue);
        if (arrivalAudio != null) returnMusic = StartCoroutine(FadeBackToCourtyard());
    }

    private IEnumerator ResumeStarterSummoning(DialogueBoxController dialogue)
    {
        yield return PlayStarterSummoning(dialogue);
        FinishPresentation();
    }

    private IEnumerator PlayStarterSummoning(DialogueBoxController dialogue)
    {
        CurrentShot = Shot.StarterSummoning;
        if (starterSummoning == null) starterSummoning = gameObject.AddComponent<CourtyardStarterSummoning>();
        yield return starterSummoning.Play(dialogue,player,figure,unveiledPortrait,follow,arrivalTheme);
    }

    private IEnumerator WalkToTrial(DialogueBoxController dialogue)
    {
        var body = player.GetComponent<Rigidbody>();
        if (body == null) yield break;
        var start = transform.position;
        var end = CourtyardGuard.Grounded(TrialSpot);
        var playerStart = body.position;
        float footOffset = Mathf.Clamp(playerStart.y-CourtyardGuard.Grounded(playerStart).y,0,1.5f);
        float span = Vector3.Distance(start,end);
        float duration = Mathf.Max(.65f,span/2.25f);
        var camera = Camera.main;
        Vector3 cameraStart = camera.transform.position;
        Quaternion angleStart = camera.transform.rotation;
        float lensStart = camera.fieldOfView;
        steppingPlayer = player.GetComponent<PlayerController>();
        elianVisual = player.GetComponent<PaperCharacterVisual>();
        elianVisual?.SetTrialLookTarget(transform);
        elianVisual?.FaceWorldDirection(transform.position-player.position);
        bool crossCourtyard = span > 1;
        walkProgress = 0;
        if (crossCourtyard)
        {
            ArkanmeisterinConversation.WalkTogether(dialogue,unveiledPortrait,
                () => new WaitUntil(() => walkProgress >= .26f),
                () => new WaitUntil(() => walkProgress >= .62f));
            yield return new WaitForSeconds(.65f);
        }
        walkingToTrial = crossCourtyard;
        steppingPlayer?.SetScriptedMovement(true);

        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            walkProgress = Mathf.Clamp01(elapsed/duration);
            Vector3 point = Vector3.Lerp(start,end,walkProgress);
            if (crossCourtyard)
            {
                // Clear the horse before taking the open lane north of Marta's cottage.
                float pastCarriage = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.31f,.49f,walkProgress));
                float atSquare = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.80f,1,walkProgress));
                point.z = Mathf.Lerp(Mathf.Lerp(start.z,1.95f,pastCarriage),end.z,atSquare);
            }
            transform.position = CourtyardGuard.Grounded(point);
            float settle = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.9f));
            var beside = point+Vector3.Lerp(playerStart-start,TrialPlayerOffset,settle);
            beside = CourtyardGuard.Grounded(beside)+Vector3.up*footOffset;
            body.position = beside;
            player.position = beside;
            figure.transform.localPosition = Vector3.up*Mathf.Abs(Mathf.Sin(elapsed*6.8f))*.034f;
            FrameTrial(cameraStart,angleStart,lensStart,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.25f)));
            yield return null;
        }

        transform.position = end;
        body.position = CourtyardGuard.Grounded(end+TrialPlayerOffset)+Vector3.up*footOffset;
        player.position = body.position;
        walkProgress = 1;
        StopTrialWalk();
        FrameTrial(cameraStart,angleStart,lensStart,1);
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(transform.position-player.position);
        yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
    }

    private void FrameTrial(Vector3 cameraStart, Quaternion angleStart, float lensStart, float blend)
    {
        float audience = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,walkProgress));
        var rotation = Quaternion.Euler(Mathf.Lerp(9,11,audience),-5,0);
        var center = transform.position + new Vector3(Mathf.Lerp(.72f,.60f,audience),1.08f,.58f);
        float audienceDistance = 8.05f*Mathf.Max(1,1.6f/Camera.main.aspect);
        var position = center-rotation*Vector3.forward*Mathf.Lerp(6.7f,audienceDistance,audience);
        follow.SetScenicView(Vector3.Lerp(cameraStart,position,blend),
            Quaternion.Slerp(angleStart,rotation,blend),Mathf.Lerp(lensStart,38,blend));
    }

    private void StopTrialWalk()
    {
        elianVisual?.SetTrialLookTarget(null);
        walkingToTrial = false;
        if (figure != null) figure.transform.localPosition = Vector3.zero;
        if (steppingPlayer != null) steppingPlayer.SetScriptedMovement(false);
        steppingPlayer = null;
    }

    private void BeginGreeting(DialogueBoxController dialogue)
    {
        if (arrivalTheme == null)
        {
            StartArrivalTheme();
            PresentGreetingMix(1);
        }
        greetingActive = true;
        greetingLinesRemaining = ArkanmeisterinConversation.HoodCueLineNumber;
        ArkanmeisterinConversation.Greet(dialogue,hoodedPortrait,unveiledPortrait,PullHoodBack);
    }

    private void OnEnable()
    {
        DialogueBoxController.OnLinePresented += OnGreetingLine;
        DialogueBoxController.OnDialogueEnded += OnGreetingEnded;
    }

    private void OnGreetingLine(string speaker, DialogueEmotion emotion)
    {
        if (greetingLinesRemaining <= 0) return;
        greetingLinesRemaining--;
        if (greetingLinesRemaining == 0) FrameHoodConversation();
    }

    private IEnumerator PullHoodBack()
    {
        // The dialogue owns this coroutine and keeps the previous line visible.
        for (int i = 0; i < hoodFrames.Length; i++)
        {
            if (!greetingActive) yield break;
            figure.sprite = hoodFrames[i];
            yield return new WaitForSeconds(HoodFrameDurations[i]);
        }
        if (!greetingActive) yield break;
        PresentHoodPose(false);
        Greeted = true;
    }

    private bool SetGreetingShot()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (follow == null) follow = FindFirstObjectByType<CourtyardCamera>();
        var camera = Camera.main;
        if (player == null || follow == null || camera == null) return false;
        var span = player.position-transform.position;
        var across = span.x < 0 ? -span : span;
        float yaw = Mathf.Clamp(-Mathf.Atan2(across.z,across.x)*Mathf.Rad2Deg,-46,46);
        var point = transform.position+new Vector3(span.x*.5f,1.12f,span.z*.5f);
        var rotation = Quaternion.Euler(7,yaw,0);
        float distance = Mathf.Max(4.8f,(new Vector2(span.x,span.z).magnitude+1.2f)
            /(2*Mathf.Tan(17*Mathf.Deg2Rad)*Mathf.Max(.7f,camera.aspect)));
        SetShot(point,rotation,distance,34);
        return true;
    }

    private void FrameHoodConversation()
    {
        if (CurrentShot == Shot.Greeting || !SetGreetingShot()) return;
        player.GetComponent<PaperCharacterVisual>()?.FaceWorldDirection(transform.position-player.position);
        HideObjective();
        hoodShotActive = true;
    }

    private void EndHoodConversationShot()
    {
        if (!hoodShotActive) return;
        hoodShotActive = false;
        RestoreExplorationView();
    }

    private void OnGreetingEnded()
    {
        greetingLinesRemaining = 0;
        EndHoodConversationShot();
        if (!greetingActive) return;
        greetingActive = false;
        if (!Greeted) { PresentHoodPose(true); return; }
        if (carriage != null && carriageDeparture == null)
            carriageDeparture = StartCoroutine(SendCarriageToStable());
        if (!presenting && arrivalAudio != null) returnMusic = StartCoroutine(FadeBackToCourtyard());
    }

    private void FinishPresentation()
    {
        if (!presenting) return;
        presenting = false;
        CurrentShot = Shot.None;
        elianVisual?.SetArrivalReaction(null);

        if (arrivalTitle != null) Destroy(arrivalTitle.gameObject);
        if (arrivalTitleShade != null)
        {
            Destroy(arrivalTitleShade.texture);
            Destroy(arrivalTitleShade);
        }
        StopTrialWalk();
        RestoreExplorationView();
        guard?.EndArrivalRun();
        if (sweat != null) Destroy(sweat.gameObject);
        Destroy(drop); Destroy(sweatInk);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        DialogueBoxController.OnLinePresented -= OnGreetingLine;
        DialogueBoxController.OnDialogueEnded -= OnGreetingEnded;
        if (greetingActive && !Greeted) PresentHoodPose(true);
        greetingActive = false;
        greetingLinesRemaining = 0;
        EndHoodConversationShot();
        vesselTrial?.ClearPresentation();
        starterSummoning?.ClearStage();
        returnMusic = null;
        StopArrivalMusic();
        FinishPresentation();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Destroy(hoodUp); Destroy(hoodDown); Destroy(hoodedPortrait); Destroy(unveiledPortrait);
        if (hoodFrames != null)
            foreach (var frame in hoodFrames) Destroy(frame);
    }
}
