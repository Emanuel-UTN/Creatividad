using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthEffects : MonoBehaviour
{
    [Header("Vida")] [Range(0f, 1f)]
    public float healthPercent = 1f;

    [Header("Volume")]
    public Volume volume;

    [Header("Vignette")]
    public bool enableVignette = true;
    public float vignetteIntensity = 0.45f;

    [Header("Heartbeat")]
    public bool enablePulse = true;
    public float pulseSpeed = 4f;
    public float pulseStrength = .08f;

    [Header("Chromatic Aberration")]
    public bool enableChromatic = true;
    public float chromaticIntensity = 1f;

    [Header("Lens Distortion")]
    public bool enableDistortion = true;
    public float distortionIntensity = -0.35f;

    [Header("Color Adjustments")]
    public bool enableColorAdjustments = true;
    public float colorAdjustmentSaturation = -60f;
    public float colorAdjustmentPostExposure = -1.5f;

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
    private float lastAppliedHealthPercent = -1f;


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
        bool healthChanged = !Mathf.Approximately(healthPercent, lastAppliedHealthPercent);

        // Vignette
        if (enableVignette)
        {
            if (healthPercent < 0.5f && enablePulse)
            {
                // Pulse is active, update every frame
                float vignetteBase = Mathf.Lerp(0f, vignetteIntensity, intensity);
                vignetteBase += Mathf.Sin(Time.time * pulseSpeed) * pulseStrength * intensity;
                vignette.intensity.value = vignetteBase;
            }
            else if (healthChanged)
            {
                // No pulse, only update when health changes
                vignette.intensity.value = Mathf.Lerp(0f, vignetteIntensity, intensity);
            }
        }

        // Aberración Cromática - only update when health changes
        if (enableChromatic && healthChanged)
        {
            chromatic.intensity.value = Mathf.Lerp(0f, chromaticIntensity, intensity);
        }

        // Distorsión
        if (enableDistortion)
        {
            if (healthPercent < 0.5f)
            {
                // Distortion pulse is active, update every frame
                float distortionBase = Mathf.Lerp(0f, distortionIntensity, intensity);
                distortionBase += Mathf.Sin(Time.time * 3f) * .05f;
                distortion.intensity.value = distortionBase;
            }
            else if (healthChanged)
            {
                // Only update when health changes
                distortion.intensity.value = Mathf.Lerp(0f, distortionIntensity, intensity);
            }
        }

        // Ajuste de Color - only update when health changes
        if (enableColorAdjustments && healthChanged)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(0f, colorAdjustmentSaturation, intensity);
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, colorAdjustmentPostExposure, intensity);
        }

        // Audio - only update when health changes
        if (heartbeatAudio && healthChanged)
        {
            heartbeatAudio.volume = Mathf.Lerp(0f, 1f, intensity);
            heartbeatAudio.pitch = Mathf.Lerp(1f, 1.3f, intensity);
        }

        // Respiración / Camara
        if (camTransform != null)
        {
            if (enableBreathing && healthPercent < 0.5f)
            {
                float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount * intensity;
                camTransform.localPosition = originalCamPos + Vector3.up * breath;
            }
            else if (healthChanged || lastAppliedHealthPercent < 0.5f)
            {
                camTransform.localPosition = originalCamPos;
            }
        }

        lastAppliedHealthPercent = healthPercent;
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
