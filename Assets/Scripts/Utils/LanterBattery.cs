using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class LanterBattery : MonoBehaviour
{
    [SerializeField] private float batteryAmount = 30f;
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 2f;

    private Vector3 initialPosition;
    private bool wasCollected;

    void Awake()
    {
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
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
