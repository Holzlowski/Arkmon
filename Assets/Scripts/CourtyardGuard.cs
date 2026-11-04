using UnityEngine;

// A fixed, reversible patrol along the authored courtyard path. Dynamic blockers make the guard wait.
[RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
public sealed class CourtyardGuard : MonoBehaviour,IInteractableAsPlayer
{
    public const string Speaker="Burgwache";
    private CourtyardGuardRoute route;
    private Rigidbody body;
    private Transform player;
    private int direction=1;
    private float wait,resumeAt;
    private bool speaking;
    private readonly RaycastHit[] hits=new RaycastHit[32];
    public int TargetIndex { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsWaitingForObstacle { get; private set; }
    public bool FacingLeft { get; private set; }
    public float TravelDistance { get; private set; }
    public DialogueEmotion Emotion { get; private set; }
    public string Label=>"Mit der Wache sprechen";
    public Vector3 GetTransform()=>transform.position+Vector3.up*.8f;

    internal static void Install()
    {
        if(FindFirstObjectByType<CourtyardGuard>()!=null)return;
        var data=Resources.Load<CourtyardGuardRoute>("Guard/Patrol");
        if(data==null || data.stops==null || data.stops.Length<2 || Resources.Load<Sprite>("Guard/Idle")==null)return;
        var root=new GameObject("Courtyard Guard");
        var guard=root.AddComponent<CourtyardGuard>();
        guard.route=data;
        guard.body.position=Grounded(data.stops[0].position);
        root.transform.position=guard.body.position;
        guard.wait=data.stops[0].wait;
        guard.FacingLeft=data.stops[0].lookLeft;
        guard.TargetIndex=1;
        root.AddComponent<CourtyardGuardVisual>();
    }
    private void Awake()
    {
        body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        body.interpolation=RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        body.constraints=RigidbodyConstraints.FreezeRotation;
        var capsule=GetComponent<CapsuleCollider>();capsule.radius=.33f;capsule.height=2.15f;
        capsule.center=Vector3.up*1.075f;
        player=GameObject.FindWithTag("Player")?.transform;
    }
    private void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted+=Pause;
        DialogueBoxController.OnDialogueEnded+=Resume;
        DialogueBoxController.OnLinePresented+=PresentLine;
    }
    private void OnDisable()
    {
        DialogueBoxController.OnDialogueStarted-=Pause;
        DialogueBoxController.OnDialogueEnded-=Resume;
        DialogueBoxController.OnLinePresented-=PresentLine;
        IsMoving=false;IsWaitingForObstacle=false;speaking=false;
    }
    private void Pause(){IsMoving=false;IsWaitingForObstacle=false;}
    private void Resume(){speaking=false;Emotion=DialogueEmotion.Neutral;resumeAt=Time.time+.65f;}
    private void PresentLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker==Speaker){speaking=true;Emotion=emotion;}
        else if(speaking)Emotion=DialogueEmotion.Neutral;
        if(speaking && player!=null && Mathf.Abs(player.position.x-transform.position.x)>.1f)
            FacingLeft=player.position.x<transform.position.x;
    }
    private void FixedUpdate()
    {
        IsMoving=false;IsWaitingForObstacle=false;
        if(route==null || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning ||
            Time.time<resumeAt || DialogueBoxController.instance!=null && DialogueBoxController.instance.IsOpen)return;
        if(wait>0){wait=Mathf.Max(0,wait-Time.fixedDeltaTime);return;}
        Vector3 current=body.position;
        Vector3 target=Grounded(route.stops[TargetIndex].position);
        Vector3 flat=target-current;flat.y=0;
        if(flat.magnitude<.05f)
        {
            wait=route.stops[TargetIndex].wait;
            if(wait>0)FacingLeft=route.stops[TargetIndex].lookLeft;
            if(TargetIndex==route.stops.Length-1)direction=-1;
            else if(TargetIndex==0)direction=1;
            TargetIndex+=direction;
            return;
        }
        var heading=flat.normalized;
        float step=Mathf.Min(route.speed*Time.fixedDeltaTime,flat.magnitude);
        if(Blocked(current,heading,step+.16f)) {IsWaitingForObstacle=true;return;}
        var next=Grounded(current+heading*step);
        // Never step off the authored ground or jump onto a prop/ledge after scene edits.
        if(Mathf.Abs(next.y-current.y)>.25f){IsWaitingForObstacle=true;return;}
        if(Mathf.Abs(heading.x)>.05f)FacingLeft=heading.x<0;
        body.MovePosition(next);TravelDistance+=step;IsMoving=true;
    }
    private bool Blocked(Vector3 position,Vector3 heading,float distance)
    {
        if(player!=null)
        {
            Vector3 toPlayer=player.position-position;toPlayer.y=0;
            if(toPlayer.magnitude<1.45f && Vector3.Dot(toPlayer,heading)>-.15f)return true;
        }
        int count=Physics.CapsuleCastNonAlloc(position+Vector3.up*.4f,position+Vector3.up*1.8f,
            .34f,heading,hits,distance,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
            if(hits[i].transform!=transform && !hits[i].transform.IsChildOf(transform) &&
                !hits[i].collider.CompareTag("Ground") && hits[i].normal.y<.65f)return true;
        return count==hits.Length;
    }
    private static Vector3 Grounded(Vector3 point)
    {
        float height=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(point.x,6,point.z),Vector3.down,12,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.CompareTag("Ground") && hit.normal.y>.65f)height=Mathf.Max(height,hit.point.y);
        point.y=float.IsNegativeInfinity(height)?-100:height+.015f;
        return point;
    }
    public void InteractWithPlayer()
    {
        var dialogue=DialogueBoxController.instance;
        if(dialogue==null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning)return;
        Pause();GuardConversation.Open(dialogue);
    }
}