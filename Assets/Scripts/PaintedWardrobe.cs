using UnityEngine;

public class PaintedWardrobe : MonoBehaviour
{
    [SerializeField] private Transform leftDoor, rightDoor;
    [SerializeField] private GameObject mantle;
    private float openness;
    private bool previewOpen;
    private bool interactionOpen;
    public void SetInteractionOpen(bool open) => interactionOpen = open;
    public void OpenForPreview(bool open) => previewOpen=open;
    private void LateUpdate()
    {
        bool open=previewOpen || interactionOpen;
        openness=Mathf.MoveTowards(openness,open?65f:0f,Time.unscaledDeltaTime*120f);
        if(leftDoor!=null)leftDoor.localRotation=Quaternion.Euler(0,openness,0);
        if(rightDoor!=null)rightDoor.localRotation=Quaternion.Euler(0,-openness,0);
        if(mantle!=null)mantle.SetActive(!DemoOpening.HasMantle);
    }
}
