using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class LoadingManager : MonoBehaviour
{
    public string gameScene = "RandomMaze";

    public VideoPlayer videoPlayer;
    public TextMeshProUGUI skipText;
    public InputActionReference skipActionReference;

    private AsyncOperation loadingOperation;

    private bool videoFinished;

    void Awake()
    {
        skipActionReference.action.performed += ctx => SkipVideo();

        var deviceType = (Gamepad.current != null) ? DeviceType.Gamepad : DeviceType.Keyboard;
        string initialSpriteTag = GetSpriteTagForActionForDevice(skipActionReference, deviceType);

        skipText.text = $"Press {initialSpriteTag} to skip";
    }

    IEnumerator Start()
    {
        loadingOperation = SceneManager.LoadSceneAsync(gameScene);

        loadingOperation.allowSceneActivation = false;

        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Play();

        Invoke("FadeSkipText", 3f);

        while (!videoFinished || loadingOperation.progress < 0.9f)
            yield return null;
        

        loadingOperation.allowSceneActivation = true;
    }

    void SkipVideo()
    {
        videoFinished = true;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
    }

    void FadeSkipText()
    {
        skipText.alpha = 1f;
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

    private string GetSpriteTag(string binding)
    {
        string key = binding.ToLower();

        if (SpriteMap.spriteMap.TryGetValue(key, out string spriteName))
        {
            return $"<sprite name=\"{spriteName}\">";
        }

        return $"[{binding}]";
    }
}