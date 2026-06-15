using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightController : MonoBehaviour
{
    [SerializeField]
    private new Light light;

    [Header("Referencias")]
    public MeshRenderer lightMesh; // Optional: Assign a mesh renderer to visually represent the light
    public Material onMaterial; // Material when light is on
    public Material offMaterial; // Material when light is off

    [Header("Flicker Settings")]
    public bool enableFlicker = true;
    public float flickerIntensityRange = 0.25f; // Range for intensity variation during flicker

    float originalIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalIntensity = light.intensity;
        if (enableFlicker && light.enabled)
            StartCoroutine(Flick());

        if (lightMesh != null)
            lightMesh.material = light.enabled ? onMaterial : offMaterial; // Set initial material based on light state
    }

    void Update()
    {
        if (!light.enabled)
            return;
        
        if (enableFlicker) // Randomly adjust the intensity to create a flickering effect
            light.intensity = originalIntensity + Random.Range(-flickerIntensityRange, flickerIntensityRange);
    }

    IEnumerator Flick()
    {
        while (true)
        {
            StartCoroutine(FlickerEnable());
            yield return new WaitForSeconds(Random.Range(3, 10));
        }
    }

    IEnumerator FlickerEnable()
    {
        float duration = Random.Range(0.5f, 1.25f);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            ToggleLight();
            float wait = Random.Range(0.05f, 0.15f);
            elapsedTime += wait;
            yield return new WaitForSeconds(wait);
        }

        ChangeLightState(true); // Ensure the light is on after flickering
    }

    public void SetLightState(bool enabled)
    {
        if (enabled == light.enabled)
            return;

        ChangeLightState(enabled);

        if (!enableFlicker)
            return;

        if (enabled)
            StartCoroutine(Flick());
        else
            StopAllCoroutines();
    }

    private void ToggleLight()
    {
        ChangeLightState(!light.enabled);
    }

    private void ChangeLightState(bool enabled)
    {
        light.enabled = enabled;
        if (lightMesh != null)
            lightMesh.material = enabled ? onMaterial : offMaterial; // Update material based on new state
    }
}
