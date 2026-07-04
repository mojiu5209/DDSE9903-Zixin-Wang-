using System.Collections;
using TMPro;
using UnityEngine;

public class DayTriggeredText : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("拖入 CityTimeManager 上的 CityTimeController。")]
    [SerializeField] private CityTimeController cityTimeController;

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI messageText;

    [SerializeField] private float displayDuration = 5f;

    [Header("Day 1")]
    [TextArea(2, 4)]
    [SerializeField]
    private string day1Message =
        "It's getting late. I should go home.";

    [SerializeField] private AudioClip day1Audio;

    [Header("Day 2")]
    [TextArea(2, 4)]
    [SerializeField]
    private string day2Message =
        "It is late again... Why do I keep thinking about that dog?";

    [SerializeField] private AudioClip day2Audio;

    [Header("Day 3")]
    [TextArea(2, 4)]
    [SerializeField]
    private string day3Message =
        "I cannot ignore this feeling anymore.";

    [SerializeField] private AudioClip day3Audio;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    // 给画框、相片等点击事件使用。
    // 会自动根据当前游戏天数选择对应文案与声音。
    public void ShowCurrentDay()
    {
        int currentDay = GetCurrentDayNumber();

        switch (currentDay)
        {
            case 1:
                ShowMessageAndAudio(day1Message, day1Audio);
                break;

            case 2:
                ShowMessageAndAudio(day2Message, day2Audio);
                break;

            default:
                ShowMessageAndAudio(day3Message, day3Audio);
                break;
        }
    }

    public void ShowDay1()
    {
        ShowMessageAndAudio(day1Message, day1Audio);
    }

    public void ShowDay2()
    {
        ShowMessageAndAudio(day2Message, day2Audio);
    }

    public void ShowDay3()
    {
        ShowMessageAndAudio(day3Message, day3Audio);
    }

    private int GetCurrentDayNumber()
    {
        if (cityTimeController == null)
        {
            Debug.LogWarning(
                "DayTriggeredText: City Time Controller has not been assigned. Using Day 1."
            );

            return 1;
        }

        int dayNumber = Mathf.FloorToInt(
            cityTimeController.CurrentTotalHour / 24f
        ) + 1;

        return Mathf.Clamp(dayNumber, 1, 3);
    }

    private void ShowMessageAndAudio(
        string message,
        AudioClip audioClip)
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);

            if (displayDuration > 0f)
            {
                hideRoutine = StartCoroutine(HideAfterTime());
            }
        }

        if (audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip, volume);
        }
    }

    private IEnumerator HideAfterTime()
    {
        yield return new WaitForSeconds(displayDuration);

        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }

        hideRoutine = null;
    }
}