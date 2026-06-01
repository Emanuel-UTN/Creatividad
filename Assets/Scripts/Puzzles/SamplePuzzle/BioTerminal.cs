using UnityEngine;
using TMPro;
using System.Collections;

public class BioTerminal : PuzzleObject
{
    private SampleType requiredSample;
    private SamplePuzzle puzzle;

    public TextMeshPro terminalText;

    private bool isCompleted = false;

    public void Initialize(SampleType requiredSample, SamplePuzzle puzzle)
    {
        this.requiredSample = requiredSample;
        this.puzzle = puzzle;

        terminalText.text = $"Muestra requerida: {requiredSample}";
    }

    public override void Interact()
    {
        if (isCompleted || PlayerController.playerController.CurrentSampleType == null)
            return;
        
        Debug.Log($"Interacting with terminal. Player sample: {PlayerController.playerController.CurrentSampleType}, Required sample: {requiredSample}");
        
        if (PlayerController.playerController.CurrentSampleType == requiredSample)
            SuccessfullInteraction();
        else
            FailedInteraction();

        PlayerController.playerController.CurrentSampleType = (SampleType?)null;
    }

    private void SuccessfullInteraction()
    {
        terminalText.text = "Capsula desbloqueada.";
        terminalText.color = Color.turquoise;

        isCompleted = true;

        puzzle.CompletePuzzle();
    }

    private void FailedInteraction()
    {
        terminalText.text = "Muestra incorrecta. Llamando a la seguridad.";
        terminalText.color = Color.red;

        StartCoroutine(TextFlicker());
    }

    private IEnumerator TextFlicker(){
        float elapsedTime = 0f;
        
        isCompleted = true; // Evita que se pueda interactuar mientras el texto parpadea
        while (elapsedTime < 5f){
            terminalText.color = terminalText.color == Color.red ? Color.white : Color.red;
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;

            GameController.gameController.AlertEnemy(PlayerController.playerController.transform.position);
        }

        terminalText.color = Color.white;
        isCompleted = false; // Permite volver a interactuar después de que el texto haya dejado de parpadear
        terminalText.text = $"Muestra requerida: {requiredSample}";
    }
}