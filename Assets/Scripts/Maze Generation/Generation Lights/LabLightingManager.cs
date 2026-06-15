using System.Collections.Generic;
using UnityEngine;

public class LabLightingManager : MonoBehaviour
{
    public static LabLightingManager Instance;

    [Header("Prefab")]
    public GameObject lightPrefab;

    [Header("Generación")]
    [Range(2, 10)]
    public int lightInterval = 4;
    [Range(0f, 1f)]
    public float branchChance = .35f;
    
    private List<LightSector> sectors = new List<LightSector>();
    private HashSet<MazeCell> reservedCells = new HashSet<MazeCell>();
    private bool powerPuzzleEnabled;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ReserveRoom(MazeRoom room)
    {
        foreach (MazeCell cell in room.cells)
            reservedCells.Add(cell);
    }

    public void GenerateLighting(bool usePowerPuzzle)
    {
        powerPuzzleEnabled = usePowerPuzzle;
        sectors.Clear();

        for(int i = 0; i < 3; i++)
            sectors.Add(new LightSector(i));
        
        GenerateCorridorLights();

        if(!powerPuzzleEnabled) foreach (var sector in sectors)
            sector.SetPower(true);
    }

    private void GenerateCorridorLights()
    {
        MazeCell[,] grid = MazeController.Grid;

        int width = MazeController.Width;
        int height = MazeController.Height;

        HashSet<MazeCell> visited = new HashSet<MazeCell>();
        Queue<(MazeCell cell, int distance)> queue = new Queue<(MazeCell, int)>();

        MazeCell start = FindStartCell(grid, width, height);

        if (start == null)
        {
            Debug.LogWarning("No se encontró una celda de inicio para la generación de luces.");
            return;
        }

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            MazeCell cell = current.cell;
            int distance = current.distance;

            if (distance > 0 && distance % lightInterval == 0)
                CreateLight(cell);

            foreach (Vector2Int neighborPos in cell.neighbors)
            {
                MazeCell neighbor = grid[neighborPos.x, neighborPos.y];

                if (neighbor == null || reservedCells.Contains(neighbor) || visited.Contains(neighbor))
                    continue;
                
                visited.Add(neighbor);
                queue.Enqueue((neighbor, distance + 1));
            }
        }

    }

    private MazeCell FindStartCell(MazeCell[,] grid, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (grid[x, z] != null && !reservedCells.Contains(grid[x, z]))
                    return grid[x, z];
            
        return null;
    }

    private void CreateLight(MazeCell cell)
    {
        int sectorIndex = GetSectorForCell(cell);

        LightController light = cell.CreateLight(lightPrefab, false);

        if (light == null)
            return;

        sectors[sectorIndex].lights.Add(light);
    }

    private int GetSectorForCell(MazeCell cell)
    {
        return GetSector(cell.transform.position.x);
    }

    private int GetSector(float posX)
    {
        float widthWorld = MazeController.Width * MazeController.CellSize;

        float normalized = Mathf.Clamp01(posX / widthWorld);

        return Mathf.Min(2, Mathf.FloorToInt(normalized * 3));
    }

    public void PowerSector(int sector)
    {
        if (sector < 0 || sector >= sectors.Count)
        {
            Debug.LogWarning("Índice de sector inválido: " + sector);
            return;
        }

        sectors[sector].SetPower(true);
    }
}