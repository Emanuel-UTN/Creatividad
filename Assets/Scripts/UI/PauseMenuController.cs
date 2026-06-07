using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu Scene";

    private float globalVolume = 0.75f;
    private float mouseSensitivity = 0.1f;

    private void Awake()
    {
        globalVolume = PlayerPrefs.GetFloat("GlobalVolume", 0.75f);
        AudioListener.volume = globalVolume;

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 0.1f);
    }

    private void Start()
    {
        ApplySensitivity();
    }

    private void ApplySensitivity()
    {
        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        if (movement != null)
        {
            movement.lookSensitivity = mouseSensitivity;
        }
    }

    public void TogglePause()
    {
        if (GameController.IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void OnGUI()
    {
        if (!GameController.IsPaused)
            return;

        GUI.depth = -1000;

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.color = previousColor;

        float panelWidth = Mathf.Clamp(Screen.width * 0.28f, 320f, 460f);
        float panelHeight = 440f;
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, string.Empty);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = new Color(1f, 1f, 1f, 0.75f) }
        };

        GUI.Label(new Rect(panelRect.x, panelRect.y + 16f, panelRect.width, 40f), "PAUSA", titleStyle);

        float itemWidth = panelRect.width - 40f;
        float itemX = panelRect.x + 20f;
        float itemY = panelRect.y + 70f;
        float itemHeight = 44f;

        // --- SLIDER VOLUMEN ---
        GUI.Label(new Rect(itemX, itemY, itemWidth, 24f), $"Volumen: {Mathf.RoundToInt(globalVolume * 100f)}%", labelStyle);
        itemY += 26f;
        float newVolume = GUI.HorizontalSlider(new Rect(itemX, itemY + 8f, itemWidth, 20f), globalVolume, 0f, 1f);
        if (!Mathf.Approximately(newVolume, globalVolume))
        {
            globalVolume = newVolume;
            AudioListener.volume = globalVolume;
            PlayerPrefs.SetFloat("GlobalVolume", globalVolume);
            PlayerPrefs.Save();
        }
        itemY += 34f;

        // --- SLIDER SENSIBILIDAD ---
        GUI.Label(new Rect(itemX, itemY, itemWidth, 24f), $"Sensibilidad: {mouseSensitivity:F2}", labelStyle);
        itemY += 26f;
        float newSens = GUI.HorizontalSlider(new Rect(itemX, itemY + 8f, itemWidth, 20f), mouseSensitivity, 0.02f, 0.5f);
        if (!Mathf.Approximately(newSens, mouseSensitivity))
        {
            mouseSensitivity = newSens;
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
            PlayerPrefs.Save();
            ApplySensitivity();
        }
        itemY += 44f;

        // --- BOTONES ---
        if (GUI.Button(new Rect(itemX, itemY, itemWidth, itemHeight), "Continuar", buttonStyle))
            ResumeGame();

        itemY += 56f;
        if (GUI.Button(new Rect(itemX, itemY, itemWidth, itemHeight), "Volver al menu", buttonStyle))
            ReturnToMainMenu();

        itemY += 56f;
        string CreativoText = GameController.IsCreativoEnabled ? "Creativo: ON" : "Creativo: OFF";
        if (GUI.Button(new Rect(itemX, itemY, itemWidth, itemHeight), CreativoText, buttonStyle))
            ToggleCreativo();

        GUI.Label(new Rect(panelRect.x, panelRect.yMax - 30f, panelRect.width, 24f), "ESC para pausar o reanudar", infoStyle);
    }

    private void PauseGame()
    {
        GameController.SetPaused(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        GameController.SetPaused(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ToggleCreativo()
    {
        GameController.SetCreativoEnabled(!GameController.IsCreativoEnabled);
    }

    private void ReturnToMainMenu()
    {
        GameController.ResetRuntimeState();
        SceneManager.LoadScene(menuSceneName);
    }
}