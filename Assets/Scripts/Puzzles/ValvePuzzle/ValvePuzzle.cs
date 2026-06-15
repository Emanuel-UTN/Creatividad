using UnityEngine;
using System.Collections.Generic;

public class ValvePuzzle : PuzzleBase
{
    public GameObject valvePrefab;
    public GameObject vaultPrefab;
    public List<GameObject> cluePrefabs = new List<GameObject>(3);
    public GameObject labLightPrefab;
    
    [Header("Tuberías")]
    public GameObject pipeSegmentPrefab;
    public GameObject pipeCornerPrefab;
    public GameObject pipeJunctionPrefab;
    public float pipeCeilingHeight = 3.1f;
    public float pipeWallInset = 0.35f;
    private Color[] clueColors = new Color[] { Color.red, Color.green, Color.blue };

    [Header("Configuración")]
    public int valveCount = 3;

    private List<Valve> correctOrder = new List<Valve>();
    private int currentStep = 0;

    public override void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        room.roomType = RoomType.ValvePuzzle;
        base.Initialize(room, spawnUtils);
    }

    public override void StartPuzzle()
    {
        correctOrder.Clear();
        currentStep = 0;

        SpawnValves();
        SpawnVault();
        SpawnPipeNetwork();
        SpawnLights(labLightPrefab, Quaternion.Euler(90, 0, 0), 3);

        MazeCell cell = MazeController.GetCellByPosition(room.GetCenter());
        Vector2Int position = MazeController.GetCellCoordinates(cell);
        List<Vector2Int> neighbors = new List<Vector2Int>(cell.neighbors);

        foreach (Vector2Int neighbor in neighbors)
            MazeController.CellsNeighbors(position, neighbor, false);
    }

    void SpawnVault()
    {
        vaultPrefab = Instantiate(vaultPrefab, room.GetCenter(), Quaternion.identity, transform);
        SpawnClues();
    }

    void SpawnClues()
    {
        Shuffle(cluePrefabs);

        for (int i = 0; i < cluePrefabs.Count; i++)
        {
            GameObject clue = Instantiate(cluePrefabs[i], vaultPrefab.transform.position, Quaternion.identity, vaultPrefab.transform.GetChild(2));
            clue.transform.localPosition = new Vector3(.25f - i * 0.25f, 0.5f, 0.5f); // Distribuir las pistas dentro de la bóveda

            
            clue = spawnUtils.SpawnObjects(cluePrefabs[i], 1, room.cells)[0];
            clue.transform.SetParent(transform);
            clue.transform.position += Vector3.up * Random.Range(0.5f, 2.25f) // Elevar la pista para que sea visible al jugador
                + clue.transform.right * Random.Range(-1.25f, 1.25f); // Agregar un poco de variación horizontal para evitar que las pistas se superpongan exactamente
            
            int index = correctOrder[i].Id; // Obtener el índice de la válvula correcta para esta pista
            clue.GetComponent<SpriteRenderer>().color = clueColors[index]; // Asignar color a la pista según el orden correcto
        }
    }

    void SpawnValves()
    {
        spawnUtils.SpawnObjects(valvePrefab, valveCount).ForEach(obj =>
        {
            obj.transform.SetParent(transform);

            Valve valve = obj.GetComponentInChildren<Valve>();
            valve.Initialize(this, correctOrder.Count);
            correctOrder.Add(valve);
        });

        Shuffle(correctOrder);
    }

    void SpawnPipeNetwork()
    {
        ValvePipeNetwork.Build(
            transform,
            room,
            vaultPrefab,
            correctOrder,
            pipeSegmentPrefab,
            pipeCornerPrefab,
            pipeJunctionPrefab,
            pipeCeilingHeight,
            pipeWallInset);
    }

    public void ActivateValve(int valveIndex)
    {
        Valve expected = correctOrder[currentStep];

        if (expected.Id == valveIndex)
        {
            vaultPrefab.transform.GetChild(3 + currentStep).gameObject.SetActive(false);
            currentStep++;
            if (currentStep >= correctOrder.Count)
                CompletePuzzle();
            
        }
        else
            FailPuzzle();
    }

    void CompletePuzzle()
    {
        Debug.Log("¡Puzzle de válvulas completado!");

        // Aquí puedes agregar efectos, recompensas, etc.

        vaultPrefab.transform.GetChild(2).gameObject.SetActive(false);
        vaultPrefab.GetComponent<Collider>().enabled = false;

        SpawnKey();
    }

    void SpawnKey()
    {
        base.SpawnKey(room.GetCenter() + Vector3.up * .5f);
    }

    void FailPuzzle()
    {
        Debug.Log("¡Orden incorrecto! Reiniciando puzzle.");
        currentStep = 0;

        for(int i = 0; i < correctOrder.Count; i++)
        {
            vaultPrefab.transform.GetChild(3 + i).gameObject.SetActive(true);
            correctOrder[i].ResetValve();
        }

        GameController.gameController.AlertEnemy(PlayerController.playerController.transform.position);

        // Vapor
        // Sonido
        // Alarma
    }
}
