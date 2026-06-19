using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "RandomMaze";
    private AsyncOperation loadingOperation;

    [Header("Menu de Opciones")]
    public GameObject optionsMenu;

    [Header("Volumen")]
    public TextMeshProUGUI volumeValueText;
    public Slider volumeSlider;
    private float globalVolume = 0.75f;

    [Header("Sensibilidad")]
    public TextMeshProUGUI sensitivityValueText;
    public Slider sensitivitySlider;
    private float mouseSensitivity = 0.1f;

    void Awake()
    {
        globalVolume = PlayerPrefs.GetFloat("GlobalVolume", 0.75f);
        volumeSlider.value = globalVolume;

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 0.1f);
        sensitivitySlider.value = mouseSensitivity;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        loadingOperation = SceneManager.LoadSceneAsync(gameSceneName);
        loadingOperation.allowSceneActivation = false;

        ApplyVolume(globalVolume);
        ApplySensitivity();
        optionsMenu.SetActive(false);
    }

    public void StartGame()
    {
        loadingOperation.allowSceneActivation = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleOptionsMenu()
    {
        optionsMenu.SetActive(!optionsMenu.activeSelf);
    }

    private void ApplyVolume(float volume)
    {
        globalVolume = volume;
        AudioListener.volume = globalVolume;
        volumeValueText.text = $"{Mathf.RoundToInt(globalVolume * 100)}%";
        PlayerPrefs.SetFloat("GlobalVolume", globalVolume);
    }

    public void OnVolumeChanged()
    {
        ApplyVolume(volumeSlider.value);
    }

    private void ApplySensitivity()
    {
        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        if (movement != null)
            movement.lookSensitivity = mouseSensitivity;
        
        sensitivityValueText.text = $"{Mathf.RoundToInt(mouseSensitivity * 100)}%";
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
    }

    public void OnSensitivityChanged()
    {
        mouseSensitivity = sensitivitySlider.value;
        ApplySensitivity();
    }
}
