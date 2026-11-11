using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public sealed class CourtyardWaterTask : MonoBehaviour, IInteractableAsPlayer
{
    public static bool HasBucket { get; private set; }
    public static bool BucketFull { get; private set; }
    public static bool Completed { get; private set; }
    public static bool ThanksReceived { get; private set; }
    static bool requested;
    static CourtyardWaterTask bucket, active;
    public static bool IsBusy => active != null;
    public static string CarriedItemText => !HasBucket ? null : BucketFull ? "Voller Eimer" : "Leerer Eimer";
    public static string ItemActivityText => active!=null && !CourtyardGarden.HasCan ? active.actionText : "Für Hannes";
    bool trough;
    string actionText;
    SpriteRenderer sprite;
    Sprite ownedSprite;
    Material bucketMaterial;
    Renderer groundContact;
    GameObject player, drops;
    PaperCharacterVisual visual;
    Coroutine routine;
    AudioSource waterAudio;
    CourtyardWell activeWell;
    Rigidbody movingBody;
    PlayerController movingController;
    bool restoringMovement,savedGravity,savedCollisions;
    RigidbodyInterpolation savedInterpolation;
    static float troughLevel;
    public static string ObjectiveText => IsBusy ? $"Wasser · {active.actionText} … [Esc] Abbrechen"
        : CourtyardGarden.HasCan ? null : HasBucket ? Completed ? "Hannes helfen · Stelle den Eimer zurück zum Stall"
            : BucketFull ? "Hannes helfen · Bringe das Wasser zur Tränke oder zurück zu Hannes" : "Hannes helfen · Fülle den Eimer am Brunnen"
        : requested && !Completed ? "Hannes helfen · Nimm den leeren Eimer neben der Tränke"
        : Completed && !ThanksReceived ? "Pferde versorgt · Sprich mit Hannes" : null;
    public string Label => trough ? Completed ? "Pferdetränke ansehen" : "Wasser in die Tränke gießen"
        : HasBucket ? "Eimer zurückstellen" : "Eimer nehmen";
    public Vector3 GetTransform() => transform.position;
    public static void OfferHelp() => requested=true;
    public static void ReceiveThanks() => ThanksReceived=true;
    public static void ResetProgress()
    {
        if(active!=null)active.Cancel();
        HasBucket=BucketFull=Completed=ThanksReceived=requested=false;troughLevel=0;
    }
    internal static void Install()
    {
        var source=GameObject.Find("Stable water bucket");var stable=GameObject.Find("Stable");
        if(source==null || stable==null || bucket!=null)return;
        bucket=Spawn("Stable bucket station",source.transform.position);
        bucket.sprite=source.GetComponent<SpriteRenderer>();
        // Use the handle pivot shared by the staged bucket illustrations.
        var s=bucket.sprite.sprite;
        bucket.ownedSprite=Sprite.Create(s.texture,s.rect,new Vector2(.5f,.94f),s.pixelsPerUnit);
        bucket.ownedSprite.name="Stable bucket handle pivot";bucket.sprite.sprite=bucket.ownedSprite;
        bucket.groundContact=GameObject.Find("Stable water bucket contact")?.GetComponent<Renderer>();
        bucket.FindPlayer();bucket.PlaceBucket();
        if(bucket.player!=null && bucket.player.GetComponent<CourtyardBorrowedTools>()==null)
            bucket.player.AddComponent<CourtyardBorrowedTools>();
        var point=stable.transform.TransformPoint(new Vector3(4.85f,0,-.8f));
        Spawn("Horse water delivery",point).trough=true;
    }
    static CourtyardWaterTask Spawn(string label,Vector3 at)
    {
        var go=new GameObject(label,typeof(SphereCollider),typeof(CourtyardWaterTask));go.transform.position=at;
        var c=go.GetComponent<SphereCollider>();c.isTrigger=true;c.radius=.45f;
        return go.GetComponent<CourtyardWaterTask>();
    }
    void FindPlayer()
    {
        if(player==null)player=GameObject.FindWithTag("Player");
        if(visual==null && player!=null)visual=player.GetComponent<PaperCharacterVisual>();
        if(sprite!=null && visual!=null)
        {
            var original=player.GetComponentInChildren<SpriteRenderer>();if(original!=null)
            {
                if(bucketMaterial==null)bucketMaterial=new Material(Resources.Load<Shader>("CourtyardBucketContents")){name="Stable bucket contents"};
                bucketMaterial.SetFloat("_Filled",BucketFull?1:0);sprite.sharedMaterial=bucketMaterial;
            }
        }
    }
    void PlaceBucket()
    {
        if(sprite==null)return;
        if(sprite.transform.parent!=transform)sprite.transform.SetParent(transform,true);
        var rotation=Camera.main!=null?Camera.main.transform.rotation:Quaternion.identity;
        sprite.transform.SetPositionAndRotation(transform.position+rotation*Vector3.up*.564f,rotation);
        sprite.transform.localScale=Vector3.one;sprite.sortingOrder=0;
        sprite.enabled=!HasBucket;
        if(groundContact!=null)groundContact.enabled=!HasBucket;
    }
    void LateUpdate()
    {
        if(trough)
        {
            var surface=GameObject.Find("Recessed still water")?.transform;
            if(surface!=null){var p=surface.localPosition;p.y=Completed ? .65f : Mathf.Lerp(.38f,.65f,troughLevel);surface.localPosition=p;}
        }
        if(sprite==null || IsBusy)return;
        FindPlayer();
        PlaceBucket();
    }
    bool CanInteract()
    {
        FindPlayer();var d=DialogueBoxController.instance;
        var c=player!=null?player.GetComponent<PlayerController>():null;
        return c!=null && visual!=null && d!=null && !d.IsOpen && !DemoOpening.IsPresenting && !SceneLoadManager.IsTransitioning
            && Vector3.Distance(player.transform.position,GetTransform())<=c.interacRange;
    }
    public void InteractWithPlayer()
    {
        if(!CanInteract())return;
        var d=DialogueBoxController.instance;
        if(CourtyardGarden.HasCan){d.ShowMessage("Ich stelle zuerst Martas Gießkanne zurück.",IntroChapter.PlayerName);return;}
        if(trough)
        {
            if(Completed){d.ShowMessage("Die Pferde haben jetzt frisches Wasser.",IntroChapter.PlayerName);return;}
            if(!HasBucket || !BucketFull){d.ShowMessage("Ich brauche einen vollen Eimer vom Brunnen für die Pferde.",IntroChapter.PlayerName);return;}
            StartAction(false,null);return;
        }
        var delta=player.transform.position-transform.position;delta.y=0;
        if(delta.magnitude>1.3f){d.ShowMessage("Ich gehe erst näher an den Eimer heran.",IntroChapter.PlayerName);return;}
        visual.FaceWorldDirection(transform.position-player.transform.position);
        actionText=HasBucket?"Eimer abstellen":"Eimer aufnehmen";
        active=this;routine=StartCoroutine(LiftBucket(!HasBucket));
    }
    IEnumerator LiftBucket(bool take)
    {
        bool introduce=take && !requested;
        bool delivering=!take && BucketFull && !Completed;
        if(delivering){yield return Perform(false,null,false);if(!Completed){Finish();yield break;}}
        // A short interaction hold, with the ordinary player figure throughout.
        visual.SetGardenPose(null,false);
        yield return new WaitForSeconds(.24f);
        HasBucket=take;
        if(take)requested=true;
        bool handedOver=delivering;
        if(handedOver){BucketFull=false;if(!Completed){Completed=true;CourtyardTaskFeedback.Complete("Hannes");}ThanksReceived=true;}
        Finish();
        if(introduce)DialogueBoxController.instance?.ShowMessage("Oh, Prinz Elian! Ihr wollt mir helfen? Meine Pferde bräuchten nämlich frisches Wasser. Füllt den Eimer am Brunnen und gießt das Wasser in die Tränke.","Hannes");
        if(handedOver)DialogueBoxController.instance?.ShowMessage("Danke fürs frische Wasser, Elian! Die Pferde haben jetzt frisches Wasser. Gut gemacht!","Hannes");
    }
    public static void BeginFill(CourtyardWell well)
    {
        if(IsBusy || bucket==null || !(HasBucket || CourtyardGarden.HasCan))return;
        bucket.FindPlayer();if(bucket.visual==null)return;
        bucket.StartAction(true,well);
    }
    void StartAction(bool filling,CourtyardWell well)
    {
        actionText=filling?"Wasser schöpfen":"Wasser in die Tränke gießen";
        if(!filling)visual.FaceWorldDirection(GameObject.Find("Stable water trough").transform.position-player.transform.position);
        active=this;routine=StartCoroutine(Perform(filling,well));
    }
    IEnumerator Perform(bool filling,CourtyardWell well,bool finish=true)
    {
        if(!filling)visual.FaceWorldDirection(GameObject.Find("Stable water trough").transform.position-player.transform.position);
        bool can=CourtyardGarden.HasCan;
        var item=can?CourtyardGarden.CanSprite:bucket.sprite;
        if(item==null){Cancel();yield break;}
        activeWell=well;
        Vector3 returnPosition=player.transform.position;
        Transform targetTrough=GameObject.Find("Stable water trough").transform;
        Vector3 waypoint=player.transform.position;
        if(!filling)
        {
            var from=targetTrough.InverseTransformPoint(player.transform.position);
            waypoint=targetTrough.TransformPoint(new Vector3(Mathf.Max(1.05f,from.x),0,-1.35f));waypoint.y=player.transform.position.y;
            if(!CanApproach(waypoint)){Finish();DialogueBoxController.instance?.ShowMessage("Ich brauche einen freien Zugang zur Vorderseite der Tränke.",IntroChapter.PlayerName);yield break;}
            yield return Approach(waypoint);
        }
        Vector3 stand=filling ? well.GetTransform()+Vector3.right*1.4f : targetTrough.TransformPoint(new Vector3(.72f,0,-1.35f));
        stand.y=player.transform.position.y;
        if(!CanApproach(stand))
        {
            Finish();DialogueBoxController.instance?.ShowMessage(filling?"Ich gehe erst näher an die freie Seite des Brunnens heran.":"Ich gehe erst näher an die freie Seite der Tränke heran.",IntroChapter.PlayerName);yield break;
        }
        yield return Approach(stand);
        visual.FaceWorldDirection((filling?well.ReceivingGrip:targetTrough.position)-player.transform.position);
        drops=new GameObject("Courtyard water transfer");drops.transform.SetParent(transform,false);
        drops.transform.position=player.transform.position;
        var effect=drops.AddComponent<GardenWaterEffect>();
        waterAudio=drops.AddComponent<AudioSource>();waterAudio.playOnAwake=false;
        waterAudio.clip=Resources.Load<AudioClip>("Audio/IntroFoley/garden-water");
        waterAudio.volume=(SoundManager.instance!=null?SoundManager.instance.effectsVolume:.55f)*.35f;
        waterAudio.spatialBlend=1;waterAudio.minDistance=1;waterAudio.maxDistance=7;
        if(!Application.isBatchMode && waterAudio.clip!=null)waterAudio.Play();
        effect.SetSingleStream();
        GardenWaterEffect channelEffect=null;
        if(filling)
        {
            var channel=new GameObject("Well channel outlet water");channel.transform.SetParent(drops.transform,false);
            channelEffect=channel.AddComponent<GardenWaterEffect>();channelEffect.SetSingleStream();
        }
        visual.SetGardenPose(null,false);
        if(filling)PresentVesselAtWell(item,well,can);
        else PresentBucketAtTrough(item,targetTrough);
        if(!can && bucket.groundContact!=null)bucket.groundContact.enabled=false;
        yield return null;
        float duration=filling?3.5f:2.6f;
        for(float t=0;t<duration;t+=Time.deltaTime)
        {
            float progress=t/duration;
            if(filling)
            {
                well.PresentDraw(progress);
                Vector3 mouth=PresentVesselAtWell(item,well,can);
                effect.Present(well.PourSource,well.ChannelInlet,t,well.Flow);
                channelEffect.Present(well.ChannelOutlet,mouth,t,well.Flow);
            }
            else
            {
                PresentBucketAtTrough(item,targetTrough);
                float flow=progress>=.22f && progress<.82f?1:0;
                var target=targetTrough.TransformPoint(new Vector3(.25f,Mathf.Lerp(.38f,.65f,troughLevel),-.38f));
                effect.Present(item.transform.TransformPoint(new Vector3(.16f,-.22f,0)),target,t,flow);
                troughLevel=Mathf.InverseLerp(.22f,.82f,progress);
                if(bucket.bucketMaterial!=null)bucket.bucketMaterial.SetFloat("_Filled",1-troughLevel);
            }
            yield return null;
        }
        if(filling)
        {
            for(float t=0;t<.45f;t+=Time.deltaTime){well.PresentDraw(1+.15f*Mathf.SmoothStep(0,1,t/.45f));yield return null;}
            well.PresentDraw(1.15f);
            if(can)CourtyardGarden.FillCan();else BucketFull=true;
        }
        else {BucketFull=false;if(!Completed){Completed=true;CourtyardTaskFeedback.Complete("Hannes");}}
        if(finish)Finish();else
        {
            ClearTransfer();bucket.PlaceBucket();
            actionText="Eimer zurückstellen";
            yield return Approach(waypoint);yield return Approach(returnPosition);
        }
    }
    static Vector3 PresentVesselAtWell(SpriteRenderer item,CourtyardWell well,bool isCan)
    {
        var rotation=Camera.main!=null?Camera.main.transform.rotation:Quaternion.identity;
        var floor=well.ChannelOutlet+Vector3.back*.08f;floor.y=well.transform.position.y+.025f;
        var lift=Vector3.up*(-item.sprite.bounds.min.y);
        var mouth=isCan?CourtyardGarden.CanArtOffset(item.sprite,new Vector2(.38f,.60f)):new Vector3(0,-.26f,0);
        if(isCan)floor-=Vector3.ProjectOnPlane(rotation*(lift+mouth),Vector3.up);
        item.transform.SetPositionAndRotation(floor+rotation*lift,rotation);
        item.transform.localScale=Vector3.one;item.sortingOrder=0;item.enabled=true;
        return item.transform.TransformPoint(mouth);
    }
    static void PresentBucketAtTrough(SpriteRenderer item,Transform target)
    {
        // A fixed illustration resting by its lip on the wooden rim, separate from Elian.
        var rotation=(Camera.main!=null?Camera.main.transform.rotation:Quaternion.identity)*Quaternion.Euler(0,0,-70);
        var lip=target.TransformPoint(new Vector3(.17f,.83f,-.26f));
        item.transform.SetPositionAndRotation(lip-rotation*new Vector3(.16f,-.22f,0),rotation);
        item.transform.localScale=Vector3.one;item.sortingOrder=0;item.enabled=true;
    }
    bool CanApproach(Vector3 goal)
    {
        var body=player.GetComponent<Rigidbody>();var delta=goal-player.transform.position;
        if(delta.magnitude>2.6f)return false;
        Physics.SyncTransforms();
        foreach(var hit in body.SweepTestAll(delta.normalized,delta.magnitude,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(player.transform) && !hit.collider.CompareTag("Ground") && hit.normal.y<.7f)return false;
        return true;
    }
    IEnumerator Approach(Vector3 goal)
    {
        Vector3 start=player.transform.position;float duration=Vector3.Distance(start,goal)/2.4f;
        if(duration<.015f)yield break;
        movingBody=player.GetComponent<Rigidbody>();movingController=player.GetComponent<PlayerController>();
        savedGravity=movingBody.useGravity;savedCollisions=movingBody.detectCollisions;savedInterpolation=movingBody.interpolation;
        restoringMovement=true;movingBody.useGravity=false;movingBody.detectCollisions=false;movingBody.interpolation=RigidbodyInterpolation.None;
        if(!movingBody.isKinematic)movingBody.linearVelocity=Vector3.zero;
        movingController.SetScriptedMovement(true);
        for(float t=0;t<duration;t+=Time.deltaTime)
        {
            var p=Vector3.Lerp(start,goal,Mathf.SmoothStep(0,1,t/duration));
            player.transform.position=p;movingBody.position=p;yield return null;
        }
        player.transform.position=goal;movingBody.position=goal;Physics.SyncTransforms();RestoreMovement();
    }
    void RestoreMovement()
    {
        if(!restoringMovement)return;
        movingController?.SetScriptedMovement(false);
        if(movingBody!=null){movingBody.useGravity=savedGravity;movingBody.detectCollisions=savedCollisions;movingBody.interpolation=savedInterpolation;}
        restoringMovement=false;
    }
    void Update()
    {
        if(active==this && (ExplorationInput.Pressed(UnityEngine.InputSystem.Key.Escape) || SceneLoadManager.IsTransitioning))Cancel();
    }
    public void Cancel()
    {
        if(active!=this)return;
        if(routine!=null)StopCoroutine(routine);
        Finish();
    }
    void ClearTransfer()
    {
        if(activeWell!=null)activeWell.ResetDraw();activeWell=null;
        if(drops!=null)Destroy(drops);drops=null;
        if(waterAudio!=null){waterAudio.Stop();Destroy(waterAudio);}waterAudio=null;
    }
    void Finish()
    {
        ClearTransfer();RestoreMovement();
        if(!Completed)troughLevel=0;
        if(bucket!=null && bucket.bucketMaterial!=null)bucket.bucketMaterial.SetFloat("_Filled",BucketFull?1:0);
        if(CourtyardGarden.HasCan)CourtyardGarden.RestoreCanPlacement();
        if(visual!=null)visual.SetGardenPose(null,false);
        routine=null;if(active==this)active=null;
        if(bucket!=null)bucket.PlaceBucket();
    }
    void OnDisable(){Cancel();if(sprite!=null && HasBucket)visual?.SetGardenPose(null,false);}
    void OnDestroy(){if(bucketMaterial!=null)Destroy(bucketMaterial);if(ownedSprite!=null)Destroy(ownedSprite);if(bucket==this)bucket=null;}
}
