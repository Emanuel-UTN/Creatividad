using UnityEngine;

public class PowerSwitch : PuzzleObject
{
    PowerPuzzle puzzle;

    private bool activated = false;

    public void Initialize(PowerPuzzle owner)
    {
        puzzle = owner;

        transform.position += Vector3.up * 1.25f + transform.forward * 0.05f;
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