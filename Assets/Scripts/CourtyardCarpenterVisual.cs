using UnityEngine;

// Work frames affect only the paper child. The resident and interaction spot stay fixed.
public sealed class CourtyardCarpenterVisual : MonoBehaviour
{
    private Transform paper,player;
    private SpriteRenderer illustration;
    private Sprite idle;
    private Sprite[] work;
    private Camera view;
    private bool engaged;
    private float workTime,reactionTime=1;
    public bool IsWorking { get; private set; }
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
        engaged=false;IsWorking=false;CurrentEmotion=DialogueEmotion.Neutral;
    }
    private void Start()
    {
        player=GameObject.FindWithTag("Player")?.transform;view=Camera.main;
        idle=Resources.Load<Sprite>("Ida/Idle");
        work=new[]{Resources.Load<Sprite>("Ida/WorkForward"),Resources.Load<Sprite>("Ida/WorkMiddle"),
            Resources.Load<Sprite>("Ida/WorkBack"),Resources.Load<Sprite>("Ida/WorkMiddle")};
        paper=new GameObject("Ida illustrated carpenter",typeof(SpriteRenderer)).transform;paper.SetParent(transform,false);
        illustration=paper.GetComponent<SpriteRenderer>();illustration.sprite=idle;
        PaperGroundShadow.Attach(gameObject,illustration);
    }
    private void BeginConversation(){engaged=false;CurrentEmotion=DialogueEmotion.Neutral;}
    private void PresentLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker=="Ida")
        {
            engaged=true;IsWorking=false;CurrentEmotion=emotion;reactionTime=0;workTime=0;
            if(illustration!=null)illustration.sprite=idle;
        }
        else if(engaged)CurrentEmotion=DialogueEmotion.Neutral;
    }
    private void EndConversation(){engaged=false;CurrentEmotion=DialogueEmotion.Neutral;reactionTime=1;workTime=0;}
    private void LateUpdate()
    {
        if(paper==null)return;
        if(view==null)view=Camera.main;
        if(view!=null)paper.rotation=view.transform.rotation;
        IsWorking=!engaged && !DemoOpening.IsPresenting && !SceneLoadManager.IsTransitioning;
        if(IsWorking)
        {
            workTime+=Time.deltaTime;
            float phase=Mathf.Repeat(workTime,2.2f);
            int frame=phase<1.32f?(int)(phase/.22f)%4:2;
            illustration.sprite=work[frame]!=null?work[frame]:idle;
            illustration.flipX=false;
        }
        else
        {
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