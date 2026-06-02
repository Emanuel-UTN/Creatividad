using UnityEngine;
using System.Collections.Generic;

public class LightPuzzle : PuzzleBase
{
    public GameObject receiverPrefab;
    public GameObject examplePrefab;
    public GameObject lockerPrefab;

    [Header("Configuración")]
    public int receiverCount = 3;
    private int activatedReceivers = 0;

    private List<LightReceiver> receivers = new List<LightReceiver>();

    public override void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        room.roomType = RoomType.LightPuzzle;
        base.Initialize(room, spawnUtils);
    }

    public override void StartPuzzle()
    {
        SpawnReceivers();
    }

    void SpawnReceivers()
    {
        spawnUtils.SpawnObjects(receiverPrefab, receiverCount, room.cells).ForEach(obj =>
        {
            LightReceiver receiver = obj.GetComponentInChildren<LightReceiver>();
            if (receiver != null)
            {
                obj.transform.parent = transform;
                receiver.Initialize(this);
                receivers.Add(receiver);
            }
        });

        lockerPrefab = spawnUtils.SpawnObjects(lockerPrefab, 1, room.cells)[0];
        lockerPrefab.transform.parent = transform;

        spawnUtils.SpawnObjects(examplePrefab, 1, room.cells)[0].transform.parent = transform; // Ejemplo visual para el jugador
    }

    public void ActivateReceiver(LightReceiver receiver)
    {
        activatedReceivers++;

        if (activatedReceivers >= receiverCount)
            CompletePuzzle();
    }

    void CompletePuzzle()
    {
        Debug.Log("¡Puzzle de luz completado!");

        // Aquí puedes agregar efectos, recompensas, etc.

        lockerPrefab.transform.GetChild(1).gameObject.SetActive(false); // Abrir la puerta del locker
        SpawnKey();
    }

    void SpawnKey()
    {
        Vector3 pos = lockerPrefab.transform.position + lockerPrefab.transform.forward * 1.5f;
        base.SpawnKey(pos);
    }
}