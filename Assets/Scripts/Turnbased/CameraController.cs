using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float MIN_FOLLOW_Y_OFFSET = 2f;
    private const float MAX_FOLLOW_Y_OFFSET = 12f;
    [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;
    private CinemachineTransposer cinemachineTransposer;

    private Vector3 targetFollowOffset;
    float moveSpeed = 20;
    float rotationSpeed = 100f;
    float zoomSpeed = 5f;
    public bool IsPresentationLocked { get; set; }

    public CameraState PrepareBattleOverview(Vector3 center)
    {
        transform.position = center;
        if (cinemachineTransposer == null)
            cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        targetFollowOffset = new Vector3(0, 12, -18);
        cinemachineTransposer.m_FollowOffset = targetFollowOffset;
        cinemachineVirtualCamera.m_Lens.FieldOfView = 55;
        CinemachineComposer composer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer != null) composer.m_ScreenY = .32f;
        cinemachineVirtualCamera.PreviousStateIsValid = false;
        cinemachineVirtualCamera.UpdateCameraState(Vector3.up, -1);
        return cinemachineVirtualCamera.State;
    }

    void Start()
    {
        cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        targetFollowOffset = cinemachineTransposer.m_FollowOffset;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPresentationLocked || SceneLoadManager.IsTransitioning) return;
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector2 inputMoveDir = InputManager.Instance.GetCameraMoveVector();

        // transform.forward und right beziehen die Rotation des Objektes mit ein
        Vector3 moveVector = transform.forward * inputMoveDir.y + transform.right * inputMoveDir.x;

        transform.position += moveVector * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        Vector3 roataionVector = new Vector3(0, 0, 0);

        roataionVector.y = InputManager.Instance.GetCameraRotateAmount();

        transform.eulerAngles += roataionVector * rotationSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        float zoomIncreaseAmount = 1f;

        targetFollowOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomIncreaseAmount;

        targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);

        cinemachineTransposer.m_FollowOffset = Vector3.Lerp(cinemachineTransposer.m_FollowOffset, targetFollowOffset, Time.deltaTime * zoomSpeed);
    }


}
