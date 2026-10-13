using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameObject quadPlayer;
    public GameObject capsulePlayer;
    private Rigidbody rb;

    public float speed = 5f;
    public float jumpForce = 10f;
    private bool isGrounded;
    private bool isJumping;
    public float fallMultiplier = 2.5f; // Multiplikator für die Fallgeschwindigkeit

    public float interacRange = 3f;

    bool inConversation;
    private PlayerInteractionFocus interactionFocus;
    private Vector2 moveInput;
    private bool jumpRequested;
    public bool IsMoving => rb != null && moveInput.sqrMagnitude > .01f
        && new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).sqrMagnitude > .02f
        && !inConversation && !SceneLoadManager.IsTransitioning && !DemoOpening.IsPresenting;
    private void Awake() => interactionFocus = GetComponent<PlayerInteractionFocus>() ?? gameObject.AddComponent<PlayerInteractionFocus>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<PaperCharacterVisual>()?.SetRestingSprite(PlayerEquipmentAppearance.RestingSprite);
    }

    void Update()
    {
        if (SceneLoadManager.IsTransitioning || DemoOpening.IsPresenting || inConversation)
        {
            moveInput = Vector2.zero; jumpRequested = false;
            if (inConversation && Input.GetKeyUp(KeyCode.E)) TryInteract();
            return;
        }
        moveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
        jumpRequested |= Input.GetKeyDown(KeyCode.Space);
        if (Input.GetKeyUp(KeyCode.E))
        {
            TryInteract();
        }
        ToggleObject();
    }

    private void FixedUpdate()
    {
        if (inConversation || SceneLoadManager.IsTransitioning || DemoOpening.IsPresenting)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
        HandleMovement();
        HandleJump();
        ApplyFallMultiplier();
    }

    private void TryInteract()
    {
        if (inConversation)
        {
            DialogueBoxController.instance.SkipLine();
        }
        else
        {
            interactionFocus.Refresh()?.InteractWithPlayer();
        }
    }

    private void ToggleObject()
    {
        // Objekte wechseln
        if (Input.GetKeyDown(KeyCode.C))
        {
            TogglePlayerObjects();
        }
    }

    void TogglePlayerObjects()
    {
        quadPlayer.SetActive(!quadPlayer.activeSelf);
        capsulePlayer.SetActive(!capsulePlayer.activeSelf);
    }


    private void HandleJump()
    {
        // Springen
        if (jumpRequested && isGrounded)
        {
            Jump();
        }
        jumpRequested = false;
    }

    private void HandleMovement()
    {
        ApplyMovement(moveInput);
    }

    // Let physics resolve walls instead of teleporting the Transform each fixed tick.
    private void ApplyMovement(Vector2 input)
    {
        if (isJumping) return;
        Vector3 movement = new Vector3(input.x, 0, input.y) * speed;
        if (movement.sqrMagnitude > speed * speed) movement = movement.normalized * speed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void ApplyFallMultiplier()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Überprüfung, ob der Spieler am Boden ist
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void JoinConversation()
    {
        inConversation = true;
    }

    void LeaveConversation()
    {
        inConversation = false;
    }

    private void OnEnable()
    {
        DialogueBoxController.OnDialogueStarted += JoinConversation;
        DialogueBoxController.OnDialogueEnded += LeaveConversation;
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero; jumpRequested = false;
        if(rb != null && !rb.isKinematic) rb.linearVelocity = new Vector3(0,rb.linearVelocity.y,0);
        DialogueBoxController.OnDialogueStarted -= JoinConversation;
        DialogueBoxController.OnDialogueEnded -= LeaveConversation;
    }

}
