using UnityEngine;

public class PowerSwitch : PuzzleObject
{
    PowerPuzzle puzzle;

    private bool activated = false;

    public AudioClip activationSound;

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
        if (activationSound != null)
            AudioSource.PlayClipAtPoint(activationSound, transform.position);

        // Luces
        // Animación
        GetComponent<Animation>()?.Play("SwitchActivate");
    }
}