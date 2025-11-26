using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;
    public float jumpForce = 8f;
    public float airControl = 0.5f; // How much control in air (0-1)

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;

    [Header("Camera")]
    public Transform cameraRoot;
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private float cameraRotationX = 0f;

    // Input System variables
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;

    void Start()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        rb.maxAngularVelocity = 7f; // Prevent excessive spinning

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMouseLook();

        // Jump input
        if (jumpInput && isGrounded)
        {
            Jump();
            jumpInput = false;
        }

        // Unlock cursor with Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        CheckGrounded();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (cameraRoot == null) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = moveInput.x;
        float vertical = moveInput.y;

        // Calculate move direction relative to where player is facing
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize();

        // Apply air control multiplier
        float controlMultiplier = isGrounded ? 1f : airControl;

        // Add force for movement
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 velocityChange = targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        velocityChange *= controlMultiplier;

        // Limit max speed
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        // Apply ground friction when not moving
        if (isGrounded && moveDirection.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y, rb.linearVelocity.z * 0.9f);
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void CheckGrounded()
    {
        // Raycast down from center of capsule
        float rayDistance = (capsuleCollider.height / 2f) + groundCheckDistance;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayDistance, groundLayer);
    }

    // Input System callbacks
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jumpInput = value.isPressed;
    }

    // Optional: Visual debug for ground check
    void OnDrawGizmos()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        Gizmos.color = isGrounded ? Color.green : Color.red;
        float rayDistance = (capsuleCollider.height / 2f) + groundCheckDistance;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * rayDistance);
    }

    // Public method to apply external forces (for knockback, explosions, etc)
    public void ApplyKnockback(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }
}
