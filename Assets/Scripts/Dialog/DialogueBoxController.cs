using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueBoxController : MonoBehaviour
{
    public static DialogueBoxController instance;
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] GameObject answerBox;
    [SerializeField] Button[] answerObjects;
    [SerializeField] string battleSceneName;


    public static event Action OnDialogueStarted;
    public static event Action OnDialogueEnded;
    public static event Action<string, DialogueEmotion> OnLinePresented;

    bool skipLineTriggered;
    bool answerTriggered;
    int answerIndex;
    private BattleConfiguration battleConfiguration;
    public bool IsOpen => dialogueBox != null && dialogueBox.activeSelf;
    private Image portraitImage;
    private bool typing;
    private bool revealLine;
    private bool stagingLine;
    private int answerCount;
    private DialogueTree temporaryDialogue;
    private string conversationSpeaker;
    private Sprite conversationPortrait;
    private DialoguePortraitMotion portraitMotion;
    public string ActiveSpeaker { get; private set; } = "";
    public DialogueEmotion ActiveEmotion { get; private set; }
    public Sprite ActivePortrait => portraitImage != null ? portraitImage.sprite : null;

    private void PresentLine(DialogueLinePresentation presentation)
    {
        ActiveSpeaker=string.IsNullOrEmpty(presentation.speaker)?conversationSpeaker:presentation.speaker;
        ActiveEmotion=presentation.emotion;
        nameText.text=ActiveSpeaker;
        if(portraitImage!=null)
        {
            Sprite explicitPortrait=presentation.portrait!=null?presentation.portrait:
                ActiveSpeaker==conversationSpeaker?conversationPortrait:null;
            portraitImage.sprite=explicitPortrait!=null?explicitPortrait:
                DialoguePortraitSet.Resolve(ActiveSpeaker,ActiveEmotion) ?? PaperUIStyle.Load()?.PortraitFor(ActiveSpeaker);
            portraitImage.enabled=portraitImage.sprite!=null;
            if(portraitMotion==null)portraitMotion=portraitImage.GetComponent<DialoguePortraitMotion>()
                ?? portraitImage.gameObject.AddComponent<DialoguePortraitMotion>();
            portraitMotion.Play(ActiveEmotion);
        }
        OnLinePresented?.Invoke(ActiveSpeaker,ActiveEmotion);
    }
    private void ClearPresentation()
    {
        ActiveSpeaker="";ActiveEmotion=DialogueEmotion.Neutral;
        if(portraitImage!=null){portraitImage.sprite=null;portraitImage.enabled=false;}
    }
    private void EndPresentation()
    {
        dialogueBox.SetActive(false);answerBox.SetActive(false);
        ClearPresentation();
        OnDialogueEnded?.Invoke();
    }

    public void ShowMessage(string message, string speaker)
    {
        DialogueTree tree = ScriptableObject.CreateInstance<DialogueTree>();
        tree.sections = new[] { new DialogueSection { dialogue = new[] { message }, endAfterDialogue = true } };
        StartDialogue(tree, 0, speaker, "");
        temporaryDialogue = tree;
    }

    // Own generated conversations just like one-line messages, including cancellation.
    public void ShowConversation(DialogueSection[] sections, string speaker, string scene,
        BattleConfiguration configuration)
    {
        DialogueTree tree = ScriptableObject.CreateInstance<DialogueTree>();
        tree.sections = sections;
        StartDialogue(tree, 0, speaker, scene, configuration);
        temporaryDialogue = tree;
    }

    float charactersPerSecond = 90;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        foreach (Transform child in transform)
        {
            // Deaktiviere jedes Kindobjekt.
            child.gameObject.SetActive(false);
        }
        if (PaperDialogueLayout.Build(this, out GameObject box, out GameObject answers,
            out TextMeshProUGUI text, out TextMeshProUGUI speaker, out Button[] choices, out Image portrait))
        { dialogueBox = box; answerBox = answers; dialogueText = text; nameText = speaker; answerObjects = choices; portraitImage = portrait; }
    }

    public void StartDialogue(DialogueTree dialogueTree, int startSection, string name, string battleSceneName,
        BattleConfiguration configuration = null, Sprite portrait = null)
    {
        ResetBox();
        this.battleSceneName = battleSceneName;
        battleConfiguration = configuration;
        conversationSpeaker=name;conversationPortrait=portrait;
        dialogueBox.SetActive(true);
        OnDialogueStarted?.Invoke();
        StartCoroutine(RunDialogue(dialogueTree, startSection));
    }


    IEnumerator RunDialogue(DialogueTree dialogueTree, int section)
    {
        for (int i = 0; i < dialogueTree.sections[section].dialogue.Length; i++)
        {
            var presentations=dialogueTree.sections[section].presentations;
            var presentation = presentations != null && i < presentations.Length ? presentations[i] : default;
            if (presentation.BeforePresentation != null)
            {
                stagingLine = true;
                yield return presentation.BeforePresentation();
                stagingLine = false;
            }
            PresentLine(presentation);
            dialogueText.text="";
            typing = true;
            revealLine = false;
            float timer = 0;
            float interval = 1 / charactersPerSecond;
            string textBuffer = null;
            char[] chars = dialogueTree.sections[section].dialogue[i].ToCharArray();
            int j = 0;
            while (j < chars.Length)
            {
                if (revealLine) { dialogueText.text = new string(chars); break; }
                if (timer < Time.deltaTime)
                {
                    textBuffer += chars[j];
                    dialogueText.text = textBuffer;
                    timer += interval;
                    j++;
                }
                else
                {
                    timer -= Time.deltaTime;
                    yield return null;
                }
            }
            typing = false;
            while (skipLineTriggered == false)
            {
                yield return null;
            }
            skipLineTriggered = false;
        }

        if (dialogueTree.sections[section].battleAfterDialogue)
        {
            EndPresentation();


            Vector3 playerTransform = GameObject.FindWithTag("Player").gameObject.transform.position;
            SceneLoadManager.instance.SavePlayerTransform(playerTransform);
            SceneLoadManager.instance.SetPendingBattle(battleConfiguration);
            SceneLoadManager.instance.SetBattleSpeaker(conversationSpeaker);
            DemoOpening.BeginTrial(conversationSpeaker);

            // Lade die Kampfszene
            SceneLoadManager.instance.LoadBattleScene(battleSceneName);
            if (temporaryDialogue != null) { Destroy(temporaryDialogue); temporaryDialogue = null; }
            //SoundManager.instance.PlayMusic(1);
            yield break;
        }

        if (dialogueTree.sections[section].endAfterDialogue)
        {
            EndPresentation();
            if (temporaryDialogue != null) { Destroy(temporaryDialogue); temporaryDialogue = null; }
            yield break;
        }

        PresentLine(dialogueTree.sections[section].branchPoint.presentation);
        dialogueText.text = dialogueTree.sections[section].branchPoint.question;
        ShowAnswers(dialogueTree.sections[section].branchPoint);
        while (answerTriggered == false)
        {
            yield return null;
        }
        answerBox.SetActive(false);
        answerTriggered = false;
        StartCoroutine(RunDialogue(dialogueTree, dialogueTree.sections[section].branchPoint.answers[answerIndex].nextElement));
    }

    void ShowAnswers(BranchPoint branchPoint)
    {
        answerBox.SetActive(true);
        answerCount = branchPoint.answers.Length;
        if (answerCount > answerObjects.Length && PaperUIStyle.Load() != null)
        {
            PaperUIFactory.ClearChildren(answerBox.transform);
            answerObjects = PaperDialogueLayout.CreateChoices(this, answerBox.transform, answerCount);
        }
        for (int i = 0; i < answerObjects.Length; i++)
        {
            if (i < branchPoint.answers.Length)
            {
                answerObjects[i].GetComponentInChildren<TextMeshProUGUI>().text = branchPoint.answers[i].answerLabel;
                answerObjects[i].gameObject.SetActive(true);
            }
            else
            {
                answerObjects[i].gameObject.SetActive(false);
            }
        }
        if (answerCount > 0 && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(answerObjects[0].gameObject);
    }

    public void SkipLine()
    {
        if (!IsOpen || answerBox.activeSelf || stagingLine) return;
        if (typing) revealLine = true;
        else skipLineTriggered = true;
    }

    public void AnswerQuestion(int answer)
    {
        if (!IsOpen || !answerBox.activeSelf || answer < 0 || answer >= answerCount || answerTriggered) return;
        answerIndex = answer;
        answerTriggered = true;
    }

    void ResetBox()
    {
        StopAllCoroutines();
        ClearPresentation();
        if (temporaryDialogue != null) { Destroy(temporaryDialogue); temporaryDialogue = null; }
        dialogueBox.SetActive(false);
        answerBox.SetActive(false);
        skipLineTriggered = false;
        answerTriggered = false;
        typing = revealLine = stagingLine = false;
        answerCount = 0;
    }

    public void CloseDialogue()
    {
        if (!IsOpen) return;
        ResetBox();
        OnDialogueEnded?.Invoke();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (IsOpen && ExplorationInput.Pressed(UnityEngine.InputSystem.Key.Escape)) CloseDialogue();
    }

    private void OnDestroy()
    {
        if (temporaryDialogue != null) Destroy(temporaryDialogue);
        if (instance != this) return;
        if (IsOpen) OnDialogueEnded?.Invoke();
        instance = null;
    }

}
