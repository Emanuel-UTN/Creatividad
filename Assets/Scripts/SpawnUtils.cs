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

    [Header("Flashlight Battery")]
    public bool spawnFlashlightBatteries = true;
    public GameObject flashlightBatteryPrefab;
    public int minFlashlightBatterySpawns = 4;
    public int maxFlashlightBatterySpawns = 7;
    public float batteryChargeAmount = 30f;

    private Transform spawnedRoot;
    private readonly List<MazeCell> eligibleCellsBuffer = new List<MazeCell>();
    private readonly List<Vector3> wallDirectionsBuffer = new List<Vector3>(4);

    public void Spawn(MazeCell[,] grid, float cellSize)
    {
        if (grid == null || grid.Length == 0)
            return;

        EnsureSpawnRoot();
        ClearSpawnedRoot();
        BuildEligibleWallCells(grid, eligibleCellsBuffer);

        if (eligibleCellsBuffer.Count == 0)
            return;

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
                if (!TryGetRandomWallCell(eligibleCellsBuffer, out MazeCell cell, out Vector3 wallDirection))
                    continue;

                Vector3 spawnPosition = GetSpawnPosition(cell, wallDirection, cellSize);
                Quaternion rotation = Quaternion.LookRotation(-wallDirection, Vector3.up);
                GameObject spawnedObject = Instantiate(entry.prefab, spawnPosition, rotation, spawnedRoot);
                AlignSpawnToGround(spawnedObject, cell.transform.position.y + groundClearance);
            }
        }

        SpawnFlashlightBatteryPickups(eligibleCellsBuffer, cellSize);
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

    private void BuildEligibleWallCells(MazeCell[,] grid, List<MazeCell> eligibleCells)
    {
        eligibleCells.Clear();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                MazeCell candidate = grid[x, z];
                if (candidate != null && HasAnyWall(candidate))
                    eligibleCells.Add(candidate);
            }
        }
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

    private void SpawnFlashlightBatteryPickups(List<MazeCell> eligibleCells, float cellSize)
    {
        if (!spawnFlashlightBatteries || eligibleCells == null || eligibleCells.Count == 0)
            return;

        int minAmount = Mathf.Max(0, Mathf.Min(minFlashlightBatterySpawns, maxFlashlightBatterySpawns));
        int maxAmount = Mathf.Max(minAmount, Mathf.Max(minFlashlightBatterySpawns, maxFlashlightBatterySpawns));
        int spawnCount = Random.Range(minAmount, maxAmount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryGetRandomWallCell(eligibleCells, out MazeCell cell, out Vector3 wallDirection))
                continue;

            Vector3 spawnPosition = GetSpawnPosition(cell, wallDirection, cellSize);
            Quaternion rotation = Quaternion.identity;
            GameObject pickupObject = CreateFlashlightBatteryPickup();
            pickupObject.transform.SetParent(spawnedRoot, false);
            pickupObject.transform.SetPositionAndRotation(spawnPosition, rotation);
            AlignSpawnToGround(pickupObject, cell.transform.position.y + groundClearance);
        }
    }

    private GameObject CreateFlashlightBatteryPickup()
    {
        if (flashlightBatteryPrefab != null)
        {
            GameObject prefabInstance = Instantiate(flashlightBatteryPrefab);
            FlashlightBatteryPickup prefabPickup = prefabInstance.GetComponent<FlashlightBatteryPickup>();
            if (prefabPickup != null)
                prefabPickup.SetBatteryAmount(batteryChargeAmount);

            return prefabInstance;
        }

        GameObject root = new GameObject("FlashlightBatteryPickup");
        FlashlightBatteryPickup pickup = root.AddComponent<FlashlightBatteryPickup>();
        pickup.SetBatteryAmount(batteryChargeAmount);

        SphereCollider trigger = root.GetComponent<SphereCollider>();
        trigger.center = new Vector3(0f, 0.45f, 0f);
        trigger.radius = 0.45f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Destroy(body.GetComponent<Collider>());

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "Cap";
        cap.transform.SetParent(root.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        cap.transform.localScale = new Vector3(0.22f, 0.12f, 0.22f);
        Destroy(cap.GetComponent<Collider>());

        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyRenderer.material.color = new Color(0.18f, 0.18f, 0.2f, 1f);

        Renderer capRenderer = cap.GetComponent<Renderer>();
        if (capRenderer != null)
            capRenderer.material.color = new Color(0.95f, 0.88f, 0.28f, 1f);

        return root;
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
