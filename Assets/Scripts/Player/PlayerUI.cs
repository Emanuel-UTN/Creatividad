using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Referencias")]
    private PlayerController playerController;
    private StaminaComponent staminaComponent;
    public Slider staminaSlider;
    public Image staminaFillImage;

    void Start()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
        }
        playerController = PlayerController.playerController;
        if (playerController != null)
            staminaComponent = playerController.GetComponent<StaminaComponent>();
    }

    void Update()
    {
        if (playerController == null || !playerController.gameObject.scene.IsValid() || !playerController.gameObject.scene.isLoaded)
        {
            PlayerController fromScene = FindAnyObjectByType<PlayerController>();
            if (fromScene != null && fromScene.gameObject.scene.IsValid() && fromScene.gameObject.scene.isLoaded)
            {
                playerController = fromScene;
                staminaComponent = playerController.GetComponent<StaminaComponent>();
            }

            if (playerController == null)
                return;
        }

        if (staminaComponent == null)
            staminaComponent = playerController.GetComponent<StaminaComponent>();

        if (staminaComponent == null)
            return;

        float normalized = staminaComponent.StaminaNormalized;

        if (staminaSlider != null)
            staminaSlider.value = normalized;

        if (staminaFillImage != null)
            staminaFillImage.fillAmount = normalized;
    }
}
