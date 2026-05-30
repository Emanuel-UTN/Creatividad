using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PowerPuzzle : PuzzleBase
{
    public GameObject generatorPrefab;
    public GameObject switchPrefab;

    [Header("Sounds Effects")]
    public AudioClip puzzleCompleteSound;
    private AudioSource audioSource;

    [Header("Configuración")]
    public int switchCount = 3;
    
    private List<GameObject> spawnedSwitches = new List<GameObject>();
    private int activatedSwitches = 0;

    public override void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        room.roomType = RoomType.PowerPuzzle;
        base.Initialize(room, spawnUtils);
    }

    public override void StartPuzzle()
    {
        audioSource = GetComponent<AudioSource>();
        SpawnGenerator();
        SpawnSwitches();
    }

    void SpawnGenerator()
    {
        generatorPrefab = SpawnCenter(generatorPrefab);
    }

    void SpawnSwitches()
    {
        if (spawnUtils == null || switchPrefab == null)
            return;
        
        spawnUtils.SpawnObjects(switchPrefab, switchCount).ForEach(s => {
            spawnedSwitches.Add(s);
            s.transform.SetParent(transform);
            s.GetComponent<PowerSwitch>().Initialize(this);
        });
    }

    public void ActivateSwitch()
    {
        activatedSwitches++;

        Debug.Log($"Interruptores activados: {activatedSwitches}/{switchCount}");

        generatorPrefab.GetComponent<Animator>().SetTrigger("ActivateSwitch");

        if(activatedSwitches >= switchCount)
            CompletePuzzle();
    }

    void CompletePuzzle()
    {
        Debug.Log("¡Puzzle de energía completado!");

        if (puzzleCompleteSound != null)
            audioSource.PlayOneShot(puzzleCompleteSound);

        GameController.gameController.AlertEnemy(room.cells[room.cells.Count/2].transform.position);

        SpawnKey();
    }

    protected void SpawnKey()
    {
        Vector3 center = room.GetCenter();

        base.SpawnKey(center + Vector3.forward * 2f);
    }
}