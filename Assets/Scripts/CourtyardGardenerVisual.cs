using UnityEngine;

// The world illustration stays upright; emotions use small nods and body motion.
// All reactions share the dialogue's per-line emotion and leave the work spot fixed.
public sealed class CourtyardGardenerVisual : MonoBehaviour
{
    private Transform paper, player;
    private SpriteRenderer illustration;
    private Camera view;
    private bool speaking;
    private float reactionTime=1;
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
        paper=new GameObject("Marta illustrated gardener",typeof(SpriteRenderer)).transform;
        paper.SetParent(transform,false);
        illustration=paper.GetComponent<SpriteRenderer>();
        illustration.sprite=Resources.Load<Sprite>("Marta/Gardener");
        PaperGroundShadow.Attach(gameObject,illustration);
    }
    private void OnLine(string speaker,DialogueEmotion emotion)
    {
        if(speaker!="Marta"){if(speaking)ResetReaction();return;}
        speaking=true;CurrentEmotion=emotion;reactionTime=0;FacePlayer(true);
    }
    private void OnDialogueEnded(){if(speaking)ResetReaction();}
    private void OnGardenCompleted()
    {
        CurrentEmotion=DialogueEmotion.Friendly;reactionTime=0;FacePlayer(true);
    }
    private void ResetReaction()
    {
        speaking=false;CurrentEmotion=DialogueEmotion.Neutral;reactionTime=1;
        if(paper!=null){paper.localPosition=Vector3.zero;paper.localScale=Vector3.one;}
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
        FacePlayer(speaking);
        reactionTime+=Time.deltaTime;
        float wave=Mathf.Sin(Mathf.Clamp01(reactionTime/.75f)*Mathf.PI);
        float breath=Mathf.Sin(Time.time*1.8f)*.006f;
        float nod=CurrentEmotion==DialogueEmotion.Worried?.025f:.014f;
        float lift=CurrentEmotion==DialogueEmotion.Friendly?.065f:
            CurrentEmotion==DialogueEmotion.Surprised?.035f:0;
        paper.localScale=new Vector3(1-breath*.4f,1+breath-wave*nod,1);
        paper.localPosition=Vector3.up*(wave*lift);
        if(!speaking && reactionTime>=.75f)CurrentEmotion=DialogueEmotion.Neutral;
    }
}