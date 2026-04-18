using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Tooltip("The prefab to spawn")]
    public GameObject prefabToSpawn; // The prefab to spawn
    [Tooltip("Options")]
    public float spawnInterval = 2f; // Time interval between spawns
    public float spawnRadius = 5f; // Radius within which to spawn the prefab
    public int maxSpawnCount = 10; // Maximum number of prefabs to spawn
    private int spawnCount = 0;
    public bool destroySpawner = true;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnPrefab), spawnInterval, spawnInterval);
    }

    private void SpawnPrefab()
    {
        if (spawnCount >= maxSpawnCount)
        {
            CancelInvoke(nameof(SpawnPrefab));
            if (destroySpawner)
            {
                Destroy(gameObject);
            }
            return;
        }
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = transform.position.y; // Keep the same height
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        spawnCount++;
    }
}
