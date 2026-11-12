using System.Collections;
using UnityEngine;

internal sealed class CourtyardPartnerRitual : MonoBehaviour
{
    private static readonly Color Gold = new Color(1f, .72f, .29f);
    private static readonly Color Violet = new Color(.75f, .41f, 1f);
    private GameObject stage;
    private Material lightInk;
    private Sprite vesselPose, proudPose, mentorIdle;
    private readonly Sprite[] lift = new Sprite[3];
    private SpriteRenderer mentorFigure, shade, flash;
    private Sprite shadeSprite, haloSprite;
    private Texture2D haloTexture;
    private Material veil;
    private float dim, pulse;
    private SoundManager sound;
    private AudioSource theme, resonance, effects;
    private float previousMusicGain, themeMix;
    private PaperCharacterVisual elian;
    private readonly System.Collections.Generic.List<(Light lamp, SpriteRenderer halo)> glows = new();

    internal IEnumerator Play(Transform player, CourtyardCamera follow, DialogueBoxController dialogue,
        SpriteRenderer mentor, Sprite portrait, Transform[] figures, int selected, CourtyardStarterMeetings.Starter chosen)
    {
        stage = new GameObject("Übertragungsritual");
        stage.transform.SetParent(transform, false);
        try
        {
            var partner = figures[selected];
            mentorFigure = mentor;
            mentorIdle = mentor != null ? mentor.sprite : null;
            var particles = stage.AddComponent<MonsterSummonEffect>();
            var trial = Resources.Load<BattleConfiguration>("Golden Field Trial");
            Color StarterColor(int index) => trial != null && trial.playerTeam != null && index < trial.playerTeam.Length
                && trial.playerTeam[index] != null ? trial.playerTeam[index].GetSummonColor() : Violet;
            Color partnerColor = StarterColor(selected);
            StartAudio();
            StartCoroutine(FadeTheme(1, 2.4f));
            ArkanmeisterinConversation.SpeakAsMentor(dialogue, portrait, "Eure Wahl ist getroffen. Ich rufe die anderen drei zu mir zurück.");
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            Vector3 recallPoint = mentor != null ? mentor.transform.position + Camera.main.transform.right * (mentor.flipX ? -.30f : .30f) + Vector3.up * 1.02f
                : transform.position + Vector3.up;
            for (int i = 0; i < figures.Length; i++)
            {
                if (i == selected) continue;
                var other = figures[i];
                var figure = other.GetComponentInChildren<SpriteRenderer>();
                Vector3 origin = figure != null ? figure.bounds.center : other.position + Vector3.up * .62f;
                yield return particles.PlayRecall(origin, recallPoint, StarterColor(i), null,
                    () => other.gameObject.SetActive(false));
                yield return new WaitForSeconds(.32f);
            }
            yield return new WaitForSeconds(.55f);

            var view = Quaternion.Euler(12, -8, 0);
            var mark = transform.position + new Vector3(1.65f, 0, .46f);
            mark = CourtyardGuard.Grounded(mark);
            elian = player.GetComponent<PaperCharacterVisual>();
            elian?.SetTrialLookTarget(partner);
            yield return ApproachMark(partner, mark, selected >= 2);
            yield return new WaitForSeconds(.30f);

            var center = (player.position + mark) * .5f + Vector3.up * .78f;
            float distance = 8.3f * Mathf.Max(1, 1.6f / Camera.main.aspect);
            yield return FrameRitual(follow, center - view * Vector3.forward * distance, view);
            elian?.FaceWorldDirection(partner.position - player.position);
            var texture = Resources.Load<Texture2D>("Elian vessel trial");
            if (elian != null && texture != null)
            {
                vesselPose = Sprite.Create(texture, new Rect(80, 0, 450, 887), new Vector2(233f / 450, 11f / 887), 440);
                elian.SetVesselTrialPose(vesselPose);
            }

            ArkanmeisterinConversation.SpeakAsMentor(dialogue, portrait, "Haltet Euer Gefäß ruhig, Elian. Ich löse die Bindung an meines. Dann kann Euer Arkan einander finden.");
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);

            lightInk = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sound?.PlayEffect("mantle-rustle", .32f);
            var clip = Resources.Load<AudioClip>("Audio/VesselTrial/resolution");
            BuildAtmosphere();
            var poses = new[] { "Vessel low", "Vessel lift", "Vessel raised" };
            float mentorHeight = mentorIdle != null ? mentorIdle.bounds.size.y : 2.48f;
            for (int i = 0; i < lift.Length; i++)
                lift[i] = CourtyardStarterSummoning.LoadArtwork("StarterSummoning/" +
                    poses[i], mentorHeight, true);
            if (mentor != null && lift[0] != null && lift[1] != null && lift[2] != null)
            {
                mentor.sprite = lift[0];
                yield return new WaitForSeconds(.52f);
                mentor.sprite = lift[1];
                yield return new WaitForSeconds(.30f);
                mentor.sprite = lift[2];
            }

            Vector3 mentorGem = mentor != null && lift[2] != null
                ? mentor.transform.position + view * new Vector3(mentor.flipX ? -.54f : .54f, 1.93f, -.08f)
                : recallPoint;
            Vector3 partnerAura = partner.position + Vector3.up * .72f;
            Vector3 elianGem = elian != null ? elian.VesselTrialOrigin : player.position + Vector3.up;
            var masterLight = MakeLight("Gefäß der Arkanmeisterin", mentorGem, Gold);
            var partnerLight = MakeLight("Aura des Arkmon", partnerAura, partnerColor);
            var elianLight = MakeLight("Elians Gefäß", elianGem, Gold);
            var ring = MakeRing(partner.position);
            var motes = new Light[7];
            for (int i = 0; i < motes.Length; i++)
                motes[i] = MakeLight("Arkanfunke", mentorGem, i % 2 == 0 ? Violet : Gold);
            ring.enabled = true;

            const float gathering = 4.3f;
            for (float elapsed = 0; elapsed < gathering; elapsed += Time.deltaTime)
            {
                float charge = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 2.8f));
                dim = charge * .92f;
                if (resonance != null)
                {
                    resonance.volume = EffectsLevel * .38f * charge;
                    resonance.pitch = Mathf.Lerp(.86f, 1.04f, charge);
                }
                masterLight.intensity = charge * 2.8f;
                partnerLight.intensity = charge * .85f;
                elianLight.intensity = charge * .65f;
                SetRingRadius(ring, Mathf.Lerp(.25f, .92f, charge));
                for (int i = 0; i < motes.Length; i++)
                {
                    float angle = elapsed * 1.08f + Mathf.PI * 2 * i / motes.Length;
                    float radius = Mathf.Lerp(.18f, 1.28f, charge);
                    motes[i].transform.position = Vector3.Lerp(mentorGem, partnerAura, i / 10f)
                        + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle * 1.6f) * .30f, Mathf.Sin(angle) * .55f);
                    motes[i].intensity = charge * (.39f + .20f * Mathf.Sin(angle * 2));
                }
                yield return null;
            }

            if (clip != null) effects.PlayOneShot(clip, EffectsLevel * .75f);
            for (float elapsed = 0; elapsed < 1.65f; elapsed += Time.deltaTime)
            {
                float rise = Mathf.Clamp01(elapsed / 1.65f);
                dim = .92f * (1 - Mathf.SmoothStep(0, 1, rise));
                if (resonance != null) resonance.volume = EffectsLevel * .38f * (1 - rise);
                pulse = Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / .52f)) * .46f;
                masterLight.intensity = 2.8f * (1 - rise);
                elianLight.intensity = 2.8f;
                partnerLight.intensity = 2.25f;
                SetRingRadius(ring, .92f + rise * 1.15f);
                foreach (var mote in motes) mote.intensity *= Mathf.Exp(-Time.deltaTime * 3);
                yield return null;
            }
            dim = pulse = 0;
            if (resonance != null) resonance.Stop();
            ring.enabled = false;
            if (mentor != null && lift[1] != null)
            {
                mentor.sprite = lift[1];
                yield return new WaitForSeconds(.32f);
                mentor.sprite = mentorIdle;
            }
            yield return new WaitForSeconds(.65f);
            StartCoroutine(Fade(partnerLight, 2.25f, 0, 1.65f));
            yield return Fade(elianLight, 2.8f, 0, 1.65f);
            yield return new WaitForSeconds(.45f);
            var proudArt = Resources.Load<Texture2D>("Elian vessel proud");
            if (elian != null && proudArt != null)
            {
                proudPose = Sprite.Create(proudArt, new Rect(0, 0, proudArt.width, proudArt.height),
                    new Vector2(.498f, .060f), proudArt.height / 2.138f);
                elian.SetVesselTrialPose(proudPose, new Vector2(.35f, 1.46f));
                yield return null;
                elianGem = elian.VesselTrialOrigin;
            }


            ArkanmeisterinConversation.SpeakAsMentor(dialogue, portrait, $"Spürt Ihr das? Euer Arkan und {chosen} sind jetzt miteinander verbunden.");
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            dialogue.ShowMessage(PartnerLine(chosen), chosen.ToString());
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            ArkanmeisterinConversation.AwardKeeperMantle(dialogue, portrait, chosen.ToString(), MantleReaction(chosen), RevealMantle);
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            EquipMantle();
            if (elian != null) elianGem = elian.VesselTrialOrigin;

            ArkanmeisterinConversation.SpeakAsMentor(dialogue, portrait, "Jetzt könnt Ihr Euren Partner selbst in das Gefäß zurückrufen.");
            yield return new WaitWhile(() => dialogue != null && dialogue.IsOpen);
            yield return particles.PlayRecall(partnerAura, elianGem, partnerColor, null,
                () => partner.gameObject.SetActive(false));
            CourtyardStarterMeetings.PartnerBound = true;
            yield return FadeTheme(0, 1.45f);
        }
        finally
        {
            Clear();
            Destroy(this);
        }
    }

    private IEnumerator ApproachMark(Transform partner, Vector3 mark, bool initiallyFacesLeft)
    {
        Vector3 start = partner.position;
        Vector3 scale = partner.localScale;
        bool facesLeft = Vector3.Dot(mark - start, Camera.main.transform.right) < 0;
        if (facesLeft != initiallyFacesLeft)
            partner.localScale = new Vector3(-scale.x, scale.y, scale.z);
        float duration = Mathf.Max(.9f, Vector3.Distance(start, mark) / 1.8f);
        float nextStep = .14f;
        int foot = 0;
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            float progress = Mathf.SmoothStep(0, 1, time / duration);
            Vector3 point = CourtyardGuard.Grounded(Vector3.Lerp(start, mark, progress));
            float step = Mathf.Abs(Mathf.Sin((time - .14f) * Mathf.PI / .33f));
            partner.position = point + Vector3.up * (step * .042f * Mathf.Sin(progress * Mathf.PI));
            if (time >= nextStep)
            {
                sound?.PlayEffect(foot++ % 2 == 0 ? "IntroFoley/footstep_grass_000" : "IntroFoley/footstep_grass_001", .30f);
                nextStep += .33f;
            }
            yield return null;
        }
        partner.position = mark;
        partner.localScale = new Vector3(initiallyFacesLeft ? scale.x : -scale.x, scale.y, scale.z);
    }

    private static IEnumerator FrameRitual(CourtyardCamera follow, Vector3 destination, Quaternion view)
    {
        var camera = Camera.main;
        Vector3 origin = camera.transform.position;
        Quaternion orientation = camera.transform.rotation;
        float lens = camera.fieldOfView;
        const float duration = 1.6f;
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            float blend = Mathf.SmoothStep(0, 1, time / duration);
            follow.SetScenicView(Vector3.Lerp(origin, destination, blend),
                Quaternion.Slerp(orientation, view, blend), Mathf.Lerp(lens, 42, blend));
            yield return null;
        }
        follow.SetScenicView(destination, view, 42);
    }

    private float EffectsLevel => sound != null ? sound.effectsVolume : .55f;

    private void StartAudio()
    {
        sound = SoundManager.instance;
        if (sound != null) previousMusicGain = sound.MusicDucking;
        theme = StartLoop("Theme der Arkanmeisterin", "Audio/arkanmeisterin-theme");
        resonance = StartLoop("Arkanresonanz", "Audio/VesselTrial/resonance");
        effects = stage.AddComponent<AudioSource>();
    }

    private AudioSource StartLoop(string label, string resource)
    {
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null) return null;
        var source = new GameObject(label, typeof(AudioSource)).GetComponent<AudioSource>();
        source.transform.SetParent(stage.transform, false);
        source.clip = clip;
        source.loop = true;
        source.volume = 0;
        source.Play();
        return source;
    }

    private IEnumerator FadeTheme(float target, float duration)
    {
        if (theme == null) yield break;
        float start = themeMix;
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            themeMix = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, time / duration));
            yield return null;
        }
        themeMix = target;
    }

    private Light MakeLight(string name, Vector3 position, Color color)
    {
        var lamp = new GameObject(name, typeof(Light)).GetComponent<Light>();
        lamp.transform.SetParent(stage.transform, false);
        lamp.transform.position = position;
        lamp.type = LightType.Point;
        lamp.color = color;
        lamp.range = 3.6f;
        lamp.intensity = 0;
        var halo = new GameObject("Magisches Licht", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        halo.transform.SetParent(stage.transform, false);
        halo.sprite = haloSprite;
        halo.sortingOrder = 19;
        halo.color = Color.clear;
        glows.Add((lamp, halo));        return lamp;
    }

    private LineRenderer MakeRing(Vector3 ground)
    {
        var ring = new GameObject("Runenring des Arkanbunds", typeof(LineRenderer)).GetComponent<LineRenderer>();
        ring.transform.SetParent(stage.transform, false);
        ring.transform.position = ground + Vector3.up * .035f;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;
        ring.widthMultiplier = .037f;
        ring.sharedMaterial = lightInk;
        ring.startColor = ring.endColor = Violet;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", Violet * 1.35f);
        ring.SetPropertyBlock(properties);
        ring.enabled = false;
        return ring;
    }

    private static void SetRingRadius(LineRenderer ring, float radius)
    {
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = Mathf.PI * 2 * i / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }
    }

    private void BuildAtmosphere()
    {
        shadeSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
        shade = MakeLayer("Ritualdämmerung", 16);
        var shader = Resources.Load<Shader>("Vessel arcane veil");
        if (shader != null)
        {
            veil = new Material(shader);
            var colors = MakeLayer("Arkanfarben", 17);
            colors.transform.SetParent(shade.transform, false);
            colors.transform.localScale = Vector3.one;
            colors.color = Color.white;
            colors.sharedMaterial = veil;
        }
        flash = MakeLayer("Magischer Impuls", 18);

        haloTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        var pixels = new Color[48 * 48];
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                var delta = new Vector2(x - 23.5f, y - 23.5f) / 24f;
                pixels[y * 48 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - delta.sqrMagnitude), 3));
            }
        haloTexture.SetPixels(pixels);
        haloTexture.Apply();
        haloSprite = Sprite.Create(haloTexture, new Rect(0, 0, 48, 48), Vector2.one * .5f, 48);
    }

    private SpriteRenderer MakeLayer(string name, int order)
    {
        var layer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        layer.transform.SetParent(stage.transform, false);
        layer.transform.localScale = Vector3.one * 8;
        layer.sprite = shadeSprite;
        layer.sortingOrder = order;
        layer.color = Color.clear;
        return layer;
    }
    private static IEnumerator Fade(Light lamp, float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            lamp.intensity = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / duration));
            yield return null;
        }
        lamp.intensity = to;
    }

    private IEnumerator RevealMantle()
    {
        sound?.PlayEffect("mantle-rustle", .55f);
        yield return new WaitForSeconds(.38f);
        EquipMantle();
        yield return new WaitForSeconds(.55f);
    }

    private void EquipMantle()
    {
        CourtyardStarterMeetings.HasKeeperMantle = true;
        if (elian == null) return;
        elian.SetVesselTrialPose(null, new Vector2(.14f, .92f));
        elian.SetRestingSprite(PlayerEquipmentAppearance.RestingSprite);
    }

    private static string MantleReaction(CourtyardStarterMeetings.Starter partner) => partner switch
    {
        CourtyardStarterMeetings.Starter.Kafla => "Der steht dir. Jetzt fehlt nur noch ein ordentliches Abenteuer.",
        CourtyardStarterMeetings.Starter.Panfrie => "Er steht dir. Und … wenn dir kalt wird, rück ich ein Stück näher.",
        CourtyardStarterMeetings.Starter.Liora => "Der steht dir, Elian. Ich bin gespannt, wohin du ihn trägst.",
        _ => "Steht dir. Aber wenn du damit an einem Ast hängen bleibst, hab ich dich gewarnt."
    };

    private static string PartnerLine(CourtyardStarterMeetings.Starter partner)
    {
        switch (partner)
        {
            case CourtyardStarterMeetings.Starter.Kafla: return "Eine Ehre, Elian! Zusammen bringen wir ordentlich Feuer rein.";
            case CourtyardStarterMeetings.Starter.Panfrie: return "Du hast mich gewählt? Dann … dann passe ich gut auf dich auf.";
            case CourtyardStarterMeetings.Starter.Liora: return "Wie schön. Ich frage mich, was wir beide zum Blühen bringen.";
            default: return "Gute Wahl, Elian. Wenn du mich brauchst, schau in den Schatten.";
        }
    }

    private void LateUpdate()
    {
        if (theme != null)
        {
            theme.volume = (sound != null ? sound.musicVolume * sound.ConversationGain : .35f) * .58f * themeMix;
            sound?.SetMusicDucking(previousMusicGain * Mathf.Lerp(1, .12f, themeMix));
        }
        var camera = Camera.main;
        if (camera == null || shade == null) return;
        float depth = camera.nearClipPlane + .2f;
        Vector3 screen = camera.transform.position + camera.transform.forward * depth;
        shade.transform.SetPositionAndRotation(screen, camera.transform.rotation);
        flash.transform.SetPositionAndRotation(screen, camera.transform.rotation);
        shade.color = new Color(.035f, .027f, .075f, dim * .79f);
        flash.color = new Color(1f, .83f, .56f, pulse);
        if (veil != null)
        {
            veil.SetVector("_Origin", new Vector4(.5f, .48f, camera.aspect, 0));
            veil.SetFloat("_Strength", dim);
        }
        foreach (var (lamp, halo) in glows)
        {
            if (lamp == null || halo == null) continue;
            var point = camera.WorldToViewportPoint(lamp.transform.position);
            var color = lamp.color;
            color.a = point.z > 0 ? Mathf.Clamp01(lamp.intensity * .46f) : 0;
            point.z = depth - .015f;
            halo.color = color;
            halo.transform.SetPositionAndRotation(camera.ViewportToWorldPoint(point), camera.transform.rotation);
            halo.transform.localScale = Vector3.one * Mathf.Lerp(.021f, .066f, Mathf.Clamp01(lamp.intensity / 2.8f));
        }
    }
    private void OnDestroy() => Clear();

    private void Clear()
    {
        elian?.SetVesselTrialPose(null);
        if (sound != null) sound.SetMusicDucking(previousMusicGain);
        sound = null;
        if (mentorFigure != null && mentorIdle != null) mentorFigure.sprite = mentorIdle;
        foreach (var pose in lift) if (pose != null) Destroy(pose);
        if (veil != null) Destroy(veil);
        if (shadeSprite != null) Destroy(shadeSprite);
        if (haloSprite != null) Destroy(haloSprite);
        if (haloTexture != null) Destroy(haloTexture);
        if (stage != null) Destroy(stage);
        if (lightInk != null) Destroy(lightInk);
        if (vesselPose != null) Destroy(vesselPose);
        if (proudPose != null) Destroy(proudPose);
        stage = null;
        lightInk = null;
        vesselPose = proudPose = null;
    }
}