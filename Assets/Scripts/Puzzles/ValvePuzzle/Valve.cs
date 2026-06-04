using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Valve : PuzzleObject
{
    private ValvePuzzle puzzle;

    private int valveIndex;
    public int Id => valveIndex;

    [Header("Referencias")]
    public MeshRenderer colorRenderer;
    public Material[] colors;
    private AudioSource audioSource;
    public AudioClip turningAudioClip;
    public AudioClip activationAudioClip;

    [Header("Configuración")]
    public float requiredInteractionTime = 2.5f;
    public float lockDuration = 5f;
    public float blockedJiggleDuration = 0.2f;
    public float blockedJiggleAngle = 18f;
    private float currentInteractionTime = 0f;
    private float lastInteractionTime = 0f;
    private float lockedUntilTime = -1f;
    private float blockedJiggleTime = 0f;
    private bool activated = false;
    private float previousInteractionTime = 0f;
    private bool turningAudioPlaying = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.loop = true;

        previousInteractionTime = currentInteractionTime;
    }

    public void Initialize(ValvePuzzle puzzle, int index)
    {
        this.puzzle = puzzle;
        valveIndex = index;

        if (colorRenderer != null && index < colors.Length)
            colorRenderer.material = colors[index];
    }

    void Update()
    {
        if (activated)
            return;

        bool locked = Time.time < lockedUntilTime;
        bool interacting = Time.time - lastInteractionTime <= 0.2f;

        if (!locked && !interacting)
            currentInteractionTime -= Time.deltaTime;

        currentInteractionTime = Mathf.Clamp(currentInteractionTime, 0f, requiredInteractionTime);

        UpdateVisuals(locked);
        UpdateTurningSound(Mathf.Abs(currentInteractionTime - previousInteractionTime) > 0.0001f);
        previousInteractionTime = currentInteractionTime;
    }

    void UpdateVisuals(bool locked)
    {
        float progress = currentInteractionTime / requiredInteractionTime;

        float rotation = progress * 360f * 2.75f;

        if (locked && blockedJiggleTime > 0f)
        {
            float jiggleProgress = 1f - (blockedJiggleTime / blockedJiggleDuration);
            float jiggleOffset = Mathf.Sin(jiggleProgress * Mathf.PI) * blockedJiggleAngle;
            rotation = jiggleOffset;

            blockedJiggleTime -= Time.deltaTime;
            if (blockedJiggleTime < 0f)
                blockedJiggleTime = 0f;
        }

        transform.localRotation = Quaternion.Euler(0f, rotation % 360f, 0f);
    }

    void UpdateTurningSound(bool isTurning)
    {
        if (audioSource == null || turningAudioClip == null)
            return;

        if (isTurning && !turningAudioPlaying)
        {
            if (audioSource.clip != turningAudioClip)
                audioSource.clip = turningAudioClip;

            audioSource.loop = true;
            audioSource.Play();
            turningAudioPlaying = true;
        }
        else if (!isTurning && turningAudioPlaying)
        {
            audioSource.Stop();
            turningAudioPlaying = false;
        }
    }

    void StopTurningSound()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    void PlayActivationSound()
    {
        if (audioSource == null || activationAudioClip == null)
            return;

        audioSource.PlayOneShot(activationAudioClip);
    }

    public override void Interact()
    {
        if (activated) return;

        if (Time.time < lockedUntilTime)
        {
            blockedJiggleTime = blockedJiggleDuration;
            return;
        }

        currentInteractionTime += Time.deltaTime;
        lastInteractionTime = Time.time;

        if (currentInteractionTime >= requiredInteractionTime)
            Activate();
    }

    void Activate()
    {
        if (puzzle == null) return;
        activated = true;
        StopTurningSound();
        turningAudioPlaying = false;
        PlayActivationSound();
        puzzle.ActivateValve(valveIndex);
    }

    public void ResetValve()
    {
        activated = false;
        currentInteractionTime = 0f;
        lastInteractionTime = 0f;
        lockedUntilTime = Time.time + lockDuration;
        blockedJiggleTime = 0f;
        previousInteractionTime = 0f;
        StopTurningSound();
        turningAudioPlaying = false;
        transform.localRotation = Quaternion.identity;
    }
}