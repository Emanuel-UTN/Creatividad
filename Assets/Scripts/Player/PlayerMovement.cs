using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(StaminaComponent))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 6f;
    public float sprintMultiplier = 1.7f;
    public float crouchSpeedMultiplier = 0.45f;
    public float crouchHeightMultiplier = 0.6f;
    public float crouchCameraDrop = 0.55f;
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

    [Header("Mouse Smoothing")]
    public bool smoothMouse = true;
    [Range(5f, 40f)]
    public float mouseSmoothingDecay = 18f;
    private float smoothMouseX = 0f;
    private float smoothMouseY = 0f;

    [Header("Camara en escondite")]
    public float hiddenYawLimit = 55f;
    public float hiddenMinPitch = -30f;
    public float hiddenMaxPitch = 30f;

    [Header("Damage Shake")]
    public float shakeDuration = .2f;
    public float shakeMagnitude = .06f;
    public float shakeSpeed = 25f;

    [Header("Head Bobbing")]
    public bool useHeadBob = true;
    public float bobFrequencyWalk = 12f;
    public float bobFrequencyRun = 16f;
    public float bobAmountWalk = 0.05f;
    public float bobAmountRun = 0.10f;
    private float bobTimer = 0f;

    private float currentShakeTime;

    private CharacterController controller;
    private PlayerInput playerInput;
    private PlayerController playerController;
    private StaminaComponent staminaComponent;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private float verticalVelocity;
    private float pitch;
    private float hiddenBaseYaw;
    private bool isGrounded;
    private bool movementLocked;
    private bool isMoving;
    private bool isSprinting;
    private bool isCrouching;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingCameraLocalPosition;

    public bool IsGrounded => isGrounded;
    public bool IsMoving => isMoving;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsMovementLocked => movementLocked;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
        staminaComponent = GetComponent<StaminaComponent>();

        standingHeight = controller.height;
        standingCenter = controller.center;
        if (cameraPivot != null)
            standingCameraLocalPosition = cameraPivot.localPosition;
        
        if (playerInput != null && playerInput.actions != null)
        {
            moveAction = playerInput.actions.FindAction("Move", false) ?? playerInput.actions["Move"];
            lookAction = playerInput.actions.FindAction("Look", false) ?? playerInput.actions["Look"];
            jumpAction = playerInput.actions.FindAction("Jump", false) ?? playerInput.actions["Jump"];
            sprintAction = playerInput.actions.FindAction("Sprint", false);
            crouchAction = playerInput.actions.FindAction("Crouch", false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GetComponent<DustParticles>() == null)
            gameObject.AddComponent<DustParticles>();
    }

    void Start()
    {
        if (GameController.gameController != null)
            GameController.gameController.player = gameObject;

        // Load mouse sensitivity setting
        lookSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", lookSensitivity);
    }

    void Update()
    {
        if (GameController.IsPaused)
            return;

        // Re-lock cursor on click in WebGL/Browser if it gets unlocked during gameplay
        if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float dt = Time.deltaTime;
        Vector2 look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        
        float targetMouseX = look.x * lookSensitivity;
        float targetMouseY = look.y * lookSensitivity;

        // WebGL/Browser Pointer Lock delta spike mitigation during lag spikes:
        // Clamping the maximum rotation per frame prevents the camera from snapping wildly.
        float maxRotationPerFrame = 20f; 
        targetMouseX = Mathf.Clamp(targetMouseX, -maxRotationPerFrame, maxRotationPerFrame);
        targetMouseY = Mathf.Clamp(targetMouseY, -maxRotationPerFrame, maxRotationPerFrame);

        if (smoothMouse)
        {
            float lerpFactor = 1f - Mathf.Exp(-mouseSmoothingDecay * dt);
            smoothMouseX = Mathf.Lerp(smoothMouseX, targetMouseX, lerpFactor);
            smoothMouseY = Mathf.Lerp(smoothMouseY, targetMouseY, lerpFactor);
        }
        else
        {
            smoothMouseX = targetMouseX;
            smoothMouseY = targetMouseY;
        }

        float mouseX = smoothMouseX;
        float mouseY = smoothMouseY;

        if (movementLocked)
        {
            isMoving = false;
            isSprinting = false;
            isCrouching = false;
            UpdateHiddenLook(mouseX, mouseY);
            return;
        }

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * dt;

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        isMoving = move.sqrMagnitude > 0.0001f;
        isCrouching = crouchAction != null && crouchAction.IsPressed();
        UpdateCrouchPose();
        bool sprintRequested = sprintAction != null && sprintAction.IsPressed();
        bool shouldTrySprint = sprintRequested && isMoving && !isCrouching;

        bool isHidden = playerController != null && playerController.IsHidden;
        isSprinting = GameController.IsCreativoEnabled
            ? shouldTrySprint && !isHidden
            : staminaComponent != null
                ? staminaComponent.ResolveSprint(shouldTrySprint, isHidden, dt)
                : shouldTrySprint;

        float currentSpeed = speed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;
        else if (isCrouching)
            currentSpeed *= Mathf.Clamp(crouchSpeedMultiplier, 0.05f, 1f);

        Vector3 motion = move * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(motion * dt);

        UpdateCameraShake(dt);
    }

    private void UpdateCrouchPose()
    {
        if (controller == null)
            return;

        if (!isCrouching)
        {
            controller.height = standingHeight;
            controller.center = standingCenter;
            if (cameraPivot != null)
                cameraPivot.localPosition = standingCameraLocalPosition;
            return;
        }

        float crouchedHeight = Mathf.Max(0.5f, standingHeight * Mathf.Clamp(crouchHeightMultiplier, 0.3f, 1f));
        float heightDelta = standingHeight - crouchedHeight;

        controller.height = crouchedHeight;
        controller.center = standingCenter - new Vector3(0f, heightDelta * 0.5f, 0f);

        if (cameraPivot != null)
            cameraPivot.localPosition = standingCameraLocalPosition + Vector3.down * Mathf.Max(0f, crouchCameraDrop);
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

    private void UpdateCameraShake(float dt)
    {
        if (cameraPivot == null)
            return;

        Vector3 basePos = standingCameraLocalPosition;
        if (isCrouching)
            basePos += Vector3.down * crouchCameraDrop;

        // Apply Head Bobbing
        Vector3 bobOffset = Vector3.zero;
        if (useHeadBob && isMoving && isGrounded && !movementLocked)
        {
            float speedFactor = isSprinting ? bobFrequencyRun : bobFrequencyWalk;
            bobTimer += dt * speedFactor;

            float bobAmount = isSprinting ? bobAmountRun : bobAmountWalk;
            if (isCrouching)
                bobAmount *= 0.5f;

            bobOffset.y = Mathf.Sin(bobTimer) * bobAmount;
            bobOffset.x = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
        }
        else
        {
            // Reset bobTimer slowly when not moving
            bobTimer = Mathf.Lerp(bobTimer, 0f, dt * 5f);
        }

        Vector3 targetPos = basePos + bobOffset;

        if (currentShakeTime > 0f)
        {
            currentShakeTime -= dt;

            float strength = currentShakeTime / shakeDuration;

            Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude * strength;

            cameraPivot.localPosition = targetPos + randomOffset;
        }
        else
        {
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPos, dt * shakeSpeed);
        }
    }

    public void DamageShake()
    {
        currentShakeTime = shakeDuration;
    }
}
