using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightReceiver : MonoBehaviour
{
    LightPuzzle puzzle;
    Light lightSource;
    
    [Header("Configuración")]
    public float requiredLightTime = 3f;
    private float currentLightTime = 0f;
    private float lastLightTime = 0f;
    private bool activated = false;

    [Header("Visuals")]
    public Color idleColor = Color.red;
    public Color chargingColor = Color.yellow;
    public Color activatedColor = Color.green;

    public float minIntensity = 0.5f;
    public float maxIntensity = 5f;

    public Renderer emissiveRenderer;

    public void Initialize(LightPuzzle owner, bool offset = true)
    {
        puzzle = owner;
        lightSource = GetComponent<Light>();

        if (!offset)
            return;
                
        float offsetY = Random.Range(0f, 1.8f);
        transform.parent.position += Vector3.up * offsetY; // Elevar el receptor para que no esté pegado al suelo
    }

    void Update()
    {
        if (activated)
        {
            ActivatedEffect();
            return;
        }

        bool receivingLight = Time.time - lastLightTime <= 0.2f;

        // Si no ha recibido luz en un tiempo
        if (!receivingLight)
            currentLightTime -= Time.deltaTime;
        
        currentLightTime = Mathf.Clamp(currentLightTime, 0f, requiredLightTime);

        UpdateVisuals(receivingLight);
    }

    void UpdateVisuals(bool receivingLight)
    {
        if (lightSource == null)
            return;
            
        float progress = currentLightTime / requiredLightTime;

        // Intensidad gradual
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, progress);

        // Color gradual
        Color targetColor = Color.Lerp(idleColor, chargingColor, progress);
        lightSource.color = targetColor;

        // Emision Material
        if (emissiveRenderer != null)
            emissiveRenderer.material.SetColor("_EmissionColor", targetColor * (1f + progress * 3f)); // Emisión más brillante a medida que se carga
        
        // Flicker leve mientras carga
        if (receivingLight)
            lightSource.intensity += Mathf.Sin(Time.time * 10f) * 0.1f;
    }

    public void ReceiveLight()
    {
        if (activated) return;

        currentLightTime += Time.deltaTime;
        lastLightTime = Time.time;

        if (currentLightTime >= requiredLightTime)
            Activate();
    }

    void Activate()
    {
        activated = true;

        Debug.Log("Receptor de luz activado!");

        lightSource.intensity = maxIntensity;
        lightSource.color = activatedColor;

        if (emissiveRenderer != null)
            emissiveRenderer.material.SetColor("_EmissionColor", activatedColor * 5f);
        
        if (puzzle != null)
            puzzle.ActivateReceiver(this);

        // Sonido
        // Particulas
    }

    void ActivatedEffect()
    {
        float pulse = Mathf.Sin(Time.time * 4f) * .3f;
        lightSource.intensity = maxIntensity + pulse;
    }
}