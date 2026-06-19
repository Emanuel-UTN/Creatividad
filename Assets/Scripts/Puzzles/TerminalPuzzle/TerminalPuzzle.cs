using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TerminalPuzzle : PuzzleBase
{
    public GameObject terminalPrefab;
    public GameObject terminalOutOfServicePrefab;
    public GameObject centralTerminalPrefab;
    public GameObject labLightPrefab;

    private List<TerminalNode> terminalNodes = new List<TerminalNode>();
    private List<TerminalNode> terminalsToActivate;
    private int currentActivatedNode = -1;
    private string[] possibleCodes = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

    [Header("Configuración")]
    public int numberOfTerminals = 6;
    public int numberOfTerminalsToActivate = 3;

    public override void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        room.roomType = RoomType.TerminalPuzzle;
        base.Initialize(room, spawnUtils);
    }

    public override void StartPuzzle()
    {
        SpawnTerminals();
        SpawnCentralTerminal();
        SpawnLights(labLightPrefab, Quaternion.Euler(90, 0, 0), 3);
    }

    void SpawnTerminals()
    {
        spawnUtils.SpawnObjects(terminalPrefab, numberOfTerminals).ForEach(obj =>
        {
            obj.transform.SetParent(transform);
            TerminalNode node = obj.GetComponent<TerminalNode>();
            if (node != null)
            {
                node.Initialize(this, possibleCodes[terminalNodes.Count]);
                terminalNodes.Add(node);
            }
        });

        spawnUtils.SpawnObjects(terminalOutOfServicePrefab, 1, room.cells).ForEach(obj =>
        {
            obj.transform.SetParent(transform);
        });
    }

    void SpawnCentralTerminal()
    {
        centralTerminalPrefab = spawnUtils.SpawnObjects(centralTerminalPrefab, 1, room.cells)[0];
        centralTerminalPrefab.transform.SetParent(transform);

        CentralTerminal centralTerminal = centralTerminalPrefab.GetComponentInChildren<CentralTerminal>();
        if (centralTerminal != null)
            centralTerminal.Initialize(this);
    }

    public List<TerminalNode> RandomizeTerminalsToActivate()
    {   
        if (terminalsToActivate != null && terminalsToActivate.Count > 0)
            terminalsToActivate.ForEach(node => node.IsToActivate = false);

        Shuffle(terminalNodes);
        terminalsToActivate = terminalNodes.GetRange(0, numberOfTerminalsToActivate);
        Shuffle(terminalsToActivate);
        currentActivatedNode = -1;

        return terminalsToActivate;
    }

    public bool ActivateTerminal(TerminalNode node)
    {
        if (terminalsToActivate == null)
            return false;

        if(node == terminalsToActivate[currentActivatedNode + 1])
        {
            if (currentActivatedNode == -1)
                node.SetActivatedTimer(node.activatedDuration);
            else
                node.SetActivatedTimer(terminalsToActivate[currentActivatedNode].ActivatedTimer);
            currentActivatedNode++;
            if (currentActivatedNode >= numberOfTerminalsToActivate - 1 )
                CompletePuzzle();
            
            return true;
        }

        StartCoroutine(AlertEnemy());
        currentActivatedNode = -1;
        return false;
    }

    IEnumerator AlertEnemy(){
        float elapsed = 0f;
        while (elapsed < 15f){
            GameController.gameController.AlertEnemy(PlayerController.playerController.transform.position);
            elapsed += .5f;
            yield return new WaitForSeconds(.5f);
        }
    }

    public void Deactivate()
    {
        currentActivatedNode = -1;
        if (terminalsToActivate != null && terminalsToActivate.Count > 0)
            terminalsToActivate.ForEach(node => node.Deactivate());
    }

    void CompletePuzzle()
    {
        centralTerminalPrefab.GetComponentInChildren<CentralTerminal>().IsCompleted = true;
        terminalNodes.ForEach(node => node.IsCompleted = true);

        SpawnKey();
    }

    void SpawnKey()
    {
        base.SpawnKey(centralTerminalPrefab.transform.position + centralTerminalPrefab.transform.forward * 2f);
    }
}
