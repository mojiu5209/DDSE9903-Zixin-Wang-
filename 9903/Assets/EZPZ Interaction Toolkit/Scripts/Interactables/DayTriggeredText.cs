using System.Collections;
using TMPro;
using UnityEngine;

public class DayTriggeredText : MonoBehaviour
{
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