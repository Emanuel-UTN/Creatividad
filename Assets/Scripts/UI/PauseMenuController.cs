using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu Scene";

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
        float panelHeight = 320f;
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, string.Empty);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
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

        float buttonWidth = panelRect.width - 40f;
        float buttonX = panelRect.x + 20f;
        float buttonY = panelRect.y + 70f;
        float buttonHeight = 44f;

        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Continuar", buttonStyle))
            ResumeGame();

        buttonY += 56f;
        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Volver al menu", buttonStyle))
            ReturnToMainMenu();

        buttonY += 56f;
        string godModeText = GameController.IsGodModeEnabled ? "GodMode: ON" : "GodMode: OFF";
        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), godModeText, buttonStyle))
            ToggleGodMode();

        GUI.Label(new Rect(panelRect.x, panelRect.yMax - 40f, panelRect.width, 24f), "ESC para pausar o reanudar", infoStyle);
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

    private void ToggleGodMode()
    {
        GameController.SetGodModeEnabled(!GameController.IsGodModeEnabled);
    }

    private void ReturnToMainMenu()
    {
        GameController.ResetRuntimeState();
        SceneManager.LoadScene(menuSceneName);
    }
}