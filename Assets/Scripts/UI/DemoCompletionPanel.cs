using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoCompletionPanel : MonoBehaviour
{
    private static DemoCompletionPanel current;
    private static bool shown;
    public static bool IsOpen => current != null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession(){current=null;shown=false;}
    public static void Queue()
    {
        if(shown || !DemoOpening.Started)return;
        var root=new GameObject("Demo conclusion",typeof(DemoCompletionPanel));root.GetComponent<DemoCompletionPanel>().StartCoroutine(root.GetComponent<DemoCompletionPanel>().WaitForDialogue());
    }
    private IEnumerator WaitForDialogue()
    {
        while(DialogueBoxController.instance != null && DialogueBoxController.instance.IsOpen)yield return null;
        if(shown){Destroy(gameObject);yield break;}
        shown=true;current=this;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        gameObject.AddComponent<GraphicRaycaster>();PaperUIFactory.Scale(canvas);
        var paper=PaperUIFactory.Image("Demo completion paper",transform,PaperUIStyle.Load().panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-330,-145),new Vector2(330,145));
        PaperUIFactory.Text("Conclusion",paper.transform,"Prüfung bestanden!\n\nNeuer Auftrag · Erwarte die Arkanmeisterin.\nDie Demo endet hier. John bietet weitere Übungskämpfe an.",24,Vector2.zero,Vector2.one,new Vector2(30,82),new Vector2(-30,-22),TextAlignmentOptions.Center);
        var button=PaperUIFactory.Button("Continue practicing",paper.transform,PaperUIStyle.Load().sageButton,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-120,22),new Vector2(120,70));
        PaperUIFactory.Text("Continue label",button.transform,"Zurück in den Hof",22,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAlignmentOptions.Center);
        button.onClick.AddListener(Close);
    }
    public void Close(){if(current==this)current=null;Destroy(gameObject);}
    private void Update(){if(current==this && Input.GetKeyDown(KeyCode.Escape))Close();}
    private void OnDestroy(){if(current==this)current=null;}
}
