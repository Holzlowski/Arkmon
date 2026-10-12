using UnityEngine;

public class ActionBusyUI : MonoBehaviour
{
    private UnitActionSystem actionSystem;

    private void Start()
    {
        PaperUIStyle style = PaperUIStyle.Load();
        if (style != null)
        {
            PaperUIFactory.HideChildren(transform);
            PaperUIFactory.Fill((RectTransform)transform);
            var paper = PaperUIFactory.Image("Busy paper hint", transform, style.panel,
                new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-160, 236), new Vector2(160, 290));
            PaperUIFactory.Text("Busy label", paper.transform, "Aktion läuft ...", 18,
                Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8), TMPro.TextAlignmentOptions.Center);
        }
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
