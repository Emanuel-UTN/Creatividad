using System.Collections.Generic;
using UnityEngine;

public class ChaseBehaviour : Behaviour
{
    private const float PathMoveOffset = 0.2f;
    private const float PathReachThreshold = 0.3f;

    private enum ChaseState
    {
        Idle,
        Chase,
        GoToNoise,
        GoToLastSeen,
        GoToNextNode,
        GoToCupboardNode,
        GoToCupboard,
        Search
    }

    private readonly float defaultMovementOffset;
    private readonly float searchDuration;
    private readonly float lookAroundAngle;
    private readonly float lookAroundSpeed;
    private readonly float lostSightForwardAdvance;
    private readonly LayerMask visionMask;

    private ChaseState state = ChaseState.Idle;
    private Transform currentPlayer;

    private readonly Transform lastSeenTarget;
    private readonly Transform nextNodeTarget;
    private readonly Transform pathTarget;
    private readonly Transform noiseTarget;

    private readonly List<MazeCell> pathCells = new List<MazeCell>();
    private readonly Queue<MazeCell> pathOpenSet = new Queue<MazeCell>();
    private readonly Dictionary<MazeCell, MazeCell> pathCameFrom = new Dictionary<MazeCell, MazeCell>();
    private readonly HashSet<MazeCell> pathVisited = new HashSet<MazeCell>();
    private readonly List<MazeCell> reversedPathBuffer = new List<MazeCell>();

    private float stateTimer;
    private float searchBaseYaw;

    private Vector3 previousPlayerPosition;
    private Vector3 beforePreviousPlayerPosition;
    private bool hasPreviousPlayerPosition;
    private bool hasBeforePreviousPlayerPosition;
    private Vector3 lastKnownPlayerMoveDirection;
    private Vector3 moveDirectionAtLoss;
    private MazeCell lastSeenCell;
    private MazeCell previousPlayerCellAtLoss;
    private MazeCell targetNodeCell;
    private MazeCell targetNoiseCell;
    private Cupboard targetCupboard;
    private MazeCell targetCupboardCell;
    private int pathCellIndex;

    public bool IsPursuingCupboard => state == ChaseState.GoToCupboardNode || state == ChaseState.GoToCupboard;
    public bool ShouldRun =>
        state == ChaseState.Chase ||
        state == ChaseState.GoToNoise ||
        state == ChaseState.GoToLastSeen ||
        state == ChaseState.GoToNextNode ||
        state == ChaseState.GoToCupboardNode ||
        state == ChaseState.GoToCupboard;

    public ChaseBehaviour(
        Movement movement,
        Transform enemyTransform,
        float defaultMovementOffset,
        float searchDuration,
        float lookAroundAngle,
        float lookAroundSpeed,
        float lostSightForwardAdvance,
        LayerMask visionMask)
        : base(movement, enemyTransform)
    {
        this.defaultMovementOffset = defaultMovementOffset;
        this.searchDuration = searchDuration;
        this.lookAroundAngle = lookAroundAngle;
        this.lookAroundSpeed = lookAroundSpeed;
        this.lostSightForwardAdvance = lostSightForwardAdvance;
        this.visionMask = visionMask;

        GameObject lastSeenObj = new GameObject($"{enemyTransform.name}_LastSeenTarget");
        lastSeenTarget = lastSeenObj.transform;
        lastSeenTarget.position = enemyTransform.position;

        GameObject nextNodeObj = new GameObject($"{enemyTransform.name}_NextNodeTarget");
        nextNodeTarget = nextNodeObj.transform;
        nextNodeTarget.position = enemyTransform.position;

        GameObject pathObj = new GameObject($"{enemyTransform.name}_PathTarget");
        pathTarget = pathObj.transform;
        pathTarget.position = enemyTransform.position;

        GameObject noiseObj = new GameObject($"{enemyTransform.name}_NoiseTarget");
        noiseTarget = noiseObj.transform;
        noiseTarget.position = enemyTransform.position;
    }

    public void OnPlayerSeen(Transform player)
    {
        if (player == null)
            return;

        currentPlayer = player;

        Vector3 currentPlayerPosition = player.position;
        if (hasPreviousPlayerPosition)
        {
            Vector3 delta = currentPlayerPosition - previousPlayerPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                lastKnownPlayerMoveDirection = delta.normalized;

            beforePreviousPlayerPosition = previousPlayerPosition;
            hasBeforePreviousPlayerPosition = true;
        }

        previousPlayerPosition = currentPlayerPosition;
        hasPreviousPlayerPosition = true;

        state = ChaseState.Chase;
        GameController.gameController?.SetChaseMusic(true);
    }

    public void OnPlayerLost(Transform player)
    {
        if (state != ChaseState.Chase)
            return;

        Vector3 lastSeenPos = enemyTransform.position;
        if (player != null)
            lastSeenPos = player.position;
        else if (currentPlayer != null)
            lastSeenPos = currentPlayer.position;
        else if (hasPreviousPlayerPosition)
            lastSeenPos = previousPlayerPosition;

        lastSeenCell = ResolveCell(lastSeenPos);
        lastSeenTarget.position = lastSeenCell != null ? lastSeenCell.transform.position : lastSeenPos;
        targetNodeCell = null;
        pathCells.Clear();
        pathCellIndex = 0;

        previousPlayerCellAtLoss = hasBeforePreviousPlayerPosition
            ? ResolveCell(beforePreviousPlayerPosition)
            : null;

        moveDirectionAtLoss = lastKnownPlayerMoveDirection;
        Transform sourcePlayer = player != null ? player : currentPlayer;
        if (moveDirectionAtLoss.sqrMagnitude <= 0.0001f && sourcePlayer != null)
        {
            Vector3 forward = sourcePlayer.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                moveDirectionAtLoss = forward.normalized;
        }

        MazeCell enemyCell = ResolveCell(enemyTransform.position);
        if (enemyCell != null && lastSeenCell != null && enemyCell != lastSeenCell && TryBuildPath(enemyCell, lastSeenCell))
        {
            pathCellIndex = 0;
            pathTarget.position = pathCells[pathCellIndex].transform.position;
        }
        else
        {
            pathCells.Clear();
            pathCellIndex = 0;
        }

        state = ChaseState.GoToLastSeen;
    }

    public bool Tick()
    {
        switch (state)
        {
            case ChaseState.Chase:
                if (currentPlayer == null)
                {
                    state = ChaseState.Idle;
                    return false;
                }

                Move(currentPlayer, defaultMovementOffset);
                EnemyController enemyController = enemyTransform.GetComponent<EnemyController>();
                if (enemyController != null)
                    enemyController.TryAttack();
                
                return true;

            case ChaseState.GoToNoise:
                if (pathCells.Count > 0)
                {
                    Move(pathTarget, PathMoveOffset);

                    if (!HasReachedWithThreshold(pathTarget.position, PathReachThreshold))
                        return true;

                    pathCellIndex++;
                    if (pathCellIndex < pathCells.Count)
                    {
                        pathTarget.position = pathCells[pathCellIndex].transform.position;
                        return true;
                    }

                    pathCells.Clear();
                    pathCellIndex = 0;
                }

                Move(noiseTarget, PathMoveOffset);
                if (!HasReachedWithThreshold(noiseTarget.position, PathReachThreshold))
                    return true;

                targetNoiseCell = null;
                BeginSearch();
                return true;

            case ChaseState.GoToLastSeen:
                if (pathCells.Count > 0)
                {
                    Move(pathTarget, PathMoveOffset);

                    if (HasReachedWithThreshold(pathTarget.position, PathReachThreshold))
                    {
                        pathCellIndex++;
                        if (pathCellIndex < pathCells.Count)
                        {
                            pathTarget.position = pathCells[pathCellIndex].transform.position;
                            return true;
                        }

                        pathCells.Clear();
                        pathCellIndex = 0;
                    }
                    else
                    {
                        return true;
                    }
                }

                if (pathCells.Count == 0)
                {
                    Move(lastSeenTarget, PathMoveOffset);
                    if (!HasReachedWithThreshold(lastSeenTarget.position, PathReachThreshold))
                        return true;
                }

                if (lastSeenCell != null)
                {
                    targetNodeCell = ChooseNextCell(lastSeenCell, previousPlayerCellAtLoss, moveDirectionAtLoss);
                }

                if (targetNodeCell != null && targetNodeCell != lastSeenCell && TryBuildPath(lastSeenCell, targetNodeCell))
                {
                    pathCellIndex = 0;
                    pathTarget.position = pathCells[pathCellIndex].transform.position;
                    state = ChaseState.GoToNextNode;
                    return true;
                }

                targetNodeCell = null;
                BeginSearch();

                return true;

            case ChaseState.GoToNextNode:
                Move(pathTarget, PathMoveOffset);

                if (!HasReachedWithThreshold(pathTarget.position, PathReachThreshold))
                    return true;

                pathCellIndex++;
                if (pathCellIndex < pathCells.Count)
                {
                    pathTarget.position = pathCells[pathCellIndex].transform.position;
                    return true;
                }

                targetNodeCell = null;
                pathCells.Clear();
                BeginSearch();

                return true;

            case ChaseState.GoToCupboardNode:
                if (pathCells.Count == 0)
                {
                    state = ChaseState.GoToCupboard;
                    return true;
                }

                Move(pathTarget, PathMoveOffset);

                if (!HasReachedWithThreshold(pathTarget.position, PathReachThreshold))
                    return true;

                pathCellIndex++;
                if (pathCellIndex < pathCells.Count)
                {
                    pathTarget.position = pathCells[pathCellIndex].transform.position;
                    return true;
                }

                pathCells.Clear();
                state = ChaseState.GoToCupboard;
                return true;

            case ChaseState.GoToCupboard:
                if (targetCupboard == null)
                {
                    BeginSearch();
                    return true;
                }

                Vector3 approachPos = targetCupboard.EnemyApproachPosition;
                approachPos.y = enemyTransform.position.y;
                nextNodeTarget.position = approachPos;
                Move(nextNodeTarget, PathMoveOffset);

                bool reachedApproach = HasReachedWithThreshold(nextNodeTarget.position, PathReachThreshold);
                bool closeEnoughToEject = targetCupboard.IsEnemyCloseEnoughToEject(enemyTransform.position);
                if (!reachedApproach && !closeEnoughToEject)
                    return true;

                targetCupboard.ForceEjectHiddenPlayer();
                targetCupboard = null;
                targetCupboardCell = null;
                BeginSearch();
                return true;

            case ChaseState.Search:
                Stop(defaultMovementOffset, false);
                stateTimer -= Time.deltaTime;
                float elapsed = searchDuration - stateTimer;
                float oscillation = Mathf.Sin(elapsed * lookAroundSpeed) * lookAroundAngle;
                enemyTransform.rotation = Quaternion.Euler(0f, searchBaseYaw + oscillation, 0f);

                if (stateTimer <= 0f)
                {
                    state = ChaseState.Idle;
                    GameController.gameController?.SetChaseMusic(false);
                    return false;
                }

                return true;

            default:
                return false;
        }
    }

    public void OnPlayerHiddenInCupboard(Cupboard cupboard, Transform player)
    {
        if (cupboard == null)
            return;

        currentPlayer = player;
        targetCupboard = cupboard;
        targetCupboardCell = ResolveCell(cupboard.EnemyApproachPosition);

        pathCells.Clear();
        pathCellIndex = 0;

        MazeCell enemyCell = ResolveCell(enemyTransform.position);
        if (enemyCell != null && targetCupboardCell != null && enemyCell != targetCupboardCell && TryBuildPath(enemyCell, targetCupboardCell))
        {
            pathCellIndex = 0;
            pathTarget.position = pathCells[pathCellIndex].transform.position;
            state = ChaseState.GoToCupboardNode;
            return;
        }

        state = ChaseState.GoToCupboard;
    }

    public void OnPlayerHeardNoise(Vector3 noisePosition, Transform player)
    {
        if (player != null)
            currentPlayer = player;

        targetNoiseCell = ResolveCell(noisePosition);
        noiseTarget.position = targetNoiseCell != null ? targetNoiseCell.transform.position : noisePosition;

        pathCells.Clear();
        pathCellIndex = 0;

        MazeCell enemyCell = ResolveCell(enemyTransform.position);
        if (enemyCell != null && targetNoiseCell != null && enemyCell != targetNoiseCell && TryBuildPath(enemyCell, targetNoiseCell))
        {
            pathCellIndex = 0;
            pathTarget.position = pathCells[pathCellIndex].transform.position;
        }
        else if (enemyCell != null && TryBuildPathToClosestReachableCell(enemyCell, noisePosition))
        {
            pathCellIndex = 0;
            pathTarget.position = pathCells[pathCellIndex].transform.position;
            if (pathCells.Count > 0)
                noiseTarget.position = pathCells[pathCells.Count - 1].transform.position;
        }

        state = ChaseState.GoToNoise;
    }

    public void Dispose()
    {
        if (lastSeenTarget != null)
            Object.Destroy(lastSeenTarget.gameObject);

        if (nextNodeTarget != null)
            Object.Destroy(nextNodeTarget.gameObject);

        if (pathTarget != null)
            Object.Destroy(pathTarget.gameObject);

        if (noiseTarget != null)
            Object.Destroy(noiseTarget.gameObject);
    }

    private MazeCell ResolveCell(Vector3 position)
    {
        if (GameController.gameController == null)
        {
            return null;
        }

        MazeCell cell = MazeController.GetCellByPosition(position);
        return cell;
    }

    private MazeCell ChooseNextCell(MazeCell currentCell, MazeCell previousCell, Vector3 moveDirection)
    {
        if (currentCell == null || currentCell.neighbors == null || currentCell.neighbors.Count == 0)
        {
            return null;
        }

        Vector3 flatMoveDirection = moveDirection;
        flatMoveDirection.y = 0f;
        if (flatMoveDirection.sqrMagnitude <= 0.0001f)
            flatMoveDirection = Vector3.forward;
        flatMoveDirection.Normalize();

        MazeCell bestCell = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < currentCell.neighbors.Count; i++)
        {
            Vector2Int coords = currentCell.neighbors[i];
            MazeCell neighbor = MazeController.Cell(coords.x, coords.y);
            if (neighbor == null)
                continue;

            Vector3 toNeighbor = neighbor.transform.position - currentCell.transform.position;
            toNeighbor.y = 0f;
            if (toNeighbor.sqrMagnitude <= 0.0001f)
                continue;

            float score = Vector3.Dot(toNeighbor.normalized, flatMoveDirection);
            
            // Strongly penalize backtracking to the previous cell
            if (previousCell != null && neighbor == previousCell)
                score -= 10f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = neighbor;
            }
        }

        return bestCell;
    }

    private bool TryBuildPath(MazeCell startCell, MazeCell goalCell)
    {
        pathCells.Clear();

        if (startCell == null || goalCell == null)
        {
            return false;
        }

        if (startCell == goalCell)
        {
            return false;
        }

        pathOpenSet.Clear();
        pathCameFrom.Clear();
        pathVisited.Clear();
        reversedPathBuffer.Clear();

        pathOpenSet.Enqueue(startCell);
        pathVisited.Add(startCell);

        while (pathOpenSet.Count > 0)
        {
            MazeCell current = pathOpenSet.Dequeue();
            if (current == goalCell)
            {
                break;
            }

            if (current.neighbors == null)
                continue;

            for (int i = 0; i < current.neighbors.Count; i++)
            {
                Vector2Int coords = current.neighbors[i];
                MazeCell neighbor = MazeController.Cell(coords.x, coords.y);
                if (neighbor == null || pathVisited.Contains(neighbor))
                    continue;

                pathVisited.Add(neighbor);
                pathCameFrom[neighbor] = current;
                pathOpenSet.Enqueue(neighbor);
            }
        }

        if (!pathCameFrom.ContainsKey(goalCell))
        {
            return false;
        }

        MazeCell step = goalCell;
        while (step != startCell)
        {
            reversedPathBuffer.Add(step);
            if (!pathCameFrom.TryGetValue(step, out MazeCell previous))
                return false;

            step = previous;
        }

        for (int i = reversedPathBuffer.Count - 1; i >= 0; i--)
            pathCells.Add(reversedPathBuffer[i]);

        return pathCells.Count > 0;
    }

    private bool TryBuildPathToClosestReachableCell(MazeCell startCell, Vector3 targetPosition)
    {
        if (MazeController.Grid == null || MazeController.Width <= 0 || MazeController.Height <= 0)
            return false;

        List<MazeCell> bestPath = null;
        MazeCell bestCell = null;
        float bestDistance = float.MaxValue;

        for (int x = 0; x < MazeController.Width; x++)
        {
            for (int z = 0; z < MazeController.Height; z++)
            {
                MazeCell candidate = MazeController.Cell(x, z);
                if (candidate == null || candidate == startCell)
                    continue;

                float candidateDistance = (candidate.transform.position - targetPosition).sqrMagnitude;
                if (candidateDistance >= bestDistance)
                    continue;

                if (!TryBuildPath(startCell, candidate))
                    continue;

                bestDistance = candidateDistance;
                bestCell = candidate;

                if (bestPath == null)
                    bestPath = new List<MazeCell>();

                bestPath.Clear();
                bestPath.AddRange(pathCells);
            }
        }

        if (bestCell == null || bestPath == null || bestPath.Count == 0)
        {
            pathCells.Clear();
            return false;
        }

        pathCells.Clear();
        pathCells.AddRange(bestPath);
        targetNoiseCell = bestCell;
        return true;
    }

    private void BeginSearch()
    {
        state = ChaseState.Search;
        stateTimer = searchDuration;
        searchBaseYaw = enemyTransform.eulerAngles.y;
    }

    private bool HasReachedWithThreshold(Vector3 destination, float threshold)
    {
        Vector3 toDestination = destination - enemyTransform.position;
        toDestination.y = 0f;
        return toDestination.magnitude <= threshold;
    }

    public void DrawDebugGizmos()
    {
        if (enemyTransform == null)
            return;

        // Always show enemy position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(enemyTransform.position, 0.5f);

        if (lastSeenTarget != null && state != ChaseState.Idle && state != ChaseState.Chase)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeenTarget.position, 0.3f);
            Gizmos.DrawLine(enemyTransform.position, lastSeenTarget.position);
        }

        if (targetNodeCell != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetNodeCell.transform.position, 0.4f);
        }

        if (targetCupboardCell != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
            Gizmos.DrawWireSphere(targetCupboardCell.transform.position, 0.35f);
        }

        if (targetNoiseCell != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 1f);
            Gizmos.DrawWireSphere(targetNoiseCell.transform.position, 0.35f);
        }

        if (targetCupboard != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
            Gizmos.DrawWireCube(targetCupboard.CupboardPosition, Vector3.one * 0.3f);
        }

        if (pathCells.Count > 0)
        {
            Vector3 prevPos = lastSeenTarget != null ? lastSeenTarget.position : enemyTransform.position;

            for (int i = 0; i < pathCells.Count; i++)
            {
                if (pathCells[i] == null)
                    continue;

                Vector3 cellPos = pathCells[i].transform.position;

                if (i == pathCellIndex && state == ChaseState.GoToNextNode)
                    Gizmos.color = Color.cyan;
                else if (i < pathCellIndex)
                    Gizmos.color = Color.gray;
                else
                    Gizmos.color = Color.green;

                Gizmos.DrawLine(prevPos, cellPos);
                Gizmos.DrawWireSphere(cellPos, 0.25f);
                prevPos = cellPos;
            }

            // Draw path target
            if (pathTarget != null && state == ChaseState.GoToNextNode)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(pathTarget.position, Vector3.one * 0.3f);
            }

            if (pathTarget != null && state == ChaseState.GoToNoise)
            {
                Gizmos.color = new Color(1f, 0.85f, 0f, 1f);
                Gizmos.DrawWireCube(pathTarget.position, Vector3.one * 0.3f);
            }
        }
    }
}
