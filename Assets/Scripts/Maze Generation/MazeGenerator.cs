using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpawnUtils))]
[RequireComponent(typeof(PuzzleManager))]
public class MazeGenerator : MonoBehaviour
{
    private static readonly Vector2Int[] NeighborDirections =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    [Header("Configuración")]
    public int width = 15;
    public int height = 15;
    public float cellSize = 4f;
    [Range(0f, .6f)]
    public float probToConnectMoreNods = 0.15f;

    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject exitPrefab;
    public GameObject roomPrefab;
    public GameObject doorPrefab;

    [Header("Materiales")]
    public Material floorMaterial;

    [Header("Salas")]
    public int numberOfRooms = 3;
    public int roomSize = 3;

    [Header("Sala Principal")]
    public bool useMainRoom = true;
    public int mainRoomSize = 5;

    private MazeCell[,] grid { get { return MazeController.Grid; } set { MazeController.Grid = value; } }
    private List<MazeRoom> rooms = new List<MazeRoom>();
    private SpawnUtils spawnUtils;
    private readonly List<Vector2Int> neighborBuffer = new List<Vector2Int>(4);

    void Start()
    {
        if (cellPrefab == null)
            return;
        
        MazeController.CellSize = cellSize;
        GenerateMaze();
        spawnUtils = GetComponent<SpawnUtils>();

        int keyCount = GetComponent<PuzzleManager>().GeneratePuzzles(rooms, spawnUtils);
        
        InstantiateDoor(keyCount);

        if (spawnUtils != null)
            spawnUtils.Spawn();

        GameController.gameController.Initialize();

        // Initialize dynamic fog
        FogManager fogManager = FindAnyObjectByType<FogManager>();
        if (fogManager == null)
        {
            fogManager = gameObject.AddComponent<FogManager>();
        }
        fogManager.InitializeFog();
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

        GenerateRooms();

        GenerateDFS(0, 0);

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
                grid[next.x, next.y].visited = Random.value >= probToConnectMoreNods; // Probabilidad de marcar como visitado
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
        neighborBuffer.Clear();

        for (int i = 0; i < NeighborDirections.Length; i++)
        {
            Vector2Int dir = NeighborDirections[i];
            int nx = x + dir.x, nz = z + dir.y;
            if (nx >= 0 && nx < width && nz >= 0 && nz < height && !grid[nx, nz].visited)
                neighborBuffer.Add(new Vector2Int(nx, nz));
        }

        return neighborBuffer;
    }

    void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        int dx = b.x - a.x, dz = b.y - a.y;
        if (dx == 1)  { grid[a.x, a.y].RemoveWall("East");  grid[b.x, b.y].RemoveWall("West"); }
        if (dx == -1) { grid[a.x, a.y].RemoveWall("West");  grid[b.x, b.y].RemoveWall("East"); }
        if (dz == 1)  { grid[a.x, a.y].RemoveWall("North"); grid[b.x, b.y].RemoveWall("South"); }
        if (dz == -1) { grid[a.x, a.y].RemoveWall("South"); grid[b.x, b.y].RemoveWall("North"); }

        MazeController.CellsNeighbors(a, b);
    }

    void GenerateRooms()
    {
        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();
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
                            MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x, z + 1));
                        }
                        if (z - 1 >= centerZ){
                            grid[x, z].RemoveWall("South");
                            MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x, z - 1));
                        }
                        if (x + 1 < centerX + mainRoomSize){
                            grid[x, z].RemoveWall("East");
                            MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x + 1, z));
                        }
                        if (x - 1 >= centerX){
                            grid[x, z].RemoveWall("West");
                            MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x - 1, z));
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
                MazeRoom newRoom = new GameObject($"Room_{roomsCreated + 1}").AddComponent<MazeRoom>();
                newRoom.origin = new Vector2Int(randomX, randomZ);
                newRoom.transform.position = new Vector3((randomX + roomSize / 2f) * cellSize, 0, (randomZ + roomSize / 2f) * cellSize);
                newRoom.size = roomSize;

                for (int x = randomX; x < randomX + roomSize; x++)
                {
                    for (int z = randomZ; z < randomZ + roomSize; z++)
                    {
                        // Marcar celdas como usadas
                        usedCells.Add(new Vector2Int(x, z));

                        // Remover paredes internas de la sala
                        if (grid[x, z] != null)
                        {
                            newRoom.cells.Add(grid[x, z]);
                            grid[x, z].visited = true; // Marcar como visitado para evitar que el DFS lo modifique

                            // Remover pared al norte
                            if (z + 1 < randomZ + roomSize){
                                grid[x, z].RemoveWall("North");
                                MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x, z + 1));
                            }// Remover pared al sur
                            if (z - 1 >= randomZ){
                                grid[x, z].RemoveWall("South");
                                MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x, z - 1));
                            }// Remover pared al este
                            if (x + 1 < randomX + roomSize){
                                grid[x, z].RemoveWall("East");
                                MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x + 1, z));
                            }// Remover pared al oeste
                            if (x - 1 >= randomX){
                                grid[x, z].RemoveWall("West");
                                MazeController.CellsNeighbors(new Vector2Int(x, z), new Vector2Int(x - 1, z));
                            }
                        }
                    }
                }

                int countDoors = Random.Range(1, 4);
                while (countDoors > 0)
                {
                    int doorX = Random.Range(randomX, randomX + roomSize);
                    int doorZ = Random.Range(randomZ, randomZ + roomSize);
                    MazeCell cell = grid[doorX, doorZ];
                    if (cell != null)
                    {
                        List<Vector2Int> possibleDirections = new List<Vector2Int>();
                        if (doorZ + 1 < height && !usedCells.Contains(new Vector2Int(doorX, doorZ + 1)))
                            possibleDirections.Add(new Vector2Int(doorX, doorZ + 1));
                        if (doorZ - 1 >= 0 && !usedCells.Contains(new Vector2Int(doorX, doorZ - 1)))
                            possibleDirections.Add(new Vector2Int(doorX, doorZ - 1));
                        if (doorX + 1 < width && !usedCells.Contains(new Vector2Int(doorX + 1, doorZ)))
                            possibleDirections.Add(new Vector2Int(doorX + 1, doorZ));
                        if (doorX - 1 >= 0 && !usedCells.Contains(new Vector2Int(doorX - 1, doorZ)))
                            possibleDirections.Add(new Vector2Int(doorX - 1, doorZ));

                        if (possibleDirections.Count > 0)
                        {
                            Vector2Int direction = possibleDirections[Random.Range(0, possibleDirections.Count)];
                            RemoveWallBetween(new Vector2Int(doorX, doorZ), direction);
                            countDoors--;
                        }
                    }
                }

                rooms.Add(newRoom);

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

    private void InstantiateDoor(int keyCount)
    {
        if (doorPrefab == null || MazeController.Grid == null)
            return;

        if (spawnUtils == null)
            spawnUtils = GetComponent<SpawnUtils>();

        // Buscar un borde cuyo slot esté libre antes de colocar la puerta.
        MazeCell borderCell = GetRandomFreeBorderCell(out string borderDirection, out Vector3 reservedSlotPosition);
        if (borderCell == null)
            return;

        if (spawnUtils != null)
        {
            if (!spawnUtils.TryReserveSpawnPosition(reservedSlotPosition))
                return;
        }

        borderCell.SetDoor(doorPrefab, borderDirection, keyCount); // Abrir el muro del borde para colocar la puerta
    }

    private MazeCell GetRandomFreeBorderCell(out string borderDirection, out Vector3 reservedSlotPosition)
    {
        borderDirection = "North"; // Por defecto
        reservedSlotPosition = Vector3.zero;
        
        int width = MazeController.Width;
        int height = MazeController.Height;

        int maxAttempts = Mathf.Max(1, width * height * 4);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            MazeCell borderCell = GetRandomBorderCellCandidate(out borderDirection);
            if (borderCell == null)
                continue;

            GameObject borderWall = GetWallObject(borderCell, borderDirection);
            if (borderWall == null || !borderWall.activeSelf)
                continue;

            reservedSlotPosition = GetBorderWallSpawnPosition(borderCell, borderDirection);
            if (spawnUtils != null && spawnUtils.IsSpawnPositionOccupied(reservedSlotPosition))
                continue;

            return borderCell;
        }

        return null;
    }

    private MazeCell GetRandomBorderCellCandidate(out string borderDirection)
    {
        borderDirection = "North";

        int width = MazeController.Width;
        int height = MazeController.Height;

        // Elegir aleatoriamente cuál borde (0: Norte, 1: Sur, 2: Este, 3: Oeste)
        int randomBorder = Random.Range(0, 4);
        int x, z;

        switch (randomBorder)
        {
            case 0: // Borde Norte (z = 0)
                x = Random.Range(0, width);
                z = 0;
                borderDirection = "South";
                break;
            case 1: // Borde Sur (z = height - 1)
                x = Random.Range(0, width);
                z = height - 1;
                borderDirection = "North";
                break;
            case 2: // Borde Este (x = 0)
                x = 0;
                z = Random.Range(0, height);
                borderDirection = "West";
                break;
            default: // Borde Oeste (x = width - 1)
                x = width - 1;
                z = Random.Range(0, height);
                borderDirection = "East";
                break;
        }

        return MazeController.Grid[x, z];
    }

    private GameObject GetWallObject(MazeCell cell, string direction)
    {
        if (cell == null)
            return null;

        switch (direction)
        {
            case "North": return cell.wallNorth;
            case "South": return cell.wallSouth;
            case "East": return cell.wallEast;
            case "West": return cell.wallWest;
            default: return null;
        }
    }

    private Vector3 GetBorderWallSpawnPosition(MazeCell cell, string borderDirection)
    {
        Vector3 wallDirection = Vector3.zero;

        switch (borderDirection)
        {
            case "North": wallDirection = Vector3.forward; break;
            case "South": wallDirection = Vector3.back; break;
            case "East": wallDirection = Vector3.right; break;
            case "West": wallDirection = Vector3.left; break;
        }

        float offset = Mathf.Max(0.1f, (cellSize * 0.5f) - (spawnUtils != null ? spawnUtils.wallInset : 0.6f));
        Vector3 position = cell.transform.position + wallDirection.normalized * offset;
        position.y = cell.transform.position.y;
        return position;
    }
}
