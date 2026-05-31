using UnityEngine;
using TMPro;

public class CentralTerminal : PuzzleObject
{
    private TerminalPuzzle puzzle;

    public TextMeshPro codeDisplay;

    bool isCompleted = false;
    public bool IsCompleted {set {
        isCompleted = value;
        if (isCompleted)
        {
            codeDisplay.text = "Terminales Activadas";
            codeDisplay.color = Color.green;
        }
    }}

    public void Initialize(TerminalPuzzle puzzle)
    {
        this.puzzle = puzzle;

        if (codeDisplay != null)
            codeDisplay.text = "Buscar terminales a activar";
    }

    public override void Interact()
    {
        if (puzzle == null || isCompleted)
            return;

        codeDisplay.text = "Terminales a activar:\n";
        
        puzzle.RandomizeTerminalsToActivate().ForEach(node =>
        {
            node.IsToActivate = true;
            codeDisplay.text += $"{node.Code}\n";
        });
    }
}