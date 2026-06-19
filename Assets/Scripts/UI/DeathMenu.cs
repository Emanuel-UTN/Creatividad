using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenu : MonoBehaviour
{
    public static DeathMenu Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        // Fix missing fonts automatically to prevent "Can't Generate Mesh" error
        foreach (var text in GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            if (text.font == null)
                text.font = TMPro.TMP_Settings.defaultFontAsset;
        }

        // Only deactivate if we haven't already died before Start runs
        if (!GameController.IsDead)
            gameObject.SetActive(false);
    }

    public void ShowMenu()
    {
        gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        GameController.gameController.resetGame = true;
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("Menu Scene");
    }
}   