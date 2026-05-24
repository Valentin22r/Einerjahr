using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller FPS complet : WASD mouvement, sourie look, Shift sprint,
/// Ctrl crouch, Space saut (optionnel). Utilise CharacterController (pas de
/// Rigidbody — pas de glitches physiques).
///
/// Note : ce controller fait à la fois le mouvement ET le look (yaw player +
/// pitch cameraPivot). Plus besoin de PlayerAim séparé. Le SpellCaster
/// utilise PlayerAim pour aim, mais si tu as déjà PlayerAim sur le player
/// les deux coexistent — préférer une seule source de rotation.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Mouvement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    [Tooltip("Lissage de l'accélération horizontale (s). 0 = instantané.")]
    public float accelSmoothing = 0.05f;

    [Header("Gravité & saut")]
    public float gravity = -20f;
    public float groundedGravity = -2f;
    public bool canJump = false;
    public float jumpHeight = 1.5f;

    [Header("Crouch")]
    public bool canCrouch = true;
    public float standingHeight = 2f;
    public float crouchingHeight = 1.2f;
    public float crouchTransitionSpeed = 10f;

    [Header("Look (souris)")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.12f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    public bool lockCursorOnStart = true;
    public Key toggleCursorKey = Key.Escape;

    CharacterController controller;
    Vector3 currentHorizontalVelocity;
    Vector3 horizontalVelocityRef;
    float verticalVelocity;
    float yaw;
    float pitch;
    bool crouching;
    float targetHeight;

    public bool IsSprinting { get; private set; }
    public bool IsCrouching => crouching;
    public bool IsGrounded => controller.isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        targetHeight = standingHeight;
        if (controller != null) controller.height = standingHeight;
    }

    void Start()
    {
        yaw = transform.eulerAngles.y;
        if (cameraPivot != null)
        {
            float p = cameraPivot.localEulerAngles.x;
            pitch = p > 180f ? p - 360f : p;
        }
        if (lockCursorOnStart) SetCursorLocked(true);
    }

    void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    void HandleLook()
    {
        if (Keyboard.current != null && Keyboard.current[toggleCursorKey].wasPressedThisFrame)
            SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);

        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleCrouch()
    {
        if (canCrouch && Keyboard.current != null)
        {
            bool wantCrouch = Keyboard.current.leftCtrlKey.isPressed;
            crouching = wantCrouch;
            targetHeight = crouching ? crouchingHeight : standingHeight;
        }
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        Vector3 c = controller.center;
        c.y = controller.height / 2f;
        controller.center = c;
    }

    void HandleMovement()
    {
        float x = 0f, z = 0f;
        bool sprintPressed = false;
        bool jumpPressed = false;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            sprintPressed = Keyboard.current.leftShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        Vector3 inputDir = new Vector3(x, 0f, z);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float targetSpeed = crouching ? crouchSpeed : (sprintPressed && inputDir.z > 0.1f ? sprintSpeed : walkSpeed);
        IsSprinting = sprintPressed && !crouching && inputDir.z > 0.1f;

        Vector3 worldDir = transform.TransformDirection(inputDir);
        Vector3 targetHorizontal = worldDir * targetSpeed;
        currentHorizontalVelocity = Vector3.SmoothDamp(currentHorizontalVelocity, targetHorizontal, ref horizontalVelocityRef, accelSmoothing);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = groundedGravity;
        verticalVelocity += gravity * Time.deltaTime;

        if (canJump && jumpPressed && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);

        Vector3 motion = currentHorizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
