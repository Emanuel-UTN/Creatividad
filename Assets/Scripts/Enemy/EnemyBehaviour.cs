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

    private Movement movement;
    private MazeCell cellPosition;
    private Transform player;

    private PatrolBehaviour patrolBehaviour;
    private ChaseBehaviour chaseBehaviour;

    private EnemyState state;
    private bool sawPlayerLastFrame;

    void Awake()
    {
        movement = GetComponent<Movement>();

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

        if (seesPlayer)
        {
            state = EnemyState.Chase;
            chaseBehaviour.OnPlayerSeen(player);
            sawPlayerLastFrame = true;
        }
        else if (sawPlayerLastFrame)
        {
            chaseBehaviour.OnPlayerLost(player);
            sawPlayerLastFrame = false;
        }

        if (state == EnemyState.Chase)
        {
            bool chaseInProgress = chaseBehaviour.Tick();
            if (!chaseInProgress)
            {
                state = EnemyState.Patrol;
                patrolBehaviour.ResetToClosestPoint();
            }
        }

        if (state == EnemyState.Patrol)
            patrolBehaviour.Tick();

        RefreshCellPosition();
    }

    void OnDestroy()
    {
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

        GetVisionOriginAndForward(out Vector3 origin, out Vector3 forward);
        Vector3 toPlayer = player.position - origin;

        float distance = toPlayer.magnitude;
        if (distance > viewDistance || distance <= 0.001f)
            return false;

        float angle = Vector3.Angle(forward, toPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance, visionMask, QueryTriggerInteraction.Ignore))
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
            player = playerObj.transform;
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
}
