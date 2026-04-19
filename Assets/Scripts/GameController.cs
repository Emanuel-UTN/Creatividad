using UnityEngine;

[RequireComponent(typeof(MazeGenerator))]
public class GameController : MonoBehaviour
{
    public static GameController gameController;
    public GameObject player;
    public GameObject enemy;

    private MazeGenerator mazeGenerator;
    private MazeCell[,] grid;

    void Awake()
    {
        if (gameController == null)
        {
            gameController = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (gameController != this)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(MazeCell [,] grid) {
        this.grid = grid;
        mazeGenerator = GetComponent<MazeGenerator>();
        player = Instantiate(player, grid[0,0].transform.position, Quaternion.identity);
        enemy = Instantiate(enemy, grid[mazeGenerator.width - 1, mazeGenerator.height - 1].transform.position, Quaternion.identity);
    }

    public MazeCell[,] Grid()
    {
        return grid;
    }

    public MazeCell Cell(int x, int z)
    {
        return grid[x,z];
    }

    public MazeCell GetCellByPosition(Vector3 position)
    {
        if (grid == null || mazeGenerator == null)
            return null;

        float size = Mathf.Max(0.0001f, mazeGenerator.cellSize);

        int x = Mathf.RoundToInt(position.x / size);
        int z = Mathf.RoundToInt(position.z / size);

        int maxX = grid.GetLength(0) - 1;
        int maxZ = grid.GetLength(1) - 1;

        x = Mathf.Clamp(x, 0, maxX);
        z = Mathf.Clamp(z, 0, maxZ);

        return grid[x, z];
    }
}
