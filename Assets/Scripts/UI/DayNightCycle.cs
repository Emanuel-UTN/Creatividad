using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float transitionDurationSeconds = 300f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Skyboxes")]
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material nightSkybox = null;
    [SerializeField, Range(0f, 1f)] private float nightSkyboxStart = 0.65f;
    [SerializeField] private float daySkyboxExposure = 0.95f;
    [SerializeField] private float nightSkyboxExposure = 0.95f;
    [SerializeField] private float skyboxTransitionExposure = 0.25f;

    [Header("Sun")]
    [SerializeField] private Light sun;
    [SerializeField] private Vector3 daySunEuler = new Vector3(50f, -30f, 0f);
    [SerializeField] private Vector3 nightSunEuler = new Vector3(-18f, -30f, 0f);
    [SerializeField] private float daySunIntensity = 1.4f;
    [SerializeField] private float nightSunIntensity = 0.05f;
    [SerializeField] private Color daySunColor = Color.white;
    [SerializeField] private Color sunsetSunColor = new Color(1f, 0.48f, 0.22f);
    [SerializeField] private Color nightSunColor = new Color(0.12f, 0.16f, 0.35f);

    [Header("Ambient")]
    [SerializeField] private float dayAmbientIntensity = 1f;
    [SerializeField] private float nightAmbientIntensity = 0.18f;
    [SerializeField] private Color dayAmbientSky = new Color(0.72f, 0.78f, 0.86f);
    [SerializeField] private Color dayAmbientEquator = new Color(0.46f, 0.48f, 0.48f);
    [SerializeField] private Color dayAmbientGround = new Color(0.22f, 0.20f, 0.16f);
    [SerializeField] private Color nightAmbientSky = new Color(0.05f, 0.06f, 0.12f);
    [SerializeField] private Color nightAmbientEquator = new Color(0.035f, 0.04f, 0.08f);
    [SerializeField] private Color nightAmbientGround = new Color(0.015f, 0.015f, 0.03f);

    [Header("Fog")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private Color dayFogColor = new Color(0.58f, 0.68f, 0.82f);
    [SerializeField] private Color nightFogColor = new Color(0.025f, 0.03f, 0.07f);
    [SerializeField] private float dayFogDensity = 0.012f;
    [SerializeField] private float nightFogDensity = 0.045f;

    private Material daySkyboxInstance;
    private Material nightSkyboxInstance;
    private Material activeSkybox;
    private float elapsedSeconds;

    public float TransitionDurationSeconds
    {
        get => transitionDurationSeconds;
        set => transitionDurationSeconds = Mathf.Max(0.001f, value);
    }
    public float Progress => transitionDurationSeconds > 0f
        ? Mathf.Clamp01(elapsedSeconds / transitionDurationSeconds)
        : 1f;

    private void OnEnable()
    {
        elapsedSeconds = 0f;
        EnsureSun();
        PrepareSkyboxes();
        ApplyProgress(0f);
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsedSeconds = Mathf.Min(elapsedSeconds + deltaTime, Mathf.Max(0.001f, transitionDurationSeconds));

        ApplyProgress(Progress);
    }

    private void OnDestroy()
    {
        DestroySkyboxInstance(daySkyboxInstance);
        DestroySkyboxInstance(nightSkyboxInstance);
    }

    private void OnValidate()
    {
        transitionDurationSeconds = Mathf.Max(0.001f, transitionDurationSeconds);
        nightSkyboxStart = Mathf.Clamp01(nightSkyboxStart);
        daySkyboxExposure = Mathf.Max(0f, daySkyboxExposure);
        nightSkyboxExposure = Mathf.Max(0f, nightSkyboxExposure);
        skyboxTransitionExposure = Mathf.Max(0f, skyboxTransitionExposure);
        dayFogDensity = Mathf.Max(0f, dayFogDensity);
        nightFogDensity = Mathf.Max(0f, nightFogDensity);
    }

    private void EnsureSun()
    {
        if (sun == null)
            sun = FindDirectionalLight();

        if (sun == null)
        {
            GameObject generatedSun = new GameObject("Generated Directional Light");
            sun = generatedSun.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
        }

        RenderSettings.sun = sun;
    }

    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
                return lights[i];
        }

        return null;
    }

    private void PrepareSkyboxes()
    {
        if (daySkybox == null)
            daySkybox = RenderSettings.skybox;

        daySkyboxInstance = CloneSkybox(daySkybox);
        nightSkyboxInstance = CloneSkybox(nightSkybox);
    }

    private Material CloneSkybox(Material source)
    {
        if (source == null)
            return null;

        Material instance = new Material(source);
        instance.name = $"{source.name} Runtime";
        return instance;
    }

    private void ApplyProgress(float progress)
    {
        float dayToSunset = GetSafeInverseLerp(0f, nightSkyboxStart, progress);
        float nightBlend = GetSafeInverseLerp(nightSkyboxStart, 1f, progress);

        ApplySun(progress, dayToSunset, nightBlend);
        ApplyAmbient(progress);
        ApplyFog(progress);
        ApplySkybox(progress, dayToSunset, nightBlend);
    }

    private void ApplySun(float progress, float dayToSunset, float nightBlend)
    {
        if (sun == null)
            return;

        sun.transform.rotation = Quaternion.Euler(Vector3.Lerp(daySunEuler, nightSunEuler, progress));
        sun.intensity = Mathf.Lerp(daySunIntensity, nightSunIntensity, progress);

        sun.color = progress < nightSkyboxStart
            ? Color.Lerp(daySunColor, sunsetSunColor, dayToSunset)
            : Color.Lerp(sunsetSunColor, nightSunColor, nightBlend);
    }

    private void ApplyAmbient(float progress)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = Mathf.Lerp(dayAmbientIntensity, nightAmbientIntensity, progress);
        RenderSettings.ambientSkyColor = Color.Lerp(dayAmbientSky, nightAmbientSky, progress);
        RenderSettings.ambientEquatorColor = Color.Lerp(dayAmbientEquator, nightAmbientEquator, progress);
        RenderSettings.ambientGroundColor = Color.Lerp(dayAmbientGround, nightAmbientGround, progress);
    }

    private void ApplyFog(float progress)
    {
        if (!controlFog)
            return;

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(dayFogColor, nightFogColor, progress);
        RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, progress);
    }

    private void ApplySkybox(float progress, float dayToSunset, float nightBlend)
    {
        SetSkyboxExposure(daySkyboxInstance, Mathf.Lerp(daySkyboxExposure, skyboxTransitionExposure, dayToSunset));
        SetSkyboxExposure(nightSkyboxInstance, Mathf.Lerp(skyboxTransitionExposure, nightSkyboxExposure, nightBlend));

        Material targetSkybox = progress >= nightSkyboxStart && nightSkyboxInstance != null
            ? nightSkyboxInstance
            : daySkyboxInstance;

        if (targetSkybox == null || activeSkybox == targetSkybox)
            return;

        activeSkybox = targetSkybox;
        RenderSettings.skybox = activeSkybox;
        DynamicGI.UpdateEnvironment();
    }

    private void SetSkyboxExposure(Material skybox, float exposure)
    {
        if (skybox != null && skybox.HasFloat("_Exposure"))
            skybox.SetFloat("_Exposure", exposure);
    }

    private float GetSafeInverseLerp(float from, float to, float value)
    {
        if (Mathf.Approximately(from, to))
            return value >= to ? 1f : 0f;

        return Mathf.InverseLerp(from, to, value);
    }

    private void DestroySkyboxInstance(Material instance)
    {
        if (instance == null)
            return;

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
    }
}
