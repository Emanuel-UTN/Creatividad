using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class WinController : MonoBehaviour
{
    public static WinController Instance;
    AsyncOperation restartOperation;

    public VideoPlayer videoPlayer;
    public GameObject winScreen;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
        
        gameObject.SetActive(false);

        videoPlayer.Prepare();
    }

    public void InitWin()
    {
        restartOperation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        restartOperation.allowSceneActivation = false;

        winScreen.SetActive(false);
        videoPlayer.gameObject.SetActive(true);
        gameObject.SetActive(true);

        videoPlayer.Play();
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoPlayerEnd;
    }

    void OnVideoPlayerEnd(VideoPlayer vp)
    {
        ShowWinMenu();
    }

    void ShowWinMenu()
    {
        videoPlayer.gameObject.SetActive(false);
        winScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        GameController.ResetRuntimeState();
        if (restartOperation != null)
            restartOperation.allowSceneActivation = true;
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("Menu Scene");
    }
}