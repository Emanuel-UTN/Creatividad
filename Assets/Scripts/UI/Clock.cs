using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public static Clock Instance { get; private set; }

    [Header("UI")]
    public TMP_Text clockText;
    private float timeRemaining;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        if (timeRemaining <= 0)
            gameObject.SetActive(false);
        else
        {
            timeRemaining -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            clockText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartClock(float timeToOpen)
    {
        timeRemaining = timeToOpen;
        gameObject.SetActive(true);
    }



}
