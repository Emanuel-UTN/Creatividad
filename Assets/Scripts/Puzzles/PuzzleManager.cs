using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    [Header("Prefabs de Puzzles")]
    public GameObject[] puzzlePrefabs;

    [Header("Configuración")]
    public int puzzleMaxCount = 5;

    private List<MazeRoom> availableRooms = new List<MazeRoom>();
    private List<PuzzleBase> activePuzzles = new List<PuzzleBase>();

    public int GeneratePuzzles(List<MazeRoom> rooms, SpawnUtils spawnUtils)
    {
        availableRooms = rooms;

        if (availableRooms.Count == 0)
        {
            Debug.LogWarning("No hay habitaciones para colocar puzzles.");
            return 0;
        }

        int puzzleCount = Random.Range(1, Mathf.Min(puzzleMaxCount, availableRooms.Count) + 1);

        for(int i = 0; i < puzzleCount; i++)
        {
            if (availableRooms.Count == 0)
                break;

            CreateRandomPuzzle(spawnUtils);
        }

        return puzzleCount;
    }

    void CreateRandomPuzzle(SpawnUtils spawnUtils)
    {
        int randomIndex = Random.Range(0, availableRooms.Count);
        MazeRoom selectedRoom = availableRooms[randomIndex];
        availableRooms.RemoveAt(randomIndex);

        if (spawnUtils == null)
            spawnUtils = GetComponent<SpawnUtils>();

        selectedRoom.roomType = RoomType.PowerPuzzle;
        PuzzleBase puzzle = Instantiate(puzzlePrefabs[0], selectedRoom.transform).GetComponent<PuzzleBase>();
        puzzle.Initialize(selectedRoom, spawnUtils);
        puzzle.StartPuzzle();
        activePuzzles.Add(puzzle);
    }
}