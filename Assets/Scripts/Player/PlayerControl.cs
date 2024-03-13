using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        ApplyFallMultiplier();
        if (Input.GetKeyUp(KeyCode.E))
        {
            TryInteract();
        }
        ToggleObject();

    }

    private void TryInteract()
    {
        if (inConversation)
        {
            DialogueBoxController.instance.SkipLine();
        }
        else
        {
            List<IInteractableArk> interactableArks = new List<IInteractableArk>();
            Collider[] colliderArray = Physics.OverlapSphere(transform.position, interacRange);
            foreach (Collider collider in colliderArray)
            {
                if (collider.TryGetComponent(out IInteractableArk interactable))
                {
                    //interactable.InteractWithPlayer();
                    interactableArks.Add(interactable);
                }
            }

            IInteractableArk closestInteractable = null;
            foreach (IInteractableArk interactable in interactableArks)
            {
                if (closestInteractable == null)
                {
                    closestInteractable = interactable;
                }
                else
                {
                    Vector3 interactablePosition = interactable.GetTransform();
                    //Vector3 interactablePosition = (interactable as MonoBehaviour).transform.position;
                    Debug.Log(interactablePosition);
                    if (Vector3.Distance(transform.position, interactablePosition) <
                        Vector3.Distance(transform.position, closestInteractable.GetTransform()))
                    {
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != null)
            {
                closestInteractable.InteractWithPlayer();
            }
            else
            {
                Debug.Log("Nix zum interagieren hier");
            }
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
        if (Input.GetKeyDown("space") && isGrounded)
        {
            Jump();
        }
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, 0, verticalInput).normalized * speed * Time.deltaTime;

        if (!isJumping)
        {
            transform.Translate(movement);
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void ApplyFallMultiplier()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
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
        DialogueBoxController.OnDialogueStarted -= JoinConversation;
        DialogueBoxController.OnDialogueEnded -= LeaveConversation;
    }

}
