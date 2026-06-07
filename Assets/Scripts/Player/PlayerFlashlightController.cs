using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerFlashlightController : MonoBehaviour
{
    public static event System.Action<float> OnFlashlightBatteryChanged;

    [Header("Linterna")]
    public Light flashlight;
    [SerializeField] private float maxFlashlightBattery = 100f;
    [SerializeField] private float batteryDrainPerSecond = 4f;
    [SerializeField] private float boostedBatteryDrainPerSecond = 14f;
    [SerializeField] [Range(0.2f, 1f)] private float flickerDurationOnEmpty = 0.5f;
    [SerializeField] private float boostedIntensityMultiplier = 2.25f;
    [SerializeField] private float boostedRangeMultiplier = 1.1f;
    [SerializeField] private float enemyStunDuration = 1f;
    [SerializeField] private LayerMask flashlightOcclusionMask = Physics.DefaultRaycastLayers;

    private PlayerInput playerInput;
    private InputAction flashlightAction;
    private InputAction flashBoostAction;
    private EnemyBehaviour enemyBehaviour;
    private float flashlightBattery;
    private float baseLightIntensity;
    private float baseLightRange;
    private bool isBoostingFlashlight;
    private bool flashlightCanTurnOn = true;

    public float CurrentFlashlightBattery => flashlightBattery;
    public float MaxFlashlightBattery => maxFlashlightBattery;
    public float FlashlightBatteryNormalized =>
        maxFlashlightBattery > 0.0001f ? Mathf.Clamp01(flashlightBattery / maxFlashlightBattery) : 0f;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        flashlightAction = (playerInput != null && playerInput.actions != null) ? playerInput.actions.FindAction("Flashlight", false) : null;
        flashBoostAction = (playerInput != null && playerInput.actions != null) ? playerInput.actions.FindAction("Attack", false) : null;

        flashlightBattery = Mathf.Max(0f, maxFlashlightBattery);

        if (flashlight != null)
        {
            baseLightIntensity = flashlight.intensity;
            baseLightRange = flashlight.range;
            if (flashlight.shadows == LightShadows.None)
            {
                #if UNITY_WEBGL
                flashlight.shadows = LightShadows.Hard;
                #else
                flashlight.shadows = LightShadows.Soft;
                #endif
            }
        }

        if (flashlightAction != null)
            flashlightAction.performed += OnToggleFlashlightPerformed;      
            
        if (flashBoostAction != null)
        {
            flashBoostAction.started += OnFlashBoostStarted;
            flashBoostAction.canceled += OnFlashBoostCanceled;
        }
    }

    void Start()
    {
        NotifyBatteryChanged();
    }

    void Update()
    {
        if (GameController.IsPaused)
            return;

        UpdateEnemyReference();
        UpdateFlashlightBattery();
        UpdateFlashlightVisuals();
    }

    void OnDestroy()
    {
        if (flashlightAction != null)
            flashlightAction.performed -= OnToggleFlashlightPerformed;

        if (flashBoostAction != null)
        {
            flashBoostAction.started -= OnFlashBoostStarted;
            flashBoostAction.canceled -= OnFlashBoostCanceled;
        }
    }

    public void ToggleFlashlight()
    {
        if (flashlight == null)
            return;

        if (!flashlight.enabled)
        {
            if (!CanUseFlashlight())
                return;

            flashlight.enabled = true;
            return;
        }

        flashlight.enabled = false;
    }

    public bool AddFlashlightBattery(float amount)
    {
        if (amount <= 0f || maxFlashlightBattery <= 0f)
            return false;

        float previousBattery = flashlightBattery;
        flashlightBattery = Mathf.Clamp(flashlightBattery + amount, 0f, maxFlashlightBattery);

        if (Mathf.Approximately(previousBattery, flashlightBattery))
            return false;

        NotifyBatteryChanged();
        flashlightCanTurnOn = true;
        return true;
    }

    private void OnToggleFlashlightPerformed(InputAction.CallbackContext context)
    {
        if (GameController.IsPaused)
            return;

        ToggleFlashlight();
    }

    private void OnFlashBoostStarted(InputAction.CallbackContext context)
    {
        if (GameController.IsPaused)
            return;

        isBoostingFlashlight = true;
        TryStunEnemyWithFlashlight();
    }

    private void OnFlashBoostCanceled(InputAction.CallbackContext context)
    {
        if (GameController.IsPaused)
            return;

        isBoostingFlashlight = false;
    }

    private void UpdateEnemyReference()
    {
        if (enemyBehaviour != null)
            return;

        if (GameController.gameController == null || GameController.gameController.enemy == null)
            return;

        enemyBehaviour = GameController.gameController.enemy.GetComponent<EnemyBehaviour>();
    }

    private void UpdateFlashlightBattery()
    {
        if (maxFlashlightBattery <= 0f)
        {
            if (flashlightBattery > 0f)
            {
                flashlightBattery = 0f;
                NotifyBatteryChanged();
            }

            ForceFlashlightOff();
            return;
        }

        if (GameController.IsCreativoEnabled)
        {
            if (!CanUseFlashlight())
                ForceFlashlightOff();

            return;
        }

        float previousBattery = flashlightBattery;

        if (flashlight != null && flashlight.enabled)
        {
            float drain = batteryDrainPerSecond;
            if (isBoostingFlashlight)
                drain += boostedBatteryDrainPerSecond;

            flashlightBattery = Mathf.Max(0f, flashlightBattery - drain * Time.deltaTime);
        }

        if (!CanUseFlashlight())
            ForceFlashlightOff();

        if (!Mathf.Approximately(previousBattery, flashlightBattery))
            NotifyBatteryChanged();
    }

    private bool CanUseFlashlight()
    {
        return flashlight != null && flashlightBattery > 0.001f;
    }

    private void ForceFlashlightOff()
    {
        isBoostingFlashlight = false;
        if (flashlight == null || !flashlightCanTurnOn)
            return;

        flashlightCanTurnOn = false;
        flashlight.intensity = baseLightIntensity;
        flashlight.range = baseLightRange;
        StartCoroutine(FlashLightFlicker(flickerDurationOnEmpty));
    }

    private IEnumerator FlashLightFlicker(float duration)
    {
        if (flashlight == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            flashlight.enabled = !flashlight.enabled;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            elapsed += 0.1f;
        }

        flashlight.enabled = false;
    }

    private void UpdateFlashlightVisuals()
    {
        if (flashlight == null)
            return;

        if (!flashlight.enabled)
        {
            flashlight.intensity = baseLightIntensity;
            flashlight.range = baseLightRange;
            return;
        }

        if (isBoostingFlashlight && flashlightBattery > 0.001f)
        {
            flashlight.intensity = baseLightIntensity * Mathf.Max(1f, boostedIntensityMultiplier);
            flashlight.range = baseLightRange * Mathf.Max(1f, boostedRangeMultiplier);
            return;
        }

        flashlight.intensity = baseLightIntensity;
        flashlight.range = baseLightRange;
    }

    private void TryStunEnemyWithFlashlight()
    {
        if (!CanUseFlashlight() || flashlight == null || !flashlight.enabled)
            return;

        UpdateEnemyReference();
        if (enemyBehaviour == null)
            return;

        Transform enemyTransform = enemyBehaviour.transform;
        Vector3 origin = flashlight.transform.position;
        Vector3 target = enemyTransform.position + Vector3.up * 1.2f;
        Vector3 toEnemy = target - origin;
        float distance = toEnemy.magnitude;

        if (distance > Mathf.Max(0.1f, flashlight.range))
            return;

        float halfAngle = flashlight.type == LightType.Spot ? flashlight.spotAngle * 0.5f : 35f;
        if (Vector3.Angle(flashlight.transform.forward, toEnemy) > halfAngle)
            return;

        if (!HasClearFlashlightSight(origin, enemyTransform, distance))
            return;

        enemyBehaviour.ApplyFlashlightStun(enemyStunDuration);
    }

    private bool HasClearFlashlightSight(Vector3 origin, Transform enemyTransform, float distance)
    {
        Vector3 direction = (enemyTransform.position + Vector3.up * 1.2f - origin).normalized;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            flashlightOcclusionMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
            return true;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.transform;
            if (hitTransform == null)
                continue;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            return hitTransform == enemyTransform || hitTransform.IsChildOf(enemyTransform);
        }

        return false;
    }

    private void NotifyBatteryChanged()
    {
        OnFlashlightBatteryChanged?.Invoke(FlashlightBatteryNormalized);
    }
}
