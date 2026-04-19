using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Configuración")]
    public int width = 15;
    public int height = 15;
    public float cellSize = 4f;

    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject exitPrefab;
    public GameObject roomPrefab;

    [Header("Materiales")]
    public Material floorMaterial;

    [Header("Salas")]
    public int numberOfRooms = 3;
    public int roomSize = 3;

    [Header("Sala Principal")]
    public bool useMainRoom = true;
    public int mainRoomSize = 5;

    private MazeCell[,] grid;

    void Start()
    {
        GenerateMaze();
        GameController.gameController.Initialize(grid);
    }

    void GenerateMaze()
    {
        grid = new MazeCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{z}";
                
                grid[x, z] = cell.GetComponent<MazeCell>();

                // Aplicar material personalizado si existe
                if (floorMaterial != null)
                {
                    Renderer renderer = grid[x, z].floor.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material = floorMaterial;
                }
            }
        }

        GenerateDFS(0, 0);

        GenerateRooms();

        if (exitPrefab != null)
        {
            Vector3 exitPos = new Vector3((width - 1) * cellSize, 0, (height - 1) * cellSize);
            Instantiate(exitPrefab, exitPos + Vector3.up * 0.1f, Quaternion.identity);
        }
    }

    void GenerateDFS(int startX, int startZ)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        grid[startX, startZ].visited = true;
        stack.Push(new Vector2Int(startX, startZ));

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current.x, current.y);

            if (neighbors.Count > 0)
            {
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWallBetween(current, next);
                grid[next.x, next.y].visited = true;
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(int x, int z)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };
        foreach (var dir in directions)
        {
            int nx = x + dir.x, nz = z + dir.y;
            if (nx >= 0 && nx < width && nz >= 0 && nz < height && !grid[nx, nz].visited)
                result.Add(new Vector2Int(nx, nz));
        }
        return result;
    }

    void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        int dx = b.x - a.x, dz = b.y - a.y;
        if (dx == 1)  { grid[a.x, a.y].RemoveWall("East");  grid[b.x, b.y].RemoveWall("West"); }
        if (dx == -1) { grid[a.x, a.y].RemoveWall("West");  grid[b.x, b.y].RemoveWall("East"); }
        if (dz == 1)  { grid[a.x, a.y].RemoveWall("North"); grid[b.x, b.y].RemoveWall("South"); }
        if (dz == -1) { grid[a.x, a.y].RemoveWall("South"); grid[b.x, b.y].RemoveWall("North"); }

        grid[a.x, a.y].neighbors.Add(b);
        grid[b.x, b.y].neighbors.Add(a);
    }

    void GenerateRooms()
    {
        List<Vector2Int> usedCells = new List<Vector2Int>();
        int roomsCreated = 0;

        // --- Sala principal central ---
        if (useMainRoom)
        {
            int centerX = width / 2 - mainRoomSize / 2;
            int centerZ = height / 2 - mainRoomSize / 2;
            // Marcar celdas usadas
            for (int x = centerX; x < centerX + mainRoomSize; x++)
                for (int z = centerZ; z < centerZ + mainRoomSize; z++)
                    usedCells.Add(new Vector2Int(x, z));
            // Remover paredes internas
            for (int x = centerX; x < centerX + mainRoomSize; x++)
            {
                for (int z = centerZ; z < centerZ + mainRoomSize; z++)
                {
                    if (grid[x, z] != null)
                    {
                        if (z + 1 < centerZ + mainRoomSize){
                            grid[x, z].RemoveWall("North");
                            grid[x, z].neighbors.Add(new Vector2Int(x, z + 1));
                            grid[x, z + 1].neighbors.Add(new Vector2Int(x, z));
                        }
                        if (z - 1 >= centerZ){
                            grid[x, z].RemoveWall("South");
                            grid[x, z].neighbors.Add(new Vector2Int(x, z - 1));
                            grid[x, z - 1].neighbors.Add(new Vector2Int(x, z));
                        }
                        if (x + 1 < centerX + mainRoomSize){
                            grid[x, z].RemoveWall("East");
                            grid[x, z].neighbors.Add(new Vector2Int(x + 1, z));
                            grid[x + 1, z].neighbors.Add(new Vector2Int(x, z));
                        }
                        if (x - 1 >= centerX){
                            grid[x, z].RemoveWall("West");
                            grid[x, z].neighbors.Add(new Vector2Int(x - 1, z));
                            grid[x - 1, z].neighbors.Add(new Vector2Int(x, z));
                        }
                    }
                }
            }
            // Instanciar prefab de sala principal
            if (roomPrefab != null)
            {
                Vector3 roomCenter = new Vector3((centerX + mainRoomSize / 2f) * cellSize, 0, (centerZ + mainRoomSize / 2f) * cellSize);
                Instantiate(roomPrefab, roomCenter, Quaternion.identity, transform);
            }
        }

        while (roomsCreated < numberOfRooms)
        {
            int randomX = Random.Range(1, width - roomSize - 1);
            int randomZ = Random.Range(1, height - roomSize - 1);
            
            bool canPlace = true;
            for (int x = randomX; x < randomX + roomSize; x++)
            {
                for (int z = randomZ; z < randomZ + roomSize; z++)
                {
                    if (usedCells.Contains(new Vector2Int(x, z)))
                    {
                        canPlace = false;
                        break;
                    }
                }
                if (!canPlace) break;
            }

            if (canPlace)
            {
                // Marcar células como usadas
                for (int x = randomX; x < randomX + roomSize; x++)
                {
                    for (int z = randomZ; z < randomZ + roomSize; z++)
                    {
                        usedCells.Add(new Vector2Int(x, z));
                    }
                }

                // Remover paredes internas de la sala
                for (int x = randomX; x < randomX + roomSize; x++)
                {
                    for (int z = randomZ; z < randomZ + roomSize; z++)
                    {
                        if (grid[x, z] != null)
                        {
                            // Remover pared al norte
                            if (z + 1 < randomZ + roomSize){
                                grid[x, z].RemoveWall("North");
                                grid[x, z].neighbors.Add(new Vector2Int(x, z + 1));
                                grid[x, z + 1].neighbors.Add(new Vector2Int(x, z));
                            }// Remover pared al sur
                            if (z - 1 >= randomZ){
                                grid[x, z].RemoveWall("South");
                                grid[x, z].neighbors.Add(new Vector2Int(x, z - 1));
                                grid[x, z - 1].neighbors.Add(new Vector2Int(x, z));
                            }// Remover pared al este
                            if (x + 1 < randomX + roomSize){
                                grid[x, z].RemoveWall("East");
                                grid[x, z].neighbors.Add(new Vector2Int(x + 1, z));
                                grid[x + 1, z].neighbors.Add(new Vector2Int(x, z));
                            }// Remover pared al oeste
                            if (x - 1 >= randomX){
                                grid[x, z].RemoveWall("West");
                                grid[x, z].neighbors.Add(new Vector2Int(x - 1, z));
                                grid[x - 1, z].neighbors.Add(new Vector2Int(x, z));
                            }
                        }
                    }
                }

                // Instanciar el prefab de la sala si existe
                if (roomPrefab != null)
                {
                    Vector3 roomCenter = new Vector3((randomX + roomSize / 2) * cellSize, 0, (randomZ + roomSize / 2) * cellSize);
                    Instantiate(roomPrefab, roomCenter, Quaternion.identity, transform);
                }
                roomsCreated++;
            }
        }
    }
}