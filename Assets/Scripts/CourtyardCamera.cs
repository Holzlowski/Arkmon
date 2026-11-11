using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(9000)]
public class CourtyardCamera : MonoBehaviour
{
    [SerializeField] private Vector2 horizontalBounds=new Vector2(-40,52);
    [SerializeField] private Vector2 depthBounds=new Vector2(-16,11);
    [SerializeField] private float distance=19, pitch=25, fieldOfView=48, damping=.28f;
    [SerializeField] private float focusHeight=2.4f;
    [SerializeField] private float forwardFocus=1.7f, conversationDistance=14, conversationEase=.85f;
    [SerializeField] private float gateYaw=38;
    [SerializeField] private Vector2 gateTurnRange=new Vector2(28,41);
    private Camera view;private Transform player;private Vector3 focus,velocity;
    private CinemachineBrain brain;private bool previousBrain;
    private float currentYaw, conversationBlend, blendVelocity;
    private Transform conversationPartner;
    private bool talking;
    public float ConversationBlend => conversationBlend;
    public Transform ConversationPartner => conversationPartner;
    public float CurrentDistance { get; private set; }
    private void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted += BeginConversation;
        DialogueBoxController.OnLinePresented += PresentLine;
        DialogueBoxController.OnDialogueEnded += EndConversation;
    }
    private void BeginConversation()
    {
        talking=true;conversationPartner=null;
        var selected=player!=null?player.GetComponent<PlayerInteractionFocus>()?.Current:null;
        if(selected is Component component && (component is NPC || component is CourtyardLife || component is CourtyardPanfrie || component is CourtyardKafla || component is CourtyardLiora || component is CourtyardGuard || component is ArkanmeisterinArrival))
            conversationPartner=component.transform;
    }
    private void PresentLine(string speaker,DialogueEmotion emotion)
    {
        if(!talking || conversationPartner!=null || player==null || speaker==IntroChapter.PlayerName)return;
        string resident=speaker=="Ida"?"Courtyard Carpenter":speaker=="Marta"?"Courtyard Gardener":
            speaker=="Hannes"?"Courtyard Stablekeeper":speaker==CourtyardGuard.Speaker?"Courtyard Guard":speaker==ArkanmeisterinConversation.Speaker?ArkanmeisterinConversation.Speaker:null;
        Transform candidate=resident!=null?GameObject.Find(resident)?.transform:null;
        if(candidate==null)
            foreach(var npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
                if(npc.npcName==speaker){candidate=npc.transform;break;}
        // A voice in an observation must not pull the view towards someone across the courtyard.
        if(candidate!=null && Vector3.Distance(candidate.position,player.position)<8)conversationPartner=candidate;
    }
    private void EndConversation()=>talking=false;
    private void Start()
    {
        view=Camera.main;player=GameObject.FindWithTag("Player")?.transform;
        if(view==null || player==null){enabled=false;return;}
        brain=view.GetComponent<CinemachineBrain>();if(brain!=null){previousBrain=brain.enabled;brain.enabled=false;}
        focus=Target();currentYaw=DesiredYaw();CurrentDistance=distance;Apply();
    }
    private Vector3 Target()=>new Vector3(Mathf.Clamp(player.position.x,horizontalBounds.x,CourtyardStarterMeetings.AtTrialField ? Mathf.Max(horizontalBounds.y, 62f) : horizontalBounds.y),focusHeight,Mathf.Clamp(player.position.z+forwardFocus,depthBounds.x,depthBounds.y));
    private float DesiredYaw()=>gateYaw*Mathf.SmoothStep(0,1,Mathf.InverseLerp(gateTurnRange.x,gateTurnRange.y,focus.x));
    private void LateUpdate()
    {
        if(view==null || player==null)return;
        bool close=talking && (DialogueBoxController.instance?.IsOpen??false) && !SceneLoadManager.IsTransitioning && !scenic;
        conversationBlend=Mathf.SmoothDamp(conversationBlend,close?1:0,ref blendVelocity,conversationEase,Mathf.Infinity,Time.unscaledDeltaTime);
        if(!close && conversationBlend<.001f){conversationBlend=0;conversationPartner=null;}
        var target=Target();
        if(conversationBlend>0)
        {
            Vector3 midpoint=conversationPartner!=null?(player.position+conversationPartner.position)*.5f:player.position;
            // Lift the figures in the image, leaving the lower dialogue panel unobstructed.
            Vector3 forward=Quaternion.Euler(0,currentYaw,0)*Vector3.forward;
            Vector3 conversationTarget=midpoint+Vector3.up*.1f-forward*1.5f;
            target=Vector3.Lerp(target,conversationTarget,conversationBlend);
        }
        focus=Vector3.SmoothDamp(focus,target,ref velocity,damping,Mathf.Infinity,Time.unscaledDeltaTime);
        // Do not catch up an entire camera turn in one frame after a short rendering hitch.
        currentYaw=Mathf.MoveTowardsAngle(currentYaw,DesiredYaw(),45*Mathf.Min(Time.unscaledDeltaTime,.05f));
        Apply();
    }
    private bool scenic;
    private Vector3 scenicPosition;
    private Quaternion scenicRotation;
    private float scenicFov;
    public void SetScenicView(Vector3 position, Quaternion rotation, float fov)
    {
        scenic = true; scenicPosition = position; scenicRotation = rotation; scenicFov = fov;
        Apply();
    }
    public void ClearScenicView() { scenic = false; if (view != null) Apply(); }
    private void Apply()
    {
        if (scenic)
        {
            view.transform.SetPositionAndRotation(scenicPosition, scenicRotation);
            view.fieldOfView = scenicFov;
            return;
        }
        var angle = Quaternion.Euler(pitch, currentYaw, 0);
        float closeDistance=conversationDistance;
        if(conversationPartner!=null)
        {
            float span=Mathf.Abs(Vector3.Dot(conversationPartner.position-player.position,angle*Vector3.right))+3.2f;
            closeDistance=Mathf.Max(closeDistance,span/(2*Mathf.Tan(fieldOfView*Mathf.Deg2Rad*.5f)*Mathf.Max(.65f,view.aspect)*.72f));
        }
        CurrentDistance=Mathf.Lerp(distance,Mathf.Min(distance,closeDistance),conversationBlend);
        view.transform.SetPositionAndRotation(focus - angle * Vector3.forward * CurrentDistance, angle);
        view.fieldOfView = fieldOfView;
    }
    private void OnDisable()
    {
        DialogueBoxController.OnDialogueStarted-=BeginConversation;
        DialogueBoxController.OnLinePresented-=PresentLine;
        DialogueBoxController.OnDialogueEnded-=EndConversation;
        talking=false;conversationBlend=blendVelocity=0;conversationPartner=null;
        if(brain!=null)brain.enabled=previousBrain;
    }
}
