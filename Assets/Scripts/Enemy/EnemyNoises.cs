using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyNoises : MonoBehaviour
{
    private AudioSource audioSource;

    public AudioClip[] noisesClips;

    public float noiseIntervalMin = 5f;
    public float noiseIntervalMax = 15f;
    private float noiseTimer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 1.0f; // Force 3D spatial sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 25f;
        }
        ResetNoiseTimer();
    }

    private void ResetNoiseTimer()
    {
        noiseTimer = Random.Range(noiseIntervalMin, noiseIntervalMax);
    }

    void Update()
    {
        if (GameController.IsPaused)
            return;

        noiseTimer -= Time.deltaTime;
        if (noiseTimer <= 0)
        {
            PlayRandomNoise();
            ResetNoiseTimer();
        }
    }

    private void PlayRandomNoise()
    {
        if (noisesClips.Length == 0)
            return;

        int randomIndex = Random.Range(0, noisesClips.Length);
        audioSource.PlayOneShot(noisesClips[randomIndex]);
    }
}