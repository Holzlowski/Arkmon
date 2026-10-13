using UnityEngine;

[DisallowMultipleComponent]
public class CollectiblePlayerLamp : MonoBehaviour
{
    [SerializeField] private GameObject appearance;
    private void Start() => Refresh();
    public void Refresh()
    {
        bool available = !DemoOpening.HasLamp;
        if (appearance != null) appearance.SetActive(available);
        foreach (var collider in GetComponents<Collider>()) collider.enabled = available;
        GetComponent<QuestHint>()?.SetObjectiveActive(available);
    }
}
