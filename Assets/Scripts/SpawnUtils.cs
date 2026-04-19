using UnityEngine;
using System.Collections.Generic;

public class SpawnUtils : MonoBehaviour
{
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

    private Transform spawnedRoot;

    public void Spawn(MazeCell[,] grid, float cellSize)
    {
        if (grid == null || grid.Length == 0)
            return;

        EnsureSpawnRoot();
        ClearSpawnedRoot();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            SpawnEntry entry = spawnEntries[i];
            if (entry == null || entry.prefab == null)
                continue;

            int minAmount = Mathf.Max(1, Mathf.Min(entry.minAmount, entry.maxAmount));
            int maxAmount = Mathf.Max(1, Mathf.Max(entry.minAmount, entry.maxAmount));
            int spawnCount = Random.Range(minAmount, maxAmount + 1);

            for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
            {
                if (!TryGetRandomWallCell(grid, out MazeCell cell, out Vector3 wallDirection))
                    continue;

                Vector3 spawnPosition = GetSpawnPosition(cell, wallDirection, cellSize);
                Quaternion rotation = Quaternion.LookRotation(-wallDirection, Vector3.up);
                GameObject spawnedObject = Instantiate(entry.prefab, spawnPosition, rotation, spawnedRoot);
                AlignSpawnToGround(spawnedObject, cell.transform.position.y + groundClearance);
            }
        }
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

    private bool TryGetRandomWallCell(MazeCell[,] grid, out MazeCell cell, out Vector3 wallDirection)
    {
        cell = null;
        wallDirection = Vector3.forward;

        List<MazeCell> eligibleCells = new List<MazeCell>();
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                MazeCell candidate = grid[x, z];
                if (candidate != null && HasAnyWall(candidate))
                    eligibleCells.Add(candidate);
            }
        }

        if (eligibleCells.Count == 0)
            return false;

        cell = eligibleCells[Random.Range(0, eligibleCells.Count)];

        List<Vector3> wallDirections = GetActiveWallDirections(cell);
        if (wallDirections.Count == 0)
            return false;

        wallDirection = wallDirections[Random.Range(0, wallDirections.Count)];
        return true;
    }

    private bool HasAnyWall(MazeCell cell)
    {
        return (cell.wallNorth != null && cell.wallNorth.activeSelf)
            || (cell.wallSouth != null && cell.wallSouth.activeSelf)
            || (cell.wallEast != null && cell.wallEast.activeSelf)
            || (cell.wallWest != null && cell.wallWest.activeSelf);
    }

    private List<Vector3> GetActiveWallDirections(MazeCell cell)
    {
        List<Vector3> directions = new List<Vector3>();

        if (cell.wallNorth != null && cell.wallNorth.activeSelf)
            directions.Add(Vector3.forward);
        if (cell.wallSouth != null && cell.wallSouth.activeSelf)
            directions.Add(Vector3.back);
        if (cell.wallEast != null && cell.wallEast.activeSelf)
            directions.Add(Vector3.right);
        if (cell.wallWest != null && cell.wallWest.activeSelf)
            directions.Add(Vector3.left);

        return directions;
    }

    private Vector3 GetSpawnPosition(MazeCell cell, Vector3 wallDirection, float cellSize)
    {
        Vector3 center = cell.transform.position;
        float offset = Mathf.Max(0.1f, (cellSize * 0.5f) - wallInset);
        Vector3 position = center + wallDirection.normalized * offset;
        position.y = center.y;
        return position;
    }

    private void AlignSpawnToGround(GameObject spawnedObject, float groundY)
    {
        if (spawnedObject == null)
            return;

        if (!TryGetSpawnBounds(spawnedObject, out Bounds bounds))
            return;

        float deltaY = groundY - bounds.min.y;
        if (Mathf.Abs(deltaY) <= 0.0001f)
            return;

        spawnedObject.transform.position += Vector3.up * deltaY;
    }

    private bool TryGetSpawnBounds(GameObject spawnedObject, out Bounds bounds)
    {
        Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
        Collider[] colliders = spawnedObject.GetComponentsInChildren<Collider>();

        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }
}