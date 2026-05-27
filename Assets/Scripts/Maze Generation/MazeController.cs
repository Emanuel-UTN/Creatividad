using UnityEngine;

public class MazeController : MonoBehaviour
{
    public static MazeController mazeController;
    private static MazeCell[,] grid;
    public static MazeCell[,] Grid { get { return grid; } set { grid = value; } }
    public static MazeCell Cell(int x, int z) { return grid != null && x >= 0 && x < Width && z >= 0 && z < Height ? grid[x, z] : null; }
    public static int Width { get { return grid != null ? grid.GetLength(0) : 0; } }
    public static int Height { get { return grid != null ? grid.GetLength(1) : 0; } }

    private static float cellSize;
    public static float CellSize { get { return cellSize; } set { cellSize = value; } }

    void Awake()
    {
        if (mazeController == null)
        {
            mazeController = this;
        }
        else if (mazeController != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public static MazeCell GetCellByPosition(Vector3 position)
    {
        if (grid == null)
            return null;

        float size = Mathf.Max(0.0001f, cellSize);

        int x = Mathf.RoundToInt(position.x / size);
        int z = Mathf.RoundToInt(position.z / size);

        int maxX = grid.GetLength(0) - 1;
        int maxZ = grid.GetLength(1) - 1;

        x = Mathf.Clamp(x, 0, maxX);
        z = Mathf.Clamp(z, 0, maxZ);

        return grid[x, z];
    }

    public static Vector2Int GetCellCoordinates(MazeCell cell)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                if (grid[x, z] == cell)
                    return new Vector2Int(x, z);
            }
        }
        return Vector2Int.zero; // Retorna (0,0) si no se encuentra la celda
    }

    public static void CellsNeighbors(Vector2Int pos1, Vector2Int pos2, bool areNeighbors = true)
    {
        MazeCell cell1 = grid[pos1.x, pos1.y];
        MazeCell cell2 = grid[pos2.x, pos2.y];

        if (areNeighbors)
        {
            if (!cell1.neighbors.Contains(pos2))
                cell1.neighbors.Add(pos2);
            if (!cell2.neighbors.Contains(pos1))
                cell2.neighbors.Add(pos1);
        }
        else
        {
            cell1.neighbors.Remove(pos2);
            cell2.neighbors.Remove(pos1);
        }
    }
}