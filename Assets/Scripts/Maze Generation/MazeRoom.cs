using UnityEngine;
using System.Collections.Generic;

public enum RoomType
{
    Empty,
    PowerPuzzle,
    LightPuzzle,
    ValvePuzzle,
    SafeRoom,
    ExitRoom
}

public class MazeRoom : MonoBehaviour
{
    public Vector2Int origin;
    public int size;
    public RoomType roomType = RoomType.Empty;

    public List<MazeCell> cells = new List<MazeCell>();
}