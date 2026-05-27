using UnityEngine;

public class PowerSwitch : PuzzleObject
{
    private bool activated = false;

    public override void Initialize(PowerPuzzle owner)
    {
        base.Initialize(owner);

        transform.position += Vector3.up * 1.25f - transform.forward * 0.35f;
    }

    public override void Interact()
    {
        if (activated)
            return;

        activated = true;
        Debug.Log("Power switch activated!");

        puzzle.ActivateSwitch();

        // Sonido
        // Luces
        // Animación
    }
}