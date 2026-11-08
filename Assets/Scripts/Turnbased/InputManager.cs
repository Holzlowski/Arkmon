using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one LevelGrid! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        playerInputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (Instance == this) playerInputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        playerInputActions?.Player.Disable();
    }

    private void OnDestroy()
    {
        playerInputActions?.Dispose();
        playerInputActions = null;
        if (Instance == this) Instance = null;
    }

    public Vector2 GetMouseScreenPosition()
    {
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    public bool IsMouseButtonDownThisFrame()
    {
        return playerInputActions.Player.Click.WasPressedThisFrame();
    }

    public Vector2 GetCameraMoveVector()
    {
        return playerInputActions.Player.CameraMovement.ReadValue<Vector2>();
    }

    public float GetCameraRotateAmount()
    {
        return playerInputActions.Player.CameraRotate.ReadValue<float>();
    }

    public float GetCameraZoomAmount()
    {
        return playerInputActions.Player.CameraZoom.ReadValue<float>();
    }
}
