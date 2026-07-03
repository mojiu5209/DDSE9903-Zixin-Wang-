using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightDogTransition : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("拖入 EZPZ Player Flat Screen WASD 根物体。")]
    [SerializeField] private GameObject ezpzPlayer;

    [Header("Dog Cinematic")]
    [Tooltip("小狗过场专用相机，平时关闭。")]
    [SerializeField] private Camera dogViewCamera;

    [Tooltip("拖入小狗根物体。")]
    [SerializeField] private Transform dogTransform;

    [Tooltip("狗眼睛附近的位置偏移。")]
    [SerializeField]
    private Vector3 cameraLocalOffset =
        new Vector3(0f, 0.45f, 0.15f);

    [Tooltip("相机相对狗头的旋转偏移。")]
    [SerializeField]
    private Vector3 cameraLocalRotation =
        new Vector3(5f, 0f, 0f);

    [SerializeField] private float cameraFollowSpeed = 12f;

    [Header("Dog Animation")]
    [Tooltip("可选。拖入狗的 Animator。")]
    [SerializeField] private Animator dogAnimator;

    [Tooltip("例如 DogNight1。没有动画 Trigger 可留空。")]
    [SerializeField] private string dogAnimationTrigger = "DogNight1";

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Timing")]
    [SerializeField] private float warningDuration = 2.5f;
    [SerializeField] private float fadeToBlackDuration = 1.5f;
    [SerializeField] private float fadeIntoDogViewDuration = 1.2f;

    [Header("Text")]
    [TextArea(2, 3)]
    [SerializeField]
    private string sleepWarning =
        "It's too late. I should get some sleep.";

    private bool dogViewActive;
    private bool hasStarted;

    private void Awake()
    {
        if (dogViewCamera != null)
        {
            dogViewCamera.gameObject.SetActive(false);
        }

        SetFadeAlpha(0f);
        SetSubtitle("");
    }

    private void LateUpdate()
    {
        if (dogViewActive)
        {
            FollowDogCamera();
        }
    }

    // 把这个方法放进 CityTimeController 的 Timed Event。
    public void StartNightDogView()
    {
        if (hasStarted)
        {
            return;
        }

        StartCoroutine(NightDogRoutine());
    }

    private IEnumerator NightDogRoutine()
    {
        hasStarted = true;

        SetSubtitle("<i>" + sleepWarning + "</i>");

        yield return new WaitForSeconds(warningDuration);

        SetSubtitle("");

        yield return StartCoroutine(FadeBlack(
            0f,
            1f,
            fadeToBlackDuration
        ));

        // 黑屏后关掉玩家，确保不能移动。
        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(false);
        }

        // 开启狗相机。
        if (dogViewCamera != null)
        {
            dogViewCamera.gameObject.SetActive(true);
        }

        // 从狗头位置开始。
        SetDogCameraInstant();

        // 触发狗狗移动动画。
        if (dogAnimator != null &&
            !string.IsNullOrEmpty(dogAnimationTrigger))
        {
            dogAnimator.SetTrigger(dogAnimationTrigger);
        }

        dogViewActive = true;

        yield return null;

        yield return StartCoroutine(FadeBlack(
            1f,
            0f,
            fadeIntoDogViewDuration
        ));
    }

    private void FollowDogCamera()
    {
        if (dogViewCamera == null || dogTransform == null)
        {
            return;
        }

        Vector3 targetPosition = dogTransform.TransformPoint(
            cameraLocalOffset
        );

        Quaternion targetRotation =
            dogTransform.rotation *
            Quaternion.Euler(cameraLocalRotation);

        float followAmount =
            cameraFollowSpeed * Time.deltaTime;

        dogViewCamera.transform.position = Vector3.Lerp(
            dogViewCamera.transform.position,
            targetPosition,
            followAmount
        );

        dogViewCamera.transform.rotation = Quaternion.Slerp(
            dogViewCamera.transform.rotation,
            targetRotation,
            followAmount
        );
    }

    private void SetDogCameraInstant()
    {
        if (dogViewCamera == null || dogTransform == null)
        {
            return;
        }

        dogViewCamera.transform.position =
            dogTransform.TransformPoint(cameraLocalOffset);

        dogViewCamera.transform.rotation =
            dogTransform.rotation *
            Quaternion.Euler(cameraLocalRotation);
    }

    private IEnumerator FadeBlack(
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (blackFade == null)
        {
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            SetFadeAlpha(
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    timer / duration
                )
            );

            yield return null;
        }

        SetFadeAlpha(toAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (blackFade == null)
        {
            return;
        }

        Color colour = blackFade.color;
        colour.a = Mathf.Clamp01(alpha);
        blackFade.color = colour;
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.text = text;
        }
    }
}