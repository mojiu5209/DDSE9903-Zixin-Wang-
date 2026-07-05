using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EndingScrollSequence : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("拖入专门给结尾使用的 EndingBlackFade。")]
    [SerializeField] private Image blackFade;

    [Tooltip("拖入 Canvas 里的 EndingText。")]
    [SerializeField] private TextMeshProUGUI endingText;

    [Header("Ending Text")]
    [TextArea(5, 12)]
    [SerializeField]
    private string endingMessage =
        "He finally found his way home.\n\n" +
        "And so did I.";

    [SerializeField]
    private Vector2 startPosition =
        new Vector2(0f, -500f);

    [SerializeField]
    private Vector2 endPosition =
        new Vector2(0f, 500f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip endingVoice;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Timing")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fadeToBlackProgress = 0.7f;

    [SerializeField] private float holdBlackSeconds = 2f;

    [Header("Finished Event")]
    public UnityEvent onStoryFinished;

    private bool hasStarted;
    private Color originalBlackColor;
    private RectTransform endingTextRect;

    private void Awake()
    {
        if (blackFade != null)
        {
            originalBlackColor = blackFade.color;

            Color hiddenBlack = originalBlackColor;
            hiddenBlack.a = 0f;

            blackFade.color = hiddenBlack;
            blackFade.gameObject.SetActive(false);
        }

        if (endingText != null)
        {
            endingTextRect = endingText.rectTransform;
            endingText.text = endingMessage;
            endingText.gameObject.SetActive(false);
        }
    }

    public void PlayEndingSequence()
    {
        if (hasStarted)
        {
            return;
        }

        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        hasStarted = true;

        if (endingText == null)
        {
            Debug.LogWarning(
                "EndingScrollSequence: Ending Text 没有设置。"
            );

            yield break;
        }

        if (blackFade != null)
        {
            blackFade.gameObject.SetActive(true);

            Color transparentBlack = originalBlackColor;
            transparentBlack.a = 0f;
            blackFade.color = transparentBlack;
        }

        endingText.gameObject.SetActive(true);
        endingText.text = endingMessage;

        if (endingTextRect == null)
        {
            endingTextRect = endingText.rectTransform;
        }

        endingTextRect.anchoredPosition = startPosition;

        float sequenceDuration = 8f;

        if (endingVoice != null)
        {
            sequenceDuration = endingVoice.length;
        }

        if (audioSource != null && endingVoice != null)
        {
            audioSource.Stop();
            audioSource.clip = endingVoice;
            audioSource.volume = volume;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.Play();
        }

        float elapsed = 0f;

        while (elapsed < sequenceDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / sequenceDuration
            );

            endingTextRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                Mathf.SmoothStep(0f, 1f, progress)
            );

            if (blackFade != null)
            {
                float fadeProgress = Mathf.Clamp01(
                    progress / fadeToBlackProgress
                );

                Color blackColor = originalBlackColor;
                blackColor.a = fadeProgress;
                blackFade.color = blackColor;
            }

            yield return null;
        }

        if (blackFade != null)
        {
            Color finalBlack = originalBlackColor;
            finalBlack.a = 1f;
            blackFade.color = finalBlack;
        }

        yield return new WaitForSeconds(holdBlackSeconds);

        onStoryFinished?.Invoke();
    }
}