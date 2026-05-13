using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthEffects : MonoBehaviour
{
    [Header("Vida")] [Range(0f, 1f)]
    public float healthPercent = 1f;

    [Header("Volume")]
    public Volume volume;

    private Vignette vignette;
    private ChromaticAberration chromatic;
    private LensDistortion distortion;
    private ColorAdjustments colorAdjustments;


    void Start()
    {
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out distortion);
        volume.profile.TryGet(out colorAdjustments);
    }

    void Update()
    {
        float intensity = 1f - healthPercent;

        // Vignette
        vignette.intensity.value = Mathf.Lerp(0f, 0.45f, intensity);

        // Aberración Cromática
        chromatic.intensity.value = Mathf.Lerp(0f, 1f, intensity);

        // Distorsión
        distortion.intensity.value = Mathf.Lerp(0f, -0.35f, intensity);

        // Saturación
        colorAdjustments.saturation.value = Mathf.Lerp(0f, -60f, intensity);
    }

    public void SetHealth(float current, float max)
    {
        healthPercent = Mathf.Clamp01(current / max);
    }
}
