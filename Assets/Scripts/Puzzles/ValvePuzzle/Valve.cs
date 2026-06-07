using UnityEngine;
using System.Collections;

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

    [Header("Efectos de Vapor")]
    public ParticleSystem steamParticleSystem;
    public float interactionSteamEmissionRate = 25f;
    public float failSteamDuration = 2.5f;
    public float failSteamEmissionRate = 120f;
    public AudioClip failAudioClip;

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

        if (steamParticleSystem == null)
            steamParticleSystem = GetComponentInChildren<ParticleSystem>();

        if (steamParticleSystem == null)
            CreateDefaultSteamParticles();
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
        {
            UpdateSteamParticles(false);
            return;
        }

        bool locked = Time.time < lockedUntilTime;
        bool interacting = Time.time - lastInteractionTime <= 0.2f;

        if (!locked && !interacting)
            currentInteractionTime -= Time.deltaTime;

        currentInteractionTime = Mathf.Clamp(currentInteractionTime, 0f, requiredInteractionTime);

        UpdateVisuals(locked);
        UpdateTurningSound(Mathf.Abs(currentInteractionTime - previousInteractionTime) > 0.0001f);
        UpdateSteamParticles(interacting && !locked);
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
        TriggerFailSteam();
    }

    void UpdateSteamParticles(bool isInteracting)
    {
        if (steamParticleSystem == null)
            return;

        var emission = steamParticleSystem.emission;
        if (isInteracting)
        {
            if (!steamParticleSystem.isPlaying)
                steamParticleSystem.Play();
            emission.rateOverTime = interactionSteamEmissionRate;
        }
        else if (Time.time >= lockedUntilTime)
        {
            emission.rateOverTime = 0f;
            if (steamParticleSystem.isPlaying)
                steamParticleSystem.Stop();
        }
    }

    private void CreateDefaultSteamParticles()
    {
        GameObject psObj = new GameObject("SteamParticles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = Vector3.up * 0.4f;
        psObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 1.0f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        main.gravityModifier = -0.05f;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0.4f, 0.1f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(1f, 2.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        steamParticleSystem = ps;
    }

    private void TriggerFailSteam()
    {
        if (steamParticleSystem != null)
        {
            var emission = steamParticleSystem.emission;
            emission.rateOverTime = failSteamEmissionRate;
            if (!steamParticleSystem.isPlaying)
                steamParticleSystem.Play();

            StartCoroutine(StopFailSteamAfterDelay(failSteamDuration));
        }

        if (audioSource != null && failAudioClip != null)
        {
            audioSource.PlayOneShot(failAudioClip);
        }
    }

    private IEnumerator StopFailSteamAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (steamParticleSystem != null && !activated)
        {
            var emission = steamParticleSystem.emission;
            emission.rateOverTime = 0f;
            steamParticleSystem.Stop();
        }
    }
}