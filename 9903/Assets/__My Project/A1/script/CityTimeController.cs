using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TimedEvent
{
    public string eventName = "New Timed Event";

    [Tooltip("例如：20 = 第一天晚上 8 点；44 = 第二天晚上 8 点。")]
    public float triggerAtTotalHour = 20f;

    public UnityEvent onTriggered;

    [HideInInspector]
    public bool hasTriggered;
}

public class CityTimeController : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("0 = 第一天 00:00；8 = 第一天 08:00；24 = 第二天 00:00。")]
    [SerializeField] private float startTotalHour = 8f;

    [Tooltip("现实 1 秒等于多少游戏小时。测试时可以填 1。")]
    [SerializeField] private float gameHoursPerRealSecond = 0.1f;

    [Header("Only Two HDRs")]
    [Tooltip("拖入白天 HDR 全景贴图。")]
    [SerializeField] private Texture2D dayHDR;

    [Tooltip("拖入夜晚 HDR 全景贴图。")]
    [SerializeField] private Texture2D nightHDR;

    [Range(0f, 360f)]
    [SerializeField] private float skyRotation = 0f;

    [Tooltip("天空上下颠倒时勾选。")]
    [SerializeField] private bool flipSkyVertical = false;

    [Header("Day / Night Brightness")]
    [Range(0f, 3f)]
    [SerializeField] private float daySkyExposure = 1f;

    [Range(0f, 3f)]
    [SerializeField] private float nightSkyExposure = 0.1f;

    [Tooltip("拖入 Hierarchy 里的 Directional Light。")]
    [SerializeField] private Light directionalLight;

    [Range(0f, 5f)]
    [SerializeField] private float dayLightIntensity = 1f;

    [Range(0f, 5f)]
    [SerializeField] private float nightLightIntensity = 0.03f;

    [Range(0f, 3f)]
    [SerializeField] private float dayAmbientIntensity = 1f;

    [Range(0f, 3f)]
    [SerializeField] private float nightAmbientIntensity = 0.12f;

    [Header("Smooth Transition")]
    [Tooltip("从夜晚开始渐变成白天的时间。")]
    [Range(0f, 23f)]
    [SerializeField] private float sunriseHour = 6f;

    [Tooltip("从白天开始渐变成夜晚的时间。")]
    [Range(0f, 23f)]
    [SerializeField] private float sunsetHour = 18f;

    [Tooltip("每次渐变持续多少游戏小时。建议 1.5 - 3。")]
    [Range(0.1f, 8f)]
    [SerializeField] private float transitionDurationHours = 2f;

    [Header("Timed Events")]
    [SerializeField] private TimedEvent[] timedEvents;

    public float CurrentTotalHour
    {
        get { return currentTotalHour; }
    }

    public int CurrentDayNumber
    {
        get
        {
            return Mathf.FloorToInt(
                currentTotalHour / 24f
            ) + 1;
        }
    }

    private float currentTotalHour;
    private Material runtimeSkybox;
    private float lastEnvironmentUpdateTime;

    private static readonly int DayTextureID =
        Shader.PropertyToID("_DayTex");

    private static readonly int NightTextureID =
        Shader.PropertyToID("_NightTex");

    private static readonly int BlendID =
        Shader.PropertyToID("_Blend");

    private static readonly int ExposureID =
        Shader.PropertyToID("_Exposure");

    private static readonly int RotationID =
        Shader.PropertyToID("_Rotation");

    private static readonly int FlipYID =
        Shader.PropertyToID("_FlipY");

    private void Awake()
    {
        currentTotalHour = startTotalHour;

        SetupSkybox();
        PrepareTimedEvents();
    }

    private void Update()
    {
        currentTotalHour +=
            gameHoursPerRealSecond *
            Time.deltaTime;

        UpdateSkybox();
        CheckTimedEvents();
    }

    private void SetupSkybox()
    {
        Shader blendShader = Shader.Find(
            "Skybox/DayNightPanoramicBlend"
        );

        if (blendShader == null)
        {
            Debug.LogError(
                "Cannot find shader: Skybox/DayNightPanoramicBlend"
            );

            enabled = false;
            return;
        }

        runtimeSkybox = new Material(blendShader);

        runtimeSkybox.SetTexture(
            DayTextureID,
            dayHDR
        );

        runtimeSkybox.SetTexture(
            NightTextureID,
            nightHDR
        );

        runtimeSkybox.SetFloat(
            RotationID,
            skyRotation
        );

        runtimeSkybox.SetFloat(
            FlipYID,
            flipSkyVertical ? 1f : 0f
        );

        RenderSettings.skybox = runtimeSkybox;

        UpdateSkybox();
    }

    private void UpdateSkybox()
    {
        if (runtimeSkybox == null)
        {
            return;
        }

        float hourOfDay = Mathf.Repeat(
            currentTotalHour,
            24f
        );

        float dayBlend = GetDayBlend(hourOfDay);

        float currentExposure = Mathf.Lerp(
            nightSkyExposure,
            daySkyExposure,
            dayBlend
        );

        runtimeSkybox.SetFloat(
            BlendID,
            dayBlend
        );

        runtimeSkybox.SetFloat(
            ExposureID,
            currentExposure
        );

        runtimeSkybox.SetFloat(
            RotationID,
            skyRotation
        );

        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(
                nightLightIntensity,
                dayLightIntensity,
                dayBlend
            );
        }

        RenderSettings.ambientIntensity = Mathf.Lerp(
            nightAmbientIntensity,
            dayAmbientIntensity,
            dayBlend
        );

        if (Time.time - lastEnvironmentUpdateTime > 2f)
        {
            DynamicGI.UpdateEnvironment();
            lastEnvironmentUpdateTime = Time.time;
        }
    }

    private float GetDayBlend(float hourOfDay)
    {
        float sunriseEnd =
            sunriseHour + transitionDurationHours;

        float sunsetEnd =
            sunsetHour + transitionDurationHours;

        // 06:00 到 08:00：夜晚渐变到白天
        if (hourOfDay >= sunriseHour &&
            hourOfDay < sunriseEnd)
        {
            float progress = Mathf.InverseLerp(
                sunriseHour,
                sunriseEnd,
                hourOfDay
            );

            return Mathf.SmoothStep(
                0f,
                1f,
                progress
            );
        }

        // 白天
        if (hourOfDay >= sunriseEnd &&
            hourOfDay < sunsetHour)
        {
            return 1f;
        }

        // 18:00 到 20:00：白天渐变到夜晚
        if (hourOfDay >= sunsetHour &&
            hourOfDay < sunsetEnd)
        {
            float progress = Mathf.InverseLerp(
                sunsetHour,
                sunsetEnd,
                hourOfDay
            );

            return Mathf.SmoothStep(
                1f,
                0f,
                progress
            );
        }

        // 夜晚
        return 0f;
    }

    private void PrepareTimedEvents()
    {
        if (timedEvents == null)
        {
            return;
        }

        for (int i = 0; i < timedEvents.Length; i++)
        {
            if (timedEvents[i] == null)
            {
                continue;
            }

            timedEvents[i].hasTriggered =
                timedEvents[i].triggerAtTotalHour <
                currentTotalHour;
        }
    }

    private void CheckTimedEvents()
    {
        if (timedEvents == null)
        {
            return;
        }

        for (int i = 0; i < timedEvents.Length; i++)
        {
            TimedEvent timedEvent = timedEvents[i];

            if (timedEvent == null ||
                timedEvent.hasTriggered)
            {
                continue;
            }

            if (currentTotalHour >=
                timedEvent.triggerAtTotalHour)
            {
                timedEvent.hasTriggered = true;

                timedEvent.onTriggered?.Invoke();
            }
        }
    }

    public void ResetTimedEvents()
    {
        if (timedEvents == null)
        {
            return;
        }

        for (int i = 0; i < timedEvents.Length; i++)
        {
            if (timedEvents[i] != null)
            {
                timedEvents[i].hasTriggered = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (runtimeSkybox == null)
        {
            return;
        }

        if (RenderSettings.skybox == runtimeSkybox)
        {
            RenderSettings.skybox = null;
        }

        Destroy(runtimeSkybox);
    }
}