using UnityEngine;
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    public static event System.Action<Cupboard, bool> OnPlayerEnteredCupboard;
    public static event System.Action<Cupboard, bool> OnPlayerExitedCupboard;

    static public PlayerController playerController;

    private PlayerMovement playerMovement;
    private Cupboard nearbyCupboard;
    private Cupboard currentCupboard;

    public bool IsHidden => currentCupboard != null;

    void Awake()
    {
        if (playerController == null)
            playerController = this;
        else if(playerController != this)
            Destroy(gameObject);
        

        playerMovement = GetComponent<PlayerMovement>();
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

    private bool HasEnemyDirectVisionNow()
    {
        if (GameController.gameController == null || GameController.gameController.enemy == null)
            return false;

        EnemyBehaviour enemyBehaviour = GameController.gameController.enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
            return false;

        return enemyBehaviour.CanSeeWorldPosition(transform.position);
    }
}
