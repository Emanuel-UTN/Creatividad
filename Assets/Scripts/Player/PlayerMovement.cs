using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 6f;
    public float sprintMultiplier = 1.7f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Suelo")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Camara")]
    public Transform cameraPivot; // objeto pivot (hijo del player, a la altura de la cabeza)
    public float lookSensitivity = 0.1f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Camara en escondite")]
    public float hiddenYawLimit = 55f;
    public float hiddenMinPitch = -25f;
    public float hiddenMaxPitch = 30f;

    private CharacterController controller;
    private PlayerInput playerInput;
    private PlayerController playerController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;

    private float verticalVelocity;
    private float pitch;
    private float hiddenBaseYaw;
    private bool isGrounded;
    private bool movementLocked;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions.FindAction("Sprint", false);
        interactAction = playerInput.actions.FindAction("Accion", false);
        if (interactAction == null)
            interactAction = playerInput.actions.FindAction("Interact", false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        if (GameController.gameController != null)
            GameController.gameController.player = gameObject;
    }

    void Update()
    {
        if (interactAction != null && interactAction.WasPressedThisFrame() && playerController != null)
            playerController.TryInteractWithCupboard();

        Vector2 look = lookAction.ReadValue<Vector2>();
        float mouseX = look.x * lookSensitivity;
        float mouseY = look.y * lookSensitivity;

        if (movementLocked)
        {
            UpdateHiddenLook(mouseX, mouseY);
            return;
        }

        float dt = Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (jumpAction.WasPressedThisFrame() && isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * dt;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        bool isMoving = move.sqrMagnitude > 0.0001f;
        bool sprintRequested = sprintAction != null && sprintAction.IsPressed();
        bool shouldTrySprint = sprintRequested && isMoving;

        bool isSprinting = shouldTrySprint;
        if (playerController != null)
            isSprinting = playerController.ResolveSprint(shouldTrySprint, dt);

        float currentSpeed = speed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        Vector3 motion = move * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(motion * dt);
    }

    public void SetMovementLocked(bool locked)
    {
        if (locked && !movementLocked)
            hiddenBaseYaw = transform.eulerAngles.y;

        movementLocked = locked;
        if (locked)
            verticalVelocity = 0f;
    }

    public void TeleportTo(Vector3 worldPosition)
    {
        if (controller != null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = worldPosition;
            controller.enabled = wasEnabled;
            return;
        }

        transform.position = worldPosition;
    }

    public void SetLookDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        hiddenBaseYaw = transform.eulerAngles.y;
        pitch = 0f;
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateHiddenLook(float mouseX, float mouseY)
    {
        float currentYaw = transform.eulerAngles.y;
        float deltaFromBase = Mathf.DeltaAngle(hiddenBaseYaw, currentYaw);
        float nextDelta = Mathf.Clamp(deltaFromBase + mouseX, -hiddenYawLimit, hiddenYawLimit);
        float nextYaw = hiddenBaseYaw + nextDelta;

        transform.rotation = Quaternion.Euler(0f, nextYaw, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, hiddenMinPitch, hiddenMaxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}