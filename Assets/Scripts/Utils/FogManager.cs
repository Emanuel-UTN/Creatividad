using UnityEngine;

public class FogManager : MonoBehaviour
{
    public static FogManager Instance { get; private set; }

    [Header("Fog Configuration")]
    public bool enableDynamicFog = false;
    public FogMode fogMode = FogMode.Linear;

    [Header("Initial Fog Range (Linear)")]
    public float initialFogStart = 2f;
    public float initialFogEndMin = 16f;
    public float initialFogEndMax = 24f;

    [Header("Thickest Fog Limits")]
    [Tooltip("The closest distance the fog will approach. A smaller value means thicker fog.")]
    public float minFogEndDistance = 4.0f;
    public float minFogStartDistance = 0.5f;

    [Header("Thickening Progression")]
    [Tooltip("Time in seconds for the fog to reach its maximum thickness.")]
    public float thickeningDuration = 240f; 

    [Header("Fog Color Palette")]
    [Tooltip("List of horror-themed colors for the fog to pick from at random.")]
    public Color[] possibleColors = new Color[]
    {
        new Color(0.01f, 0.01f, 0.015f, 1f), // Pitch Black / Void
        new Color(0.04f, 0.06f, 0.05f, 1f),  // Dark Greenish Swamp / Decay
        new Color(0.03f, 0.03f, 0.05f, 1f),  // Grim Abyssal Blue
        new Color(0.05f, 0.05f, 0.05f, 1f)   // Graveyard Ash Grey
    };

    [Header("Ambient Configuration")]
    [Tooltip("If true, the ambient light color will match the selected fog color multiplied by ambientLightMultiplier. Otherwise, it uses customAmbientColor.")]
    public bool matchAmbientToFog = false;
    public float ambientLightMultiplier = 1.8f;
    public Color customAmbientColor = new Color(0.18f, 0.18f, 0.21f, 1f); // Good dark visibility color

    private float currentFogStart;
    private float currentFogEnd;
    private float initialFogEnd;
    private float elapsedTime;
    private Color selectedColor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void InitializeFog()
    {
        if (!enableDynamicFog)
        {
            // Do not override Unity's static scene lighting and fog settings
            return;
        }

        // 1. Enable fog and set mode
        RenderSettings.fog = true;
        RenderSettings.fogMode = fogMode;

        // 2. Select a random color from the list
        if (possibleColors != null && possibleColors.Length > 0)
        {
            selectedColor = possibleColors[Random.Range(0, possibleColors.Length)];
        }
        else
        {
            selectedColor = new Color(0.02f, 0.02f, 0.02f, 1f); // Fallback to near black
        }

        RenderSettings.fogColor = selectedColor;

        // Optionally match Ambient Light Source to the fog color to keep the environment cohesive
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        if (matchAmbientToFog)
        {
            RenderSettings.ambientLight = selectedColor * ambientLightMultiplier;
        }
        else
        {
            RenderSettings.ambientLight = customAmbientColor;
        }

        // 3. Set starting ranges
        currentFogStart = initialFogStart;
        initialFogEnd = Random.Range(initialFogEndMin, initialFogEndMax);
        currentFogEnd = initialFogEnd;

        RenderSettings.fogStartDistance = currentFogStart;
        RenderSettings.fogEndDistance = currentFogEnd;

        elapsedTime = 0f;
    }

    void Update()
    {
        if (!enableDynamicFog || GameController.IsPaused)
            return;

        if (elapsedTime < thickeningDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / thickeningDuration);

            // Interpolate fog start and end distances closer to the player
            currentFogEnd = Mathf.Lerp(initialFogEnd, minFogEndDistance, progress);
            currentFogStart = Mathf.Lerp(initialFogStart, minFogStartDistance, progress);

            RenderSettings.fogStartDistance = currentFogStart;
            RenderSettings.fogEndDistance = currentFogEnd;
        }
    }
}
