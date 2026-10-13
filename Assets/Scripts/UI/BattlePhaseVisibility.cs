using UnityEngine;

// Keep UI controllers subscribed while hiding controls outside the combat phase.
[RequireComponent(typeof(CanvasGroup))]
public class BattlePhaseVisibility : MonoBehaviour
{
    private CanvasGroup group;
    private void Awake() { group = GetComponent<CanvasGroup>(); Refresh(); }
    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        bool visible = UnitManager.Instance != null && UnitManager.Instance.IsBattleRunning
            && !SceneLoadManager.IsTransitioning;
        group.alpha = visible ? 1 : 0;
        group.interactable = group.blocksRaycasts = visible;
    }
}
