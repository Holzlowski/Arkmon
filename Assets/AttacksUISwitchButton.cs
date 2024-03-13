using UnityEngine;
using UnityEngine.UI;

public class AttacksUISwitchButton : MonoBehaviour
{
    [SerializeField] private Button button;
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(() =>
        {
            Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
            selectedUnit.SwitchAttacksUI();
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
