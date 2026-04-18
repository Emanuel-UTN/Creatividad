using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Movement))]
public class EnemyBehaviour : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        WaitAtEnd,
        Chase,
        GoToLastKnown,
        Search
    }

    [Header("Patrulla")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float waitAtEndTime = 2f;

    [Header("Vision")]
    public Transform eyePoint;
    public float viewDistance = 12f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask visionMask = Physics.DefaultRaycastLayers;
    public bool showVisionInScene = true;

    [Header("Busqueda")]
    public float searchDuration = 3f;
    public float lookAroundAngle = 45f;
    public float lookAroundSpeed = 2f;
    public float lostSightForwardAdvance = 2f;

    private Movement movement;
    private Transform player;
    private Transform lastKnownTarget;

    private EnemyState state;
    private int patrolIndex;
    private float stateTimer;

    private float searchBaseYaw;
    private float defaultMovementOffset;
    private int patrolVisitStep;
    private readonly Dictionary<int, int> patrolLastVisitStep = new Dictionary<int, int>();
    private Vector3 previousPlayerPosition;
    private bool hasPreviousPlayerPosition;
    private Vector3 lastKnownPlayerMoveDirection;

    void Awake()
    {
        movement = GetComponent<Movement>();
        defaultMovementOffset = movement.offset;

        GameObject lastKnownObj = new GameObject($"{name}_LastKnownTarget");
        lastKnownTarget = lastKnownObj.transform;
        lastKnownTarget.position = transform.position;
    }

    void Start()
    {
        TryAssignPlayer();

        if (patrolPoints.Count > 0)
        {
            state = EnemyState.Patrol;
            movement.target = patrolPoints[patrolIndex];
            MarkPatrolPointVisited(patrolIndex);
        }
        else
        {
            state = EnemyState.Search;
            StartSearch();
        }
    }

    void Update()
    {
        if (player == null)
            TryAssignPlayer();

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer)
        {
            EnterChase();
        }
        else if (state == EnemyState.Chase)
        {
            EnterGoToLastKnown();
        }

        switch (state)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.WaitAtEnd:
                UpdateWaitAtEnd();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.GoToLastKnown:
                UpdateGoToLastKnown();
                break;
            case EnemyState.Search:
                UpdateSearch();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (patrolPoints.Count == 0)
            return;

        movement.offset = 0f;

        Transform patrolTarget = patrolPoints[patrolIndex];
        if (patrolTarget == null)
        {
            if (TryGetRandomVisiblePatrolIndex(out int nextIndex))
            {
                patrolIndex = nextIndex;
                patrolTarget = patrolPoints[patrolIndex];
            }

            if (patrolTarget == null)
                return;
        }

        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = patrolTarget;

        if (HasReached(patrolTarget.position))
            AdvancePatrolPoint();
    }

    private void UpdateWaitAtEnd()
    {
        movement.offset = defaultMovementOffset;
        movement.moveToTarget = false;
        movement.rotateTowardsTarget = true;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            state = EnemyState.Patrol;
    }

    private void UpdateChase()
    {
        if (player == null)
            return;

        movement.offset = defaultMovementOffset;
        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = player;
    }

    private void UpdateGoToLastKnown()
    {
        movement.offset = defaultMovementOffset;
        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = lastKnownTarget;

        if (HasReached(lastKnownTarget.position))
        {
            state = EnemyState.Search;
            StartSearch();
        }
    }

    private void UpdateSearch()
    {
        movement.offset = defaultMovementOffset;
        movement.moveToTarget = false;
        movement.rotateTowardsTarget = false;

        stateTimer -= Time.deltaTime;
        float oscillation = Mathf.Sin((searchDuration - stateTimer) * lookAroundSpeed) * lookAroundAngle;
        transform.rotation = Quaternion.Euler(0f, searchBaseYaw + oscillation, 0f);

        if (stateTimer <= 0f)
        {
            if (patrolPoints.Count > 0)
            {
                SetClosestPatrolIndex();
                state = EnemyState.Patrol;
            }
        }
    }

    private void EnterChase()
    {
        if (player == null)
            return;

        Vector3 currentPlayerPosition = player.position;
        if (hasPreviousPlayerPosition)
        {
            Vector3 delta = currentPlayerPosition - previousPlayerPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                lastKnownPlayerMoveDirection = delta.normalized;
        }

        previousPlayerPosition = currentPlayerPosition;
        hasPreviousPlayerPosition = true;

        state = EnemyState.Chase;
        movement.target = player;
    }

    private void EnterGoToLastKnown()
    {
        if (player != null)
        {
            Vector3 targetPos = player.position;
            Vector3 moveDir = lastKnownPlayerMoveDirection;

            if (moveDir.sqrMagnitude <= 0.0001f)
            {
                Vector3 forward = player.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                    moveDir = forward.normalized;
            }

            if (moveDir.sqrMagnitude > 0.0001f && lostSightForwardAdvance > 0f)
                targetPos = GetReachablePointInDirection(targetPos, moveDir.normalized, lostSightForwardAdvance);

            lastKnownTarget.position = targetPos;
        }

        state = EnemyState.GoToLastKnown;
        movement.target = lastKnownTarget;
    }

    private void StartSearch()
    {
        stateTimer = searchDuration;
        searchBaseYaw = transform.eulerAngles.y;
    }

    private void AdvancePatrolPoint()
    {
        if (patrolPoints.Count <= 1)
            return;

        MarkPatrolPointVisited(patrolIndex);

        if (!TryGetRandomVisiblePatrolIndex(out int nextIndex))
            return;

        patrolIndex = nextIndex;
    }

    private bool HasReached(Vector3 destination)
    {
        Vector3 toDestination = destination - transform.position;
        toDestination.y = 0f;
        return toDestination.magnitude <= movement.offset + 0.05f;
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

    private void SetClosestPatrolIndex()
    {
        if (patrolPoints.Count == 0)
            return;

        int closest = 0;
        float best = float.MaxValue;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float sqr = (patrolPoints[i].position - transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                closest = i;
            }
        }

        patrolIndex = closest;
    }

    private bool TryGetRandomVisiblePatrolIndex(out int nextIndex)
    {
        nextIndex = -1;

        List<int> candidates = new List<int>();
        List<float> weights = new List<float>();

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (i == patrolIndex)
                continue;

            Transform point = patrolPoints[i];
            if (point == null)
                continue;

            if (!HasDirectPathToPoint(point))
                continue;

            candidates.Add(i);
            weights.Add(GetPatrolPointWeight(i));
        }

        if (candidates.Count == 0)
            return false;

        float totalWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
            totalWeight += weights[i];

        if (totalWeight <= 0f)
        {
            nextIndex = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        float pick = Random.Range(0f, totalWeight);
        float running = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            running += weights[i];
            if (pick <= running)
            {
                nextIndex = candidates[i];
                return true;
            }
        }

        nextIndex = candidates[candidates.Count - 1];
        return true;
    }

    private float GetPatrolPointWeight(int index)
    {
        if (!patrolLastVisitStep.TryGetValue(index, out int lastVisit))
            return patrolVisitStep + 2f;

        int age = patrolVisitStep - lastVisit;
        return Mathf.Max(0.2f, age);
    }

    private void MarkPatrolPointVisited(int index)
    {
        if (index < 0)
            return;

        patrolVisitStep++;
        patrolLastVisitStep[index] = patrolVisitStep;
    }

    private bool HasDirectPathToPoint(Transform point)
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        Vector3 destination = point.position + Vector3.up * 0.25f;
        Vector3 toPoint = destination - origin;

        float distance = toPoint.magnitude;
        if (distance <= 0.001f)
            return true;

        if (Physics.Raycast(origin, toPoint.normalized, out RaycastHit hit, distance, visionMask, QueryTriggerInteraction.Ignore))
            return hit.transform == point || hit.transform.IsChildOf(point);

        return true;
    }

    private Vector3 GetReachablePointInDirection(Vector3 start, Vector3 direction, float distance)
    {
        Vector3 horizontalDir = direction;
        horizontalDir.y = 0f;

        if (horizontalDir.sqrMagnitude <= 0.0001f || distance <= 0f)
            return start;

        horizontalDir.Normalize();

        Vector3 rayOrigin = start + Vector3.up * 0.25f;
        if (Physics.Raycast(rayOrigin, horizontalDir, out RaycastHit hit, distance, visionMask, QueryTriggerInteraction.Ignore))
        {
            float travel = Mathf.Max(0f, hit.distance - 0.1f);
            return start + horizontalDir * travel;
        }

        return start + horizontalDir * distance;
    }

    private void TryAssignPlayer()
    {
        GameObject playerObj = null;

        if (GameController.gameController != null)
            playerObj = GameController.gameController.player;

        if (playerObj == null)
            playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;

        if (player != null && !hasPreviousPlayerPosition)
        {
            previousPlayerPosition = player.position;
            hasPreviousPlayerPosition = true;
        }
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
    }
}
