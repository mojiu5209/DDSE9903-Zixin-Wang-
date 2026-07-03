using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightDogTransition : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject ezpzPlayer;
    [SerializeField] private Transform morningPlayerSpawn;

    [Header("Dog Cinematic")]
    [SerializeField] private Camera dogViewCamera;
    [SerializeField] private Transform dogTransform;

    [Tooltip("拖入挂在 DOG 根物体上的 DogRouteWalker。")]
    [SerializeField] private DogRouteWalker dogRouteWalker;

    [SerializeField]
    private Vector3 cameraLocalOffset =
        new Vector3(0f, 0.45f, 0.15f);

    [SerializeField]
    private Vector3 cameraLocalRotation =
        new Vector3(5f, 0f, 0f);

    [SerializeField] private float cameraFollowSpeed = 12f;

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Timing")]
    [SerializeField] private float warningDuration = 2.5f;
    [SerializeField] private float fadeToBlackDuration = 1.5f;
    [SerializeField] private float fadeIntoDogViewDuration = 1.2f;
    [SerializeField] private float morningFadeToBlackDuration = 1.5f;
    [SerializeField] private float morningFadeIntoPlayerDuration = 1.5f;

    [Header("Text")]
    [TextArea(2, 3)]
    [SerializeField]
    private string sleepWarning =
        "It's too late. I should get some sleep.";

    [TextArea(2, 3)]
    [SerializeField]
    private string morningText =
        "The next morning...";

    private bool dogViewActive;
    private bool hasStartedNight;
    private bool hasReturnedToPlayer;

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

    public void StartNightDogView()
    {
        if (hasStartedNight)
        {
            return;
        }

        StartCoroutine(NightDogRoutine());
    }

    public void ReturnToPlayerView()
    {
        if (!hasStartedNight || hasReturnedToPlayer)
        {
            return;
        }

        StartCoroutine(ReturnToPlayerRoutine());
    }

    private IEnumerator NightDogRoutine()
    {
        hasStartedNight = true;

        SetSubtitle("<i>" + sleepWarning + "</i>");

        yield return new WaitForSeconds(warningDuration);

        SetSubtitle("");

        yield return StartCoroutine(FadeBlack(
            0f,
            1f,
            fadeToBlackDuration
        ));

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(false);
        }

        if (dogViewCamera != null)
        {
            dogViewCamera.gameObject.SetActive(true);
        }

        SetDogCameraInstant();

        // 黑屏切到狗视角后，让狗开始走路线。
        if (dogRouteWalker != null)
        {
            dogRouteWalker.BeginRoute();
        }
        else
        {
            Debug.LogWarning(
                "NightDogTransition: Dog Route Walker is not assigned."
            );
        }

        dogViewActive = true;

        yield return null;

        yield return StartCoroutine(FadeBlack(
            1f,
            0f,
            fadeIntoDogViewDuration
        ));
    }

    private IEnumerator ReturnToPlayerRoutine()
    {
        hasReturnedToPlayer = true;
        dogViewActive = false;

        SetSubtitle("<i>" + morningText + "</i>");

        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(FadeBlack(
            0f,
            1f,
            morningFadeToBlackDuration
        ));

        // 早上回玩家前停止狗路线。
        if (dogRouteWalker != null)
        {
            dogRouteWalker.StopRoute();
        }

        if (dogViewCamera != null)
        {
            dogViewCamera.gameObject.SetActive(false);
        }

        if (ezpzPlayer != null && morningPlayerSpawn != null)
        {
            ezpzPlayer.transform.SetPositionAndRotation(
                morningPlayerSpawn.position,
                morningPlayerSpawn.rotation
            );
        }

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(true);
        }

        yield return null;

        yield return StartCoroutine(FadeBlack(
            1f,
            0f,
            morningFadeIntoPlayerDuration
        ));

        SetSubtitle("");
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