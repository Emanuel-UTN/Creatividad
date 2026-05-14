using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthEffects : MonoBehaviour
{
    [Header("Vida")] [Range(0f, 1f)]
    public float healthPercent = 1f;

    [Header("Volume")]
    public Volume volume;

    [Header("Heartbeat")]
    public bool enablePulse = true;
    public float pulseSpeed = 4f;
    public float pulseStrength = .08f;

    [Header("Audio")]
    public AudioSource heartbeatAudio;

    [Header("Breathing")]
    public bool enableBreathing = true;
    public float breathingAmount = .03f;
    public float breathingSpeed = 1.5f;
    private Transform camTransform;
    private Vector3 originalCamPos;

    [Header("Damage Feedback")]
    public float damageBlurDuration = .35f;
    public float damageBlurIntensity = 1f;

    private Vignette vignette;
    private ChromaticAberration chromatic;
    private LensDistortion distortion;
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;


    void Start()
    {
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out distortion);
        volume.profile.TryGet(out colorAdjustments);
        volume.profile.TryGet(out depthOfField);   
    }

    public void SetCamera(Transform cam)
    {
        camTransform = cam;
        if (camTransform != null)
            originalCamPos = camTransform.localPosition;
    }

    void Update()
    {
        float intensity = 1f - healthPercent;

        // Vignette
        float vignetteBase = Mathf.Lerp(0f, 0.45f, intensity);

        if(enablePulse && healthPercent < 0.5f)
            vignetteBase += Mathf.Sin(Time.time * pulseSpeed) * pulseStrength * intensity;

        vignette.intensity.value = vignetteBase;

        // Aberración Cromática
        chromatic.intensity.value = Mathf.Lerp(0f, 1f, intensity);

        // Distorsión
        float distortionBase = Mathf.Lerp(0f, -0.35f, intensity);

        if (healthPercent < 0.5f)
            distortionBase += Mathf.Sin(Time.time * 3f) * .05f;
        
        distortion.intensity.value = distortionBase;

        // Saturación
        colorAdjustments.saturation.value = Mathf.Lerp(0f, -60f, intensity);

        // Post Exposure
        colorAdjustments.postExposure.value = Mathf.Lerp(0f, -1.5f, intensity);

        // Audio
        if (heartbeatAudio)
        {
            heartbeatAudio.volume = Mathf.Lerp(0f, 1f, intensity);
            heartbeatAudio.pitch = Mathf.Lerp(1f, 1.3f, intensity);
        }

        // Respiración
        if (camTransform == null)
            return;
        
        if (enableBreathing && healthPercent < 0.5f)
        {
            float breath = 
                Mathf.Sin(Time.time * breathingSpeed)
                * breathingAmount
                * intensity;

            camTransform.localPosition = originalCamPos + Vector3.up * breath;
        } else {
            camTransform.localPosition = originalCamPos;
        }
    }

    public void SetHealth(float current, float max)
    {
        float newHealthPercent = Mathf.Clamp01(current / max);
        if (newHealthPercent < healthPercent)
            DamageFeedback();
        healthPercent = newHealthPercent;
    }

    public void DamageFeedback()
    {
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        damageCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    private Coroutine damageCoroutine;
    private System.Collections.IEnumerator DamageEffectCoroutine()
    {
        float timer = 0f;

        while(timer < damageBlurDuration)
        {
            timer += Time.deltaTime;
            float t = timer / damageBlurDuration;

            // Blur
            depthOfField.gaussianMaxRadius.value = Mathf.Lerp(damageBlurIntensity, 0f, t);

            // Chromatic Spike
            chromatic.intensity.value += Mathf.Lerp(.4f, 0f, t);

            yield return null;
        }

        depthOfField.gaussianMaxRadius.value = 0f;
    }
}
