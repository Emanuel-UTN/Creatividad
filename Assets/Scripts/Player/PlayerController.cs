using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 12f;
    public float staminaDepletionRate = 20f;

    private float stamina;
    private PlayerMovement playerMovement;

    public float CurrentStamina => stamina;
    public float MaxStamina => maxStamina;
    public float StaminaNormalized => maxStamina > 0f ? stamina / maxStamina : 0f;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        stamina = Mathf.Max(0f, maxStamina);
    }

    void Start()
    {
        if (GameController.gameController != null)
            GameController.gameController.player = gameObject;
    }

    public bool ResolveSprint(bool sprintRequestedAndMoving, float deltaTime)
    {
        if (sprintRequestedAndMoving && stamina > 0f)
        {
            stamina -= staminaDepletionRate * deltaTime;
            if (stamina < 0f)
                stamina = 0f;

            return stamina > 0f;
        }

        stamina += staminaRegenRate * deltaTime;
        if (stamina > maxStamina)
            stamina = maxStamina;

        return false;
    }
}
