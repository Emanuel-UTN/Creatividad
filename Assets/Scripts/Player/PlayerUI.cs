using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlayerUI : MonoBehaviour
{
    static public PlayerUI playerUI;
    private PlayerController playerController;


    private const float PlayerLookupInterval = 0.5f;
    private static readonly Color FlashlightTextColor = new Color(1f, 0.95f, 0.45f, 1f);
    private static readonly Color FlashlightPanelColor = new Color(0.05f, 0.07f, 0.1f, 0.82f);

    [Header("Stamina")]
    public Slider staminaSlider;
    public Image staminaFillImage;

    [Header("Flashlight Battery")]
    public Slider flashlightBatterySlider;
    public Image flashlightBatteryImage;
    public TMP_Text batteryCountText;

    private StaminaComponent staminaComponent;
    private float nextLookupTime;
    private float lastNormalizedValue = -1f;
    private float lastBatteryPercentage = -1f;

    [Header("Key Count")]
    [SerializeField] private TMP_Text keyCountText;

    [Header("Interaction Point")]
    public RawImage interactionPoint;

    [Header("Sample")]
    public TMP_Text sampleTypeText;

    [Header("Screamer")]
    public VideoPlayer videoPlayer;
    public GameObject deathMenu;

    void Start()
    {
        if (playerUI == null)
        {
            playerUI = this;
        }
        else if (playerUI != this)
        {
            Destroy(gameObject);
            return;
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;

            staminaFillImage.color = new Color(1f, 1f, 1f, 0f);
        }

        keyCountText.text = "0";
        batteryCountText.text = "0";

        TryAssignPlayerController(true);

        videoPlayer.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!HasValidPlayerController())
            TryAssignPlayerController(false);

        UpdateBatteryUI();
        UpdateStaminaUI();        
    }

    void UpdateStaminaUI()
    {
        if (staminaComponent == null)
            return;
        

        float normalized = staminaComponent.StaminaNormalized;
        if (!Mathf.Approximately(normalized, lastNormalizedValue))
        {
            lastNormalizedValue = normalized;

            if (staminaSlider != null)
                staminaSlider.value = normalized;

            if (staminaFillImage != null)
                staminaFillImage.fillAmount = normalized;
        }

        if (normalized > 0.90)
                staminaFillImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(staminaFillImage.color.a, 0f, Time.deltaTime * 5f));
            else
                staminaFillImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(staminaFillImage.color.a, 1f, Time.deltaTime * 5f));
    }

    private void UpdateBatteryUI()
    {
        if (playerController == null)
            return;

        float batteryPercentage = playerController.FlashlightBatteryNormalized;
        if (batteryPercentage == lastBatteryPercentage)
            return;

        lastBatteryPercentage = batteryPercentage;

        if (flashlightBatterySlider != null)
            flashlightBatterySlider.value = batteryPercentage;

        if (flashlightBatteryImage != null)
            flashlightBatteryImage.color = Color.Lerp(Color.red, Color.green, batteryPercentage);
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

    public void UpdateKeyCount(int count)
    {
        if (keyCountText != null)
            keyCountText.text = $"{count}";
    }

    public void SetInteractionPointActive(bool active)
    {
        if (interactionPoint != null)
            interactionPoint.rectTransform.sizeDelta = active ? new Vector2(17.5f, 17.5f) : new Vector2(10f, 10f);
    }

    public void UpdateSampleType(SampleType? sampleType)
    {
        if (sampleTypeText != null)
            sampleTypeText.text = sampleType.HasValue ? $"Sample: {sampleType.Value}" : "";
    }

    public void UpdateBatteryCount(int count)
    {
        if (batteryCountText != null)
            batteryCountText.text = $"{count}";
    }

    public void PlayScreamer(VideoClip clip)
    {
        if (videoPlayer == null || clip == null)
        {
            ShowDeathMenu();
            return;
        }

        try {
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.clip = clip;
            videoPlayer.Play();
            videoPlayer.isLooping = false;
            Cursor.visible = false;

            float clipLength = 2f;
            try { clipLength = (float)clip.length; } catch {}
            
            DeathMenu.Instance?.InitializeRestart();
            videoPlayer.loopPointReached += (vp) => {
                ShowDeathMenu();
            };
        } catch (System.Exception e) {
            Debug.LogWarning("Error playing screamer: " + e.Message);
            ShowDeathMenu();
        }
    }

    public void ShowDeathMenu()
    {
        Time.timeScale = 0f;
        gameObject.SetActive(false);
        
        if (DeathMenu.Instance != null)
            DeathMenu.Instance.ShowMenu();
        else if (deathMenu != null)
            deathMenu.SetActive(true);
        else
        {
            DeathMenu fallbackMenu = FindAnyObjectByType<DeathMenu>(FindObjectsInactive.Include);
            if (fallbackMenu != null)
                fallbackMenu.ShowMenu();
        }
            
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
