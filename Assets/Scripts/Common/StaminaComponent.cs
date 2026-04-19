using UnityEngine;

public class StaminaComponent : MonoBehaviour
{
    [Header("Stamina")]
    [Min(0f)]
    public float maxStamina = 100f;
    [Min(0f)]
    public float staminaRegenRate = 12f;
    [Min(0f)]
    public float staminaDepletionRate = 20f;

    private float stamina;

    public float CurrentStamina => stamina;
    public float MaxStamina => maxStamina;
    public float StaminaNormalized => Mathf.Clamp01(maxStamina > 0f ? stamina / maxStamina : 0f);
    public bool HasStamina => stamina > 0.001f;

    private void Awake()
    {
        maxStamina = Mathf.Max(0f, maxStamina);
        staminaRegenRate = Mathf.Max(0f, staminaRegenRate);
        staminaDepletionRate = Mathf.Max(0f, staminaDepletionRate);
        stamina = maxStamina;
    }

    private void OnValidate()
    {
        maxStamina = Mathf.Max(0f, maxStamina);
        staminaRegenRate = Mathf.Max(0f, staminaRegenRate);
        staminaDepletionRate = Mathf.Max(0f, staminaDepletionRate);
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    public bool Consume(float deltaTime)
    {
        if (!HasStamina)
            return false;

        stamina = Mathf.Max(0f, stamina - staminaDepletionRate * Mathf.Max(0f, deltaTime));
        return true;
    }

    public void Regenerate(float deltaTime)
    {
        if (maxStamina <= 0f)
            return;

        stamina = Mathf.Min(maxStamina, stamina + staminaRegenRate * Mathf.Max(0f, deltaTime));
    }

    public bool ResolveSprint(bool sprintRequestedAndMoving, bool blocked, float deltaTime)
    {
        bool canSprint = sprintRequestedAndMoving && !blocked && HasStamina;

        if (canSprint)
            Consume(deltaTime);
        else
            Regenerate(deltaTime);

        return canSprint;
    }

    public void SetStamina(float value)
    {
        stamina = Mathf.Clamp(value, 0f, maxStamina);
    }
}
