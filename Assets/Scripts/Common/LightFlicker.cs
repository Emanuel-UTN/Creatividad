using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    private new Light light;

    float originalIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        light = GetComponent<Light>();
        originalIntensity = light.intensity;
        StartCoroutine(Flick());
    }

    void Update()
    {
        if (!light.enabled)
            return;
        
        // Randomly adjust the intensity to create a flickering effect
        light.intensity = originalIntensity + Random.Range(-0.25f, 0.25f);
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
            light.enabled = !light.enabled; // Toggle light on/off
            float wait = Random.Range(0.05f, 0.15f);
            elapsedTime += wait;
            yield return new WaitForSeconds(wait);
        }

        light.enabled = true;
    }
}
