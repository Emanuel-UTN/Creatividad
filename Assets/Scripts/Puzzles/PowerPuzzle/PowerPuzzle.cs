using System.Collections.Generic;
using UnityEngine;

public class PowerPuzzle : PuzzleBase
{
    [Header("Prefabs")]
    public GameObject generatorPrefab;
    public GameObject switchPrefab;
    public GameObject keyPrefab;

    [Header("Configuración")]
    public int switchCount = 3;
    
    private List<GameObject> spawnedSwitches = new List<GameObject>();
    private int activatedSwitches = 0;

    public override void StartPuzzle()
    {
        SpawnGenerator();
        SpawnSwitches();
    }

    void SpawnGenerator()
    {
        Vector3 center = GetRoomCenter();
        MazeCell cell = MazeController.GetCellByPosition(center);
        if (cell == null)
        {
            Instantiate(generatorPrefab, center, Quaternion.identity, transform);
            return;
        }

        Vector2Int position = MazeController.GetCellCoordinates(cell);
        List<Vector2Int> neighbors = new List<Vector2Int>(cell.neighbors);

        foreach (Vector2Int neighbor in neighbors)
            MazeController.CellsNeighbors(position, neighbor, false);

        Instantiate(generatorPrefab, center, Quaternion.identity, transform);
    }

    void SpawnSwitches()
    {
        if (spawnUtils == null || switchPrefab == null)
            return;
        
        spawnUtils.SpawnObjects(switchPrefab, switchCount, true).ForEach(s => {
            spawnedSwitches.Add(s);
            s.transform.SetParent(transform);
            s.GetComponent<PowerSwitch>().Initialize(this);
        });
    }

    public void ActivateSwitch()
    {
        activatedSwitches++;

        Debug.Log($"Interruptores activados: {activatedSwitches}/{switchCount}");

        if(activatedSwitches >= switchCount)
            CompletePuzzle();
    }

    void CompletePuzzle()
    {
        Debug.Log("¡Puzzle de energía completado!");
        SpawnKey();
    }

    void SpawnKey()
    {
        Vector3 center = GetRoomCenter();

        Instantiate(keyPrefab, center + Vector3.up, Quaternion.identity, transform);
    }

    Vector3 GetRoomCenter()
    {
        Vector3 total = Vector3.zero;

        foreach(MazeCell cell in room.cells)
        {
            total += cell.transform.position;
        }

        return total / room.cells.Count;
    }
}