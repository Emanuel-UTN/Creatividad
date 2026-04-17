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

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private float verticalVelocity;
    private float pitch;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions.FindAction("Sprint", false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        GameManager.gameManager.player = gameObject;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        Vector2 look = lookAction.ReadValue<Vector2>();
        float mouseX = look.x * lookSensitivity;
        float mouseY = look.y * lookSensitivity;

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

        float currentSpeed = speed;
        if (sprintAction != null && sprintAction.IsPressed())
            currentSpeed *= sprintMultiplier;

        Vector3 motion = move * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(motion * dt);
    }
}