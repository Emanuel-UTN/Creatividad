using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerNoises : MonoBehaviour
{
    private struct NoiseGizmoSample
    {
        public Vector3 position;
        public float range;
        public bool isSprinting;
        public float expiresAt;
    }

    public static event System.Action<Transform, Vector3, float, bool> OnNoiseEmitted;

    [Header("Hearing Ranges")]
    public float walkHearingRange = 6f;
    public float sprintHearingRange = 12f;

    [Header("Footsteps")]
    public float walkStepInterval = 0.55f;
    public float sprintStepInterval = 0.33f;

    [Header("Debug Gizmos")]
    public bool showNoiseGizmos = true;
    public float noiseGizmoDuration = 0.5f;
    public Color walkNoiseColor = new Color(0.3f, 0.8f, 1f, 0.9f);
    public Color sprintNoiseColor = new Color(1f, 0.45f, 0.2f, 0.9f);

    [Header("Audio Clips")]
    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    public AudioClip[] footstepClips;
    public AudioClip sprintFootstepClip;
    public AudioClip cupboardInteractionClip;
    private float footstepTimer;
    private readonly List<NoiseGizmoSample> noiseGizmoSamples = new List<NoiseGizmoSample>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        HandleFootsteps(Time.deltaTime);
    }

    public void PlayFootstep(bool isSprinting)
    {
        if (isSprinting && sprintFootstepClip != null)
        {
            audioSource.PlayOneShot(sprintFootstepClip);
        }
        else if (footstepClips != null && footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    public void EmitMovementNoise(bool isSprinting)
    {
        float hearingRange = isSprinting ? sprintHearingRange : walkHearingRange;
        if (hearingRange <= 0f)
            return;

        RegisterNoiseGizmo(transform.position, hearingRange, isSprinting);
        OnNoiseEmitted?.Invoke(transform, transform.position, hearingRange, isSprinting);
    }

    public void PlayFootstepAndEmit(bool isSprinting)
    {
        PlayFootstep(isSprinting);
        EmitMovementNoise(isSprinting);
    }

    public void PlayCupboardInteraction()
    {
        if (cupboardInteractionClip != null)
            audioSource.PlayOneShot(cupboardInteractionClip);
    }

    private void HandleFootsteps(float dt)
    {
        if (playerMovement == null
            || playerMovement.IsMovementLocked
            || !playerMovement.IsGrounded
            || !playerMovement.IsMoving
            || playerMovement.IsCrouching)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= dt;
        if (footstepTimer > 0f)
            return;

        bool isSprinting = playerMovement.IsSprinting;
        PlayFootstepAndEmit(isSprinting);

        float interval = isSprinting ? sprintStepInterval : walkStepInterval;
        footstepTimer = Mathf.Max(0.05f, interval);
    }

    private void RegisterNoiseGizmo(Vector3 position, float range, bool isSprinting)
    {
        NoiseGizmoSample sample = new NoiseGizmoSample
        {
            position = position,
            range = range,
            isSprinting = isSprinting,
            expiresAt = Time.time + Mathf.Max(0.05f, noiseGizmoDuration)
        };

        noiseGizmoSamples.Add(sample);
    }

    private void OnDrawGizmos()
    {
        if (!showNoiseGizmos || noiseGizmoSamples.Count == 0)
            return;

        float now = Time.time;
        for (int i = noiseGizmoSamples.Count - 1; i >= 0; i--)
        {
            NoiseGizmoSample sample = noiseGizmoSamples[i];
            if (sample.expiresAt <= now)
            {
                noiseGizmoSamples.RemoveAt(i);
                continue;
            }

            Color baseColor = sample.isSprinting ? sprintNoiseColor : walkNoiseColor;
            float t = Mathf.Clamp01((sample.expiresAt - now) / Mathf.Max(0.05f, noiseGizmoDuration));
            Color fadedColor = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0.15f, baseColor.a, t));

            Gizmos.color = fadedColor;
            Gizmos.DrawWireSphere(sample.position, sample.range);
            Gizmos.DrawLine(sample.position, sample.position + Vector3.up * 1.25f);
        }
    }
}