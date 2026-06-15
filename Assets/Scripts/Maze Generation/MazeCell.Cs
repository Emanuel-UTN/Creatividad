using UnityEngine;
using System.Collections.Generic;

public class MazeCell : MonoBehaviour
{
    public bool visited = false;
    public bool hasDoor = false;
    public GameObject wallNorth, wallSouth, wallEast, wallWest;
    public GameObject floor;
    public GameObject ceiling;
    private Light generatedLight;

    [Header("Celdas Vecinas")]
    public List<Vector2Int> neighbors = new List<Vector2Int>();

    private void ActiveWallDirection(string direction, bool active)
    {
        switch (direction)
        {
            case "North": if (wallNorth) wallNorth.SetActive(active); break;
            case "South": if (wallSouth) wallSouth.SetActive(active); break;
            case "East":  if (wallEast)  wallEast.SetActive(active);  break;
            case "West":  if (wallWest)  wallWest.SetActive(active);  break;
        }
    }

    public void RemoveWall(string direction)
    {
        ActiveWallDirection(direction, false);
    }

    public void SetWall(string direction, bool active = true)
    {
        ActiveWallDirection(direction, active);
    }

    public void SetDoor(GameObject doorPrefab, string direction, int lockCount = 0)
    {
        hasDoor = true;
        // Si se especifica un número de candados, agregar el componente Door
        if (lockCount > 0)
        {
            GameObject wall = null;
            switch (direction)
            {
                case "North": wall = wallNorth; break;
                case "South": wall = wallSouth; break;
                case "East":  wall = wallEast;  break;
                case "West":  wall = wallWest;  break;
            }

            if (wall != null)
            {
                wall.SetActive(false); // Desactivar la pared existente
                // Reemplazar la pared con una puerta
                Vector3 position = wall.transform.position;
                Quaternion rotation = Quaternion.identity;
                switch (direction)
                {
                    case "North": rotation = Quaternion.Euler(0, 180, 0); break;
                    case "South": rotation = Quaternion.Euler(0, 0, 0); break;
                    case "East":  rotation = Quaternion.Euler(0, 270, 0); break;
                    case "West":  rotation = Quaternion.Euler(0, 90, 0); break;
                }
                GameObject doorInstance = Instantiate(doorPrefab, position + Vector3.up * 1.5f, rotation, transform);
                Door doorScript = doorInstance.GetComponent<Door>();
                if (doorScript != null)
                {
                    doorScript.Initiate(lockCount);
                }
                Debug.Log(direction + " wall replaced with door having " + lockCount + " locks.");
            }
        }
    }

    public GameObject TrySpawnInFrontOfWall(GameObject prefab, Vector3 wallDirection, float wallInset, float groundClearance, Transform parent = null, List<Vector3> occupiedSpawnPositions = null)
    {
        if (prefab == null)
            return null;

        if (!TryGetWallObject(wallDirection, out GameObject wall) || wall == null || !wall.activeSelf)
            return null;

        Vector3 wallForward = wall.transform.forward;
        wallForward.y = 0f;
        if (wallForward.sqrMagnitude <= 0.0001f)
            return null;

        Vector3 spawnPosition = wall.transform.position + wallForward.normalized * wallInset;
        spawnPosition.y = GetFloorY() + groundClearance;

        Quaternion rotation = Quaternion.LookRotation(wallForward.normalized, Vector3.up);
        GameObject spawnedObject = Instantiate(prefab, spawnPosition, rotation, parent);

        AlignSpawnToGroundAndWall(spawnedObject, wall.transform, wallForward.normalized, wallInset, groundClearance);

        if (IsTooCloseToOccupiedPosition(spawnedObject.transform.position, occupiedSpawnPositions))
        {
            Destroy(spawnedObject);
            return null;
        }

        if (occupiedSpawnPositions != null)
            RegisterSpawnedPosition(spawnedObject.transform.position, occupiedSpawnPositions);

        MazeSpawnAnchor spawnAnchor = spawnedObject.GetComponent<MazeSpawnAnchor>();
        if (spawnAnchor == null)
            spawnAnchor = spawnedObject.AddComponent<MazeSpawnAnchor>();
        spawnAnchor.Initialize(this, wallForward.normalized);

        return spawnedObject;
    }

    private bool TryGetWallObject(Vector3 wallDirection, out GameObject wall)
    {
        wall = null;

        if (wallDirection == Vector3.forward)
        {
            wall = wallNorth;
            return true;
        }

        if (wallDirection == Vector3.back)
        {
            wall = wallSouth;
            return true;
        }

        if (wallDirection == Vector3.right)
        {
            wall = wallEast;
            return true;
        }

        if (wallDirection == Vector3.left)
        {
            wall = wallWest;
            return true;
        }

        return false;
    }

    private float GetFloorY()
    {
        if (floor != null)
            return floor.transform.position.y;

        return transform.position.y;
    }

    private bool IsTooCloseToOccupiedPosition(Vector3 spawnPosition, List<Vector3> occupiedSpawnPositions)
    {
        if (occupiedSpawnPositions == null || occupiedSpawnPositions.Count == 0)
            return false;

        float minSqrDistance = 0.0001f;

        for (int i = 0; i < occupiedSpawnPositions.Count; i++)
        {
            if ((spawnPosition - occupiedSpawnPositions[i]).sqrMagnitude <= minSqrDistance)
                return true;
        }

        return false;
    }

    private void AlignSpawnToGroundAndWall(GameObject spawnedObject, Transform wall, Vector3 wallForward, float wallInset, float groundClearance)
    {
        if (spawnedObject == null || wall == null)
            return;

        if (!TryGetSpawnBounds(spawnedObject, out Bounds bounds))
            return;

        float targetGroundY = GetFloorY() + groundClearance;
        float groundDelta = targetGroundY - bounds.min.y;
        if (Mathf.Abs(groundDelta) > 0.0001f)
            spawnedObject.transform.position += Vector3.up * groundDelta;

        float currentEdge = GetBoundsEdgeOppositeDirection(bounds, wallForward);
        float targetEdge = GetTargetEdgeAlongDirection(wall.position, wallForward, wallInset);
        float wallDelta = IsPositiveDirection(wallForward)
            ? targetEdge - currentEdge
            : currentEdge - targetEdge;

        if (Mathf.Abs(wallDelta) > 0.0001f)
            spawnedObject.transform.position += wallForward * wallDelta;
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

    private float GetBoundsEdgeOppositeDirection(Bounds bounds, Vector3 wallForward)
    {
        if (Mathf.Abs(wallForward.x) > Mathf.Abs(wallForward.z))
            return wallForward.x > 0f ? bounds.min.x : bounds.max.x;

        return wallForward.z > 0f ? bounds.min.z : bounds.max.z;
    }

    private float GetTargetEdgeAlongDirection(Vector3 wallPosition, Vector3 wallForward, float wallInset)
    {
        float effectiveInset = Mathf.Clamp(wallInset, 0.02f, 0.05f);

        if (Mathf.Abs(wallForward.x) > Mathf.Abs(wallForward.z))
            return wallPosition.x + (wallForward.x > 0f ? effectiveInset : -effectiveInset);

        return wallPosition.z + (wallForward.z > 0f ? effectiveInset : -effectiveInset);
    }

    private bool IsPositiveDirection(Vector3 wallForward)
    {
        return wallForward.x > 0f || wallForward.z > 0f;
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

    public LightController CreateLight(GameObject lightPrefab, bool enabled = true)
    {
        GameObject obj = Instantiate(lightPrefab, ceiling.transform.position - Vector3.up * 0.3f, Quaternion.Euler(90, 0, 0), transform);

        LightController light = obj.GetComponent<LightController>();

        if (light != null)
            light.SetLightState(enabled);
        
        return light;
    }
}