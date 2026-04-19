using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class FlashlightBatteryPickup : MonoBehaviour
{
    [SerializeField] private float batteryAmount = 30f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 2f;

    private Vector3 initialPosition;
    private bool wasCollected;

    void Awake()
    {
        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(0.2f, trigger.radius);

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        initialPosition = transform.position;
    }

    void Update()
    {
        if (wasCollected)
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        Vector3 position = initialPosition;
        position.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (wasCollected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        wasCollected = true;
        player.AddFlashlightBattery(batteryAmount);
        SetVisualState(false);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void SetBatteryAmount(float amount)
    {
        batteryAmount = Mathf.Max(1f, amount);
    }

    public void CaptureCurrentPosition()
    {
        initialPosition = transform.position;
    }

    private void SetVisualState(bool visible)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = visible;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;
    }
}
