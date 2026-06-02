using UnityEngine;

public class LightExample : MonoBehaviour
{
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
            lightComponent.enabled = !lightComponent.enabled;
            currentTime = lightComponent.enabled ? timeOn : timeOff;
        }
    }
}
