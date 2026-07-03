using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HospitalIntro : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera introCamera;

    [Tooltip("把 Hierarchy 最上面的 Main Camera 拖进来。不要拖 EZPZ Player 里面的 PlayerCamera。")]
    [SerializeField] private Camera[] camerasToDisableForIntro;

    [Tooltip("拖入 EZPZ Player Flat Screen WASD 根物体。")]
    [SerializeField] private GameObject ezpzPlayer;

    [Header("Intro Camera Positions")]
    [SerializeField] private Transform wakeLying;
    [SerializeField] private Transform wakeSitting;

    [Header("Next Day Player")]
    [SerializeField] private Transform nextDaySpawn;
    [SerializeField] private float nextDayTitleTime = 1.5f;
    [SerializeField] private float nextDayFadeInTime = 2f;

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private Image whiteHaze;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Timing")]
    [SerializeField] private float openingBlackScreenTime = 0.8f;
    [SerializeField] private float sitUpTime = 2f;

    private const string DoctorFirstLine =
        "Can you hear me? Try not to move too quickly.";

    private const string PlayerReply =
        "Who... who am I? Why am I here?";

    private const string DoctorFinalLine =
        "You were involved in a car accident. You have a mild concussion. It may be causing temporary memory loss, but physically, you are recovering well.";

    private const string InnerMonologue =
        "A car accident... Why does it feel like I have forgotten something important?";

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // 关闭会抢画面的普通 Main Camera。
        DisableOtherCameras();

        // 先关闭 EZPZ 玩家，避免 PlayerCamera 和 IntroCamera 同时显示。
        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(false);
        }

        // 开启电影镜头，初始朝向天花板。
        if (introCamera != null && wakeLying != null)
        {
            introCamera.gameObject.SetActive(true);

            introCamera.transform.SetPositionAndRotation(
                wakeLying.position,
                wakeLying.rotation
            );
        }

        SetImageAlpha(blackFade, 1f);
        SetImageAlpha(whiteHaze, 0.85f);
        SetSubtitle("");

        yield return new WaitForSeconds(openingBlackScreenTime);

        // 第一次睁眼
        yield return StartCoroutine(BlinkOpen(0.35f, 0.75f, 0.18f));
        yield return new WaitForSeconds(0.18f);
        yield return StartCoroutine(BlinkClose(0.12f));

        // 第二次睁眼
        yield return new WaitForSeconds(0.08f);
        yield return StartCoroutine(BlinkOpen(0.18f, 0.55f, 0.28f));
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(BlinkClose(0.10f));

        // 第三次睁眼
        yield return new WaitForSeconds(0.10f);
        yield return StartCoroutine(BlinkOpen(0.05f, 0.35f, 0.40f));
        yield return new WaitForSeconds(0.40f);
        yield return StartCoroutine(BlinkClose(0.08f));

        // 第四次睁眼
        yield return new WaitForSeconds(0.12f);
        yield return StartCoroutine(BlinkOpen(0f, 0.12f, 0.80f));

        yield return StartCoroutine(FadeImage(
            whiteHaze,
            0.12f,
            0f,
            1.5f
        ));

        // 真正的“躺着看天花板 → 慢慢坐起”镜头。
        if (introCamera != null &&
            wakeLying != null &&
            wakeSitting != null)
        {
            yield return StartCoroutine(MoveCamera(
                wakeLying,
                wakeSitting,
                sitUpTime
            ));
        }

        yield return StartCoroutine(ShowDialogue(
            "Doctor",
            DoctorFirstLine,
            3f
        ));

        yield return StartCoroutine(ShowDialogue(
            "You",
            PlayerReply,
            2.5f
        ));

        yield return StartCoroutine(ShowDialogue(
            "Doctor",
            DoctorFinalLine,
            5f
        ));

        yield return StartCoroutine(ShowThought(
            InnerMonologue,
            4f
        ));

        SetSubtitle("<i>My eyelids feel heavy...</i>");
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeImage(
            blackFade,
            0f,
            1f,
            2.5f
        ));

        SetSubtitle("The next morning...");
        yield return new WaitForSeconds(nextDayTitleTime);

        // 下一天切到 EZPZ 玩家视角。
        if (ezpzPlayer != null && nextDaySpawn != null)
        {
            ezpzPlayer.transform.SetPositionAndRotation(
                nextDaySpawn.position,
                nextDaySpawn.rotation
            );
        }

        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(false);
        }

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(true);
        }

        yield return null;

        yield return StartCoroutine(FadeImage(
            blackFade,
            1f,
            0f,
            nextDayFadeInTime
        ));

        SetSubtitle("");
    }

    private void DisableOtherCameras()
    {
        if (camerasToDisableForIntro == null)
        {
            return;
        }

        for (int i = 0; i < camerasToDisableForIntro.Length; i++)
        {
            Camera targetCamera = camerasToDisableForIntro[i];

            if (targetCamera == null ||
                targetCamera == introCamera)
            {
                continue;
            }

            targetCamera.gameObject.SetActive(false);
        }
    }

    private IEnumerator BlinkOpen(
        float targetBlackAlpha,
        float targetHazeAlpha,
        float duration)
    {
        float startBlack = GetImageAlpha(blackFade);
        float startHaze = GetImageAlpha(whiteHaze);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            SetImageAlpha(
                blackFade,
                Mathf.Lerp(startBlack, targetBlackAlpha, t)
            );

            SetImageAlpha(
                whiteHaze,
                Mathf.Lerp(startHaze, targetHazeAlpha, t)
            );

            yield return null;
        }

        SetImageAlpha(blackFade, targetBlackAlpha);
        SetImageAlpha(whiteHaze, targetHazeAlpha);
    }

    private IEnumerator BlinkClose(float duration)
    {
        float startBlack = GetImageAlpha(blackFade);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            SetImageAlpha(
                blackFade,
                Mathf.Lerp(startBlack, 1f, timer / duration)
            );

            yield return null;
        }

        SetImageAlpha(blackFade, 1f);
    }

    private IEnumerator MoveCamera(
        Transform startPoint,
        Transform endPoint,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                timer / duration
            );

            Vector3 position = Vector3.Lerp(
                startPoint.position,
                endPoint.position,
                t
            );

            Quaternion rotation = Quaternion.Slerp(
                startPoint.rotation,
                endPoint.rotation,
                t
            );

            // 坐起时极轻微的晕眩。
            position += new Vector3(
                Mathf.Sin(timer * 9f) * 0.008f,
                Mathf.Cos(timer * 7f) * 0.005f,
                0f
            );

            introCamera.transform.SetPositionAndRotation(
                position,
                rotation
            );

            yield return null;
        }

        introCamera.transform.SetPositionAndRotation(
            endPoint.position,
            endPoint.rotation
        );
    }

    private IEnumerator ShowDialogue(
        string speaker,
        string line,
        float duration)
    {
        SetSubtitle("<b>" + speaker + ":</b> " + line);
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator ShowThought(
        string line,
        float duration)
    {
        SetSubtitle("<i>" + line + "</i>");
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator FadeImage(
        Image image,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            SetImageAlpha(
                image,
                Mathf.Lerp(startAlpha, endAlpha, timer / duration)
            );

            yield return null;
        }

        SetImageAlpha(image, endAlpha);
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.text = text;
        }
    }

    private float GetImageAlpha(Image image)
    {
        return image != null ? image.color.a : 0f;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color colour = image.color;
        colour.a = Mathf.Clamp01(alpha);
        image.color = colour;
    }
}