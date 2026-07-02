using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

[System.Serializable]
public class SkyTimeKey
{
    [Header("Time of Day")]
    [Range(0f, 24f)]
    public float startHourOfDay = 0f;

    [Header("HDR Skybox")]
    public Material skyboxMaterial;

    [Range(0.01f, 8f)]
    public float skyExposure = 1f;

    public Color skyTint = Color.white;

    [Range(0f, 360f)]
    public float skyRotation = 0f;

    [Header("Directional Light")]
    public Color lightColor = Color.white;

    [Min(0f)]
    public float lightIntensity = 1f;

    public Vector3 lightEulerAngles = new Vector3(45f, -30f, 0f);

    [Header("Ambient Light")]
    public Color ambientSky = new Color(0.45f, 0.5f, 0.6f);

    public Color ambientEquator = new Color(0.35f, 0.35f, 0.4f);

    public Color ambientGround = new Color(0.2f, 0.2f, 0.2f);
}

[System.Serializable]
public class TimedEvent
{
    [Header("Event")]
    public string eventName = "Story Event";

    [Tooltip("总游戏小时。例如：第 1 天晚上 20 点填 20；第 2 天晚上 20 点填 44。")]
    [Min(0f)]
    public float triggerAtTotalHour = 20f;

    [Tooltip("到达指定时间时触发。这里不包含任何狗或玩家控制逻辑。")]
    public UnityEvent onTriggered;
}

public class CityTimeController : MonoBehaviour
{
    [Header("Clock")]
    [SerializeField] private bool startTimeOnPlay = true;

    [Tooltip("从第几个总游戏小时开始。18 = 第一天傍晚 6 点。")]
    [Min(0f)]
    [SerializeField] private float startTotalHour = 18f;

    [Tooltip("1 = 现实 1 秒经过游戏 1 小时。测试时可填 1。")]
    [Min(0.01f)]
    [SerializeField] private float gameHoursPerRealSecond = 0.5f;

    [Tooltip("-1 = 永不自动停止。72 = 第三天结束时停止。")]
    [SerializeField] private float stopAtTotalHour = -1f;

    [Header("Sky and Lighting")]
    [SerializeField] private Light directionalLight;

    [Tooltip("必须按照 Start Hour Of Day 从小到大排列。")]
    [SerializeField]
    private List<SkyTimeKey> skyKeys =
        new List<SkyTimeKey>();

    [Tooltip("切换到新的 HDR Skybox 时更新环境光反射。")]
    [SerializeField] private bool updateEnvironmentOnSkyChange = true;

    [Header("Timed Events")]
    [SerializeField]
    private List<TimedEvent> timedEvents =
        new List<TimedEvent>();

    public float CurrentTotalHour => currentTotalHour;

    public float CurrentHourOfDay
    {
        get { return Mathf.Repeat(currentTotalHour, 24f); }
    }

    public bool IsTimeRunning => timeRunning;

    private readonly List<Material> runtimeSkyboxes =
        new List<Material>();

    private bool[] eventAlreadyTriggered;

    private float currentTotalHour;
    private bool timeRunning;
    private int activeSkyIndex = -1;

    private void Awake()
    {
        currentTotalHour = startTotalHour;

        CreateRuntimeSkyboxes();

        eventAlreadyTriggered = new bool[timedEvents.Count];

        // 开始时间之前的事件视为已经发生，不会在游戏一开始全部触发
        for (int i = 0; i < timedEvents.Count; i++)
        {
            if (timedEvents[i].triggerAtTotalHour <= startTotalHour)
            {
                eventAlreadyTriggered[i] = true;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
    }

    private void Start()
    {
        UpdateSkyAndLighting();

        if (startTimeOnPlay)
        {
            StartTime();
        }
    }

    private void Update()
    {
        if (!timeRunning)
        {
            return;
        }

        float previousTotalHour = currentTotalHour;

        currentTotalHour +=
            gameHoursPerRealSecond * Time.deltaTime;

        if (stopAtTotalHour >= 0f &&
            currentTotalHour >= stopAtTotalHour)
        {
            currentTotalHour = stopAtTotalHour;
            timeRunning = false;
        }

        UpdateSkyAndLighting();

        CheckTimedEvents(
            previousTotalHour,
            currentTotalHour
        );
    }

    private void OnDestroy()
    {
        for (int i = 0; i < runtimeSkyboxes.Count; i++)
        {
            if (runtimeSkyboxes[i] != null)
            {
                Destroy(runtimeSkyboxes[i]);
            }
        }
    }

    public void StartTime()
    {
        timeRunning = true;
    }

    public void PauseTime()
    {
        timeRunning = false;
    }

    public void SetTime(float newTotalHour)
    {
        currentTotalHour = Mathf.Max(0f, newTotalHour);
        UpdateSkyAndLighting();
    }

    public void SetTimeAndTriggerPassedEvents(float newTotalHour)
    {
        float oldTime = currentTotalHour;

        currentTotalHour = Mathf.Max(0f, newTotalHour);

        UpdateSkyAndLighting();

        CheckTimedEvents(
            oldTime,
            currentTotalHour
        );
    }

    private void UpdateSkyAndLighting()
    {
        if (skyKeys == null || skyKeys.Count == 0)
        {
            return;
        }

        int currentIndex = GetCurrentSkyIndex();
        int nextIndex = (currentIndex + 1) % skyKeys.Count;

        SkyTimeKey currentKey = skyKeys[currentIndex];
        SkyTimeKey nextKey = skyKeys[nextIndex];

        float hourOfDay = CurrentHourOfDay;

        float segmentLength = Mathf.Repeat(
            nextKey.startHourOfDay -
            currentKey.startHourOfDay,
            24f
        );

        if (segmentLength < 0.01f)
        {
            segmentLength = 24f;
        }

        float hoursIntoSegment = Mathf.Repeat(
            hourOfDay - currentKey.startHourOfDay,
            24f
        );

        float blend = Mathf.Clamp01(
            hoursIntoSegment / segmentLength
        );

        if (activeSkyIndex != currentIndex)
        {
            activeSkyIndex = currentIndex;
            SetSkyboxMaterial(currentIndex);

            if (updateEnvironmentOnSkyChange)
            {
                DynamicGI.UpdateEnvironment();
            }
        }

        Material activeSkybox = GetRuntimeSkybox(currentIndex);

        ApplySkyboxProperties(
            activeSkybox,
            Mathf.LerpAngle(
                currentKey.skyRotation,
                nextKey.skyRotation,
                blend
            ),
            Mathf.Lerp(
                currentKey.skyExposure,
                nextKey.skyExposure,
                blend
            ),
            Color.Lerp(
                currentKey.skyTint,
                nextKey.skyTint,
                blend
            )
        );

        RenderSettings.ambientMode = AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = Color.Lerp(
            currentKey.ambientSky,
            nextKey.ambientSky,
            blend
        );

        RenderSettings.ambientEquatorColor = Color.Lerp(
            currentKey.ambientEquator,
            nextKey.ambientEquator,
            blend
        );

        RenderSettings.ambientGroundColor = Color.Lerp(
            currentKey.ambientGround,
            nextKey.ambientGround,
            blend
        );

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(
                currentKey.lightColor,
                nextKey.lightColor,
                blend
            );

            directionalLight.intensity = Mathf.Lerp(
                currentKey.lightIntensity,
                nextKey.lightIntensity,
                blend
            );

            directionalLight.transform.rotation =
                Quaternion.Slerp(
                    Quaternion.Euler(
                        currentKey.lightEulerAngles
                    ),
                    Quaternion.Euler(
                        nextKey.lightEulerAngles
                    ),
                    blend
                );
        }
    }

    private int GetCurrentSkyIndex()
    {
        float hourOfDay = CurrentHourOfDay;

        int selectedIndex = skyKeys.Count - 1;

        for (int i = 0; i < skyKeys.Count; i++)
        {
            if (hourOfDay >= skyKeys[i].startHourOfDay)
            {
                selectedIndex = i;
            }
            else
            {
                break;
            }
        }

        return selectedIndex;
    }

    private void CheckTimedEvents(
        float previousTime,
        float currentTime)
    {
        for (int i = 0; i < timedEvents.Count; i++)
        {
            if (eventAlreadyTriggered[i])
            {
                continue;
            }

            float triggerTime =
                timedEvents[i].triggerAtTotalHour;

            if (previousTime < triggerTime &&
                currentTime >= triggerTime)
            {
                eventAlreadyTriggered[i] = true;

                if (timedEvents[i].onTriggered != null)
                {
                    timedEvents[i].onTriggered.Invoke();
                }
            }
        }
    }

    private void CreateRuntimeSkyboxes()
    {
        runtimeSkyboxes.Clear();

        for (int i = 0; i < skyKeys.Count; i++)
        {
            Material original = skyKeys[i].skyboxMaterial;

            if (original == null)
            {
                runtimeSkyboxes.Add(null);
            }
            else
            {
                runtimeSkyboxes.Add(new Material(original));
            }
        }
    }

    private void SetSkyboxMaterial(int index)
    {
        Material skybox = GetRuntimeSkybox(index);

        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }
    }

    private Material GetRuntimeSkybox(int index)
    {
        if (index < 0 ||
            index >= runtimeSkyboxes.Count)
        {
            return null;
        }

        return runtimeSkyboxes[index];
    }

    private void ApplySkyboxProperties(
        Material skybox,
        float rotation,
        float exposure,
        Color tint)
    {
        if (skybox == null)
        {
            return;
        }

        if (skybox.HasProperty("_Rotation"))
        {
            skybox.SetFloat("_Rotation", rotation);
        }

        if (skybox.HasProperty("_Exposure"))
        {
            skybox.SetFloat("_Exposure", exposure);
        }

        if (skybox.HasProperty("_Tint"))
        {
            skybox.SetColor("_Tint", tint);
        }
    }
}