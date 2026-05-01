using UnityEngine;

public class DoorKey : MonoBehaviour
{
    public float rotationSpeed = 90f; // Degrees per second
    public float amplitude = 0.1f; // Vertical movement amplitude
    public float frequency = 1f; // Vertical movement frequency

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Sin(Time.time * frequency) * amplitude, transform.localPosition.z);
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
