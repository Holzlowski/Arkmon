using UnityEngine;

public class PaintedWardrobe : MonoBehaviour
{
    [SerializeField] private Transform leftDoor, rightDoor;
    [SerializeField] private GameObject mantle;
    private float openness;
    private bool previewOpen;
    private bool interactionOpen;
    private void Awake() { if(GetComponent<QuestHint>()==null)gameObject.AddComponent<QuestHint>(); }
    private void Start()
    {
        DemoInteractable nearest=null;float distance=float.PositiveInfinity;
        foreach(var item in FindObjectsByType<DemoInteractable>(FindObjectsSortMode.None))
        {
            float candidate=(item.transform.position-transform.position).sqrMagnitude;
            if(item.kind==DemoInteractable.Kind.Wardrobe && candidate<distance){nearest=item;distance=candidate;}
        }
        if(nearest!=null)GetComponent<QuestHint>().SetInteractionTarget(nearest);
    }
    public void SetInteractionOpen(bool open) => interactionOpen = open;
    public void OpenForPreview(bool open) => previewOpen=open;
    private void LateUpdate()
    {
        bool open=previewOpen || interactionOpen;
        openness=Mathf.MoveTowards(openness,open?65f:0f,Time.unscaledDeltaTime*120f);
        if(leftDoor!=null)leftDoor.localRotation=Quaternion.Euler(0,openness,0);
        if(rightDoor!=null)rightDoor.localRotation=Quaternion.Euler(0,-openness,0);
        if(mantle!=null)mantle.SetActive(!DemoOpening.HasMantle);
        GetComponent<QuestHint>()?.SetObjectiveActive(!DemoOpening.HasMantle);
    }
}
