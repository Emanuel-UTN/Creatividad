using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerFlashlightController))]
public class PlayerController : MonoBehaviour
{
    public static event System.Action<Cupboard, bool> OnPlayerEnteredCupboard;
    public static event System.Action<Cupboard, bool> OnPlayerExitedCupboard;
    public static event System.Action<float> OnFlashlightBatteryChanged
    {
        add => PlayerFlashlightController.OnFlashlightBatteryChanged += value;
        remove => PlayerFlashlightController.OnFlashlightBatteryChanged -= value;
    }

    public static PlayerController playerController;

    private PlayerMovement playerMovement;
    private PlayerFlashlightController flashlightController;
    private Cupboard nearbyCupboard;
    private Cupboard currentCupboard;
    private EnemyBehaviour enemyBehaviour;

    public bool IsHidden => currentCupboard != null;
    public float CurrentFlashlightBattery => flashlightController != null ? flashlightController.CurrentFlashlightBattery : 0f;
    public float MaxFlashlightBattery => flashlightController != null ? flashlightController.MaxFlashlightBattery : 0f;
    public float FlashlightBatteryNormalized => flashlightController != null ? flashlightController.FlashlightBatteryNormalized : 0f;

    void Awake()
    {
        if (playerController == null)
            playerController = this;
        else if (playerController != this)
            Destroy(gameObject);

        playerMovement = GetComponent<PlayerMovement>();
        flashlightController = GetComponent<PlayerFlashlightController>();
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

    public void ToggleLanter()
    {
        flashlightController?.ToggleLanter();
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
}
