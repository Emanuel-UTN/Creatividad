using UnityEngine;

public class DoorKey : MonoBehaviour
{
    public float rotationSpeed = 90f; // Degrees per second
    public float amplitude = 0.1f; // Vertical movement amplitude
    public float frequency = 1f; // Vertical movement frequency
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        transform.localPosition = initialPosition + Vector3.up * Mathf.Sin(Time.time * frequency) * amplitude;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.playerController.KeyCount++;
            Destroy(gameObject);
        }
    }
}
