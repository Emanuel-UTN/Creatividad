using UnityEngine;

[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(StaminaComponent))]
public class EnemyBehaviour : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        Chase
    }

    [Header("Patrulla")]
    public float waitAtEndTime = 2f;

    [Header("Vision")]
    public Transform eyePoint;
    public float viewDistance = 12f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask visionMask = Physics.DefaultRaycastLayers;
    public bool showVisionInScene = true;

    [Header("Caza")]
    public float searchDuration = 3f;
    public float lookAroundAngle = 45f;
    public float lookAroundSpeed = 2f;
    public float lostSightForwardAdvance = 2f;

    [Header("Correr")]
    public float runSpeedMultiplier = 1.7f;
    [Range(0f, 1f)] public float runResumeStaminaNormalized = 0.2f;
    
    private Movement movement;
    private StaminaComponent staminaComponent;
    private MazeCell cellPosition;
    private Transform player;
    private PlayerController playerController;
    private PlayerMovement playerMovement;

    private PatrolBehaviour patrolBehaviour;
    private ChaseBehaviour chaseBehaviour;

    private EnemyState state;
    private bool sawPlayerLastFrame;
    private bool isRunning;
    private bool runBlockedByExhaustion;
    private float baseWalkSpeed;
    private float stunTimer;

    public bool IsRunning => isRunning;
    public bool IsChasing => state == EnemyState.Chase;

    void Awake()
    {
        movement = GetComponent<Movement>();
        baseWalkSpeed = movement != null ? movement.speed : 5f;
        staminaComponent = GetComponent<StaminaComponent>();
        EnsureValidStaminaSetup();
        PlayerController.OnPlayerEnteredCupboard += HandlePlayerEnteredCupboard;
        PlayerNoises.OnNoiseEmitted += HandlePlayerNoise;

        patrolBehaviour = new PatrolBehaviour(
            movement,
            transform,
            waitAtEndTime,
            movement.offset,
            SetCellPosition);
        chaseBehaviour = new ChaseBehaviour(
            movement,
            transform,
            movement.offset,
            searchDuration,
            lookAroundAngle,
            lookAroundSpeed,
            lostSightForwardAdvance,
            visionMask);

        state = EnemyState.Patrol;
    }

    void Start()
    {
        TryAssignPlayer();

        RefreshCellPosition();
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        if (player == null)
            TryAssignPlayer();

        if (stunTimer > 0f)
        {
            stunTimer -= deltaTime;
            isRunning = false;
            runBlockedByExhaustion = false;
            movement.speed = 0f;
            movement.moveToTarget = false;
            movement.rotateTowardsTarget = false;
            RefreshCellPosition();
            return;
        }

        bool seesPlayer = CanSeePlayer();
        UpdateDetectionState(seesPlayer);

        bool runningWithStamina = state == EnemyState.Chase
            ? UpdateChaseMovement(deltaTime)
            : UpdatePatrolMovement(deltaTime);

        isRunning = runningWithStamina;

        if (state == EnemyState.Patrol)
            patrolBehaviour.Tick();

        RefreshCellPosition();
    }

    void OnDestroy()
    {
        PlayerController.OnPlayerEnteredCupboard -= HandlePlayerEnteredCupboard;
        PlayerNoises.OnNoiseEmitted -= HandlePlayerNoise;
        patrolBehaviour?.Dispose();
        chaseBehaviour?.Dispose();
    }

    private void RefreshCellPosition()
    {
        if (GameController.gameController == null)
            return;

        MazeCell current = GameController.gameController.GetCellByPosition(transform.position);
        if (current != null)
            cellPosition = current;
    }

    private void SetCellPosition(MazeCell cell)
    {
        if (cell != null)
            cellPosition = cell;
    }

    private void EnsureValidStaminaSetup()
    {
        if (staminaComponent == null)
            return;

        if (staminaComponent.maxStamina <= 0f)
            staminaComponent.maxStamina = 100f;

        staminaComponent.SetStamina(staminaComponent.MaxStamina);
    }

    private void UpdateDetectionState(bool seesPlayer)
    {
        if (seesPlayer)
        {
            SetState(EnemyState.Chase);
            chaseBehaviour.OnPlayerSeen(player);
            sawPlayerLastFrame = true;
            return;
        }

        if (!sawPlayerLastFrame)
            return;

        if (!chaseBehaviour.IsPursuingCupboard)
            chaseBehaviour.OnPlayerLost(player);

        sawPlayerLastFrame = false;
    }

    private bool UpdateChaseMovement(float deltaTime)
    {
        bool wantsToRun = chaseBehaviour != null && chaseBehaviour.ShouldRun;
        bool canRun = wantsToRun;

        if (staminaComponent != null)
        {
            float resumeThreshold = Mathf.Clamp01(runResumeStaminaNormalized);

            if (runBlockedByExhaustion)
            {
                staminaComponent.Regenerate(deltaTime);
                if (staminaComponent.StaminaNormalized >= resumeThreshold)
                    runBlockedByExhaustion = false;

                canRun = false;
            }
            else if (wantsToRun)
            {
                canRun = staminaComponent.Consume(deltaTime);
                if (!canRun)
                {
                    runBlockedByExhaustion = true;
                    staminaComponent.Regenerate(deltaTime);
                }
            }
            else
            {
                staminaComponent.Regenerate(deltaTime);
                canRun = false;
            }
        }

        movement.speed = canRun ? CalculateEnemyRunSpeed() : baseWalkSpeed;

        bool chaseInProgress = chaseBehaviour.Tick();
        if (chaseInProgress)
            return canRun;

        SetState(EnemyState.Patrol);
        movement.speed = baseWalkSpeed;
        runBlockedByExhaustion = false;
        patrolBehaviour.ResetToClosestPoint();
        return false;
    }

    private bool UpdatePatrolMovement(float deltaTime)
    {
        staminaComponent?.Regenerate(deltaTime);
        movement.speed = baseWalkSpeed;
        if (staminaComponent != null && staminaComponent.StaminaNormalized >= Mathf.Clamp01(runResumeStaminaNormalized))
            runBlockedByExhaustion = false;
        return false;
    }

    private float CalculateEnemyRunSpeed()
    {
        return baseWalkSpeed * Mathf.Max(1f, runSpeedMultiplier);
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        if (playerController != null && playerController.IsHidden)
            return false;

        return CanSeeWorldPosition(player.position);
    }

    public bool CanSeeWorldPosition(Vector3 worldPosition)
    {
        GetVisionOriginAndForward(out Vector3 origin, out Vector3 forward);
        Vector3 toTarget = worldPosition - origin;

        float distance = toTarget.magnitude;
        if (distance > viewDistance || distance <= 0.001f)
            return false;

        float angle = Vector3.Angle(forward, toTarget);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance, visionMask, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);

        return false;
    }

    private void TryAssignPlayer()
    {
        GameObject playerObj = null;

        if (PlayerController.playerController != null)
            playerObj = PlayerController.playerController.gameObject;

        if (playerObj == null)
            playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    private void HandlePlayerEnteredCupboard(Cupboard cupboard, bool enemyHadDirectVision)
    {
        if (cupboard == null || chaseBehaviour == null)
            return;

        if (!enemyHadDirectVision)
            return;

        SetState(EnemyState.Chase);
        sawPlayerLastFrame = false;
        chaseBehaviour.OnPlayerHiddenInCupboard(cupboard, player);
    }

    private void HandlePlayerNoise(Transform noiseSource, Vector3 noisePosition, float hearingRange, bool isSprinting)
    {
        if (chaseBehaviour == null)
            return;

        if (player == null)
            TryAssignPlayer();

        if (noiseSource == null || player == null || noiseSource != player)
            return;

        if (playerController != null && playerController.IsHidden)
            return;

        if (CanSeePlayer())
            return;

        Vector3 flatDelta = noisePosition - transform.position;
        flatDelta.y = 0f;
        if (flatDelta.sqrMagnitude > hearingRange * hearingRange)
            return;

        SetState(EnemyState.Chase);
        sawPlayerLastFrame = false;
        chaseBehaviour.OnPlayerHeardNoise(noisePosition, player);
    }

    private void GetVisionOriginAndForward(out Vector3 origin, out Vector3 forward)
    {
        origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
        forward = eyePoint != null ? eyePoint.forward : transform.forward;
    }

    private void OnDrawGizmos()
    {
        if (!showVisionInScene)
            return;

        GetVisionOriginAndForward(out Vector3 origin, out Vector3 forward);

        float halfAngle = viewAngle * 0.5f;
        Vector3 left = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
        Vector3 right = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawLine(origin, origin + left.normalized * viewDistance);
        Gizmos.DrawLine(origin, origin + right.normalized * viewDistance);

        const int segments = 24;
        Vector3 prev = origin + (Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward).normalized * viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = (Quaternion.AngleAxis(angle, Vector3.up) * forward).normalized;
            Vector3 next = origin + dir * viewDistance;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        if (chaseBehaviour != null)
            chaseBehaviour.DrawDebugGizmos();
    }

    public void ApplyFlashlightStun(float duration)
    {
        if (duration <= 0f)
            return;

        stunTimer = Mathf.Max(stunTimer, duration);
        movement.speed = 0f;
        movement.moveToTarget = false;
        movement.rotateTowardsTarget = false;
    }

    private void SetState(EnemyState newState, bool notifyMusic = true)
    {
        if (state == newState)
            return;

        state = newState;

        if (notifyMusic)
            GameController.gameController?.SetChaseMusic(state == EnemyState.Chase);
    }
}
