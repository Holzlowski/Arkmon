using UnityEngine;

public class NPC : MonoBehaviour, IInteractableAsPlayer
{
    [SerializeField] bool firstInteraction = true;
    [SerializeField] int startSection;
    [SerializeField] string battleSceneName;
    [SerializeField] BattleConfiguration battleConfiguration;
    public string npcName;
    public DialogueTree dialogueTree;


    public int DialogueStartPosition
    {
        get
        {
            if (firstInteraction)
            {
                firstInteraction = false;
                return 0;
            }
            else
            {
                return startSection;
            }
        }
    }

    private void Awake()
    { if (GetComponent<NPCSummoningFocus>() == null) gameObject.AddComponent<NPCSummoningFocus>(); }

    public void FacePlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        PaperCharacterVisual visual = GetComponent<PaperCharacterVisual>();
        if (visual != null) visual.FaceWorldDirection(player.transform.position - transform.position);
    }

    private void Update()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && (player.transform.position - transform.position).sqrMagnitude <= 9f) FacePlayer();
    }

    public void InteractWithPlayer()
    {
        FacePlayer();
        DialogueBoxController.instance.StartDialogue(dialogueTree, startSection, npcName, battleSceneName, battleConfiguration);
    }

    public string GetBattleSceneName()
    {
        return battleSceneName;
    }


    Vector3 IInteractableAsPlayer.GetTransform()
    {
        return transform.position;
    }
}
