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

    private EnemyBehaviour enemyBehaviour;

    private void UpdateEnemyReference()
    {
        if (enemyBehaviour != null)
            return;

        if (GameController.gameController == null || GameController.gameController.enemy == null)
            return;

        enemyBehaviour = GameController.gameController.enemy.GetComponent<EnemyBehaviour>();
    }

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
        float healthIntensity = 1f - healthPercent;
        float panicFactor = 0f;

        UpdateEnemyReference();
        if (enemyBehaviour != null && PlayerController.playerController != null)
        {
            float sqrDist = (enemyBehaviour.transform.position - PlayerController.playerController.transform.position).sqrMagnitude;
            float maxPanicDistance = 15f; // start panic at 15 meters
            float sqrMaxPanicDistance = maxPanicDistance * maxPanicDistance;
            if (sqrDist < sqrMaxPanicDistance)
            {
                float dist = Mathf.Sqrt(sqrDist);
                panicFactor = 1f - (dist / maxPanicDistance); // 0 to 1
                panicFactor = Mathf.Clamp01(panicFactor);
            }
        }

        float fearIntensity = Mathf.Max(healthIntensity, panicFactor);

        // Vignette
        if (enableVignette && vignette != null)
        {
            if (fearIntensity > 0f)
            {
                float vignetteBase = Mathf.Lerp(0f, vignetteIntensity, fearIntensity);
                if (enablePulse)
                {
                    vignetteBase += Mathf.Sin(Time.time * pulseSpeed) * pulseStrength * fearIntensity;
                }
                vignette.intensity.value = Mathf.Max(0f, vignetteBase);
            }
            else
            {
                vignette.intensity.value = 0f;
            }
        }

        // Aberración Cromática
        if (enableChromatic && chromatic != null)
        {
            chromatic.intensity.value = Mathf.Lerp(0f, chromaticIntensity, fearIntensity);
        }

        // Distorsión
        if (enableDistortion && distortion != null)
        {
            if (fearIntensity > 0f)
            {
                float distortionBase = Mathf.Lerp(0f, distortionIntensity, fearIntensity);
                distortionBase += Mathf.Sin(Time.time * 3f) * .05f * fearIntensity;
                distortion.intensity.value = distortionBase;
            }
            else
            {
                distortion.intensity.value = 0f;
            }
        }

        // Ajuste de Color
        if (enableColorAdjustments && colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(0f, colorAdjustmentSaturation, fearIntensity);
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, colorAdjustmentPostExposure, fearIntensity);
        }

        // Audio (Heartbeat)
        if (heartbeatAudio)
        {
            if (fearIntensity > 0.01f)
            {
                if (!heartbeatAudio.isPlaying)
                {
                    heartbeatAudio.loop = true;
                    heartbeatAudio.Play();
                }
                heartbeatAudio.volume = Mathf.Lerp(0f, 1f, fearIntensity);
                heartbeatAudio.pitch = Mathf.Lerp(1f, 1.4f, fearIntensity);
            }
            else
            {
                if (heartbeatAudio.isPlaying)
                    heartbeatAudio.Stop();
            }
        }

        // Respiración / Camara
        if (camTransform != null)
        {
            if (enableBreathing && fearIntensity > 0f)
            {
                float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount * fearIntensity;
                camTransform.localPosition = originalCamPos + Vector3.up * breath;
            }
            else
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
