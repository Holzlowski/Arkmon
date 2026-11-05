using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(9000)]
public class CourtyardCamera : MonoBehaviour
{
    [SerializeField] private Vector2 horizontalBounds=new Vector2(-34,52);
    [SerializeField] private Vector2 depthBounds=new Vector2(-9,11);
    [SerializeField] private float distance=21, pitch=32, fieldOfView=45, damping=.28f;
    [SerializeField] private float focusHeight=1.1f;
    [SerializeField] private float gateYaw=38;
    [SerializeField] private Vector2 gateTurnRange=new Vector2(28,41);
    private Camera view;private Transform player;private Vector3 focus,velocity;
    private CinemachineBrain brain;private bool previousBrain;
    private float currentYaw;
    private void Start()
    {
        view=Camera.main;player=GameObject.FindWithTag("Player")?.transform;
        if(view==null || player==null){enabled=false;return;}
        brain=view.GetComponent<CinemachineBrain>();if(brain!=null){previousBrain=brain.enabled;brain.enabled=false;}
        focus=Target();currentYaw=DesiredYaw();Apply();
    }
    private Vector3 Target()=>new Vector3(Mathf.Clamp(player.position.x,horizontalBounds.x,horizontalBounds.y),focusHeight,Mathf.Clamp(player.position.z+3,depthBounds.x,depthBounds.y));
    private float DesiredYaw()=>gateYaw*Mathf.SmoothStep(0,1,Mathf.InverseLerp(gateTurnRange.x,gateTurnRange.y,focus.x));
    private void LateUpdate()
    {
        if(view==null || player==null)return;
        focus=Vector3.SmoothDamp(focus,Target(),ref velocity,damping,Mathf.Infinity,Time.unscaledDeltaTime);
        // Do not catch up an entire camera turn in one frame after a short rendering hitch.
        currentYaw=Mathf.MoveTowardsAngle(currentYaw,DesiredYaw(),45*Mathf.Min(Time.unscaledDeltaTime,.05f));
        Apply();
    }
    private bool scenic;
    private Vector3 scenicPosition;
    private Quaternion scenicRotation;
    private float scenicFov;
    public void SetScenicView(Vector3 position, Quaternion rotation, float fov)
    {
        scenic = true; scenicPosition = position; scenicRotation = rotation; scenicFov = fov;
        Apply();
    }
    public void ClearScenicView() { scenic = false; if (view != null) Apply(); }
    private void Apply()
    {
        if (scenic)
        {
            view.transform.SetPositionAndRotation(scenicPosition, scenicRotation);
            view.fieldOfView = scenicFov;
            return;
        }
        var angle = Quaternion.Euler(pitch, currentYaw, 0);
        view.transform.SetPositionAndRotation(focus - angle * Vector3.forward * distance, angle);
        view.fieldOfView = fieldOfView;
    }
    private void OnDisable(){if(brain!=null)brain.enabled=previousBrain;}
}
