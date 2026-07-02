using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

[System.Serializable]
public class CityTimePhase
{
    [Header("Phase Name")]
    public string phaseName = "Night";

    [TextArea(1, 3)]
    public string phaseCaption = "";

    [Header("HDR Skybox")]
    public Material skyboxMaterial;

    [Header("Duration")]
    [Min(0f)] public float holdDuration = 1.5f;
    [Min(0.1f)] public float transitionDuration = 2f;

    [Header("Skybox Settings")]
    [Range(0f, 360f)] public float skyRotation = 0f;
    [Range(0.01f, 8f)] public float skyExposure = 1f;
    public Color skyTint = Color.white;

    [Header("Directional Light")]
    public Color sunColor = Color.white;
    [Min(0f)] public float sunIntensity = 1f;
    public Vector3 sunEulerAngles = new Vector3(45f, -30f, 0f);

    [Header("Ambient Lighting")]
    public Color ambientSky = new Color(0.45f, 0.5f, 0.6f);
    public Color ambientEquator = new Color(0.35f, 0.35f, 0.4f);
    public Color ambientGround = new Color(0.2f, 0.2f, 0.2f);

    [Header("Story Changes")]
    [Tooltip("进入这个时间阶段时，自动显示的物体。")]
    public GameObject[] activateAtPhaseStart;

    [Tooltip("进入这个时间阶段时，自动隐藏的物体。")]
    public GameObject[] deactivateAtPhaseStart;

    [Tooltip("用于更复杂剧情，例如开始下雨、播放狗叫、启动 NPC 动画。")]
    public UnityEvent onPhaseStart;
}

public class CityTimeLapse : MonoBehaviour
{
    [Header("Start Settings")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private float initialBlackHoldTime = 0.8f;

    [Header("Cinematic Camera")]
    [SerializeField] private Camera cinematicCamera;

    [Tooltip("放在城市高处、窗边或街道旁，镜头朝向天空的位置。")]
    [SerializeField] private Transform timeLapseView;

    [Tooltip("例如场景里额外存在的 Main Camera。需要在时间流失时关闭。")]
    [SerializeField] private Camera[] camerasToDisableDuringCinematic;

    [Header("EZPZ Player")]
    [SerializeField] private GameObject ezpzPlayer;

    [Tooltip("时间流失结束后，EZPZ 玩家出现的位置。放在地面高度。")]
    [SerializeField] private Transform playerSpawnAtFinish;

    [SerializeField] private bool returnPlayerControlAtFinish = true;

    [Header("UI")]
    [SerializeField] private Image blackFade;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Lighting")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private bool updateEnvironmentLighting = true;

    [Header("Sky Transitions")]
    [Range(0f, 1f)]
    [SerializeField] private float skySwapBlackAlpha = 0.85f;

    [SerializeField] private float initialRevealTime = 1.2f;
    [SerializeField] private float skySwapFadeInTime = 0.25f;
    [SerializeField] private float finalFadeToPlayerTime = 1f;
    [SerializeField] private float playerRevealTime = 2f;

    [Header("Time Phases")]
    [SerializeField]
    private List<CityTimePhase> phases =
        new List<CityTimePhase>();

    private readonly List<Material> runtimeSkyboxes =
        new List<Material>();

    private bool[] savedCameraStates;
    private Coroutine timeLapseRoutine;

    private Material originalSkybox;
    private AmbientMode originalAmbientMode;
    private Color originalAmbientSky;
    private Color originalAmbientEquator;
    private Color originalAmbientGround;

    private void Awake()
    {
        originalSkybox = RenderSettings.skybox;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientSky = RenderSettings.ambientSkyColor;
        originalAmbientEquator = RenderSettings.ambientEquatorColor;
        originalAmbientGround = RenderSettings.ambientGroundColor;

        CreateRuntimeSkyboxes();
    }

    private void Start()
    {
        if (autoPlayOnStart)
        {
            PlayTimeLapse();
        }
        else if (phases.Count > 0)
        {
            ApplyPhaseInstant(0, true);
        }
    }

    private void OnDestroy()
    {
        RenderSettings.skybox = originalSkybox;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalAmbientSky;
        RenderSettings.ambientEquatorColor = originalAmbientEquator;
        RenderSettings.ambientGroundColor = originalAmbientGround;

        for (int i = 0; i < runtimeSkyboxes.Count; i++)
        {
            if (runtimeSkyboxes[i] != null)
            {
                Destroy(runtimeSkyboxes[i]);
            }
        }
    }

    public void PlayTimeLapse()
    {
        if (timeLapseRoutine != null)
        {
            return;
        }

        timeLapseRoutine = StartCoroutine(PlayTimeLapseRoutine());
    }

    public void SetPhaseInstant(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count)
        {
            return;
        }

        ApplyPhaseInstant(phaseIndex, true);
    }

    private IEnumerator PlayTimeLapseRoutine()
    {
        if (phases.Count == 0)
        {
            Debug.LogWarning("CityTimeLapse: No time phases added.");
            timeLapseRoutine = null;
            yield break;
        }

        DisableExtraCameras();

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(false);
        }

        if (cinematicCamera != null)
        {
            cinematicCamera.gameObject.SetActive(true);

            if (timeLapseView != null)
            {
                cinematicCamera.transform.SetPositionAndRotation(
                    timeLapseView.position,
                    timeLapseView.rotation
                );
            }
        }

        SetSubtitle("");
        SetFadeAlpha(1f);

        ApplyPhaseInstant(0, true);

        yield return new WaitForSeconds(initialBlackHoldTime);

        yield return StartCoroutine(FadeBlack(
            1f,
            0f,
            initialRevealTime
        ));

        yield return new WaitForSeconds(phases[0].holdDuration);

        for (int i = 1; i < phases.Count; i++)
        {
            yield return StartCoroutine(TransitionToPhase(
                i - 1,
                i
            ));
        }

        if (!returnPlayerControlAtFinish)
        {
            SetSubtitle("");
            timeLapseRoutine = null;
            yield break;
        }

        yield return StartCoroutine(FadeBlack(
            GetFadeAlpha(),
            1f,
            finalFadeToPlayerTime
        ));

        SetSubtitle("The next morning...");
        yield return new WaitForSeconds(1.5f);

        if (ezpzPlayer != null && playerSpawnAtFinish != null)
        {
            ezpzPlayer.transform.SetPositionAndRotation(
                playerSpawnAtFinish.position,
                playerSpawnAtFinish.rotation
            );
        }

        if (cinematicCamera != null)
        {
            cinematicCamera.gameObject.SetActive(false);
        }

        RestoreExtraCameras();

        if (ezpzPlayer != null)
        {
            ezpzPlayer.SetActive(true);
        }

        yield return null;

        yield return StartCoroutine(FadeBlack(
            1f,
            0f,
            playerRevealTime
        ));

        SetSubtitle("");
        timeLapseRoutine = null;
    }

    private IEnumerator TransitionToPhase(
        int fromIndex,
        int toIndex)
    {
        CityTimePhase fromPhase = phases[fromIndex];
        CityTimePhase toPhase = phases[toIndex];

        yield return StartCoroutine(FadeBlack(
            GetFadeAlpha(),
            skySwapBlackAlpha,
            skySwapFadeInTime
        ));

        ApplyPhaseStoryObjects(toPhase);
        SetSkyboxMaterial(toIndex);

        if (!string.IsNullOrEmpty(toPhase.phaseCaption))
        {
            SetSubtitle(toPhase.phaseCaption);
        }

        float timer = 0f;
        float duration = Mathf.Max(0.1f, toPhase.transitionDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                timer / duration
            );

            ApplyLightingLerp(fromPhase, toPhase, t);

            Material targetSkybox = GetRuntimeSkybox(toIndex);

            float rotation = Mathf.LerpAngle(
                fromPhase.skyRotation,
                toPhase.skyRotation,
                t
            );

            float exposure = Mathf.Lerp(
                0.05f,
                toPhase.skyExposure,
                t
            );

            Color tint = Color.Lerp(
                fromPhase.skyTint,
                toPhase.skyTint,
                t
            );

            ApplySkyboxProperties(
                targetSkybox,
                rotation,
                exposure,
                tint
            );

            SetFadeAlpha(
                Mathf.Lerp(
                    skySwapBlackAlpha,
                    0f,
                    t
                )
            );

            yield return null;
        }

        ApplyPhaseVisuals(toIndex);

        if (updateEnvironmentLighting)
        {
            DynamicGI.UpdateEnvironment();
        }

        yield return new WaitForSeconds(toPhase.holdDuration);
    }

    private void ApplyPhaseInstant(
        int phaseIndex,
        bool triggerStory)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count)
        {
            return;
        }

        SetSkyboxMaterial(phaseIndex);
        ApplyPhaseVisuals(phaseIndex);

        CityTimePhase phase = phases[phaseIndex];

        if (!string.IsNullOrEmpty(phase.phaseCaption))
        {
            SetSubtitle(phase.phaseCaption);
        }

        if (triggerStory)
        {
            ApplyPhaseStoryObjects(phase);
        }

        if (updateEnvironmentLighting)
        {
            DynamicGI.UpdateEnvironment();
        }
    }

    private void ApplyPhaseVisuals(int phaseIndex)
    {
        CityTimePhase phase = phases[phaseIndex];

        ApplySkyboxProperties(
            GetRuntimeSkybox(phaseIndex),
            phase.skyRotation,
            phase.skyExposure,
            phase.skyTint
        );

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = phase.ambientSky;
        RenderSettings.ambientEquatorColor = phase.ambientEquator;
        RenderSettings.ambientGroundColor = phase.ambientGround;

        if (directionalLight != null)
        {
            directionalLight.color = phase.sunColor;
            directionalLight.intensity = phase.sunIntensity;
            directionalLight.transform.rotation =
                Quaternion.Euler(phase.sunEulerAngles);
        }
    }

    private void ApplyLightingLerp(
        CityTimePhase fromPhase,
        CityTimePhase toPhase,
        float t)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = Color.Lerp(
            fromPhase.ambientSky,
            toPhase.ambientSky,
            t
        );

        RenderSettings.ambientEquatorColor = Color.Lerp(
            fromPhase.ambientEquator,
            toPhase.ambientEquator,
            t
        );

        RenderSettings.ambientGroundColor = Color.Lerp(
            fromPhase.ambientGround,
            toPhase.ambientGround,
            t
        );

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(
                fromPhase.sunColor,
                toPhase.sunColor,
                t
            );

            directionalLight.intensity = Mathf.Lerp(
                fromPhase.sunIntensity,
                toPhase.sunIntensity,
                t
            );

            directionalLight.transform.rotation =
                Quaternion.Slerp(
                    Quaternion.Euler(fromPhase.sunEulerAngles),
                    Quaternion.Euler(toPhase.sunEulerAngles),
                    t
                );
        }
    }

    private void ApplyPhaseStoryObjects(CityTimePhase phase)
    {
        if (phase.deactivateAtPhaseStart != null)
        {
            foreach (GameObject item in phase.deactivateAtPhaseStart)
            {
                if (item != null)
                {
                    item.SetActive(false);
                }
            }
        }

        if (phase.activateAtPhaseStart != null)
        {
            foreach (GameObject item in phase.activateAtPhaseStart)
            {
                if (item != null)
                {
                    item.SetActive(true);
                }
            }
        }

        if (phase.onPhaseStart != null)
        {
            phase.onPhaseStart.Invoke();
        }
    }

    private void CreateRuntimeSkyboxes()
    {
        runtimeSkyboxes.Clear();

        for (int i = 0; i < phases.Count; i++)
        {
            Material sourceMaterial = phases[i].skyboxMaterial;

            if (sourceMaterial == null)
            {
                runtimeSkyboxes.Add(null);
            }
            else
            {
                runtimeSkyboxes.Add(new Material(sourceMaterial));
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
        if (index < 0 || index >= runtimeSkyboxes.Count)
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

    private void DisableExtraCameras()
    {
        if (camerasToDisableDuringCinematic == null)
        {
            return;
        }

        savedCameraStates = new bool[
            camerasToDisableDuringCinematic.Length
        ];

        for (int i = 0; i < camerasToDisableDuringCinematic.Length; i++)
        {
            Camera targetCamera =
                camerasToDisableDuringCinematic[i];

            if (targetCamera == null)
            {
                continue;
            }

            savedCameraStates[i] = targetCamera.gameObject.activeSelf;
            targetCamera.gameObject.SetActive(false);
        }
    }

    private void RestoreExtraCameras()
    {
        if (camerasToDisableDuringCinematic == null ||
            savedCameraStates == null)
        {
            return;
        }

        for (int i = 0; i < camerasToDisableDuringCinematic.Length; i++)
        {
            Camera targetCamera =
                camerasToDisableDuringCinematic[i];

            if (targetCamera == null)
            {
                continue;
            }

            targetCamera.gameObject.SetActive(
                savedCameraStates[i]
            );
        }
    }

    private IEnumerator FadeBlack(
        float startAlpha,
        float endAlpha,
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
                    startAlpha,
                    endAlpha,
                    timer / duration
                )
            );

            yield return null;
        }

        SetFadeAlpha(endAlpha);
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

    private float GetFadeAlpha()
    {
        return blackFade != null ? blackFade.color.a : 0f;
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.text = text;
        }
    }
}