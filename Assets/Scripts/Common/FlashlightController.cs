using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Light))]
public class FlashlightController : MonoBehaviour
{
    private Light flashlight;

    [Header("Referencias")]
    public Transform cameraTransform;

    [Header("Movimiento")]
    public float swayAmount = 2f;
    public float smoothSpeed = .8f;

    [Header("Rotación")]
    public float rotationAmount = 4f;
    public float maxRotationAngle = 15f;
    private Vector2 maxRotationAngles;
    private Vector2 minRotationAngles;

    [Header("Breathing")]
    public bool enableBreathing = true;
    public float breathingSpeed = 1.5f;
    public float breathingAmount = .015f;

    [Header("Flicker")]
    public bool enableFlicker = true;
    public float intensityVariance = .2f;
    private float initialIntensity;
    public float flickerSpeed = .08f;

    [Header("Blink")]
    public bool enableBlink = true;
    public float blinkDuration = 0.35f;
    public float blinkInterval = 50f;
    private float nextBlinkTime;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private InputAction lookAction;
    private float timer;

    void Start()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;

        maxRotationAngles = new Vector2(
            initialRotation.eulerAngles.x + maxRotationAngle,
            initialRotation.eulerAngles.y + maxRotationAngle
        );
        minRotationAngles = new Vector2(
            initialRotation.eulerAngles.x - maxRotationAngle,
            initialRotation.eulerAngles.y - maxRotationAngle
        );

        PlayerInput input = GetComponentInParent<PlayerInput>();
        lookAction = (input != null && input.actions != null) ? input.actions.FindAction("Look", false) : null;
        
        flashlight = GetComponent<Light>();
        nextBlinkTime = Time.time + Random.Range(blinkInterval/2, blinkInterval * 1.5f);
    }

    void Update()
    {
        // Movimiento
        if (lookAction == null)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        Quaternion targetRotation = initialRotation * Quaternion.Euler(-look.y * rotationAmount, look.x * rotationAmount, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);

        float angleX = transform.localRotation.eulerAngles.x;
        float angleY = transform.localRotation.eulerAngles.y;

        transform.localRotation = Quaternion.Euler(
            (angleX < 180) ? Mathf.Min(angleX, maxRotationAngles.x) : Mathf.Max(angleX, 360 + minRotationAngles.x),
            (angleY < 180) ? Mathf.Min(angleY, maxRotationAngles.y) : Mathf.Max(angleY, 360 + minRotationAngles.y),
            transform.localRotation.eulerAngles.z
        );

        // Breathing
        if (enableBreathing)
        {
            float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;

            transform.localPosition = initialPosition + new Vector3(0f, breath, 0f);
        }

        // Blink
        if (CanBlink())
        {
            StartCoroutine(Blink());
            nextBlinkTime = Time.time + Random.Range(blinkInterval/2, blinkInterval * 1.5f);
        }

        // Flicker
        if (!enableFlicker || flashlight == null)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            flashlight.intensity = initialIntensity + Random.Range(-intensityVariance, intensityVariance);
            timer = Random.Range(flickerSpeed * 0.75f, flickerSpeed * 1.25f);
        }
    }

    private bool CanBlink()
    {
        return enableBlink &&
            Time.time >= nextBlinkTime &&
            flashlight != null &&
            flashlight.enabled &&
            PlayerController.playerController != null &&
            PlayerController.playerController.FlashlightBatteryNormalized >= 0.025f;
    }

    private IEnumerator Blink()
    {
        float elapsed = 0f;
        while (elapsed < blinkDuration)        {
            flashlight.enabled = !flashlight.enabled;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            elapsed += 0.1f;
        }
    }
}