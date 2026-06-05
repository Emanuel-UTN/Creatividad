using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerFlashlightController))]
public class PlayerController : MonoBehaviour
{
    [Header("Puzzles Interactions")]
    public float interactionRange = 2f;
    public LayerMask interactionMask = Physics.DefaultRaycastLayers;
    private InputAction interactAction;
    private InputAction pauseAction;

    public static event System.Action<Cupboard, bool> OnPlayerEnteredCupboard;
    public static event System.Action<Cupboard, bool> OnPlayerExitedCupboard;
    public static event System.Action<float> OnFlashlightBatteryChanged
    {
        add => PlayerFlashlightController.OnFlashlightBatteryChanged += value;
        remove => PlayerFlashlightController.OnFlashlightBatteryChanged -= value;
    }
    public static event System.Action<int> OnKeyCountChanged;

    public static PlayerController playerController;

    private PlayerMovement playerMovement;
    private PlayerFlashlightController flashlightController;
    private Cupboard nearbyCupboard;
    private Cupboard currentCupboard;
    private EnemyBehaviour enemyBehaviour;
    private Camera playerCamera;

    public bool IsHidden => currentCupboard != null;
    public float CurrentFlashlightBattery => flashlightController != null ? flashlightController.CurrentFlashlightBattery : 0f;
    public float MaxFlashlightBattery => flashlightController != null ? flashlightController.MaxFlashlightBattery : 0f;
    public float FlashlightBatteryNormalized => flashlightController != null ? flashlightController.FlashlightBatteryNormalized : 0f;
    
    private int keyCount = 0;
    public int KeyCount
    {
        get => keyCount;
        set
        {
            keyCount = Mathf.Max(0, value);
            OnKeyCountChanged?.Invoke(keyCount);
            PlayerUI.playerUI?.UpdateKeyCount(keyCount);
        }
    }

    private SampleType? currentSampleType;
    public SampleType? CurrentSampleType
    {
        get => currentSampleType;
        set
        {
            currentSampleType = value;
            PlayerUI.playerUI?.UpdateSampleType(currentSampleType);
        }
    }

    void Awake()
    {
        if (playerController == null)
            playerController = this;
        else if (playerController != this)
            Destroy(gameObject);

        playerMovement = GetComponent<PlayerMovement>();
        flashlightController = GetComponent<PlayerFlashlightController>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions.FindAction("Interact", false);
        pauseAction = playerInput.actions.FindAction("Pause", false);
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            GameController.gameController?.GetComponent<PauseMenuController>()?.TogglePause();
            return;
        }

        if (GameController.IsPaused)
            return;
        
        if (CanInteractWithPuzzleObject(out RaycastHit hit) && hit.collider != null)
        {
            PlayerUI.playerUI?.SetInteractionPointActive(true);
            if (interactAction != null && interactAction.IsPressed())
                hit.collider.GetComponent<PuzzleObject>()?.Interact();   
        }else{
            PlayerUI.playerUI?.SetInteractionPointActive(false);

            if (interactAction != null && interactAction.WasPressedThisFrame())
                TryInteractWithCupboard();
        }
    }

    private bool CanInteractWithPuzzleObject(out RaycastHit hit)
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            Transform cameraTransform = playerCamera.transform;
            Vector3 origin = cameraTransform.position;
            Vector3 direction = cameraTransform.forward;
            return Physics.Raycast(origin, direction, out hit, interactionRange, interactionMask);
        }

        hit = default;
        return false;
    }

    void OnDestroy()
    {
        if (playerController == this)
            playerController = null;
    }

    public void SetNearbyCupboard(Cupboard cupboard)
    {
        nearbyCupboard = cupboard;
    }

    public void ClearNearbyCupboard(Cupboard cupboard)
    {
        if (nearbyCupboard == cupboard)
            nearbyCupboard = null;
    }

    public void TryInteractWithCupboard()
    {
        if (IsHidden)
        {
            currentCupboard?.TryExitPlayer(this);
            return;
        }

        if (nearbyCupboard == null)
            return;

        bool enemyHasDirectVision = HasEnemyDirectVisionNow();
        nearbyCupboard.TryHidePlayer(this, enemyHasDirectVision);
    }

    public void EnterCupboard(Cupboard cupboard, Vector3 hidePosition, Vector3 lookDirection, bool enemyHasDirectVision)
    {
        if (cupboard == null)
            return;

        currentCupboard = cupboard;
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(true);
            playerMovement.TeleportTo(hidePosition);
            playerMovement.SetLookDirection(lookDirection);
        }

        OnPlayerEnteredCupboard?.Invoke(cupboard, enemyHasDirectVision);
    }

    public void ExitCupboard(Cupboard cupboard, Vector3 exitPosition, bool forcedByEnemy)
    {
        if (currentCupboard != cupboard)
            return;

        currentCupboard = null;
        if (playerMovement != null)
        {
            playerMovement.TeleportTo(exitPosition);
            playerMovement.SetMovementLocked(false);
        }

        OnPlayerExitedCupboard?.Invoke(cupboard, forcedByEnemy);
    }

    public void ToggleFlashlight()
    {
        flashlightController?.ToggleFlashlight();
    }

    private bool HasEnemyDirectVisionNow()
    {
        UpdateEnemyReference();
        return enemyBehaviour != null && enemyBehaviour.CanSeeWorldPosition(transform.position);
    }

    private void UpdateEnemyReference()
    {
        if (enemyBehaviour != null)
            return;

        if (GameController.gameController == null || GameController.gameController.enemy == null)
            return;

        enemyBehaviour = GameController.gameController.enemy.GetComponent<EnemyBehaviour>();
    }

    public bool AddFlashlightBattery(float amount)
    {
        return flashlightController != null && flashlightController.AddFlashlightBattery(amount);
    }

    public void TakeDamage(float damage)
    {
        if (GameController.IsPaused || GameController.IsGodModeEnabled)
            return;

        Debug.Log($"Player takes {damage} damage.");
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
        if (playerMovement != null)
            playerMovement.DamageShake();
    }
}
