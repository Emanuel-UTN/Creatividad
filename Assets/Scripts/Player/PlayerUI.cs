using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    private const float PlayerLookupInterval = 0.5f;
    private static readonly Color FlashlightTextColor = new Color(1f, 0.95f, 0.45f, 1f);
    private static readonly Color FlashlightPanelColor = new Color(0.05f, 0.07f, 0.1f, 0.82f);

    [Header("Referencias")]
    public Slider staminaSlider;
    public Image staminaFillImage;
    public TMP_Text flashlightBatteryText;
    public Image flashlightBatteryPanel;

    private PlayerController playerController;
    private StaminaComponent staminaComponent;
    private float nextLookupTime;
    private float lastNormalizedValue = -1f;
    private int lastBatteryPercentage = -1;

    void Start()
    {
        EnsureBatteryUIExists();

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
        }

        TryAssignPlayerController(true);
    }

    private void EnsureBatteryUIExists()
    {
        if (flashlightBatteryText != null && flashlightBatteryPanel != null)
            return;

        if (staminaSlider == null)
            return;

        RectTransform staminaRect = staminaSlider.GetComponent<RectTransform>();
        if (staminaRect == null || staminaRect.parent == null)
            return;

        GameObject panelObject = new GameObject("FlashlightBatteryPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = staminaSlider.gameObject.layer;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(staminaRect.parent, false);
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = staminaRect.anchoredPosition + new Vector2(42f, -58f);
        panelRect.sizeDelta = new Vector2(150f, 42f);
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = FlashlightPanelColor;
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("FlashlightBatteryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = staminaSlider.gameObject.layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI batteryText = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text referenceText = FindAnyObjectByType<TMP_Text>();
        if (referenceText != null)
        {
            batteryText.font = referenceText.font;
            batteryText.fontSharedMaterial = referenceText.fontSharedMaterial;
        }

        batteryText.fontSize = 24f;
        batteryText.color = FlashlightTextColor;
        batteryText.alignment = TextAlignmentOptions.Center;
        batteryText.raycastTarget = false;
        batteryText.text = "BAT 100%";

        flashlightBatteryPanel = panelImage;
        flashlightBatteryText = batteryText;
    }

    void Update()
    {
        if (!HasValidPlayerController())
            TryAssignPlayerController(false);

        if (staminaComponent == null)
        {
            UpdateBatteryUI();
            return;
        }

        float normalized = staminaComponent.StaminaNormalized;
        if (!Mathf.Approximately(normalized, lastNormalizedValue))
        {
            lastNormalizedValue = normalized;

            if (staminaSlider != null)
                staminaSlider.value = normalized;

            if (staminaFillImage != null)
                staminaFillImage.fillAmount = normalized;
        }

        UpdateBatteryUI();
    }

    private void UpdateBatteryUI()
    {
        if (playerController == null)
            return;

        int batteryPercentage = Mathf.RoundToInt(playerController.FlashlightBatteryNormalized * 100f);
        if (batteryPercentage == lastBatteryPercentage)
            return;

        lastBatteryPercentage = batteryPercentage;

        if (flashlightBatteryText != null)
            flashlightBatteryText.text = $"BAT {batteryPercentage}%";
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
        lastBatteryPercentage = -1;
    }
}
