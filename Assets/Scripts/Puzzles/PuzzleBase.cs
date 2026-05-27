using UnityEngine;

public abstract class PuzzleBase : MonoBehaviour
{
    protected MazeRoom room;
    protected SpawnUtils spawnUtils;

    public virtual void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        this.room = room;
        this.spawnUtils = spawnUtils;
    }

    public abstract void StartPuzzle();
}