using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    private Light targetLight;
    private float baseIntensity;

    [Header("Configuración de Parpadeo")]
    public float minFlickerIntensity = 0.1f;
    public float maxFlickerIntensity = 1f;
    public float flickerChance = 0.05f;
    public float flickerSpeed = 0.08f;

    [Header("Desactivación por Proximidad")]
    public bool enableProximityTurnOff = true;
    public float proximityRange = 1.6f;
    public float turnOffDuration = 3.5f;

    private bool isProximityOff = false;
    private float nextProximityCheckTime = 0f;
    private float turnOnTime = 0f;

    void Start()
    {
        targetLight = GetComponent<Light>();
        if (targetLight != null)
            baseIntensity = targetLight.intensity;
    }

    void Update()
    {
        if (targetLight == null)
            return;

        if (isProximityOff)
        {
            if (Time.time >= turnOnTime)
            {
                isProximityOff = false;
                targetLight.enabled = true;
                targetLight.intensity = baseIntensity;
            }
            return;
        }

        // Check proximity directly under the light
        if (enableProximityTurnOff && Time.time >= nextProximityCheckTime)
        {
            nextProximityCheckTime = Time.time + 0.15f; // Check 6 times per second
            if (PlayerController.playerController != null)
            {
                Vector3 playerPos = PlayerController.playerController.transform.position;
                Vector3 lightPos = transform.position;

                // 2D horizontal distance
                float dx = playerPos.x - lightPos.x;
                float dz = playerPos.z - lightPos.z;
                float dist2DSqr = dx * dx + dz * dz;

                if (dist2DSqr < proximityRange * proximityRange && playerPos.y < lightPos.y)
                {
                    TriggerProximityOff();
                    return;
                }
            }
        }

        // Random flickering behavior
        if (Random.value < flickerChance)
        {
            StartCoroutine(FlickerRoutine());
        }
    }

    private void TriggerProximityOff()
    {
        isProximityOff = true;
        targetLight.enabled = false;
        turnOnTime = Time.time + Random.Range(turnOffDuration * 0.7f, turnOffDuration * 1.3f);
        
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.Play();
        }
    }

    private IEnumerator FlickerRoutine()
    {
        int flickers = Random.Range(2, 6);
        for (int i = 0; i < flickers; i++)
        {
            if (isProximityOff)
                yield break;

            targetLight.intensity = baseIntensity * Random.Range(minFlickerIntensity, maxFlickerIntensity);
            yield return new WaitForSeconds(Random.Range(0.02f, flickerSpeed));
        }

        if (!isProximityOff)
            targetLight.intensity = baseIntensity;
    }
}
