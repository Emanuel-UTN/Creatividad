using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Referencias")]
    private PlayerController playerController;
    public Slider staminaSlider;
    public Image staminaFillImage;

    void Start()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
        }
    }

    void Update()
    {
        if (playerController == null)
            return;

        float normalized = playerController.StaminaNormalized;

        if (staminaSlider != null)
            staminaSlider.value = normalized;

        if (staminaFillImage != null)
            staminaFillImage.fillAmount = normalized;
    }
}
