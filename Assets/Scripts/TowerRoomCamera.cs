using Cinemachine;
using UnityEngine;

// A fixed view with restrained tracking: walking never changes angle or zoom.
[DefaultExecutionOrder(9000)]
public class TowerRoomCamera : MonoBehaviour
{
    [SerializeField] private Vector3 focus = new Vector3(0,1,2);
    [SerializeField] private Vector3 angles = new Vector3(25,0,0);
    [SerializeField] private float distance = 12, fieldOfView = 40;
    [SerializeField] private Vector2 trackingLimits = new Vector2(.85f,.55f);
    [SerializeField] private float trackingWeight = .18f, smoothing = .35f;
    private Transform player;
    private Vector3 tracking, velocity;
    private Camera view;
    private CinemachineBrain brain;
    private bool brainWasEnabled;
    private Vector3 presentationOffset;
    private float presentationWeight;
    private float presentationBlend, presentationVelocity;
    public void SetPresentationFocus(Vector3 target, float weight)
    {
        if(weight > 0) presentationOffset = new Vector3(Mathf.Clamp(target.x-focus.x,-.8f,.8f), 0, Mathf.Clamp(target.z-focus.z,-.4f,.4f));
        presentationWeight = Mathf.Clamp01(weight);
    }
    private void Start()
    {
        view = Camera.main; player = GameObject.FindWithTag("Player")?.transform;
        if(view == null) { enabled = false; return; }
        brain = view.GetComponent<CinemachineBrain>();
        if(brain != null) { brainWasEnabled = brain.enabled; brain.enabled = false; }
        ApplyView();
    }
    private void LateUpdate()
    {
        if(view == null) return;
        Vector3 target = Vector3.zero;
        if(player != null) {
            target.x = Mathf.Clamp((player.position.x-focus.x)*trackingWeight,-trackingLimits.x,trackingLimits.x);
            target.z = Mathf.Clamp((player.position.z-focus.z)*trackingWeight,-trackingLimits.y,trackingLimits.y);
        }
        tracking = Vector3.SmoothDamp(tracking,target,ref velocity,smoothing,Mathf.Infinity,Time.unscaledDeltaTime);
        presentationBlend = Mathf.SmoothDamp(presentationBlend,presentationWeight,ref presentationVelocity,.25f,Mathf.Infinity,Time.unscaledDeltaTime);
        ApplyView();
    }
    private void ApplyView()
    {
        Quaternion rotation = Quaternion.Euler(angles);
        view.transform.SetPositionAndRotation(focus+tracking+presentationOffset*presentationBlend-rotation*Vector3.forward*distance,rotation);
        view.fieldOfView = fieldOfView;
    }
    private void OnDisable() { if(brain != null) brain.enabled = brainWasEnabled; }
}
