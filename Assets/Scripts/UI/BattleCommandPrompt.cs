using TMPro;
using UnityEngine;

public class BattleCommandPrompt : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI text;
    private UnitActionSystemUI ui;
    private AttackPreview preview;
    public void Build(UnitActionSystemUI controller)
    {
        ui=controller;preview=controller.GetComponent<AttackPreview>();
        var image=PaperUIFactory.Image("Command phase hint",controller.transform,PaperUIStyle.Load().panel,
            new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-355,232),new Vector2(355,285));
        image.raycastTarget=false;panel=image.gameObject;
        text=PaperUIFactory.Text("Command instruction",image.transform,"",19,Vector2.zero,Vector2.one,new Vector2(18,6),new Vector2(-18,-6),TextAlignmentOptions.Center);
    }
    public static string Instruction(BaseAction action)
        => action==null ? "Bereich → Befehl → Zielfeld · Wähle einen Befehl in der unteren Leiste."
        : action is GuardAction ? "Abwehr · Wähle Kaflas eigenes Feld. Zurück bricht die Auswahl ab."
        : action is StandUpAction ? "Aufstehen · Wähle das eigene Feld deines Monsters."
        : PaperUIStyle.ActionName(action)+" · Wähle ein markiertes Zielfeld. Zurück bricht die Auswahl ab.";
    private void LateUpdate()
    {
        if(panel==null)return;
        var system=UnitActionSystem.Instance;
        bool visible=system!=null && UnitManager.Instance!=null && UnitManager.Instance.IsBattleRunning
            && system.GetSelectedAction()!=null
            && TurnSystem.Instance.IsPlayerTurn() && !BaseAction.IsAnyActionActive && !system.IsBusy
            && !SceneLoadManager.IsTransitioning && !ui.IsShowingExplanation && !(preview!=null && preview.IsShowing);
        panel.SetActive(visible);
        text.text = system != null && system.GetSelectedAction() == null
            ? ui.IsCategoryOpen ? "2 · Wähle einen Befehl. Erst danach bestimmst du das Zielfeld." : "1 · Wähle links Aktionen oder Attacken."
            : Instruction(system!=null?system.GetSelectedAction():null);
    }
}
