using UnityEngine;

public class FlashlightDetector : MonoBehaviour
{
    public float range = 2.0f;
    [Tooltip("El radio del haz de detección. Mayor valor hace que sea más fácil apuntar al receptor.")]
    public float detectionRadius = 0.7f;

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
        
        // Usamos SphereCastAll para detectar receptores en un haz más ancho
        RaycastHit[] hits = Physics.SphereCastAll(ray, detectionRadius, range);
        
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            LightReceiver receiver = hit.collider.GetComponent<LightReceiver>();
            if (receiver != null)
            {
                // Verificar línea de visión directa (que no haya una pared bloqueando la luz en medio)
                Vector3 toReceiver = hit.collider.transform.position - transform.position;
                float distance = toReceiver.magnitude;
                
                if (distance > 0.001f)
                {
                    if (Physics.Raycast(transform.position, toReceiver.normalized, out RaycastHit occlusionHit, distance))
                    {
                        // Si chocamos con algo que no es el receptor (ej. una pared), la luz está bloqueada
                        if (occlusionHit.collider != hit.collider && !occlusionHit.transform.IsChildOf(hit.transform))
                        {
                            continue; 
                        }
                    }
                }
                
                receiver.ReceiveLight();
                break; // Activamos un receptor por frame
            }
        }
    }
}