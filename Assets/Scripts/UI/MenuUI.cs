using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "RandomMaze";
    private AsyncOperation loadingOperation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        loadingOperation = SceneManager.LoadSceneAsync(gameSceneName);
        loadingOperation.allowSceneActivation = false;
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
}
