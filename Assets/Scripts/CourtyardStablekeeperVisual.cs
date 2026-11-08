using UnityEngine;

// Work frames affect only the paper child. The resident and interaction spot stay fixed.
public sealed class CourtyardStablekeeperVisual : MonoBehaviour
{
    private Transform paper,player,horse;
    private SpriteRenderer illustration;
    private Sprite idle;
    private Sprite[] work;
    private CourtyardGroomPose groom;
    private Camera view;
    private bool engaged;
    private float workTime,reactionTime=1,resumeAt;
    private int lastStroke=-1;
    private StableLocalFoley foley;
    private AudioClip brush;
    public bool IsWorking { get; private set; }
    public bool IsCheckingHorse { get; private set; }
    public DialogueEmotion CurrentEmotion { get; private set; }

    private void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted+=BeginConversation;
        DialogueBoxController.OnLinePresented+=PresentLine;
        DialogueBoxController.OnDialogueEnded+=EndConversation;
    }
    private void OnDisable()
    {
        DialogueBoxController.OnDialogueStarted-=BeginConversation;
        DialogueBoxController.OnLinePresented-=PresentLine;
        DialogueBoxController.OnDialogueEnded-=EndConversation;
        if(foley!=null)foley.Stop();
        engaged=false;IsWorking=false;IsCheckingHorse=false;CurrentEmotion=DialogueEmotion.Neutral;
    }
    private void Start()
    {
        player=GameObject.FindWithTag("Player")?.transform;view=Camera.main;
        idle=Resources.Load<Sprite>("Hannes/GroomIdle");
        foreach (var candidate in FindObjectsByType<CourtyardHorse>(FindObjectsSortMode.None))
            if (horse == null || (candidate.transform.position-transform.position).sqrMagnitude < (horse.position-transform.position).sqrMagnitude)
                horse=candidate.transform;
        work=new[]{Resources.Load<Sprite>("Hannes/GroomForward"),Resources.Load<Sprite>("Hannes/GroomMiddle"),
            Resources.Load<Sprite>("Hannes/GroomBack"),Resources.Load<Sprite>("Hannes/GroomMiddle")};
        paper=new GameObject("Hannes illustrated stablekeeper",typeof(SpriteRenderer)).transform;paper.SetParent(transform,false);
        illustration=paper.GetComponent<SpriteRenderer>();illustration.sprite=idle;
        illustration.sortingOrder=1;
        if(GetComponent<UnityEngine.Rendering.SortingGroup>()==null)gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
        groom=paper.gameObject.AddComponent<CourtyardGroomPose>();
        PaperGroundShadow.Attach(gameObject,illustration);
        foley=gameObject.AddComponent<StableLocalFoley>();brush=Resources.Load<AudioClip>("Audio/Stable/saddle-brush");
    }
    private void BeginConversation()
    {
        engaged=false;CurrentEmotion=DialogueEmotion.Neutral;IsWorking=false;IsCheckingHorse=false;
        if(foley!=null)foley.Stop();
        if(illustration!=null)illustration.sprite=idle;
    }
    private void PresentLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker=="Hannes")
        {
            engaged=true;IsWorking=false;IsCheckingHorse=false;CurrentEmotion=emotion;reactionTime=0;workTime=0;
            if(foley!=null)foley.Stop();
            if(illustration!=null)illustration.sprite=idle;
        }
        else if(engaged)CurrentEmotion=DialogueEmotion.Neutral;
    }
    private void EndConversation(){engaged=false;CurrentEmotion=DialogueEmotion.Neutral;reactionTime=1;workTime=0;lastStroke=-1;resumeAt=Time.time+.4f;}
    private void LateUpdate()
    {
        if(paper==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.rotation=view.transform.rotation;
        IsWorking=!engaged && !(DialogueBoxController.instance!=null && DialogueBoxController.instance.IsOpen) && !DemoOpening.IsPresenting && !SceneLoadManager.IsTransitioning && !CourtyardLookout.IsPresenting;
        if(IsWorking)
        {
            if(Time.time>=resumeAt)workTime+=Time.deltaTime;
            float cycle = Mathf.Repeat(workTime, 14.4f);
            IsCheckingHorse = cycle >= 10.8f;
            float phase=Mathf.Repeat(cycle,3.6f);
            float strokePhase=Mathf.Repeat(phase,.72f);
            float reach=!IsCheckingHorse && phase<2.16f?.5f-.5f*Mathf.Cos(strokePhase/.72f*Mathf.PI*2):0;
            // Back -> middle -> forward -> middle; ease at the ends, then rest.
            int pose=Mathf.RoundToInt(reach*2);
            int frame=pose==0?2:pose==1?1:0;
            int stroke=Mathf.FloorToInt(workTime/3.6f)*3+Mathf.FloorToInt(phase/.72f);
            if(!IsCheckingHorse && phase<2.16f && strokePhase>.12f && stroke!=lastStroke && Time.time>=resumeAt)
            {
                lastStroke=stroke;foley.Play(brush,.15f,.94f+(stroke%3)*.035f);
            }
            illustration.sprite=IsCheckingHorse ? idle : work[frame]!=null?work[frame]:idle;
            illustration.flipX=IsCheckingHorse && horse!=null && horse.position.x<transform.position.x;
            if(IsCheckingHorse)foley.Stop();
        }
        else
        {
            IsCheckingHorse=false;
            illustration.sprite=idle;
            if(player!=null && Mathf.Abs(player.position.x-transform.position.x)>.18f)
                illustration.flipX=player.position.x<transform.position.x;
        }
        if(horse!=null)horse.GetComponent<CourtyardHorse>()?.PairWith(transform);
        groom.Present(illustration,IsWorking && !IsCheckingHorse);
        reactionTime+=Time.deltaTime;
        float wave=engaged?Mathf.Sin(Mathf.Clamp01(reactionTime/.6f)*Mathf.PI):0;
        float breath=Mathf.Sin(Time.time*1.9f)*.005f;
        paper.localScale=new Vector3(1-breath*.3f,1+breath-wave*.012f,1);
        paper.localPosition=Vector3.up*(wave*(CurrentEmotion==DialogueEmotion.Friendly?.035f:.015f));
    }
}