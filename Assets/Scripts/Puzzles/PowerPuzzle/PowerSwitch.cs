using UnityEngine;

public class PowerSwitch : PuzzleObject
{
    PowerPuzzle puzzle;

    private bool activated = false;

    public AudioClip activationSound;

    public void Initialize(PowerPuzzle owner)
    {
        puzzle = owner;

        transform.position += transform.forward * 0.05f;
        transform.position = new Vector3(transform.position.x, 1.7f, transform.position.z);
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