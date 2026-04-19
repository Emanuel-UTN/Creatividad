using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float ClockLookupInterval = 0.5f;

    [Header("Configuración")]
    public int startHour = 9;
    public float realMinutesPerGameHour = 15f;
    public bool showSeconds = false;

    [Header("UI")]
    public TMP_Text clockText;

    private float gameMinutes;
    private float nextLookupTime;

    void OnEnable()
    {
        TryAssignClockText(true);
        if (gameMinutes <= 0f)
            gameMinutes = Mathf.Clamp(startHour, 0, 23) * 60f;
        UpdateClockText();
    }

    void Start()
    {
        TryAssignClockText(true);

        gameMinutes = Mathf.Clamp(startHour, 0, 23) * 60f;
        UpdateClockText();
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

        if (showSeconds)
        {
            int seconds = Mathf.FloorToInt((gameMinutes - totalMinutes) * 60f);
            clockText.text = $"{hours:00}:{minutes:00}:{seconds:00}";
        }
        else
        {
            clockText.text = $"{hours:00}:{minutes:00}";
        }
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
