using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "RandomMaze";

    [Header("Text")]
    [SerializeField] private string title = "Proyecto Quimera";
    [SerializeField] private string subtitle = "Tu mascota fue secuestrada por una corporacion turbia. Entra, encontrala y sali con vida.";
    [SerializeField] private string objectiveText = "Busca la puerta de salida, reunite con las llaves y esquiva al Cuidador.";
    [SerializeField] private string hintText = "Vas a aprender todo jugando: la puerta te dice cuantas llaves necesita cuando la encuentres.";

    private bool showHowToPlay;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        GUI.depth = -1000;

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.color = previousColor;

        float panelWidth = Mathf.Clamp(Screen.width * 0.34f, 360f, 560f);
        float panelHeight = Mathf.Clamp(Screen.height * 0.72f, 420f, 720f);
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, string.Empty);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 18,
            wordWrap = true,
            normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
        };

        GUIStyle smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 14,
            wordWrap = true,
            normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        float contentX = panelRect.x + 24f;
        float contentWidth = panelRect.width - 48f;

        GUI.Label(new Rect(contentX, panelRect.y + 20f, contentWidth, 44f), title, titleStyle);
        GUI.Label(new Rect(contentX, panelRect.y + 72f, contentWidth, 72f), subtitle, bodyStyle);

        float buttonX = panelRect.x + 24f;
        float buttonWidth = panelRect.width - 48f;
        float buttonHeight = 48f;
        float buttonY = panelRect.y + 160f;

        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Empezar", buttonStyle))
            StartGame();

        buttonY += 58f;
        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), showHowToPlay ? "Ocultar como se juega" : "Como se juega", buttonStyle))
            showHowToPlay = !showHowToPlay;

        buttonY += 58f;
        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Salir", buttonStyle))
            QuitGame();

        float infoTop = buttonY + 76f;
        GUI.Label(new Rect(contentX, infoTop, contentWidth, 54f), objectiveText, smallStyle);

        if (showHowToPlay)
        {
            GUI.Box(new Rect(contentX, infoTop + 70f, contentWidth, 140f), string.Empty);
            GUI.Label(new Rect(contentX + 14f, infoTop + 84f, contentWidth - 28f, 112f), hintText + "\n\nEsc: pausa | Click: empezar | En la partida: exploracion, sigilo y linterna.", smallStyle);
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
