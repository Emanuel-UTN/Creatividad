using UnityEngine;
using TMPro;
using System.Collections;

public class TerminalNode : PuzzleObject
{
    private TerminalPuzzle puzzle;
    private string code;
    public string Code => code;

    public TextMeshPro codeDisplay;

    public float activatedDuration = 45f;
    private float activatedTimer = 0f;
    public float ActivatedTimer => activatedTimer;
    public void SetActivatedTimer(float time){ activatedTimer = time; isActivated = true; }
    private bool isActivated = false;
    public bool IsActivated => isActivated;

    private bool isToActivate = false;
    public bool IsToActivate
    {
        get => isToActivate;
        set
        {
            isToActivate = value;
            if (isToActivate)
            {
                codeDisplay.color = Color.yellow;
                codeDisplay.text = $"{code}\n?";
            }
            else
            {
                codeDisplay.color = Color.white;
                codeDisplay.text = code;
            }
        }
    }

    bool isCompleted = false;
    public bool IsCompleted {set {
        isCompleted = value;
        if (isCompleted)
        {
            codeDisplay.text = "OK";
            codeDisplay.color = Color.green;
        }
    }}

    public void Initialize(TerminalPuzzle puzzle, string code)
    {
        this.puzzle = puzzle;
        this.code = code;
        if (codeDisplay != null)
        {
            codeDisplay.text = code;
        }
    }

    void Update()
    {
        if (isCompleted)
            return;

        if (isActivated)
        {
            codeDisplay.text = $"{code}\nOK\n{Mathf.CeilToInt(activatedTimer)}s";
            activatedTimer -= Time.deltaTime;
            if (activatedTimer <= 0f)
                puzzle.Deactivate();
        }
    }

    public override void Interact()
    {
        if (isActivated || puzzle == null || isCompleted)
            return;

        if (isToActivate && puzzle.ActivateTerminal(this))
        {
            codeDisplay.color = Color.green;
            codeDisplay.text = $"{code}\nOK";

            GameController.gameController.AlertEnemy(transform.position, 20f);
        }
        else
        {
            codeDisplay.color = Color.red;
            codeDisplay.text = $"{code}\nX";
            puzzle.Deactivate();
            StartCoroutine(FlickerError());
            GameController.gameController.AlertEnemy(transform.position);
        }
    }

    IEnumerator FlickerError()
    {
        float elapsedTime = 0f;
        isCompleted = true; // Evita que se pueda interactuar mientras el texto parpadea

        while (elapsedTime < 5f)
        {
            codeDisplay.color = codeDisplay.color == Color.red ? Color.white : Color.red;
            elapsedTime += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        isCompleted = false;
        if (isToActivate)
        {
            codeDisplay.color = Color.yellow;
            codeDisplay.text = $"{code}\n?";
        }
        else
        {
            codeDisplay.color = Color.white;
            codeDisplay.text = code;
        }
    }

    public void Deactivate()
    {
        isActivated = false;
        codeDisplay.color = Color.yellow;
        codeDisplay.text = $"{code}\n?";
        activatedTimer = 0f;
    }
}