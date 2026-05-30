using UnityEngine;
using System.Collections.Generic;

public class SamplePuzzle : PuzzleBase
{
    public GameObject samplePrefab;
    private List<BioSample> samples = new List<BioSample>();
    public GameObject capsulePrefab;
    public GameObject terminalPrefab;

    [Header("Materiales")]
    public Material unlockedMaterial;

    [Header("Configuración")]
    public int numberOfSamples = 4;

    public override void StartPuzzle()
    {
        SpawnSamples();
        SpawnCapsule();
    }

    void SpawnSamples()
    {
        spawnUtils.SpawnObjects(samplePrefab, numberOfSamples).ForEach(obj =>
        {
            obj.transform.SetParent(transform);

            BioSample sample = obj.GetComponent<BioSample>();
            SampleType type = (SampleType)samples.Count;
            sample.Initialize(type, this);

            samples.Add(sample);
        });
    }

    void SpawnCapsule()
    {
        capsulePrefab = SpawnCenter(capsulePrefab);
        capsulePrefab.transform.position += Vector3.up * 1.5f;

        terminalPrefab = spawnUtils.SpawnObjects(terminalPrefab, 1, room.cells)[0];
        terminalPrefab.transform.SetParent(transform);
        terminalPrefab.GetComponentInChildren<BioTerminal>().Initialize((SampleType)Random.Range(0, 4), this);
    }

    public void CompletePuzzle()
    {
        capsulePrefab.transform.GetChild(1).GetComponent<MeshRenderer>().material = unlockedMaterial;
        capsulePrefab.GetComponent<Collider>().enabled = false;
        capsulePrefab.GetComponentInChildren<Light>().color = Color.turquoise;
        
        SpawnKey();
    }

    void SpawnKey()
    {
        base.SpawnKey(room.GetCenter() + Vector3.forward * 2f);
    }
}