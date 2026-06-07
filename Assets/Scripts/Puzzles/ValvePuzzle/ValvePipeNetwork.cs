using System.Collections.Generic;
using UnityEngine;

public static class ValvePipeNetwork
{
    private static readonly Vector2Int North = new Vector2Int(0, 1);
    private static readonly Vector2Int South = new Vector2Int(0, -1);
    private static readonly Vector2Int East = new Vector2Int(1, 0);
    private static readonly Vector2Int West = new Vector2Int(-1, 0);

    private enum PipeShape
    {
        Straight,
        Corner,
        Junction
    }

    private sealed class NetworkGraph
    {
        public readonly Dictionary<Vector2Int, HashSet<Vector2Int>> Adjacency = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

        public void AddNode(Vector2Int node)
        {
            if (!Adjacency.ContainsKey(node))
                Adjacency[node] = new HashSet<Vector2Int>();
        }

        public void AddPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
                return;

            for (int i = 0; i < path.Count; i++)
                AddNode(path[i]);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2Int a = path[i];
                Vector2Int b = path[i + 1];
                Adjacency[a].Add(b);
                Adjacency[b].Add(a);
            }
        }
    }

    public static void Build(
        Transform parent,
        MazeRoom room,
        GameObject vaultObject,
        List<Valve> valves,
        GameObject straightPipePrefab,
        GameObject elbowPipePrefab,
        GameObject junctionPipePrefab,
        float ceilingHeightOffset,
        float wallInset)
    {
        if (parent == null || room == null || vaultObject == null || valves == null || valves.Count == 0)
            return;

        ClearExistingNetwork(parent);

        MazeCell startCell = FindBestStartCell(room, vaultObject.transform.position);
        if (startCell == null)
        {
            Debug.LogWarning("ValvePipeNetwork: no se pudo resolver la celda inicial de la bóveda.");
            return;
        }

        NetworkGraph graph = BuildGraph(startCell, valves);
        if (graph.Adjacency.Count == 0)
        {
            Debug.LogWarning("ValvePipeNetwork: no se generó ninguna conexión para las tuberías.");
            return;
        }

        GameObject networkRoot = new GameObject("ValvePipeNetwork");
        networkRoot.transform.SetParent(parent, false);

        // Build list of cells to exclude from rendering standard ceiling pipes
        HashSet<Vector2Int> excludeCells = new HashSet<Vector2Int>();
        
        // 1. Exclude all room cells (so room ceiling remains clean)
        if (room.cells != null)
        {
            for (int i = 0; i < room.cells.Count; i++)
            {
                if (room.cells[i] != null)
                    excludeCells.Add(MazeController.GetCellCoordinates(room.cells[i]));
            }
        }

        // 2. Exclude all valve cells (where players interact with valves)
        for (int i = 0; i < valves.Count; i++)
        {
            Valve valve = valves[i];
            if (valve == null)
                continue;

            MazeSpawnAnchor anchor = valve.GetComponentInParent<MazeSpawnAnchor>();
            MazeCell targetCell = anchor != null ? anchor.SourceCell : null;
            if (targetCell != null)
                excludeCells.Add(MazeController.GetCellCoordinates(targetCell));
        }

        RenderGraph(
            networkRoot.transform,
            graph,
            straightPipePrefab,
            elbowPipePrefab,
            junctionPipePrefab,
            ceilingHeightOffset,
            wallInset,
            excludeCells);
    }

    private static void ClearExistingNetwork(Transform parent)
    {
        Transform existing = parent.Find("ValvePipeNetwork");
        if (existing != null)
            Object.Destroy(existing.gameObject);
    }

    private static NetworkGraph BuildGraph(MazeCell startCell, List<Valve> valves)
    {
        NetworkGraph graph = new NetworkGraph();
        Vector2Int start = MazeController.GetCellCoordinates(startCell);
        graph.AddNode(start);

        for (int i = 0; i < valves.Count; i++)
        {
            Valve valve = valves[i];
            if (valve == null)
                continue;

            MazeSpawnAnchor anchor = valve.GetComponentInParent<MazeSpawnAnchor>();
            MazeCell targetCell = anchor != null ? anchor.SourceCell : null;
            if (targetCell == null)
            {
                Debug.LogWarning("ValvePipeNetwork: no se pudo resolver la celda de una válvula.");
                continue;
            }

            List<Vector2Int> path = FindPath(startCell, targetCell);
            if (path.Count < 2)
            {
                Debug.LogWarning("ValvePipeNetwork: no hay camino entre la bóveda y una válvula.");
                continue;
            }

            graph.AddPath(path);
        }

        return graph;
    }

    private static MazeCell FindBestStartCell(MazeRoom room, Vector3 position)
    {
        MazeCell closestCell = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < room.cells.Count; i++)
        {
            MazeCell candidate = room.cells[i];
            if (candidate == null)
                continue;

            float sqrDistance = (candidate.transform.position - position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestCell = candidate;
            }
        }

        return closestCell;
    }

    private static List<Vector2Int> FindPath(MazeCell startCell, MazeCell targetCell)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        if (startCell == null || targetCell == null || MazeController.Grid == null)
            return path;

        Vector2Int start = MazeController.GetCellCoordinates(startCell);
        Vector2Int target = MazeController.GetCellCoordinates(targetCell);

        if (start == target)
        {
            path.Add(start);
            return path;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == target)
                break;

            MazeCell currentCell = MazeController.Cell(current.x, current.y);
            if (currentCell == null)
                continue;

            for (int i = 0; i < currentCell.neighbors.Count; i++)
            {
                Vector2Int neighbor = currentCell.neighbors[i];
                if (visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        if (!visited.Contains(target))
            return path;

        Vector2Int step = target;
        path.Add(step);

        while (step != start)
        {
            if (!cameFrom.TryGetValue(step, out Vector2Int previous))
                break;

            step = previous;
            path.Add(step);
        }

        path.Reverse();
        return path;
    }

    private static void RenderGraph(
        Transform parent,
        NetworkGraph graph,
        GameObject straightPipePrefab,
        GameObject elbowPipePrefab,
        GameObject junctionPipePrefab,
        float ceilingHeightOffset,
        float wallInset,
        HashSet<Vector2Int> excludeCells)
    {
        foreach (KeyValuePair<Vector2Int, HashSet<Vector2Int>> entry in graph.Adjacency)
        {
            Vector2Int cellCoords = entry.Key;
            if (excludeCells != null && excludeCells.Contains(cellCoords))
                continue;

            HashSet<Vector2Int> neighbors = entry.Value;

            MazeCell cell = MazeController.Cell(cellCoords.x, cellCoords.y);
            if (cell == null || neighbors == null || neighbors.Count == 0)
                continue;

            Vector2Int[] directions = GetDirections(cellCoords, neighbors);
            PipeShape shape = GetPipeShape(directions);
            GameObject prefab = GetPrefabForShape(shape, straightPipePrefab, elbowPipePrefab, junctionPipePrefab);
            if (prefab == null)
                continue;

            Quaternion rotation = GetRotationForShape(shape, directions);
            Vector3 position = cell.transform.position + Vector3.up * ceilingHeightOffset;
            Object.Instantiate(prefab, position, rotation, parent);
        }
    }

    private static PipeShape GetPipeShape(Vector2Int[] directions)
    {
        if (directions.Length >= 3)
            return PipeShape.Junction;

        if (directions.Length == 2)
            return AreOpposite(directions[0], directions[1]) ? PipeShape.Straight : PipeShape.Corner;

        return PipeShape.Straight;
    }

    private static GameObject GetPrefabForShape(
        PipeShape shape,
        GameObject straightPipePrefab,
        GameObject elbowPipePrefab,
        GameObject junctionPipePrefab)
    {
        switch (shape)
        {
            case PipeShape.Corner:
                return elbowPipePrefab != null ? elbowPipePrefab : straightPipePrefab;
            case PipeShape.Junction:
                return junctionPipePrefab != null ? junctionPipePrefab : (elbowPipePrefab != null ? elbowPipePrefab : straightPipePrefab);
            default:
                return straightPipePrefab != null ? straightPipePrefab : elbowPipePrefab;
        }
    }

    private static Quaternion GetRotationForShape(PipeShape shape, Vector2Int[] directions)
    {
        switch (shape)
        {
            case PipeShape.Corner:
                return Quaternion.Euler(0f, 90f * GetCornerQuarterTurns(directions[0], directions[1]), 0f);
            case PipeShape.Junction:
                return Quaternion.Euler(0f, 90f * GetJunctionQuarterTurns(directions), 0f);
            default:
                return Quaternion.Euler(0f, 90f * GetStraightQuarterTurns(directions), 0f);
        }
    }

    private static Vector2Int[] GetDirections(Vector2Int cell, HashSet<Vector2Int> neighbors)
    {
        List<Vector2Int> directions = new List<Vector2Int>(neighbors.Count);

        foreach (Vector2Int neighbor in neighbors)
        {
            Vector2Int direction = neighbor - cell;
            if (direction == North || direction == South || direction == East || direction == West)
                directions.Add(direction);
        }

        return directions.ToArray();
    }

    private static bool AreOpposite(Vector2Int a, Vector2Int b)
    {
        return a + b == Vector2Int.zero;
    }

    private static int GetStraightQuarterTurns(Vector2Int[] directions)
    {
        if (directions.Length == 0)
            return 0;

        if (directions.Length == 1)
        {
            if (directions[0] == North || directions[0] == South)
                return 0;
            return 1;
        }

        if ((directions[0] == North && directions[1] == South) || (directions[0] == South && directions[1] == North))
            return 0;

        return 1;
    }

    private static int GetCornerQuarterTurns(Vector2Int first, Vector2Int second)
    {
        if ((first == North && second == East) || (first == East && second == North))
            return 0;

        if ((first == East && second == South) || (first == South && second == East))
            return 1;

        if ((first == South && second == West) || (first == West && second == South))
            return 2;

        return 3;
    }

    private static int GetJunctionQuarterTurns(Vector2Int[] directions)
    {
        bool hasNorth = ContainsDirection(directions, North);
        bool hasSouth = ContainsDirection(directions, South);
        bool hasEast = ContainsDirection(directions, East);
        bool hasWest = ContainsDirection(directions, West);

        if (!hasSouth)
            return 0;

        if (!hasWest)
            return 1;

        if (!hasNorth)
            return 2;

        if (!hasEast)
            return 3;

        return 0;
    }

    private static bool ContainsDirection(Vector2Int[] directions, Vector2Int direction)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            if (directions[i] == direction)
                return true;
        }

        return false;
    }
}
