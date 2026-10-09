using UnityEngine;

public class ActionBusyUI : MonoBehaviour
{
    private UnitActionSystem actionSystem;

    private void Start()
    {
        actionSystem = UnitActionSystem.Instance;
        actionSystem.OnBusyChange += UnitActionSystem_OnBusyChanged;

        Hide();
    }

    private void OnDestroy()
    {
        if (actionSystem != null) actionSystem.OnBusyChange -= UnitActionSystem_OnBusyChanged;
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UnitActionSystem_OnBusyChanged(object sender, bool isBusy)
    {
        if (isBusy)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
}
