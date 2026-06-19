using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenu : MonoBehaviour
{
    public void RestartGame()
    {
        GameController.gameController.resetGame = true;
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}   