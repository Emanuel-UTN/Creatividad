using UnityEngine;

public class Carta : PuzzleObject
{
    [TextArea(3, 10)]
    public string mensaje = "Este es un mensaje misterioso...";
    public float tiempoEnPantalla = 5f;

    public override void Interact()
    {
        if (TutorialUI.Instance != null)
        {
            TutorialUI.Instance.ShowPlainMessage(mensaje, tiempoEnPantalla);
        }
        else
        {
            Debug.Log("Carta leída: " + mensaje);
        }
    }
}
