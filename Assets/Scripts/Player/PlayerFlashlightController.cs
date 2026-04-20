using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerFlashlightController : MonoBehaviour
{
    public static event System.Action<float> OnFlashlightBatteryChanged;

    [Header("Linterna")]
    public Light lanter;
    [SerializeField] private float maxFlashlightBattery = 100f;
    [SerializeField] private float batteryDrainPerSecond = 4f;
    [SerializeField] private float boostedBatteryDrainPerSecond = 14f;
    [SerializeField] private int batteryShutdownClockHour = 10;
    [SerializeField] private float boostedIntensityMultiplier = 2.25f;
    [SerializeField] private float boostedRangeMultiplier = 1.1f;
    [SerializeField] private float enemyStunDuration = 1f;
    [SerializeField] private LayerMask flashlightOcclusionMask = Physics.DefaultRaycastLayers;

    private PlayerInput playerInput;
    private InputAction lanterAction;
    private InputAction flashBoostAction;
    private EnemyBehaviour enemyBehaviour;
    private float flashlightBattery;
    private float baseLightIntensity;
    private float baseLightRange;
    private bool isBoostingFlashlight;

    public float CurrentFlashlightBattery => flashlightBattery;
    public float MaxFlashlightBattery => maxFlashlightBattery;
    public float FlashlightBatteryNormalized =>
        maxFlashlightBattery > 0.0001f ? Mathf.Clamp01(flashlightBattery / maxFlashlightBattery) : 0f;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        lanterAction = playerInput != null ? playerInput.actions.FindAction("Lanter", false) : null;
        flashBoostAction = playerInput != null ? playerInput.actions.FindAction("Attack", false) : null;

        flashlightBattery = Mathf.Max(0f, maxFlashlightBattery);

        if (lanter != null)
        {
            baseLightIntensity = lanter.intensity;
            baseLightRange = lanter.range;
            if (lanter.shadows == LightShadows.None)
                lanter.shadows = LightShadows.Soft;
        }

        if (lanterAction != null)
            lanterAction.performed += OnToggleLanterPerformed;

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
        UpdateEnemyReference();
        UpdateFlashlightBattery();
        UpdateFlashlightVisuals();
    }

    void OnDestroy()
    {
        if (lanterAction != null)
            lanterAction.performed -= OnToggleLanterPerformed;

        if (flashBoostAction != null)
        {
            flashBoostAction.started -= OnFlashBoostStarted;
            flashBoostAction.canceled -= OnFlashBoostCanceled;
        }
    }

    public void ToggleLanter()
    {
        if (lanter == null)
            return;

        if (!lanter.enabled)
        {
            if (!CanUseFlashlight())
                return;

            lanter.enabled = true;
            return;
        }

        lanter.enabled = false;
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
        return true;
    }

    private void OnToggleLanterPerformed(InputAction.CallbackContext context)
    {
        ToggleLanter();
    }

    private void OnFlashBoostStarted(InputAction.CallbackContext context)
    {
        isBoostingFlashlight = true;
        TryStunEnemyWithFlashlight();
    }

    private void OnFlashBoostCanceled(InputAction.CallbackContext context)
    {
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

        float previousBattery = flashlightBattery;

        if (lanter != null && lanter.enabled)
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
        return lanter != null && flashlightBattery > 0.001f && !HasReachedBatteryShutdownHour();
    }

    private bool HasReachedBatteryShutdownHour()
    {
        Clock clock = Clock.Instance;
        if (clock == null)
            return false;

        return clock.CurrentGameMinutes >= GetShutdownAbsoluteMinutes(clock.startHour);
    }

    private float GetShutdownAbsoluteMinutes(int startHour)
    {
        int normalizedStartHour = Mathf.Clamp(startHour, 0, 23);
        int targetClockHour = Mathf.Clamp(batteryShutdownClockHour, 1, 12);

        for (int elapsedHours = 1; elapsedHours <= 24; elapsedHours++)
        {
            int absoluteHour = normalizedStartHour + elapsedHours;
            if (Clock.ToDisplayHour(absoluteHour) == targetClockHour)
                return absoluteHour * 60f;
        }

        return (normalizedStartHour + 24) * 60f;
    }

    private void ForceFlashlightOff()
    {
        isBoostingFlashlight = false;
        if (lanter == null)
            return;

        lanter.enabled = false;
        lanter.intensity = baseLightIntensity;
        lanter.range = baseLightRange;
    }

    private void UpdateFlashlightVisuals()
    {
        if (lanter == null)
            return;

        if (!lanter.enabled)
        {
            lanter.intensity = baseLightIntensity;
            lanter.range = baseLightRange;
            return;
        }

        if (isBoostingFlashlight && flashlightBattery > 0.001f)
        {
            lanter.intensity = baseLightIntensity * Mathf.Max(1f, boostedIntensityMultiplier);
            lanter.range = baseLightRange * Mathf.Max(1f, boostedRangeMultiplier);
            return;
        }

        lanter.intensity = baseLightIntensity;
        lanter.range = baseLightRange;
    }

    private void TryStunEnemyWithFlashlight()
    {
        if (!CanUseFlashlight() || lanter == null || !lanter.enabled)
            return;

        UpdateEnemyReference();
        if (enemyBehaviour == null)
            return;

        Transform enemyTransform = enemyBehaviour.transform;
        Vector3 origin = lanter.transform.position;
        Vector3 target = enemyTransform.position + Vector3.up * 1.2f;
        Vector3 toEnemy = target - origin;
        float distance = toEnemy.magnitude;

        if (distance > Mathf.Max(0.1f, lanter.range))
            return;

        float halfAngle = lanter.type == LightType.Spot ? lanter.spotAngle * 0.5f : 35f;
        if (Vector3.Angle(lanter.transform.forward, toEnemy) > halfAngle)
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
