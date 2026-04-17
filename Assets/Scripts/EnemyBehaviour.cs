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

    [Header("Busqueda")]
    public float searchDuration = 3f;
    public float lookAroundAngle = 45f;
    public float lookAroundSpeed = 2f;

    private Movement movement;
    private Transform player;
    private Transform lastKnownTarget;

    private EnemyState state;
    private int patrolIndex;
    private int patrolDirection = 1;
    private float stateTimer;

    private float searchBaseYaw;

    void Awake()
    {
        movement = GetComponent<Movement>();

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

        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = patrolPoints[patrolIndex];

        if (HasReached(movement.target.position))
            AdvancePatrolPoint();
    }

    private void UpdateWaitAtEnd()
    {
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

        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = player;
    }

    private void UpdateGoToLastKnown()
    {
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

        state = EnemyState.Chase;
        movement.target = player;
    }

    private void EnterGoToLastKnown()
    {
        if (player != null)
            lastKnownTarget.position = player.position;

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

        bool atEnd = patrolIndex == patrolPoints.Count - 1;
        bool atStart = patrolIndex == 0;

        if (patrolDirection > 0 && atEnd)
        {
            patrolDirection = -1;
            state = EnemyState.WaitAtEnd;
            stateTimer = waitAtEndTime;
            return;
        }

        if (patrolDirection < 0 && atStart)
        {
            patrolDirection = 1;
            state = EnemyState.WaitAtEnd;
            stateTimer = waitAtEndTime;
            return;
        }

        patrolIndex += patrolDirection;
        patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Count - 1);
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

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 forward = eyePoint != null ? eyePoint.forward : transform.forward;
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
        if (patrolIndex >= patrolPoints.Count - 1)
            patrolDirection = -1;
        else if (patrolIndex <= 0)
            patrolDirection = 1;
    }

    private void TryAssignPlayer()
    {
        GameObject playerObj = null;

        if (GameManager.gameManager != null)
            playerObj = GameManager.gameManager.player;

        if (playerObj == null)
            playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
    }
}
