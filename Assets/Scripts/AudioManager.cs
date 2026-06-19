using System.Collections;
using UnityEngine;

public enum MusicState
{
    Ambient,
    Suspense,
    Encounter,
    Chase,
    Escape,
    Victory
}

public class AudioManager : MonoBehaviour
{

    [Header("Referencias")]
    private Transform player;
    private Transform enemy;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;
    public AudioSource backgroundNoiseSource;

    [Header("Musica")]
    public AudioClip ambientClip;
    public AudioClip suspenseClip;
    public AudioClip encounterClip;
    public AudioClip chaseClip;
    public AudioClip escapeClip;
    public AudioClip victoryClip;

    [Header("Distancias")]
    public float suspenseDistance = 30f;
    public float encounterDistance = 15f;

    [Header("Fade")]
    public float fadeSpeed = 1.5f;

    [Header("Estado")]
    public bool killerIsChasing = false;

    [Header("Sonidos de fondo")]
    public AudioClip[] backgroundNoise;
    public float backgroundNoiseIntervalMin = 20f;
    public float backgroundNoiseIntervalMax = 35f;
    private float backgroundNoiseTimer;

    private MusicState currentState;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private Coroutine fadeCoroutine;
    private bool dinamicMusicEnabled = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activeSource = sourceA;
        inactiveSource = sourceB;

        ChangeMusic(MusicState.Ambient, true);

        backgroundNoiseTimer = Random.Range(backgroundNoiseIntervalMin, backgroundNoiseIntervalMax);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMusicState();
    }

    void UpdateMusicState()
    {
        if (GameController.IsPaused || !dinamicMusicEnabled)
            return;

        if (player == null || enemy == null)
            return;

        if (killerIsChasing)
        {
            ChangeMusic(MusicState.Chase);
            return;
        }

        float sqrDistance = (player.position - enemy.position).sqrMagnitude;
        float encounterDistSqr = encounterDistance * encounterDistance;
        float suspenseDistSqr = suspenseDistance * suspenseDistance;

        if (sqrDistance <= encounterDistSqr)
        {
            ChangeMusic(MusicState.Encounter);
        }
        else if (sqrDistance <= suspenseDistSqr)
        {
            ChangeMusic(MusicState.Suspense);
        }
        else
        {
            ChangeMusic(MusicState.Ambient);
        }

        BackgroundNoiseUpdate();
    }

    void BackgroundNoiseUpdate()
    {
        if (backgroundNoise.Length == 0)
                return;
        
        backgroundNoiseTimer -= Time.deltaTime;
        if (backgroundNoiseTimer <= 0f)
        {
            backgroundNoiseTimer = Random.Range(backgroundNoiseIntervalMin, backgroundNoiseIntervalMax);

            int randomIndex = Random.Range(0, backgroundNoise.Length);
            backgroundNoiseSource.PlayOneShot(backgroundNoise[randomIndex]);
        }
    }

    public void ChangeMusic(MusicState newState, bool instant = false)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        AudioClip newClip = GetClipFromState(newState);

        if (newClip == null)
            return;
        
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Crossfade(newClip, instant));
    }

    AudioClip GetClipFromState(MusicState state)
    {
        return state switch
        {
            MusicState.Ambient => ambientClip,
            MusicState.Suspense => suspenseClip,
            MusicState.Encounter => encounterClip,
            MusicState.Chase => chaseClip,
            MusicState.Escape => escapeClip,
            MusicState.Victory => victoryClip,
            _ => null
        };
    }

    IEnumerator Crossfade(AudioClip newClip, bool instant)
    {
        inactiveSource.clip = newClip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float timer = 0f;

        float duration = instant ? .01f : fadeSpeed;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeSpeed;

            activeSource.volume = Mathf.Lerp(1f, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Asegurarse de que los volúmenes estén al final del proceso
        activeSource.volume = 0f;
        inactiveSource.volume = 1f;

        // Detener la fuente inactiva
        activeSource.Stop();

        // Intercambiar las fuentes
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }

    // ---------- EVENTOS PUBLICOS ----------

    public void StartChase()
    {
        killerIsChasing = true;
    }

    public void StopChase()
    {
        killerIsChasing = false;

        ChangeMusic(MusicState.Escape);
    }

    public void PlayerWon()
    {
        ChangeMusic(MusicState.Victory);
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void SetEnemy(Transform enemyTransform)
    {
        enemy = enemyTransform;
    }

    public void PlayersDeath(float delay)
    {
        dinamicMusicEnabled = false;
        activeSource.Stop();

        Invoke(nameof(VictoryMusic), delay);
    }

    private void VictoryMusic()
    {
        ChangeMusic(MusicState.Victory, true);
    }
}
