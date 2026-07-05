using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HospitalIntro : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera introCamera;

    [Tooltip("拖入 Main Camera 和 PlayerFollowCamera。开场时会关闭它们。")]
    [SerializeField] private Camera[] camerasToDisableAtIntro;

    [Tooltip("拖入 EZPZ Player Flat Screen WASD 根物体。")]
    [SerializeField] private GameObject ezpzPlayer;

    [Tooltip("第二天切回玩家时要打开的相机，例如 PlayerFollowCamera。")]
    [SerializeField] private Camera playerCameraToEnableAfterIntro;

    [Header("Intro Camera Positions")]
    [SerializeField] private Transform wakeLying;
    [SerializeField] private Transform wakeSitting;

    [Header("Next Day")]
    [SerializeField] private Transform nextDaySpawn;
    [SerializeField] private float nextDayTitleTime = 1.5f;
    [SerializeField] private float nextDayFadeInTime = 2f;

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private Image whiteHaze;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("把 AlbumPanel 拖进来，开场时会强制隐藏。")]
    [SerializeField] private GameObject[] uiPanelsToHideAtStart;

    [Header("Wake Up Player Voice")]
    [Tooltip("拖入 IntroManager 上的 Audio Source。")]
    [SerializeField] private AudioSource wakeVoiceSource;

    [Tooltip("拖入玩家刚醒来时的语音。")]
    [SerializeField] private AudioClip wakeVoiceClip;

    [Tooltip("画面完全睁开后，等待多久播放玩家声音。")]
    [SerializeField] private float wakeVoiceDelay = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float wakeVoiceVolume = 1f;

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

    private void Awake()
    {
        PrepareIntroState();
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private void PrepareIntroState()
    {
        HideExtraUIPanels();
        DisableOtherCameras();

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(false);
        }

        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(true);
        }

        ResetFullScreenOverlay(whiteHaze);
        ResetFullScreenOverlay(blackFade);

        if (whiteHaze != null)
        {
            whiteHaze.raycastTarget = false;
            whiteHaze.gameObject.SetActive(true);
        }

        if (blackFade != null)
        {
            blackFade.raycastTarget = false;
            blackFade.gameObject.SetActive(true);
        }

        SetImageAlpha(blackFade, 1f);
        SetImageAlpha(whiteHaze, 0f);

        if (wakeVoiceSource != null)
        {
            wakeVoiceSource.playOnAwake = false;
            wakeVoiceSource.loop = false;
            wakeVoiceSource.spatialBlend = 0f;
        }

        SetSubtitle("");
    }

    private IEnumerator PlayIntro()
    {
        if (introCamera != null && wakeLying != null)
        {
            introCamera.transform.SetPositionAndRotation(
                wakeLying.position,
                wakeLying.rotation
            );
        }

        yield return new WaitForSeconds(openingBlackScreenTime);

        // 第一次睁眼
        yield return StartCoroutine(BlinkOpen(
            0.65f,
            0.32f,
            0.18f
        ));

        yield return new WaitForSeconds(0.18f);

        yield return StartCoroutine(BlinkClose(0.12f));

        // 第二次睁眼
        yield return new WaitForSeconds(0.08f);

        yield return StartCoroutine(BlinkOpen(
            0.35f,
            0.22f,
            0.28f
        ));

        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(BlinkClose(0.10f));

        // 第三次睁眼
        yield return new WaitForSeconds(0.10f);

        yield return StartCoroutine(BlinkOpen(
            0.12f,
            0.10f,
            0.40f
        ));

        yield return new WaitForSeconds(0.40f);

        yield return StartCoroutine(BlinkClose(0.08f));

        // 第四次睁眼：画面完全清楚
        yield return new WaitForSeconds(0.12f);

        yield return StartCoroutine(BlinkOpen(
            0f,
            0f,
            0.80f
        ));

        // 玩家醒来后 1 秒播放自己的声音
        yield return new WaitForSeconds(wakeVoiceDelay);

        PlayWakeVoice();

        // 躺着看天花板 → 慢慢坐起
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

        if (playerCameraToEnableAfterIntro != null)
        {
            playerCameraToEnableAfterIntro.gameObject.SetActive(true);
        }

        yield return StartCoroutine(FadeImage(
            blackFade,
            1f,
            0f,
            nextDayFadeInTime
        ));

        SetSubtitle("");
    }

    private void PlayWakeVoice()
    {
        if (wakeVoiceSource == null || wakeVoiceClip == null)
        {
            Debug.LogWarning(
                "HospitalIntro: Wake Voice Source 或 Wake Voice Clip 没有设置。"
            );

            return;
        }

        wakeVoiceSource.Stop();
        wakeVoiceSource.clip = wakeVoiceClip;
        wakeVoiceSource.volume = wakeVoiceVolume;
        wakeVoiceSource.loop = false;
        wakeVoiceSource.spatialBlend = 0f;
        wakeVoiceSource.Play();
    }

    private void DisableOtherCameras()
    {
        if (camerasToDisableAtIntro == null)
        {
            return;
        }

        for (int i = 0; i < camerasToDisableAtIntro.Length; i++)
        {
            Camera cameraToDisable = camerasToDisableAtIntro[i];

            if (cameraToDisable == null ||
                cameraToDisable == introCamera)
            {
                continue;
            }

            cameraToDisable.gameObject.SetActive(false);
        }
    }

    private void HideExtraUIPanels()
    {
        if (uiPanelsToHideAtStart == null)
        {
            return;
        }

        for (int i = 0; i < uiPanelsToHideAtStart.Length; i++)
        {
            if (uiPanelsToHideAtStart[i] != null)
            {
                uiPanelsToHideAtStart[i].SetActive(false);
            }
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
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    timer / duration
                )
            );

            yield return null;
        }

        SetImageAlpha(image, endAlpha);
    }

    private void ResetFullScreenOverlay(Image image)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
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