using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightDogTransition : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject ezpzPlayer;
    [SerializeField] private Transform morningPlayerSpawn;

    [Header("Dog")]
    [SerializeField] private Transform dogTransform;
    [SerializeField] private DogRouteWalker dogRouteWalker;

    [Header("Night Route")]
    [Tooltip("0 = 第一晚，1 = 第二晚，2 = 第三晚")]
    [SerializeField] private int nightRouteIndex = 0;

    [Header("Dog Camera")]
    [SerializeField] private Camera dogViewCamera;

    [Tooltip("拖入 Head 下面的 DogCameraAnchor。不要拖 DOG 根物体。")]
    [SerializeField] private Transform dogCameraAnchor;

    [Tooltip("相机在 DogCameraAnchor 里的本地位置。")]
    [SerializeField]
    private Vector3 cameraLocalOffset =
        new Vector3(0f, 0f, 0f);

    [Tooltip("相机在 DogCameraAnchor 里的本地旋转。")]
    [SerializeField]
    private Vector3 cameraLocalRotation =
        new Vector3(0f, 0f, 0f);

    [Header("Walking Camera Motion")]
    [SerializeField] private bool enableWalkingCameraMotion = true;

    [Tooltip("走路时上下起伏。")]
    [SerializeField] private float headBobHeight = 0.03f;

    [Tooltip("走路时左右轻微晃动。")]
    [SerializeField] private float headBobSideAmount = 0.02f;

    [Tooltip("走路晃动速度。")]
    [SerializeField] private float headBobSpeed = 8f;

    [Tooltip("走路时左右东张西望角度。")]
    [SerializeField] private float lookYawAmount = 8f;

    [Tooltip("走路时上下观察角度。")]
    [SerializeField] private float lookPitchAmount = 3f;

    [Tooltip("东张西望变化速度。")]
    [SerializeField] private float lookWanderSpeed = 0.7f;

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
    private float dogViewStartTime;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

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
            UpdateDogCameraLocalMotion();
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

        AttachCameraToDogHead();

        if (dogViewCamera != null)
        {
            dogViewCamera.gameObject.SetActive(true);
        }

        dogViewStartTime = Time.time;
        dogViewActive = true;

        if (dogRouteWalker != null)
        {
            dogRouteWalker.BeginRoute(nightRouteIndex);
        }

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

        if (dogRouteWalker != null)
        {
            dogRouteWalker.StopRoute();
        }

        DetachCameraFromDogHead();

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

    private void AttachCameraToDogHead()
    {
        if (dogViewCamera == null)
        {
            return;
        }

        if (dogCameraAnchor == null)
        {
            Debug.LogWarning(
                "NightDogTransition: Dog Camera Anchor is empty. Camera will follow DOG root, not head."
            );

            dogCameraAnchor = dogTransform;
        }

        originalCameraParent = dogViewCamera.transform.parent;
        originalCameraLocalPosition =
            dogViewCamera.transform.localPosition;
        originalCameraLocalRotation =
            dogViewCamera.transform.localRotation;

        dogViewCamera.transform.SetParent(
            dogCameraAnchor,
            false
        );

        dogViewCamera.transform.localPosition =
            cameraLocalOffset;

        dogViewCamera.transform.localRotation =
            Quaternion.Euler(cameraLocalRotation);
    }

    private void DetachCameraFromDogHead()
    {
        if (dogViewCamera == null)
        {
            return;
        }

        dogViewCamera.transform.SetParent(
            originalCameraParent,
            false
        );

        dogViewCamera.transform.localPosition =
            originalCameraLocalPosition;

        dogViewCamera.transform.localRotation =
            originalCameraLocalRotation;
    }

    private void UpdateDogCameraLocalMotion()
    {
        if (dogViewCamera == null)
        {
            return;
        }

        bool dogIsWalking =
            dogRouteWalker != null &&
            dogRouteWalker.IsWalkingRoute;

        Vector3 finalLocalPosition = cameraLocalOffset;
        Vector3 finalLocalRotation = cameraLocalRotation;

        if (enableWalkingCameraMotion && dogIsWalking)
        {
            float timePassed = Time.time - dogViewStartTime;

            float bob = Mathf.Sin(timePassed * headBobSpeed);

            finalLocalPosition += new Vector3(
                bob * headBobSideAmount,
                Mathf.Abs(bob) * headBobHeight,
                0f
            );

            float yawNoise = Mathf.PerlinNoise(
                timePassed * lookWanderSpeed,
                0.15f
            );

            float pitchNoise = Mathf.PerlinNoise(
                0.75f,
                timePassed * lookWanderSpeed
            );

            float yaw =
                (yawNoise - 0.5f) *
                2f *
                lookYawAmount;

            float pitch =
                (pitchNoise - 0.5f) *
                2f *
                lookPitchAmount;

            finalLocalRotation += new Vector3(
                pitch,
                yaw,
                0f
            );
        }

        dogViewCamera.transform.localPosition =
            Vector3.Lerp(
                dogViewCamera.transform.localPosition,
                finalLocalPosition,
                Time.deltaTime * 12f
            );

        dogViewCamera.transform.localRotation =
            Quaternion.Slerp(
                dogViewCamera.transform.localRotation,
                Quaternion.Euler(finalLocalRotation),
                Time.deltaTime * 10f
            );
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