using UnityEngine;

public enum SampleType
{
    Alpha,
    Beta,
    Gamma,
    Delta
}

public class BioSample : PuzzleObject
{
    private SampleType sampleType;
    private SamplePuzzle puzzle;

    public Material[] sampleMaterials = new Material[4];

    public void Initialize(SampleType type, SamplePuzzle puzzle)
    {
        sampleType = type;
        this.puzzle = puzzle;

        transform.position += transform.forward * 0.5f;

        transform.GetChild(1).GetComponent<MeshRenderer>().material = sampleMaterials[(int)sampleType];
    }

    public override void Interact()
    {
        Debug.Log($"Picked up sample {sampleType}");
        PlayerController.playerController.CurrentSampleType = sampleType;
    }
}
