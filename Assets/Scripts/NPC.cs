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
        if (npcName == "John" && !DemoOpening.TrialIntroductionSeen)
        {
            DialogueBoxController.instance.ShowConversation(new[] {
                new DialogueSection {
                    dialogue = new[] { "Da bist du ja! Bevor die Arkanmeisterin eintrifft, wollen wir sehen, wie gut du und Kafla zusammenarbeiten.",
                        "Ich rufe Distelwolle mit meinem Fokus. Ruf Kafla aus deiner Lampe auf ein freies Feld in deiner Nähe. Beobachte erst die Felder und entscheide dann, wie du angreifst." },
                    branchPoint = new BranchPoint { question = "Bereit für deine Prüfung?", answers = new[] {
                        new Answer { answerLabel = "Kafla und ich sind bereit!", nextElement = 1 },
                        new Answer { answerLabel = "Ich bereite mich noch kurz vor.", nextElement = 2 } } } },
                new DialogueSection { dialogue = new[] { "Gut! Ich gebe dir unterwegs ein paar Hinweise. Lass dir Zeit – eine kluge Position ist genauso wichtig wie ein kräftiger Angriff." }, battleAfterDialogue = true },
                new DialogueSection { dialogue = new[] { "Natürlich. Sprich mich an, sobald ihr bereit seid." }, endAfterDialogue = true }
            }, npcName, battleSceneName, battleConfiguration);
            return;
        }
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
