using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(MazeGenerator))]
public class GameController : MonoBehaviour
{
    public static event System.Action<Vector3, float> OnNoiseEmitted;

    public static GameController gameController;
    public static bool IsPaused { get; private set; }
    public static bool IsCreativoEnabled { get; private set; }
    public static bool IsDead { get; private set; }

    [SerializeField]
    private PauseMenuController pauseMenu;
    [SerializeField]
    private StartUI startUI;

    [Header("GameObjects")]
    public GameObject player;
    public GameObject[] enemies;
    public GameObject enemy;
    public GameObject cartaPrefab;

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
    }

    public void Initialize() {
        mazeGenerator = GetComponent<MazeGenerator>();
        if (player != null)
        {
            player = Instantiate(player, MazeController.Grid[0,0].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            audioManager.SetPlayer(player.transform);
        }

        if (cartaPrefab != null)
        {
            Instantiate(cartaPrefab, MazeController.Grid[0,0].transform.position + Vector3.up * 0.5f + Vector3.forward * 1.5f, Quaternion.identity);
        }

        enemy = Instantiate(enemies[Random.Range(0, enemies.Length)], MazeController.Grid[mazeGenerator.width - 1, mazeGenerator.height - 1].transform.position, Quaternion.identity);
        audioManager.SetEnemy(enemy.transform);

        startUI.Initialize();
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
        IsDead = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PlayersDie()
    {
        if (IsDead) return;
        IsDead = true;
        
        VideoClip deathClip = null;
        if (enemy != null)
        {
            var enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
                deathClip = enemyController.screamerClip;
            Destroy(enemy);
        }
        
        SetPaused(true);
        
        float clipLength = 2f;
        try {
            if (deathClip != null)
                clipLength = (float)deathClip.length;
        } catch {}

        if (deathClip != null && PlayerUI.playerUI != null)
        {
            PlayerUI.playerUI.PlayScreamer(deathClip);
            if (audioManager != null)
                audioManager.PlayersDeath(clipLength);
        }
        else if (PlayerUI.playerUI != null)
        {
            PlayerUI.playerUI.ShowDeathMenu();
        }
    }

    public void TogglePause()
    {
        if (pauseMenu == null)
            pauseMenu = GetComponent<PauseMenuController>() ?? FindAnyObjectByType<PauseMenuController>();

        if (pauseMenu != null)
            pauseMenu.TogglePause();
        else
            Debug.LogWarning("No PauseMenuController assigned or found.");
    }
}
