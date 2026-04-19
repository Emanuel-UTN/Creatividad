
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Movement))]
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

    [Header("Velocidades")]
    public float runSpeedMultiplier = 1.7f;
    public float minRunSpeedMultiplier = 1.5f;
    public float relativeToPlayerSprintMultiplier = 1.05f;
    public float chaseAdvantageOverPlayer = 1.2f;

    [Header("Debug")]
    public bool showEnemyStaminaDebug = false;
    public Vector2 debugPanelPosition = new Vector2(20f, 80f);

    [SerializeField] private float debugCurrentStamina;
    [SerializeField] private float debugMaxStamina;
    [SerializeField, Range(0f, 1f)] private float debugNormalizedStamina;
    [SerializeField] private bool debugRunningWithStamina;
    
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
    private float baseWalkSpeed;

    void Awake()
    {
        movement = GetComponent<Movement>();
        baseWalkSpeed = movement != null ? movement.speed : 0f;
        staminaComponent = GetComponent<StaminaComponent>();
        if (staminaComponent == null)
            staminaComponent = gameObject.AddComponent<StaminaComponent>();
        if (staminaComponent.maxStamina <= 0f)
        {
            staminaComponent.maxStamina = 100f;
            staminaComponent.SetStamina(staminaComponent.maxStamina);
        }
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
        cellPosition = GameController.gameController.GetCellByPosition(transform.position);
    }

    void Update()
    {
        if (player == null)
            TryAssignPlayer();

        bool seesPlayer = CanSeePlayer();
        bool runningWithStamina = false;

        if (seesPlayer)
        {
            state = EnemyState.Chase;
            chaseBehaviour.OnPlayerSeen(player);
            sawPlayerLastFrame = true;
        }
        else if (sawPlayerLastFrame)
        {
            if (!chaseBehaviour.IsPursuingCupboard)
                chaseBehaviour.OnPlayerLost(player);
            sawPlayerLastFrame = false;
        }

        if (state == EnemyState.Chase)
        {
            bool shouldSpendStamina = seesPlayer;
            bool canRun = staminaComponent != null && staminaComponent.UseStamina(shouldSpendStamina, Time.deltaTime) && shouldSpendStamina;
            if (canRun)
            {
                float fromPlayer = playerMovement != null ? playerMovement.sprintMultiplier : 0f;
                float sprintMultiplier = fromPlayer > 1f ? fromPlayer : runSpeedMultiplier;
                float enemyRunSpeed = baseWalkSpeed * Mathf.Max(minRunSpeedMultiplier, sprintMultiplier);

                if (playerMovement != null)
                {
                    float playerRunSpeed = playerMovement.speed * Mathf.Max(1f, playerMovement.sprintMultiplier);
                    float targetFromPlayer = playerRunSpeed * Mathf.Max(0.1f, chaseAdvantageOverPlayer);
                    float minimumFromPlayer = playerRunSpeed * Mathf.Max(0.1f, relativeToPlayerSprintMultiplier);
                    enemyRunSpeed = Mathf.Max(enemyRunSpeed, minimumFromPlayer);
                    enemyRunSpeed = Mathf.Max(enemyRunSpeed, targetFromPlayer);
                }

                movement.speed = enemyRunSpeed;
                runningWithStamina = true;
            }
            else
            {
                movement.speed = baseWalkSpeed;
            }
            bool chaseInProgress = chaseBehaviour.Tick();
            if (!chaseInProgress)
            {
                state = EnemyState.Patrol;
                patrolBehaviour.ResetToClosestPoint();
            }
        }
        else if (staminaComponent != null)
        {
            staminaComponent.UseStamina(false, Time.deltaTime); // Regenera estamina fuera de persecución
            movement.speed = baseWalkSpeed;
        }

        if (state == EnemyState.Patrol)
            patrolBehaviour.Tick();

        UpdateStaminaDebugCache(runningWithStamina);
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

        state = EnemyState.Chase;
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

        state = EnemyState.Chase;
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

    private void UpdateStaminaDebugCache(bool runningWithStamina)
    {
        if (staminaComponent == null)
            return;

        debugCurrentStamina = staminaComponent.CurrentStamina;
        debugMaxStamina = staminaComponent.MaxStamina;
        debugNormalizedStamina = staminaComponent.StaminaNormalized;
        debugRunningWithStamina = runningWithStamina;
    }

    private void OnGUI()
    {
        if (!showEnemyStaminaDebug || !Application.isPlaying)
            return;

        Rect rect = new Rect(debugPanelPosition.x, debugPanelPosition.y, 360f, 70f);
        string stateLabel = state == EnemyState.Chase ? "CHASE" : "PATROL";
        string message = $"Enemy: {stateLabel} | Running: {debugRunningWithStamina}\nStamina: {debugCurrentStamina:0.0}/{debugMaxStamina:0.0} ({debugNormalizedStamina:P0})";
        GUI.Label(rect, message);
    }
}
