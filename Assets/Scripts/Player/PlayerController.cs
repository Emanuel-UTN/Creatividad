using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerFlashlightController))]
public class PlayerController : MonoBehaviour
{
    [Header("Puzzles Interactions")]
    public float interactionRange = 2f;
    public LayerMask interactionMask = Physics.DefaultRaycastLayers;

    // Actions
    private InputAction interactAction;
    private InputAction pauseAction;
    private InputAction rechargeAction;

    // Events
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
    private PlayerHealth playerHealth;

    private Cupboard nearbyCupboard;
    private Cupboard currentCupboard;
    private EnemyBehaviour enemyBehaviour;
    private Camera playerCamera;

    public bool IsHidden => currentCupboard != null;
    public float CurrentFlashlightBattery => flashlightController != null ? flashlightController.CurrentFlashlightBattery : 0f;
    public float MaxFlashlightBattery => flashlightController != null ? flashlightController.MaxFlashlightBattery : 0f;
    public float FlashlightBatteryNormalized => flashlightController != null ? flashlightController.FlashlightBatteryNormalized : 0f;
    

    // Inventory
    [SerializeField]
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

    private int batteryCount = 0;
    private float batteryAmount = 0f;

    void Awake()
    {
        if (playerController == null)
            playerController = this;
        else if (playerController != this)
            Destroy(gameObject);

        playerMovement = GetComponent<PlayerMovement>();
        flashlightController = GetComponent<PlayerFlashlightController>();
        playerHealth = GetComponent<PlayerHealth>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            interactAction = playerInput.actions.FindAction("Interact", false);
            pauseAction = playerInput.actions.FindAction("Pause", false);
            rechargeAction = playerInput.actions.FindAction("Recharge", false);
        }
        playerCamera = GetComponentInChildren<Camera>();

        if (GetComponent<DustParticles>() == null)
            gameObject.AddComponent<DustParticles>();
    }

    void Update()
    {
        if (GameController.IsDead)
            return;

        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            GameController.gameController.TogglePause();
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

        if (rechargeAction != null && rechargeAction.WasPressedThisFrame())
            UseBattery();
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
        batteryAmount = amount;
        batteryCount++;
        PlayerUI.playerUI?.UpdateBatteryCount(batteryCount);
        return true;
    }

    void UseBattery()
    {
        if (batteryCount <= 0 || flashlightController == null)
            return;

        if (!flashlightController.AddFlashlightBattery(batteryAmount))
            return;

        batteryCount--;
        PlayerUI.playerUI?.UpdateBatteryCount(batteryCount);
    }

    public void TakeDamage(float damage)
    {
        if (GameController.IsPaused || GameController.IsCreativoEnabled)
            return;

        Debug.Log($"Player takes {damage} damage.");
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
        if (playerMovement != null)
            playerMovement.DamageShake();
    }

    public bool RestoreHealth(float amount)
    {
        if (GameController.IsPaused || GameController.IsCreativoEnabled)
            return false;

        if (playerHealth != null)
            return playerHealth.RestoreHealth(amount);

        return false;
    }
}
