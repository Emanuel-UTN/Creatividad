using UnityEngine;

public class MazeSpawnAnchor : MonoBehaviour
{
    public MazeCell SourceCell { get; private set; }
    public Vector3 WallDirection { get; private set; }

    public void Initialize(MazeCell sourceCell, Vector3 wallDirection)
    {
        SourceCell = sourceCell;
        WallDirection = wallDirection;
    }
}