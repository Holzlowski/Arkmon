using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PaperChoiceVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private void Highlight(bool selected)
    {
        PaperUIStyle style = PaperUIStyle.Load();
        GetComponent<Image>().sprite = selected ? style.sageButton : style.card;
        GetComponentInChildren<TextMeshProUGUI>().color = selected ? style.cream : style.ink;
    }
    public void OnPointerEnter(PointerEventData data) { EventSystem.current?.SetSelectedGameObject(gameObject); Highlight(true); }
    public void OnPointerExit(PointerEventData data) { Highlight(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject); }
    public void OnSelect(BaseEventData data) => Highlight(true);
    public void OnDeselect(BaseEventData data) => Highlight(false);
    private void OnDisable() { if (PaperUIStyle.Load() != null) Highlight(false); }
}
