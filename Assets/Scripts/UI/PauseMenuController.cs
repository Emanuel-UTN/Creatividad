using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu Scene";
    public TextMeshProUGUI titleText;

    [Header("Botones Principales")]
    public GameObject mainButtons;

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

    private void Awake()
    {
        globalVolume = PlayerPrefs.GetFloat("GlobalVolume", 0.75f);
        volumeSlider.value = globalVolume;

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 0.1f);
        sensitivitySlider.value = mouseSensitivity;
    }

    private void Start()
    {
        ApplyVolume(globalVolume);
        ApplySensitivity();

        gameObject.SetActive(false);
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

    public void TogglePause()
    {
        if (GameController.IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        GameController.SetPaused(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameObject.SetActive(true);

        titleText.text = "Pausa";
        titleText.color = Color.white;
        mainButtons.SetActive(true);
        optionsMenu.SetActive(false);
    }

    public void ResumeGame()
    {
        GameController.SetPaused(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameObject.SetActive(false);
    }

    private void ToggleCreativo()
    {
        GameController.SetCreativoEnabled(!GameController.IsCreativoEnabled);
    }

    public void ReturnToMainMenu()
    {
        GameController.ResetRuntimeState();
        SceneManager.LoadScene(menuSceneName);
    }

    public void ToggleOptionsMenu()
    {
        mainButtons.SetActive(!mainButtons.activeSelf);
        optionsMenu.SetActive(!optionsMenu.activeSelf);

        titleText.text = optionsMenu.activeSelf ? "Opciones" : "Pausa";
        titleText.color = optionsMenu.activeSelf ? Color.black : Color.white;
    }
}