using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(9000)]
public class CourtyardCamera : MonoBehaviour
{
    [SerializeField] private Vector2 horizontalBounds=new Vector2(-34,52);
    [SerializeField] private Vector2 depthBounds=new Vector2(-9,11);
    [SerializeField] private float distance=21, pitch=32, fieldOfView=45, damping=.28f;
    [SerializeField] private float focusHeight=1.1f;
    private Camera view;private Transform player;private Vector3 focus,velocity;
    private CinemachineBrain brain;private bool previousBrain;
    private void Start()
    {
        view=Camera.main;player=GameObject.FindWithTag("Player")?.transform;
        if(view==null || player==null){enabled=false;return;}
        brain=view.GetComponent<CinemachineBrain>();if(brain!=null){previousBrain=brain.enabled;brain.enabled=false;}
        focus=Target();Apply();
    }
    private Vector3 Target()=>new Vector3(Mathf.Clamp(player.position.x,horizontalBounds.x,horizontalBounds.y),focusHeight,Mathf.Clamp(player.position.z+3,depthBounds.x,depthBounds.y));
    private void LateUpdate(){if(view==null || player==null)return;focus=Vector3.SmoothDamp(focus,Target(),ref velocity,damping,Mathf.Infinity,Time.unscaledDeltaTime);Apply();}
    private void Apply(){var angle=Quaternion.Euler(pitch,0,0);view.transform.SetPositionAndRotation(focus-angle*Vector3.forward*distance,angle);view.fieldOfView=fieldOfView;}
    private void OnDisable(){if(brain!=null)brain.enabled=previousBrain;}
}
