using System.Collections;
using Unity.VisualScripting;
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

    private MusicState currentState;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private Coroutine fadeCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activeSource = sourceA;
        inactiveSource = sourceB;

        ChangeMusic(MusicState.Ambient, true);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMusicState();
    }

    void UpdateMusicState()
    {
        if (player == null || enemy == null)
            return;

        if (killerIsChasing)
        {
            ChangeMusic(MusicState.Chase);
            return;
        }

        float distance = Vector3.Distance(player.position, enemy.position);

        if (distance <= encounterDistance)
        {
            ChangeMusic(MusicState.Encounter);
        }
        else if (distance <= suspenseDistance)
        {
            ChangeMusic(MusicState.Suspense);
        }
        else
        {
            ChangeMusic(MusicState.Ambient);
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
}
