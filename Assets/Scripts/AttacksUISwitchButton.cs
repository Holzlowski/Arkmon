using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttacksUISwitchButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(() =>
        {
            Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
            selectedUnit.SwitchAttacksUI();
        });
    }

    private void Update()
    {
        SwitchLabelText();
    }
    private void OnEnable()
    {
        // Abonniere das Ereignis beim Aktivieren des Skripts
        Unit.OnAttackUISwitch += SwitchLabelTextOnEvent;
    }

    private void OnDisable()
    {
        // Entferne das Abonnement beim Deaktivieren des Skripts
        Unit.OnAttackUISwitch -= SwitchLabelTextOnEvent;
    }

    public void SwitchLabelTextOnEvent(object sender, EventArgs e)
    {
        SwitchLabelText();
    }

    private void SwitchLabelText()
    {
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        label.text = selectedUnit.GetIsInAttackSelection() ? "BACK" : "ATTACKS";
    }
}
