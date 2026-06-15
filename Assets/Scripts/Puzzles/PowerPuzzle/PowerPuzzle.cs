using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PowerPuzzle : PuzzleBase
{
    public GameObject generatorPrefab;
    public GameObject switchPrefab;

    [Header("Sounds Effects")]
    public AudioClip puzzleCompleteSound;
    public AudioClip generatorWorkingSound;
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
        generatorPrefab.transform.position += Vector3.up * .75f;
    }

    void SpawnSwitches()
    {
        if (spawnUtils == null || switchPrefab == null)
            return;
        
        spawnUtils.SpawnObjects(switchPrefab, switchCount).ForEach(s => {
            spawnedSwitches.Add(s);
            s.transform.SetParent(transform);
            s.GetComponent<PowerSwitch>().Initialize(this, spawnedSwitches.Count - 1);
        });
    }

    public void ActivateSwitch(int index)
    {
        activatedSwitches++;

        LabLightingManager.Instance.PowerSector(index);

        Debug.Log($"Interruptores activados: {activatedSwitches}/{switchCount}");

        if(activatedSwitches >= switchCount)
            CompletePuzzle();
    }

    void CompletePuzzle()
    {
        Debug.Log("¡Puzzle de energía completado!");

        if (puzzleCompleteSound != null)
        {
            audioSource.PlayOneShot(puzzleCompleteSound);
            // When the one-shot finishes, start the generator working sound in loop
            if (generatorWorkingSound != null)
                Invoke("PlayGeneratorWorking", puzzleCompleteSound.length);
        }else
            PlayGeneratorWorking();

        GameController.gameController.AlertEnemy(room.cells[room.cells.Count/2].transform.position);

        generatorPrefab.GetComponent<Animation>()?.Play("Working");

        SpawnKey();
    }

    void PlayGeneratorWorking()
    {
        if (generatorWorkingSound == null)
            return;

        audioSource.clip = generatorWorkingSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    protected void SpawnKey()
    {
        Vector3 center = room.GetCenter();

        base.SpawnKey(center + Vector3.forward * 2f);
    }
}