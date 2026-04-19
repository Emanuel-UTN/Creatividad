using System.Collections.Generic;
using UnityEngine;

public class PatrolBehaviour : Behaviour
{
    private readonly float waitAtEndTime;
    private readonly float defaultMovementOffset;
    private readonly System.Action<MazeCell> onCellPositionChanged;

    private readonly Dictionary<MazeCell, Vector2Int> cellToCoords = new Dictionary<MazeCell, Vector2Int>();
    private readonly Dictionary<Vector2Int, int> lastVisitByCell = new Dictionary<Vector2Int, int>();
    private readonly Queue<Vector2Int> recentVisitedQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> recentVisitedSet = new HashSet<Vector2Int>();
    private readonly List<MazeCell> freshCandidates = new List<MazeCell>();
    private readonly List<float> freshWeights = new List<float>();
    private readonly List<MazeCell> allCandidates = new List<MazeCell>();
    private readonly List<float> allWeights = new List<float>();
    private const int recentVisitMemory = 4;

    private int patrolVisitStep;

    private MazeCell currentCell;
    private MazeCell targetCell;
    private readonly Transform patrolTarget;

    public PatrolBehaviour(
        Movement movement,
        Transform enemyTransform,
        float waitAtEndTime,
        float defaultMovementOffset,
        System.Action<MazeCell> onCellPositionChanged)
        : base(movement, enemyTransform)
    {
        this.waitAtEndTime = waitAtEndTime;
        this.defaultMovementOffset = defaultMovementOffset;
        this.onCellPositionChanged = onCellPositionChanged;

        BuildCellLookup();

        GameObject patrolObj = new GameObject($"{enemyTransform.name}_PatrolTarget");
        patrolTarget = patrolObj.transform;
        patrolTarget.position = enemyTransform.position;

        currentCell = GetCellByEnemyPosition();
        UpdateCellPosition(currentCell);
        MarkCellVisited(currentCell);
    }

    public void ResetToClosestPoint()
    {
        currentCell = GetCellByEnemyPosition();
        targetCell = null;
        UpdateCellPosition(currentCell);
        MarkCellVisited(currentCell);
    }

    public void Tick()
    {
        if (currentCell == null)
        {
            currentCell = GetCellByEnemyPosition();
            UpdateCellPosition(currentCell);
        }

        if (currentCell == null)
        {
            Stop(defaultMovementOffset, true);
            return;
        }

        UpdatePatrol();
    }

    private void UpdatePatrol()
    {
        if (targetCell == null)
        {
            if (!TrySelectNextNeighborCell(out MazeCell selectedCell))
                return;

            targetCell = selectedCell;
            patrolTarget.position = targetCell.transform.position;
        }

        Move(patrolTarget, 0f);

        float arrivalThreshold = Mathf.Max(defaultMovementOffset * 0.35f, movement.speed * Time.deltaTime * 1.5f, 0.18f);
        if (!HasReachedWithThreshold(patrolTarget.position, arrivalThreshold))
            return;

        currentCell = targetCell;
        UpdateCellPosition(currentCell);
        MarkCellVisited(currentCell);
        targetCell = null;
    }

    public void Dispose()
    {
        if (patrolTarget != null)
            Object.Destroy(patrolTarget.gameObject);
    }

    private bool TrySelectNextNeighborCell(out MazeCell nextCell)
    {
        nextCell = null;

        if (currentCell == null || currentCell.neighbors == null || currentCell.neighbors.Count == 0)
            return false;

        freshCandidates.Clear();
        freshWeights.Clear();
        allCandidates.Clear();
        allWeights.Clear();

        for (int i = 0; i < currentCell.neighbors.Count; i++)
        {
            Vector2Int coords = currentCell.neighbors[i];
            MazeCell neighbor = TryGetCell(coords);
            if (neighbor == null)
                continue;

            float weight = GetCellWeight(coords);
            allCandidates.Add(neighbor);
            allWeights.Add(weight);

            if (recentVisitedSet.Contains(coords))
                continue;

            freshCandidates.Add(neighbor);
            freshWeights.Add(weight);
        }

        if (freshCandidates.Count > 0)
        {
            nextCell = PickWeightedRandomCell(freshCandidates, freshWeights);
            return nextCell != null;
        }

        if (allCandidates.Count > 0)
        {
            nextCell = PickWeightedRandomCell(allCandidates, allWeights);
            return nextCell != null;
        }

        return false;
    }

    private MazeCell PickWeightedRandomCell(List<MazeCell> candidates, List<float> weights)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
            totalWeight += weights[i];

        if (totalWeight <= 0f)
            return candidates[Random.Range(0, candidates.Count)];

        float pick = Random.Range(0f, totalWeight);
        float running = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            running += weights[i];
            if (pick <= running)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private float GetCellWeight(Vector2Int coords)
    {
        if (!lastVisitByCell.TryGetValue(coords, out int lastVisit))
            return patrolVisitStep + 2f;

        int age = patrolVisitStep - lastVisit;
        return Mathf.Max(0.2f, age);
    }

    private void MarkCellVisited(MazeCell cell)
    {
        if (cell == null || !cellToCoords.TryGetValue(cell, out Vector2Int coords))
            return;

        patrolVisitStep++;
        lastVisitByCell[coords] = patrolVisitStep;

        recentVisitedQueue.Enqueue(coords);
        recentVisitedSet.Add(coords);

        while (recentVisitedQueue.Count > recentVisitMemory)
        {
            Vector2Int old = recentVisitedQueue.Dequeue();
            recentVisitedSet.Remove(old);
        }
    }

    private void UpdateCellPosition(MazeCell cell)
    {
        onCellPositionChanged?.Invoke(cell);
    }

    private bool HasReachedWithThreshold(Vector3 destination, float threshold)
    {
        Vector3 toDestination = destination - enemyTransform.position;
        toDestination.y = 0f;
        return toDestination.magnitude <= threshold;
    }

    private void BuildCellLookup()
    {
        cellToCoords.Clear();

        if (GameController.gameController == null)
            return;

        MazeCell[,] grid = GameController.gameController.Grid();
        if (grid == null)
            return;

        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                MazeCell cell = grid[x, z];
                if (cell != null)
                    cellToCoords[cell] = new Vector2Int(x, z);
            }
        }
    }

    private MazeCell TryGetCell(Vector2Int coords)
    {
        if (GameController.gameController == null)
            return null;

        MazeCell[,] grid = GameController.gameController.Grid();
        if (grid == null)
            return null;

        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        if (coords.x < 0 || coords.y < 0 || coords.x >= width || coords.y >= height)
            return null;

        return GameController.gameController.Cell(coords.x, coords.y);
    }

    private MazeCell GetCellByEnemyPosition()
    {
        if (GameController.gameController == null)
            return null;

        MazeCell byPosition = GameController.gameController.GetCellByPosition(enemyTransform.position);
        if (byPosition != null)
            return byPosition;

        if (cellToCoords.Count == 0)
            BuildCellLookup();

        MazeCell closest = null;
        float best = float.MaxValue;

        foreach (KeyValuePair<MazeCell, Vector2Int> entry in cellToCoords)
        {
            MazeCell candidate = entry.Key;
            if (candidate == null)
                continue;

            float sqr = (candidate.transform.position - enemyTransform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                closest = candidate;
            }
        }

        return closest;
    }
}
