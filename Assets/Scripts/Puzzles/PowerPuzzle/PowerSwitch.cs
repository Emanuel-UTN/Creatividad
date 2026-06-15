using UnityEngine;

public class PowerSwitch : PuzzleObject
{
    PowerPuzzle puzzle;
    int index;

    private bool activated = false;

    public AudioClip activationSound;

    public void Initialize(PowerPuzzle owner, int index)
    {
        puzzle = owner;
        this.index = index;

        transform.position += transform.forward * 0.05f;
        transform.position = new Vector3(transform.position.x, 1.7f, transform.position.z);
    }

    public override void Interact()
    {
        if (activated)
            return;

        activated = true;
        Debug.Log("Power switch activated!");

        puzzle.ActivateSwitch(index);

        // Sonido
        if (activationSound != null)
            AudioSource.PlayClipAtPoint(activationSound, transform.position);

        // Luces
        // Animación
        GetComponent<Animation>()?.Play("SwitchActivate");
    }
}