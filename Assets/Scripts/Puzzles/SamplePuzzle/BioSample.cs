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
        PlayerController player = PlayerController.playerController;
        if (player == null)
            return;

        Debug.Log($"Picked up sample {sampleType}");

        // Si el jugador ya tiene una muestra cargada, dejarla en el piso
        if (player.CurrentSampleType.HasValue)
        {
            SampleType oldSampleType = player.CurrentSampleType.Value;
            if (puzzle != null && puzzle.samplePrefab != null)
            {
                // Instanciar la muestra vieja un poco delante del jugador
                Vector3 dropPosition = player.transform.position + player.transform.forward * 0.8f;
                dropPosition.y = transform.position.y; // Mantener la altura original del suelo de la muestra

                GameObject droppedObj = Instantiate(puzzle.samplePrefab, dropPosition, Quaternion.identity, puzzle.transform);
                BioSample droppedSample = droppedObj.GetComponent<BioSample>();
                if (droppedSample != null)
                {
                    droppedSample.Initialize(oldSampleType, puzzle);
                    // Forzar posición exacta después de inicializar (evita el desplazamiento de Initialize)
                    droppedObj.transform.position = dropPosition;
                }
            }
        }

        // Equipar la nueva muestra y hacer desaparecer esta del suelo
        player.CurrentSampleType = sampleType;
        Destroy(gameObject);
    }
}
