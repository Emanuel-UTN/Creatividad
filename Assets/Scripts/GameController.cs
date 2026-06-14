using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MazeGenerator))]
public class GameController : MonoBehaviour
{
    public static event System.Action<Vector3, float> OnNoiseEmitted;

    public static GameController gameController;
    public static bool IsPaused { get; private set; }
    public static bool IsCreativoEnabled { get; private set; }
    [Header("GameObjects")]
    public GameObject player;
    public GameObject[] enemies;
    public GameObject enemy;

    [Header("Actions")]
    public bool resetGame = false;

    private MazeGenerator mazeGenerator;
    private AudioManager audioManager;

    void Awake()
    {
        if (gameController == null)
        {
            gameController = this;
            SetPaused(false);
            SetCreativoEnabled(false);

            // Limit framerate to reduce CPU usage, especially in WebGL
            #if UNITY_WEBGL
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            #else
            Application.targetFrameRate = 60; // Keep it capped at 60 to prevent unnecessary CPU/GPU usage
            #endif
        }
        else if (gameController != this)
        {
            Destroy(gameObject);
            return;
        }

        audioManager = GetComponentInChildren<AudioManager>();
        if (GetComponent<PauseMenuController>() == null)
            gameObject.AddComponent<PauseMenuController>();
    }

    public void Initialize() {
        mazeGenerator = GetComponent<MazeGenerator>();
        if (player != null)
        {
            player = Instantiate(player, MazeController.Grid[0,0].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            audioManager.SetPlayer(player.transform);
        }
        enemy = Instantiate(enemies[Random.Range(0, enemies.Length)], MazeController.Grid[mazeGenerator.width - 1, mazeGenerator.height - 1].transform.position, Quaternion.identity);
        audioManager.SetEnemy(enemy.transform);
    }

    void Update()
    {
        if (resetGame)
        {
            resetGame = false;
            ResetRuntimeState();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reinicia la escena actual
        }
    }

    public void PlayerWin(){
        Debug.Log("¡Has ganado!");
        audioManager.PlayerWon();
        ResetRuntimeState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reinicia la escena actual
    }

    public void OpenDoor(float timeToOpen) {
        StartClock(timeToOpen);
        
        if (enemy == null || player == null)
            return;

        AlertEnemy(player.transform.position);
    }

    public void AlertEnemy(Vector3 position, float alertRadius = 0f)
    {
        if (enemy == null || player == null)
            return;

        float distance = (alertRadius == 0f) ? Vector3.Distance(enemy.transform.position, position) : alertRadius;
        OnNoiseEmitted?.Invoke(position, distance);
    }

    public void StartClock(float time)
    {
        Clock.Instance?.StartClock(time);
    }

    public void SetChaseMusic(bool isChasing)
    {
        if (audioManager == null)
            return;
        
        if (isChasing)
            audioManager.StartChase();
        else
            audioManager.StopChase();
    }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
    }

    public static void SetCreativoEnabled(bool enabled)
    {
        IsCreativoEnabled = enabled;
    }

    public static void ResetRuntimeState()
    {
        SetPaused(false);
        SetCreativoEnabled(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
