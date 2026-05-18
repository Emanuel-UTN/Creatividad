using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionReference;

    [TextArea]
    public string message = "realizar esta acción";

    [Header("Show Tutorial")]
    public bool showOnStart = false;
    public bool hideOnInput = true;

    [Header("Next Tutorial")]
    public TutorialTrigger nextTutorial;

    private bool completed = false;

    public void Start()
    {
        if (showOnStart)
            StartCoroutine(ShowTutorialDelayed());
    }

    IEnumerator ShowTutorialDelayed()
    {
        // Wait one frame to ensure Input System and other singletons are initialized
        yield return null;
        ShowTutorial();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !completed)
            ShowTutorial();
    }

    public void ShowTutorial()
    {
        if (completed)
            return;
        // Ensure referenced action is enabled so its performed callback fires
        try
        {
            actionReference?.action?.Enable();
        }
        catch { }

        TutorialUI.Instance.ShowTutorial(message, actionReference, hideOnInput, nextTutorial);
        completed = true;
    }
}