using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MazeGenerator))]
public class GameController : MonoBehaviour
{
    public static GameController gameController;
    public static bool IsPaused { get; private set; }
    public static bool IsGodModeEnabled { get; private set; }
    public GameObject player;
    public GameObject[] enemies;
    public GameObject enemy;

    [Header("Escape")]
    public GameObject doorPrefab;
    public GameObject doorKeyPrefab;

    private MazeGenerator mazeGenerator;
    private AudioManager audioManager;

    void Awake()
    {
        if (gameController == null)
        {
            gameController = this;
            SetPaused(false);
            SetGodModeEnabled(false);
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
        player = Instantiate(player, MazeController.Grid[0,0].transform.position, Quaternion.identity);
        enemy = Instantiate(enemies[Random.Range(0, enemies.Length)], MazeController.Grid[mazeGenerator.width - 1, mazeGenerator.height - 1].transform.position, Quaternion.identity);
        InstantiateDoorAndKey();

        audioManager.SetPlayer(player.transform);
        audioManager.SetEnemy(enemy.transform);
    }

    private void InstantiateDoorAndKey()
    {
        if (doorPrefab == null || doorKeyPrefab == null || MazeController.Grid == null || mazeGenerator == null)
            return;

        // Seleccionar una celda aleatoria del borde del laberinto
        MazeCell borderCell = GetRandomBorderCell(out string borderDirection);
        if (borderCell == null)
            return;

        // Generar número aleatorio de candados (1-5)
        int lockCount = Random.Range(1, 6);

        borderCell.SetWall(doorPrefab, borderDirection, lockCount); // Abrir el muro del borde para colocar la puerta

        // Instanciar las llaves distribuidas aleatoriamente en el mapa
        InstantiateKeysRandomly(lockCount);
    }

    private MazeCell GetRandomBorderCell(out string borderDirection)
    {
        borderDirection = "North"; // Por defecto
        
        int width = MazeController.Width;
        int height = MazeController.Height;

        // Elegir aleatoriamente cuál borde (0: Norte, 1: Sur, 2: Este, 3: Oeste)
        int randomBorder = Random.Range(0, 4);
        int x, z;

        switch (randomBorder)
        {
            case 0: // Borde Norte (z = 0)
                x = Random.Range(0, width);
                z = 0;
                borderDirection = "South";
                break;
            case 1: // Borde Sur (z = height - 1)
                x = Random.Range(0, width);
                z = height - 1;
                borderDirection = "North";
                break;
            case 2: // Borde Este (x = 0)
                x = 0;
                z = Random.Range(0, height);
                borderDirection = "West";
                break;
            default: // Borde Oeste (x = width - 1)
                x = width - 1;
                z = Random.Range(0, height);
                borderDirection = "East";
                break;
        }

        return MazeController.Grid[x, z];
    }

    public void PlayerWin(){
        Debug.Log("¡Has ganado!");
        audioManager.PlayerWon();
        ResetRuntimeState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reinicia la escena actual
    }

    private void InstantiateKeysRandomly(int keyCount)
    {
        int width = MazeController.Width;
        int height = MazeController.Height;

        for (int i = 0; i < keyCount; i++)
        {
            // Seleccionar una celda aleatoria del mapa
            int randomX = Random.Range(0, width);
            int randomZ = Random.Range(0, height);
            MazeCell randomCell = MazeController.Grid[randomX, randomZ];

            if (randomCell != null)
            {
                // Instanciar la llave en la posición de la celda
                Vector3 keyPosition = randomCell.transform.position + Vector3.up * 0.5f;
                GameObject key = Instantiate(doorKeyPrefab, keyPosition, Quaternion.identity);
                key.transform.rotation = Quaternion.Euler(90, 0, 0); // Asegura que la llave esté orientada correctamente
            }
        }
    }

    public void OpenDoor(float timeToOpen) {
        StartClock(timeToOpen);
        
        if (enemy == null || player == null)
            return;

        float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
        player.GetComponent<PlayerNoises>()?.AlertEnemiesInRange(distance, false);
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

    public static void SetGodModeEnabled(bool enabled)
    {
        IsGodModeEnabled = enabled;
    }

    public static void ResetRuntimeState()
    {
        SetPaused(false);
        SetGodModeEnabled(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
