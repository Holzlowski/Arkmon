using UnityEngine;

// Work frames affect only the paper child. The resident and interaction spot stay fixed.
public sealed class CourtyardCarpenterVisual : MonoBehaviour
{
    private const float PushDuration=.62f, ReturnDuration=.78f, StrokeDuration=1.55f, CycleDuration=4.5f;
    private Transform paper,player;
    private SpriteRenderer illustration;
    private Sprite idle;
    private Sprite[] work;
    private Camera view;
    private bool engaged;
    private float workTime,reactionTime=1,resumeAt;
    private int lastStroke=-1;
    private StableLocalFoley foley;
    private AudioClip[] scrapes;
    public bool IsWorking { get; private set; }
    public bool IsCutting { get; private set; }
    public DialogueEmotion CurrentEmotion { get; private set; }

    private void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted+=BeginConversation;
        DialogueBoxController.OnLinePresented+=PresentLine;
        DialogueBoxController.OnDialogueEnded+=EndConversation;
        ResetWork();
    }
    private void OnDisable()
    {
        DialogueBoxController.OnDialogueStarted-=BeginConversation;
        DialogueBoxController.OnLinePresented-=PresentLine;
        DialogueBoxController.OnDialogueEnded-=EndConversation;
        engaged=false;IsWorking=false;CurrentEmotion=DialogueEmotion.Neutral;
        ResetWork();
    }
    private void Start()
    {
        player=GameObject.FindWithTag("Player")?.transform;view=Camera.main;
        idle=Resources.Load<Sprite>("Ida/Idle");
        work=new[]{Resources.Load<Sprite>("Ida/WorkBack"),Resources.Load<Sprite>("Ida/WorkMiddle"),
            Resources.Load<Sprite>("Ida/WorkForward")};
        foley=gameObject.AddComponent<StableLocalFoley>();
        scrapes=new[]{Resources.Load<AudioClip>("Audio/Workshop/plane-stroke-1"),
            Resources.Load<AudioClip>("Audio/Workshop/plane-stroke-2")};
        paper=new GameObject("Ida illustrated carpenter",typeof(SpriteRenderer)).transform;paper.SetParent(transform,false);
        illustration=paper.GetComponent<SpriteRenderer>();illustration.sprite=idle;
        PaperGroundShadow.Attach(gameObject,illustration);
    }
    private void BeginConversation()
    {
        engaged=false;IsWorking=false;CurrentEmotion=DialogueEmotion.Neutral;
        ResetWork();
        if(illustration!=null)illustration.sprite=idle;
    }
    private void PresentLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker=="Ida")
        {
            engaged=true;IsWorking=false;CurrentEmotion=emotion;reactionTime=0;
            ResetWork();
            if(illustration!=null)illustration.sprite=idle;
        }
        else if(engaged)CurrentEmotion=DialogueEmotion.Neutral;
    }
    private void EndConversation()
    {
        // Also settle after an observation cancelled during Elian's first line.
        engaged=false;CurrentEmotion=DialogueEmotion.Neutral;reactionTime=1;
        ResetWork();
    }
    private void ResetWork()
    {
        workTime=0;lastStroke=-1;IsCutting=false;resumeAt=Time.time+.45f;
        if(foley!=null)foley.Stop();
    }
    private void LateUpdate()
    {
        if(paper==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.rotation=view.transform.rotation;
        IsWorking=!engaged && !(DialogueBoxController.instance?.IsOpen??false) &&
            !DemoOpening.IsPresenting && !SceneLoadManager.IsTransitioning;
        if(IsWorking)
        {
            if(Time.time>=resumeAt)workTime+=Time.deltaTime;
            float phase=Mathf.Repeat(workTime,CycleDuration);
            float strokePhase=Mathf.Repeat(phase,StrokeDuration);
            bool stroking=Time.time>=resumeAt && phase<2*StrokeDuration;
            bool cutting=stroking && strokePhase<PushDuration;
            float reach=0;
            if(stroking)
                reach=cutting?Mathf.SmoothStep(0,1,strokePhase/PushDuration):
                    1-Mathf.SmoothStep(0,1,(strokePhase-PushDuration)/ReturnDuration);
            illustration.sprite=work[Mathf.RoundToInt(reach*2)]??idle;
            illustration.flipX=false;

            // A short real scrape accompanies the forward cut only, never the return.
            int stroke=Mathf.FloorToInt(workTime/CycleDuration)*2+Mathf.FloorToInt(phase/StrokeDuration);
            if(cutting && strokePhase>=.06f && stroke!=lastStroke)
            {
                lastStroke=stroke;
                foley.Play(scrapes[stroke%2],.32f);
            }
            if(IsCutting && !cutting)foley.Stop();
            IsCutting=cutting;
        }
        else
        {
            if(IsCutting)ResetWork();
            illustration.sprite=idle;
            if(player!=null && Mathf.Abs(player.position.x-transform.position.x)>.18f)
                illustration.flipX=player.position.x<transform.position.x;
        }
        reactionTime+=Time.deltaTime;
        float wave=engaged?Mathf.Sin(Mathf.Clamp01(reactionTime/.6f)*Mathf.PI):0;
        float breath=Mathf.Sin(Time.time*1.9f)*.005f;
        paper.localScale=new Vector3(1-breath*.3f,1+breath-wave*.012f,1);
        paper.localPosition=Vector3.up*(wave*(CurrentEmotion==DialogueEmotion.Friendly?.035f:.015f));
    }
}
