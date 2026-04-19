using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    private const float PlayerLookupInterval = 0.5f;

    [Header("Referencias")]
    public Slider staminaSlider;
    public Image staminaFillImage;

    private PlayerController playerController;
    private StaminaComponent staminaComponent;
    private float nextLookupTime;
    private float lastNormalizedValue = -1f;

    void Start()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
        }

        TryAssignPlayerController(true);
    }

    void Update()
    {
        if (!HasValidPlayerController())
            TryAssignPlayerController(false);

        if (staminaComponent == null)
            return;

        float normalized = staminaComponent.StaminaNormalized;
        if (Mathf.Approximately(normalized, lastNormalizedValue))
            return;

        lastNormalizedValue = normalized;

        if (staminaSlider != null)
            staminaSlider.value = normalized;

        if (staminaFillImage != null)
            staminaFillImage.fillAmount = normalized;
    }

    private bool HasValidPlayerController()
    {
        return playerController != null
            && playerController.gameObject.scene.IsValid()
            && playerController.gameObject.scene.isLoaded;
    }

    private void TryAssignPlayerController(bool force)
    {
        if (!force && Time.unscaledTime < nextLookupTime)
            return;

        nextLookupTime = Time.unscaledTime + PlayerLookupInterval;

        PlayerController candidate = PlayerController.playerController;
        if (candidate == null)
            candidate = FindAnyObjectByType<PlayerController>();

        if (candidate == null || !candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded)
            return;

        playerController = candidate;
        staminaComponent = playerController.GetComponent<StaminaComponent>();
        lastNormalizedValue = -1f;
    }
}
