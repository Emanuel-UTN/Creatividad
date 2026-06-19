using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

    [Header("Fade Out")]
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 3f; // Tiempo que el tutorial se muestra antes de empezar a desvanecerse automáticamente

    [Header("UI")]
    public TextMeshProUGUI textUI;
    public CanvasGroup canvasGroup;

    private string[] tutorialsCompleted = new string[0];

    private float _scheduledMessageTimer = 0f;
    private float _scheduledMessageTime = -1f;
    private bool _isMessageScheduled = false;
    private bool _scheduledMessageTriggered = false;
    private bool _triggeredMsgGustar = false;

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;

        // Schedules a subtle gameplay atmospheric reminder
        _scheduledMessageTime = UnityEngine.Random.Range(300f, 600f);
        _isMessageScheduled = UnityEngine.Random.value < 0.5f;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameController.gameController != null && !GameController.IsPaused)
        {
            if (!_scheduledMessageTriggered && _isMessageScheduled)
            {
                _scheduledMessageTimer += Time.deltaTime;
                if (_scheduledMessageTimer >= _scheduledMessageTime)
                {
                    _scheduledMessageTriggered = true;
                    byte[] rawMessage = new byte[] {
                        69, 115, 101, 32, 109, 97, 108, 100, 105, 116, 111, 32,
                        109, 111, 109, 101, 110, 116, 111, 32, 100, 101, 32,
                        109, 105, 114, 97, 114, 32, 112, 97, 114, 97, 32, 117,
                        110, 32, 99, 111, 115, 116, 97, 100, 111
                    };
                    ShowPlainMessage(System.Text.Encoding.UTF8.GetString(rawMessage), 1.5f);
                }
            }

            float elapsed = Time.timeSinceLevelLoad;

            if (!_triggeredMsgGustar && PlayerController.playerController != null && PlayerController.playerController.IsHidden)
            {
                if (PlayerController.playerController.FlashlightBatteryNormalized > 0.001f && PlayerController.playerController.FlashlightBatteryNormalized <= 0.3f)
                {
                    _triggeredMsgGustar = true;
                    byte[] rawGustar = new byte[] {
                        103, 117, 115, 116, 97, 114, 63, 32, 78, 111, 
                        44, 32, 110, 111, 32, 109, 101, 32, 118, 97, 
                        32, 97, 32, 103, 117, 115, 116, 97, 114
                    };
                    ShowPlainMessage(System.Text.Encoding.UTF8.GetString(rawGustar), 1.5f);
                }
            }
        }
    }

    private string currentMessage;
    private InputActionReference currentActionReference;
    private bool currentHideOnInput;
    private System.Collections.Generic.List<InputAction> subscribedActions = new System.Collections.Generic.List<InputAction>();

    private TutorialTrigger currentNextTutorial = null;

    public void ShowTutorial(string message, InputActionReference actionReference, bool hideOnInput = true, TutorialTrigger nextTutorial = null)
    {
        if (Array.Exists(tutorialsCompleted, element => element == message))
            return;

        tutorialsCompleted = AppendToArray(tutorialsCompleted, message);

        // Store for dynamic updates
        currentMessage = message;
        currentActionReference = actionReference;
        currentHideOnInput = hideOnInput;

        // Initial sprite based on current device (prefer Gamepad if connected)
        var deviceType = (UnityEngine.InputSystem.Gamepad.current != null) ? DeviceType.Gamepad : DeviceType.Keyboard;
        string initialSpriteTag = GetSpriteTagForActionForDevice(actionReference, deviceType);
        textUI.text = $"Utiliza {initialSpriteTag} para {message}";

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;

        if (hideOnInput)
            actionReference.action.performed += OnActionPerformed;
        else
            Invoke(nameof(HideTutorial), displayDuration);
        

        // Subscribe to enabled actions to detect device changes and update sprite dynamically
        SubscribeToEnabledActions();

        if (nextTutorial)
            currentNextTutorial = nextTutorial;
        else
            currentNextTutorial = null;
    }

    private string GetSpriteTag(string binding)
    {
        string key = binding.ToLower();

        if (SpriteMap.spriteMap.TryGetValue(key, out string spriteName))
        {
            return $"<sprite name=\"{spriteName}\">";
        }

        return $"[{binding}]";
    }

    private string GetSpriteTagFromControl(InputControl control)
    {
        if (control == null) return "";
        string key = control.name.ToLower();
        if (SpriteMap.spriteMap.TryGetValue(key, out string spriteName))
            return $"<sprite name=\"{spriteName}\">";

        // Fallback: try layout-based keys (gamepad vs keyboard)
        if (control.device is Gamepad)
        {
            key = control.name.ToLower();
            if (SpriteMap.spriteMap.TryGetValue(key, out spriteName))
                return $"<sprite name=\"{spriteName}\">";
        }

        if (control.device is Keyboard)
        {
            key = control.name.ToLower();
            if (SpriteMap.spriteMap.TryGetValue(key, out spriteName))
                return $"<sprite name=\"{spriteName}\">";
        }

        return $"[{control.displayName}]";
    }

    private string GetSpriteTagFromAction(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null) return "";
        try
        {
            var controls = actionReference.action.controls;
            if (controls.Count > 0)
            {
                return GetSpriteTagFromControl(controls[0]);
            }
            else
            {
                string binding = actionReference.action.GetBindingDisplayString();
                return GetSpriteTag(binding);
            }
        }
        catch
        {
            return GetSpriteTag(actionReference.action.GetBindingDisplayString());
        }
    }

    private enum DeviceType { Keyboard, Gamepad }

    private string GetSpriteTagForActionForDevice(InputActionReference actionReference, DeviceType deviceType)
    {
        if (actionReference == null || actionReference.action == null) return "";
        try
        {
            var bindings = actionReference.action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (string.IsNullOrEmpty(b.effectivePath)) continue;
                string path = b.effectivePath.ToLower();
                if (deviceType == DeviceType.Gamepad && path.Contains("<gamepad>"))
                {
                    var parts = path.Split('/');
                    if (parts.Length > 1)
                        return GetSpriteTag(parts[1]);
                }
                else if (deviceType == DeviceType.Keyboard && (path.Contains("<keyboard>") || path.Contains("<mouse>")))
                {
                    var parts = path.Split('/');
                    if (parts.Length > 1)
                        return GetSpriteTag(parts[1]);
                }
            }

            string binding = actionReference.action.GetBindingDisplayString();
            return GetSpriteTag(binding);
        }
        catch
        {
            return GetSpriteTag(actionReference.action.GetBindingDisplayString());
        }
    }

    private void OnAnyActionPerformedUpdateSprite(InputAction.CallbackContext ctx)
    {
        try
        {
            var control = ctx.control;
            if (control != null)
            {
                DeviceType dt = (control.device is UnityEngine.InputSystem.Gamepad) ? DeviceType.Gamepad : DeviceType.Keyboard;
                string spriteTag = GetSpriteTagForActionForDevice(currentActionReference, dt);
                textUI.text = $"Utiliza {spriteTag} para {currentMessage}";
            }
        }
        catch { }
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        HideTutorial();
    }

    private void HideTutorial()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = false;
        StartCoroutine(FadeOut(fadeOutDuration));

        // Unsubscribe from all actions to prevent memory leaks
        foreach (var action in InputSystem.ListEnabledActions())
        {
            action.performed -= OnActionPerformed;
        }

        // Unsubscribe dynamic listeners
        UnsubscribeEnabledActions();
    }

    private void OnDestroy()
    {
        if (currentActionReference != null && currentActionReference.action != null)
            currentActionReference.action.performed -= OnActionPerformed;

        UnsubscribeEnabledActions();
    }

    private void SubscribeToEnabledActions()
    {
        UnsubscribeEnabledActions();
        var list = InputSystem.ListEnabledActions();
        for (int i = 0; i < list.Count; i++)
        {
            var action = list[i];
            if (action != null)
            {
                action.performed += OnAnyActionPerformedUpdateSprite;
                subscribedActions.Add(action);
            }
        }
    }

    private void UnsubscribeEnabledActions()
    {
        for (int i = 0; i < subscribedActions.Count; i++)
        {
            var a = subscribedActions[i];
            if (a != null) a.performed -= OnAnyActionPerformedUpdateSprite;
        }
        subscribedActions.Clear();
    }

    private IEnumerator FadeOut(float duration = 0.5f)
    {
        float start = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, time / duration);
            yield return null;
        }

        textUI.text = string.Empty;
        if (currentNextTutorial != null)
        {
            currentNextTutorial.ShowTutorial();
            currentNextTutorial = null;
        }
    }

    private T[] AppendToArray<T>(T[] array, T item)
    {
        T[] newArray = new T[array.Length + 1];
        Array.Copy(array, newArray, array.Length);
        newArray[newArray.Length - 1] = item;
        return newArray;
    }
}