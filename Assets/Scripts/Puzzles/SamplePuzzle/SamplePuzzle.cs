using UnityEngine;
using System.Collections.Generic;

public class SamplePuzzle : PuzzleBase
{
    public GameObject samplePrefab;
    private List<BioSample> samples = new List<BioSample>();
    public GameObject capsulePrefab;
    public GameObject terminalPrefab;
    public GameObject lightAlarmPrefab;

    [Header("Materiales")]
    public Material unlockedMaterial;
    public Material lightAlarmMaterial;
    private Color lightAlarmColor = Color.red;

    [Header("Configuración")]
    public int numberOfSamples = 4;
    public int numberOfLightAlarms = 3;

    public override void Initialize(MazeRoom room, SpawnUtils spawnUtils)
    {
        room.roomType = RoomType.SamplePuzzle;
        base.Initialize(room, spawnUtils);
    }

    public override void StartPuzzle()
    {
        SpawnSamples();
        SpawnCapsule();
        SpawnLights(lightAlarmPrefab, Quaternion.identity, numberOfLightAlarms);
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

    private float flickerIntensityRange;
    private Material originalOnMaterial;
    private Color originalLightColor;

    public void TriggerAlarm(float duration)
    {
        flickerIntensityRange = lights[0].flickerIntensityRange;
        originalOnMaterial = lights[0].onMaterial;
        originalLightColor = lights[0].GetComponent<Light>().color;

        foreach (LightController light in lights)
        {
            light.onMaterial = lightAlarmMaterial;
            light.lightMesh.material = lightAlarmMaterial;
            light.GetComponent<Light>().color = lightAlarmColor;
            light.flickerIntensityRange = 5f;
        }

        Invoke(nameof(StopAlarm), duration);
    }

    void StopAlarm()
    {
        foreach (LightController light in lights)
        {
            light.onMaterial = originalOnMaterial;
            light.lightMesh.material = originalOnMaterial;
            light.GetComponent<Light>().color = originalLightColor;
            light.flickerIntensityRange = flickerIntensityRange;
        }
    }
}