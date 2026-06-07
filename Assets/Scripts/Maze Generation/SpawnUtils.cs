using UnityEngine;
using System.Collections.Generic;

public class SpawnUtils : MonoBehaviour
{
    private static readonly Vector3[] WallDirections =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.right,
        Vector3.left
    };

    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [Header("Spawnables")]
    public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    [Header("Placement")]
    public float wallInset = 0.6f;
    public float groundClearance = 0.02f;
    public float minimumSpawnSeparation = 0.15f;

    private Transform spawnedRoot;
    private readonly List<MazeCell> eligibleCellsBuffer = new List<MazeCell>();
    private readonly List<Vector3> wallDirectionsBuffer = new List<Vector3>(4);
    private readonly List<SpawnSlot> spawnSlotsBuffer = new List<SpawnSlot>();
    private readonly List<Vector3> occupiedSpawnPositionsBuffer = new List<Vector3>();

    private struct SpawnSlot
    {
        public MazeCell cell;
        public Vector3 wallDirection;
        public Vector3 spawnPosition;
    }

    public void Spawn(List<MazeCell> eligibleCells = null)
    {
        if (MazeController.Grid == null || MazeController.Grid.Length == 0)
            return;

        EnsureSpawnRoot();
        ClearSpawnedRoot();
        BuildEligibleWallCells(eligibleCells, eligibleCellsBuffer);
        BuildSpawnSlots(eligibleCellsBuffer, spawnSlotsBuffer);

        if (spawnSlotsBuffer.Count == 0)
            return;

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            SpawnEntry entry = spawnEntries[i];
            if (entry == null || entry.prefab == null)
                continue;

            int minAmount = Mathf.Max(1, Mathf.Min(entry.minAmount, entry.maxAmount));
            int maxAmount = Mathf.Max(1, Mathf.Max(entry.minAmount, entry.maxAmount));
            int spawnCount = Random.Range(minAmount, maxAmount + 1);

            SpawnObjectsFromSlots(entry.prefab, spawnCount, spawnSlotsBuffer, occupiedSpawnPositionsBuffer);
        }
    }

    public List<GameObject> SpawnObjects(GameObject prefab, int count, List<MazeCell> eligibleCells = null)
    {
        if (prefab == null || count <= 0)
            return new List<GameObject>();

        EnsureSpawnRoot();

        BuildEligibleWallCells(eligibleCells, eligibleCellsBuffer);

        BuildSpawnSlots(eligibleCellsBuffer, spawnSlotsBuffer);

        List<GameObject> spawnedObjects = new List<GameObject>();
        RegisterExistingSpawnPositions();

        spawnedObjects.AddRange(SpawnObjectsFromSlots(prefab, count, spawnSlotsBuffer, occupiedSpawnPositionsBuffer));

        return spawnedObjects;
    }

    public bool TryReserveSpawnPosition(Vector3 position)
    {
        return TryReserveSpawnPosition(position, occupiedSpawnPositionsBuffer);
    }

    public bool IsSpawnPositionOccupied(Vector3 position)
    {
        return IsTooCloseToOccupiedPosition(position, occupiedSpawnPositionsBuffer);
    }

    private List<GameObject> SpawnObjectsFromSlots(GameObject prefab, int count, List<SpawnSlot> availableSlots, List<Vector3> occupiedSpawnPositions)
    {
        List<GameObject> spawnedObjects = new List<GameObject>();

        if (prefab == null || count <= 0 || availableSlots == null || availableSlots.Count == 0)
            return spawnedObjects;

        int spawnedCount = 0;
        while (spawnedCount < count && availableSlots.Count > 0)
        {
            int slotIndex = SelectBestSpawnSlotIndex(availableSlots, occupiedSpawnPositions);
            SpawnSlot slot = availableSlots[slotIndex];
            availableSlots.RemoveAt(slotIndex);

            GameObject spawnedObject = slot.cell != null
                ? slot.cell.TrySpawnInFrontOfWall(prefab, slot.wallDirection, wallInset, groundClearance, spawnedRoot, occupiedSpawnPositions)
                : null;

            if (spawnedObject == null)
                continue;

            spawnedObjects.Add(spawnedObject);
            spawnedCount++;
        }

        return spawnedObjects;
    }

    private void EnsureSpawnRoot()
    {
        if (spawnedRoot != null)
            return;

        GameObject root = new GameObject("SpawnedObjects");
        root.transform.SetParent(transform, false);
        spawnedRoot = root.transform;
    }

    private void ClearSpawnedRoot()
    {
        if (spawnedRoot == null)
            return;

        for (int i = spawnedRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(spawnedRoot.GetChild(i).gameObject);
        }
    }

    private void BuildEligibleWallCells(List<MazeCell> sourceCells, List<MazeCell> eligibleCells)
    {
        eligibleCells.Clear();

        if (sourceCells != null && sourceCells.Count > 0)
        {
            for (int i = 0; i < sourceCells.Count; i++)
            {
                MazeCell candidate = sourceCells[i];
                if (candidate != null && !candidate.hasDoor && HasAnyWall(candidate))
                    eligibleCells.Add(candidate);
            }

            return;
        }

        if (MazeController.Grid == null)
            return;

        for (int x = 0; x < MazeController.Grid.GetLength(0); x++)
        {
            for (int z = 0; z < MazeController.Grid.GetLength(1); z++)
            {
                MazeCell candidate = MazeController.Grid[x, z];
                if (candidate != null && !candidate.hasDoor && HasAnyWall(candidate))
                    eligibleCells.Add(candidate);
            }
        }
    }

    private void BuildSpawnSlots(List<MazeCell> eligibleCells, List<SpawnSlot> spawnSlots)
    {
        spawnSlots.Clear();

        for (int i = 0; i < eligibleCells.Count; i++)
        {
            MazeCell cell = eligibleCells[i];
            if (cell == null)
                continue;

            if (!TryGetActiveWallDirections(cell, wallDirectionsBuffer))
                continue;

            for (int directionIndex = 0; directionIndex < wallDirectionsBuffer.Count; directionIndex++)
            {
                Vector3 wallDirection = wallDirectionsBuffer[directionIndex];

                spawnSlots.Add(new SpawnSlot
                {
                    cell = cell,
                    wallDirection = wallDirection,
                    spawnPosition = GetSpawnPosition(cell, wallDirection, MazeController.CellSize)
                });
            }
        }
    }

    private int SelectBestSpawnSlotIndex(List<SpawnSlot> availableSlots, List<Vector3> occupiedSpawnPositions)
    {
        if (availableSlots.Count == 1)
            return 0;

        int bestIndex = -1;
        float bestScore = float.NegativeInfinity;
        bool foundSeparatedSlot = false;

        for (int i = 0; i < availableSlots.Count; i++)
        {
            if (IsTooCloseToOccupiedPosition(availableSlots[i].spawnPosition, occupiedSpawnPositions))
                continue;

            foundSeparatedSlot = true;
            float score = GetSpawnSlotScore(availableSlots[i], occupiedSpawnPositions);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
            else if (Mathf.Abs(score - bestScore) <= 0.0001f && Random.value < 0.5f)
            {
                bestIndex = i;
            }
        }

        if (foundSeparatedSlot)
            return bestIndex >= 0 ? bestIndex : 0;

        bestIndex = 0;
        bestScore = float.NegativeInfinity;

        for (int i = 0; i < availableSlots.Count; i++)
        {
            float score = GetSpawnSlotScore(availableSlots[i], occupiedSpawnPositions);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool IsTooCloseToOccupiedPosition(Vector3 spawnPosition, List<Vector3> occupiedSpawnPositions)
    {
        if (occupiedSpawnPositions == null || occupiedSpawnPositions.Count == 0)
            return false;

        float minSqrDistance = minimumSpawnSeparation * minimumSpawnSeparation;

        for (int i = 0; i < occupiedSpawnPositions.Count; i++)
        {
            if ((spawnPosition - occupiedSpawnPositions[i]).sqrMagnitude <= minSqrDistance)
                return true;
        }

        return false;
    }

    private bool TryReserveSpawnPosition(Vector3 position, List<Vector3> occupiedSpawnPositions)
    {
        if (occupiedSpawnPositions == null)
            return false;

        if (IsTooCloseToOccupiedPosition(position, occupiedSpawnPositions))
            return false;

        occupiedSpawnPositions.Add(position);
        return true;
    }

    private float GetSpawnSlotScore(SpawnSlot slot, List<Vector3> occupiedSpawnPositions)
    {
        if (occupiedSpawnPositions == null || occupiedSpawnPositions.Count == 0)
            return Random.value;

        float minSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < occupiedSpawnPositions.Count; i++)
        {
            float sqrDistance = (slot.spawnPosition - occupiedSpawnPositions[i]).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
                minSqrDistance = sqrDistance;
        }

        return minSqrDistance;
    }

    private bool TryGetRandomWallCell(List<MazeCell> eligibleCells, out MazeCell cell, out Vector3 wallDirection)
    {
        cell = null;
        wallDirection = Vector3.forward;

        if (eligibleCells.Count == 0)
            return false;

        cell = eligibleCells[Random.Range(0, eligibleCells.Count)];

        if (!TryGetActiveWallDirections(cell, wallDirectionsBuffer))
            return false;

        wallDirection = wallDirectionsBuffer[Random.Range(0, wallDirectionsBuffer.Count)];
        return true;
    }

    private void ResetOccupiedSpawnPositions()
    {
        occupiedSpawnPositionsBuffer.Clear();
    }

    private void RegisterExistingSpawnPositions()
    {
        if (spawnedRoot == null)
            return;

        for (int i = 0; i < spawnedRoot.childCount; i++)
        {
            RegisterSpawnedPosition(spawnedRoot.GetChild(i).position, occupiedSpawnPositionsBuffer);
        }
    }

    private void RegisterSpawnedPosition(Vector3 position, List<Vector3> occupiedSpawnPositions)
    {
        if (occupiedSpawnPositions == null)
            return;

        for (int i = 0; i < occupiedSpawnPositions.Count; i++)
        {
            if ((position - occupiedSpawnPositions[i]).sqrMagnitude <= 0.0001f)
                return;
        }

        occupiedSpawnPositions.Add(position);
    }

    private bool HasAnyWall(MazeCell cell)
    {
        return (cell.wallNorth != null && cell.wallNorth.activeSelf)
            || (cell.wallSouth != null && cell.wallSouth.activeSelf)
            || (cell.wallEast != null && cell.wallEast.activeSelf)
            || (cell.wallWest != null && cell.wallWest.activeSelf);
    }

    private bool TryGetActiveWallDirections(MazeCell cell, List<Vector3> directions)
    {
        directions.Clear();

        if (cell.wallNorth != null && cell.wallNorth.activeSelf)
            directions.Add(WallDirections[0]);
        if (cell.wallSouth != null && cell.wallSouth.activeSelf)
            directions.Add(WallDirections[1]);
        if (cell.wallEast != null && cell.wallEast.activeSelf)
            directions.Add(WallDirections[2]);
        if (cell.wallWest != null && cell.wallWest.activeSelf)
            directions.Add(WallDirections[3]);

        return directions.Count > 0;
    }

    private Vector3 GetSpawnPosition(MazeCell cell, Vector3 wallDirection, float cellSize)
    {
        Vector3 center = cell.transform.position;
        float offset = Mathf.Max(0.1f, (cellSize * 0.5f) - wallInset);
        Vector3 position = center + wallDirection.normalized * offset;
        position.y = center.y;
        return position;
    }

}
