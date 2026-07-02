using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HospitalIntro : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera introCamera;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject playerObject;

    [Header("Camera Positions")]
    [SerializeField] private Transform wakeLying;
    [SerializeField] private Transform wakeSitting;
    [SerializeField] private Transform wakeStanding;

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private Image whiteHaze;

    [Header("Hospital Audio")]
    [SerializeField] private AudioSource hospitalAmbience;
    [SerializeField] private AudioSource heartMonitor;
    [SerializeField] private AudioSource tinnitus;

    [Header("Timing")]
    [SerializeField] private float openingBlackScreenTime = 0.8f;
    [SerializeField] private float sitUpTime = 2f;
    [SerializeField] private float standUpTime = 1.5f;

    [Header("Tinnitus Settings")]
    [SerializeField] private float tinnitusBaseVolume = 0.08f;
    [SerializeField] private float tinnitusVariation = 0.035f;
    [SerializeField] private float tinnitusWaveSpeed = 1.5f;
    [SerializeField] private float tinnitusPulseFadeSpeed = 0.28f;

    private float tinnitusPulse;
    private Coroutine tinnitusRoutine;

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // 先关闭真实玩家，避免玩家相机抢画面
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        // 开启开场相机，并放到躺下位置
        if (introCamera != null && wakeLying != null)
        {
            introCamera.gameObject.SetActive(true);
            introCamera.transform.SetPositionAndRotation(
                wakeLying.position,
                wakeLying.rotation
            );
        }

        // 初始状态：全黑，画面很模糊
        SetImageAlpha(blackFade, 1f);
        SetImageAlpha(whiteHaze, 0.85f);

        // 播放医院声音
        if (hospitalAmbience != null)
        {
            hospitalAmbience.Play();
        }

        if (heartMonitor != null)
        {
            heartMonitor.Play();
        }

        StartTinnitus();

        yield return new WaitForSeconds(openingBlackScreenTime);

        // 第一次睁眼：很短、很模糊
        PulseTinnitus(0.18f);
        yield return StartCoroutine(BlinkOpen(
            targetBlackAlpha: 0.35f,
            targetHazeAlpha: 0.75f,
            duration: 0.18f
        ));

        yield return new WaitForSeconds(0.18f);

        yield return StartCoroutine(BlinkClose(0.12f));

        // 第二次睁眼：稍微清楚一点
        yield return new WaitForSeconds(0.08f);

        PulseTinnitus(0.13f);
        yield return StartCoroutine(BlinkOpen(
            targetBlackAlpha: 0.18f,
            targetHazeAlpha: 0.55f,
            duration: 0.28f
        ));

        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(BlinkClose(0.10f));

        // 第三次睁眼：开始能看见医院轮廓
        yield return new WaitForSeconds(0.10f);

        PulseTinnitus(0.08f);
        yield return StartCoroutine(BlinkOpen(
            targetBlackAlpha: 0.05f,
            targetHazeAlpha: 0.35f,
            duration: 0.40f
        ));

        yield return new WaitForSeconds(0.40f);

        yield return StartCoroutine(BlinkClose(0.08f));

        // 第四次睁眼：基本恢复
        yield return new WaitForSeconds(0.12f);

        PulseTinnitus(0.04f);
        yield return StartCoroutine(BlinkOpen(
            targetBlackAlpha: 0f,
            targetHazeAlpha: 0.12f,
            duration: 0.80f
        ));

        // 最后一点模糊感慢慢消失
        yield return StartCoroutine(FadeImage(
            whiteHaze,
            0.12f,
            0f,
            1.6f
        ));

        // 从躺着慢慢坐起
        if (wakeLying != null && wakeSitting != null)
        {
            yield return StartCoroutine(MoveCamera(
                wakeLying,
                wakeSitting,
                sitUpTime,
                true
            ));
        }

        // 再慢慢站起来
        if (wakeSitting != null && wakeStanding != null)
        {
            yield return StartCoroutine(MoveCamera(
                wakeSitting,
                wakeStanding,
                standUpTime,
                false
            ));
        }

        // 耳鸣渐弱后停止
        yield return StartCoroutine(FadeOutTinnitus(1.2f));

        // 切回玩家
        if (playerObject != null)
        {
            playerObject.SetActive(true);
        }

        if (playerCamera != null && wakeStanding != null)
        {
            playerCamera.transform.SetPositionAndRotation(
                wakeStanding.position,
                wakeStanding.rotation
            );
        }

        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(false);
        }
    }

    // 睁眼：黑幕减少，白雾减少
    private IEnumerator BlinkOpen(
        float targetBlackAlpha,
        float targetHazeAlpha,
        float duration)
    {
        float startBlack = blackFade.color.a;
        float startHaze = whiteHaze.color.a;

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

    // 闭眼：黑幕快速回到全黑
    private IEnumerator BlinkClose(float duration)
    {
        float startBlack = blackFade.color.a;

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

    // 镜头躺下 → 坐起 / 坐起 → 站立
    private IEnumerator MoveCamera(
        Transform startPoint,
        Transform endPoint,
        float duration,
        bool addDizziness)
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

            // 坐起时有非常轻微的头晕偏移
            if (addDizziness)
            {
                position += new Vector3(
                    Mathf.Sin(timer * 10f) * 0.012f,
                    Mathf.Cos(timer * 8f) * 0.008f,
                    0f
                );
            }

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

    // ---------- Tinnitus ----------

    private void StartTinnitus()
    {
        if (tinnitus == null || tinnitus.clip == null)
        {
            return;
        }

        tinnitus.loop = true;
        tinnitus.volume = 0f;

        if (!tinnitus.isPlaying)
        {
            tinnitus.Play();
        }

        if (tinnitusRoutine != null)
        {
            StopCoroutine(tinnitusRoutine);
        }

        tinnitusRoutine = StartCoroutine(TinnitusWave());
    }

    private IEnumerator TinnitusWave()
    {
        while (true)
        {
            // PerlinNoise 会产生比较自然、平滑的音量起伏
            float noise = Mathf.PerlinNoise(
                Time.time * tinnitusWaveSpeed,
                0.5f
            );

            float wave = (noise - 0.5f) * 2f;

            float targetVolume =
                tinnitusBaseVolume +
                wave * tinnitusVariation +
                tinnitusPulse;

            tinnitus.volume = Mathf.Lerp(
                tinnitus.volume,
                Mathf.Clamp01(targetVolume),
                Time.deltaTime * 4f
            );

            // 每次睁眼后的突然耳鸣会慢慢退回基础音量
            tinnitusPulse = Mathf.MoveTowards(
                tinnitusPulse,
                0f,
                tinnitusPulseFadeSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    private void PulseTinnitus(float strength)
    {
        tinnitusPulse = Mathf.Max(tinnitusPulse, strength);
    }

    private IEnumerator FadeOutTinnitus(float duration)
    {
        if (tinnitus == null)
        {
            yield break;
        }

        float startVolume = tinnitus.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            tinnitus.volume = Mathf.Lerp(
                startVolume,
                0f,
                timer / duration
            );

            yield return null;
        }

        tinnitus.Stop();

        if (tinnitusRoutine != null)
        {
            StopCoroutine(tinnitusRoutine);
            tinnitusRoutine = null;
        }
    }
}