using UnityEngine;

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

    protected virtual void SpawnKey(Vector3 position)
    {
        if (keyPrefab == null)
            return;

        GameObject key = Instantiate(keyPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        key.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}