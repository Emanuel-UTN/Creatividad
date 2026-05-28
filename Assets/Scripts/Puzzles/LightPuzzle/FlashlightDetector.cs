using UnityEngine;

public class FlashlightDetector : MonoBehaviour
{
    public float range = 25f;

    private Light lightSource;

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (lightSource == null)
            Debug.LogError("FlashlightDetector requires a Light component.");
    }

    void Update()
    {
        if (lightSource == null || !lightSource.enabled)
            return;
        
        Ray ray = new Ray(transform.position, transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            LightReceiver receiver = hit.collider.GetComponent<LightReceiver>();
            if (receiver != null)
                receiver.ReceiveLight();
        }
    }
}