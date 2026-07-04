using System.Collections;
using TMPro;
using UnityEngine;

public class DayTriggeredText : MonoBehaviour
{
    [Header("Text UI")]
    [Tooltip("拖入 Canvas 里的 TextMeshProUGUI 文字。")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("每次文字显示多久。设为 0 表示不自动隐藏。")]
    [SerializeField] private float displayDuration = 3f;

    [Header("Day Messages")]
    [TextArea(2, 4)]
    [SerializeField]
    private string day1Message =
        "It's getting late. I should go home.";

    [TextArea(2, 4)]
    [SerializeField]
    private string day2Message =
        "It is late again... Why do I keep thinking about that dog?";

    [TextArea(2, 4)]
    [SerializeField]
    private string day3Message =
        "I cannot ignore this feeling anymore.";

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    // 第一天触发
    public void ShowDay1Text()
    {
        ShowMessage(day1Message);
    }

    // 第二天触发
    public void ShowDay2Text()
    {
        ShowMessage(day2Message);
    }

    // 第三天触发
    public void ShowDay3Text()
    {
        ShowMessage(day3Message);
    }

    // 你也可以在其他 Unity Event 中直接调用这个
    public void HideText()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void ShowMessage(string newMessage)
    {
        if (messageText == null)
        {
            Debug.LogWarning(
                "DayTriggeredText: Message Text has not been assigned."
            );
            return;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        messageText.text = newMessage;
        messageText.gameObject.SetActive(true);

        if (displayDuration > 0f)
        {
            hideRoutine = StartCoroutine(HideAfterTime());
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