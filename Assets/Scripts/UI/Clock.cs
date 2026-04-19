using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float ClockLookupInterval = 0.5f;

    public static Clock Instance { get; private set; }

    [Header("Configuración")]
    public int startHour = 9;
    public float realMinutesPerGameHour = 15f;
    public bool showSeconds = false;

    [Header("UI")]
    public TMP_Text clockText;

    private float gameMinutes;
    private float nextLookupTime;

    public float CurrentGameMinutes => gameMinutes;
    public float CurrentHour => gameMinutes / 60f;
    public int CurrentDisplayHour => ToDisplayHour(Mathf.FloorToInt(CurrentHour));

    void OnEnable()
    {
        Instance = this;
        TryAssignClockText(true);
        if (gameMinutes <= 0f)
            gameMinutes = Mathf.Clamp(startHour, 0, 23) * 60f;
        UpdateClockText();
    }

    void Start()
    {
        Instance = this;
        TryAssignClockText(true);

        gameMinutes = Mathf.Clamp(startHour, 0, 23) * 60f;
        UpdateClockText();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (clockText == null)
            TryAssignClockText(false);

        if (realMinutesPerGameHour <= 0f)
            return;

        gameMinutes += Time.unscaledDeltaTime / realMinutesPerGameHour;

        float fullDayMinutes = 24f * 60f;
        if (gameMinutes >= fullDayMinutes)
            gameMinutes -= fullDayMinutes;

        UpdateClockText();
    }

    private void UpdateClockText()
    {
        if (clockText == null)
            return;

        int totalMinutes = Mathf.FloorToInt(gameMinutes);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        int displayHours = ToDisplayHour(hours);

        if (showSeconds)
        {
            int seconds = Mathf.FloorToInt((gameMinutes - totalMinutes) * 60f);
            clockText.text = $"{displayHours:00}:{minutes:00}:{seconds:00}";
        }
        else
        {
            clockText.text = $"{displayHours:00}:{minutes:00}";
        }
    }

    public static int ToDisplayHour(int absoluteHour)
    {
        int normalizedHour = ((absoluteHour % 24) + 24) % 24;
        int displayHour = normalizedHour % 12;
        return displayHour == 0 ? 12 : displayHour;
    }

    private void TryAssignClockText(bool force)
    {
        if (clockText != null)
            return;

        if (!force && Time.unscaledTime < nextLookupTime)
            return;

        nextLookupTime = Time.unscaledTime + ClockLookupInterval;

        clockText = GetComponent<TMP_Text>();
        if (clockText == null)
            clockText = GetComponentInChildren<TMP_Text>(true);

        if (clockText == null)
            clockText = GameObject.Find("Clock")?.GetComponent<TMP_Text>();

        if (clockText == null)
            clockText = FindAnyObjectByType<TMP_Text>();

        if (clockText != null)
            clockText.text = string.Empty;
    }
}
