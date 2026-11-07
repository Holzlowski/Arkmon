using UnityEngine;

// The world illustration stays upright; emotions use small nods and body motion.
// All reactions share the dialogue's per-line emotion and leave the work spot fixed.
public sealed class CourtyardGardenerVisual : MonoBehaviour
{
    // Keep the corrected source aspect and boot anchor through every reaction.
    private static readonly Vector3 SilhouetteScale=Vector3.one;
    private Transform paper, player;
    private SpriteRenderer illustration;
    private Camera view;
    private bool speaking;
    private float reactionTime=1;
    private float gardenLookTime;
    private Vector3 gardenLookTarget;
    private Transform seedTray;
    public bool IsTending { get; private set; }
    public bool IsWatchingWatering { get; private set; }
    public DialogueEmotion CurrentEmotion { get; private set; }

    private void OnEnable()
    {
        DialogueBoxController.OnLinePresented+=OnLine;
        DialogueBoxController.OnDialogueEnded+=OnDialogueEnded;
        CourtyardGarden.OnCompleted+=OnGardenCompleted;
    }
    private void OnDisable()
    {
        DialogueBoxController.OnLinePresented-=OnLine;
        DialogueBoxController.OnDialogueEnded-=OnDialogueEnded;
        CourtyardGarden.OnCompleted-=OnGardenCompleted;
        ResetReaction();
    }
    private void Start()
    {
        view=Camera.main;player=GameObject.FindWithTag("Player")?.transform;
        seedTray=GameObject.Find("Marta seedling tray")?.transform;
        paper=new GameObject("Marta illustrated gardener",typeof(SpriteRenderer)).transform;
        paper.SetParent(transform,false);
        paper.localScale=SilhouetteScale;
        illustration=paper.GetComponent<SpriteRenderer>();
        illustration.sprite=Resources.Load<Sprite>("Marta/Gardener");
        PaperGroundShadow.Attach(gameObject,illustration);
    }
    private void OnLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker!="Marta"){if(speaking)ResetReaction();return;}
        gardenLookTime=0;
        speaking=true;CurrentEmotion=emotion;reactionTime=0;FacePlayer(true);
    }
    private void OnDialogueEnded(){if(speaking)ResetReaction();}
    private void OnGardenCompleted()
    {
        CurrentEmotion=DialogueEmotion.Friendly;reactionTime=0;
        var beds=FindObjectsByType<CourtyardVegetableBed>(FindObjectsSortMode.None);
        if(beds.Length>0)
        {
            gardenLookTarget=Vector3.zero;
            foreach(var bed in beds)gardenLookTarget+=bed.transform.position;
            gardenLookTarget/=beds.Length;gardenLookTime=.48f;
        }
        else FacePlayer(true);
    }
    private void ResetReaction()
    {
        gardenLookTime=0;IsTending=false;IsWatchingWatering=false;
        speaking=false;CurrentEmotion=DialogueEmotion.Neutral;reactionTime=1;
        if(paper!=null){paper.localPosition=Vector3.zero;paper.localScale=SilhouetteScale;}
    }
    private void FacePlayer(bool force)
    {
        if(player==null || illustration==null)return;
        Vector3 offset=player.position-transform.position;
        if((force || offset.sqrMagnitude<16) && Mathf.Abs(offset.x)>.18f)
            illustration.flipX=offset.x<0;
    }
    private void LateUpdate()
    {
        if(paper==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.rotation=view.transform.rotation;
        IsWatchingWatering = !speaking && CourtyardGarden.IsWatering && !SceneLoadManager.IsTransitioning;
        if(IsWatchingWatering)
        {
            IsTending=false;
            FacePlayer(true);
        }
        else if(gardenLookTime>0 && !speaking)
        {
            gardenLookTime-=Time.deltaTime;
            illustration.flipX=gardenLookTarget.x<transform.position.x;
            if(gardenLookTime<=0)FacePlayer(true);
        }
        else
        {
            IsTending=!speaking && !(DialogueBoxController.instance?.IsOpen??false) &&
                !DemoOpening.IsPresenting && !SceneLoadManager.IsTransitioning && !CourtyardLookout.IsPresenting && seedTray!=null &&
                (player==null || (player.position-transform.position).sqrMagnitude>9);
            if(IsTending)
            {
                // Inspect seedlings, then the beds. Stay planted at the worktable; no skating NPC.
                var target=Mathf.Repeat(Time.time,12)<7?seedTray.position:
                    CourtyardResidentLayout.GardenPoint(new Vector3(6.85f,0,7.2f));
                illustration.flipX=target.x<transform.position.x;
            }
            else FacePlayer(speaking);
        }
        reactionTime+=Time.deltaTime;
        float wave=Mathf.Sin(Mathf.Clamp01(reactionTime/.75f)*Mathf.PI);
        float breath=Mathf.Sin(Time.time*1.8f)*.006f;
        float tend=IsTending?Mathf.Pow(Mathf.Max(0,Mathf.Sin(Time.time*.85f)),2)*.012f:0;
        float nod=CurrentEmotion==DialogueEmotion.Worried?.006f:.003f;
        float lift=CurrentEmotion==DialogueEmotion.Friendly?.065f:
            CurrentEmotion==DialogueEmotion.Surprised?.035f:0;
        paper.localScale=Vector3.Scale(SilhouetteScale,new Vector3(1-breath*.2f,1+breath-wave*nod-tend,1));
        paper.localPosition=Vector3.up*(wave*lift);
        if(!speaking && reactionTime>=.75f)CurrentEmotion=DialogueEmotion.Neutral;
    }
}