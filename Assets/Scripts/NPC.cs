using UnityEngine;

public class NPC : MonoBehaviour, IInteractableArk
{
    [SerializeField] bool firstInteraction = true;
    [SerializeField] int startSection;
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

    void Update()
    {

    }

    public void InteractWithPlayer()
    {
        DialogueBoxController.instance.StartDialogue(dialogueTree, startSection, npcName);
    }


    Vector3 IInteractableArk.GetTransform()
    {
        return transform.position;
    }
}
