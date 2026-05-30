using UnityEngine;
using System.Collections.Generic;

public abstract class PuzzleBase : MonoBehaviour
{
    protected MazeRoom room;
    protected SpawnUtils spawnUtils;

    [Header("Prefabs")]
    public GameObject keyPrefab;

    public virtual void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        this.room = room;
        this.spawnUtils = spawnUtils;
    }

    public abstract void StartPuzzle();

    protected GameObject SpawnCenter(GameObject prefab)
    {
        if (prefab == null)
            return null;

        Vector3 center = room.GetCenter();
        MazeCell cell = MazeController.GetCellByPosition(center);
        if (cell == null)
        {
            return Instantiate(prefab, center, Quaternion.identity, transform);
        }

        Vector2Int position = MazeController.GetCellCoordinates(cell);
        List<Vector2Int> neighbors = new List<Vector2Int>(cell.neighbors);

        foreach (Vector2Int neighbor in neighbors)
            MazeController.CellsNeighbors(position, neighbor, false);

        return Instantiate(prefab, center, Quaternion.identity, transform);
    }

    protected virtual void SpawnKey(Vector3 position)
    {
        if (keyPrefab == null)
            return;

        GameObject key = Instantiate(keyPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        key.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    protected void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}