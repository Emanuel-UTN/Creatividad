using UnityEngine;

public class LightExample : MonoBehaviour
{
    [Header("Referencias")]
    public MeshRenderer lightMesh; // Optional: Assign a mesh renderer to visually represent the light
    public Material onMaterial; // Material when light is on
    public Material offMaterial; // Material when light is off

    [Header("Flicker Settings")]
    public float timeOn = 1.5f;
    public float timeOff = 1.5f;
    private float currentTime = 0f;

    private Light lightComponent;

    void Start()
    {
        lightComponent = GetComponent<Light>();
        transform.parent.GetComponentInChildren<LightReceiver>()?.Initialize(null, false); // Solo para ejemplo, no es necesario pasar el puzzle real
    }

    void Update()
    {
        if (GameController.IsPaused)
            return;

        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            ChangeLightState(!lightComponent.enabled);
            currentTime = lightComponent.enabled ? timeOn : timeOff;
        }
    }

    private void ChangeLightState(bool enabled)
    {
        lightComponent.enabled = enabled;
        if (lightMesh != null)
            lightMesh.material = enabled ? onMaterial : offMaterial; // Update material based on new state
    }
}
