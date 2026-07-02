using UnityEngine;
using UnityEngine.Profiling;
using System;

namespace OccaSoftware.Altos
{
    public static class StaticTimeOfDayManager
    {
        public static Material activeSkyboxMaterial;

        public static Action<float> UpdateSunIntensityEvent;
        public static Action<float> UpdateTimeOfDayEvent;
        
        public static void OnSunLightIntensityChangeEvent(float sunBrightnessRelative)
        {
            if(UpdateSunIntensityEvent != null)
                UpdateSunIntensityEvent(sunBrightnessRelative);
        }

        public static void OnTimeOfDayChange(float newTimeOfDay)
        {
            if (UpdateTimeOfDayEvent != null)
                UpdateTimeOfDayEvent(newTimeOfDay);
        }

        /* Source: Wikipedia (https://en.wikipedia.org/wiki/Golden_hour_(photography))
         * The color temperature of daylight varies with the time of day. 
         * It tends to be around 2,000 K shortly after sunrise or before sunset, 
         * around 3,500 K during "golden hour", 
         * and around 5,500 K at midday.
        */

        public static bool UpdateDirectionalLightProperties(ref Light light, float peakIntensity, float horizonAngle)
        {
            float lightAngle = light.transform.eulerAngles.x;

            lightAngle = lightAngle > 180f ? lightAngle - 360f : lightAngle;

            light.useColorTemperature = true;
            light.color = Color.white;
            float cachedTemp = light.colorTemperature;
            float cachedIntensity = light.intensity;

            if (lightAngle < -horizonAngle)
			{
                light.intensity = 0;
                light.colorTemperature = 2000;
			}

            if(lightAngle > -horizonAngle)
			{
                float t = Helpers.Remap01(lightAngle, -horizonAngle, 70f);
                light.intensity = Mathf.Lerp(0f, peakIntensity, t);
                light.colorTemperature = Mathf.Lerp(2000, 5500, t);
            }

            if(Mathf.Abs(cachedTemp - light.colorTemperature) > 1f || Mathf.Abs(cachedIntensity - light.intensity) > 0.01f)
			{
                return true;
			}

            return false;
        }
    }

    [ExecuteAlways]
    public class TimeOfDayManager : MonoBehaviour
    {
        public SkyboxDefinitionScriptableObject skyboxDefinition = null;
        private Material skyboxMaterial = null;
        
        public Light sun = null;

        private int periodIndex = 0;
        private int nextPeriodIndex = 0;
        private int previousPeriodIndex = 0;

        private float timeHours = 0f;
        private Color fogColor = Color.white;
        private Color zenithAmbient = Color.white;

        private bool setupCorrect = false;

        private float cachedTimeOfDay = 0f;

        public float SunLightIntensityRelative
        {
            get
            {
                if (sun == null || skyboxDefinition == null)
                    return 0;

                return Helpers.Remap(sun.intensity, 0f, skyboxDefinition.sunLightIntensity, 0f, 1f);
            }
        }


        public void Awake()
        {
            InitializeSkybox();
        }

        public void InitializeSkybox()
        {
            //Debug.Log("Initializing Skybox...");
            // Error Handling
            setupCorrect = ValidateSetup();
        }


        private bool ValidateSetup()
        {
            #if !UNITY_2021_3_OR_NEWER
                Debug.LogError("This version of Altos is designed for Unity 2021.3 or newer. Please upgrade your Unity Editor to ensure that Altos is compatible with your Unity version.");    
            #endif

            if (skyboxDefinition == null)
            {
                Debug.Log("Must set Skybox Definition");
                return false;
            }

            if (skyboxMaterial == null)
            {
                //Debug.Log("Setting up Skybox Material...");
                skyboxMaterial = GetSkyboxMaterial();
                StaticTimeOfDayManager.activeSkyboxMaterial = skyboxMaterial;
                if (skyboxMaterial == null)
                {
                    Debug.Log("Failed to set up Skybox Material. Exiting.");
                    return false;
                }
            }

            if (RenderSettings.skybox != skyboxMaterial)
            {
                //Debug.Log("Setting up Skybox in Lighting settings...");
                SetLightingEnvironment(skyboxMaterial);

                if (RenderSettings.skybox != skyboxMaterial)
                {
                    Debug.Log("Failed to set up Skybox in Lighting settings. Exiting.");
                    return false;
                }
            }

            if (sun == null)
            {
                Debug.Log("Sun Lamp is not set. Exiting.");
                return false;
            }
            RenderSettings.sun = sun;

            return true;
        }

        private void OnDestroy()
        {
            if (RenderSettings.skybox = skyboxMaterial)
                RenderSettings.skybox = null;
        }


        void Start()
        {
            if (setupCorrect)
            {
                //Debug.Log("Skybox Initialized Successfully.");
                InitialSetup();
            }
        }

        public void InitialSetup()
        {
            if (setupCorrect)
            {
                skyboxDefinition.activeTimeOfDay = skyboxDefinition.timeOfDay;
                cachedTimeOfDay = skyboxDefinition.activeTimeOfDay;
                
                HandleSunLight();
                SetPeriod();
                SetSkyColors();

                UpdateActiveShaderProperties();
                UpdateStaticShaderProperties();
            }
        }

        private Material GetSkyboxMaterial()
        {
            return new Material(Shader.Find("Shader Graphs/Skybox Shader_OS"));
        }

        private void SetLightingEnvironment(Material skybox)
        {
            RenderSettings.skybox = skybox;
        }

        public void SetSkyboxDefinition(SkyboxDefinitionScriptableObject skyboxDefinition)
        {
            this.skyboxDefinition = skyboxDefinition;
        }

        public void SetSunLamp(Light sun)
        {
            this.sun = sun;
        }

        public void UpdateStaticShaderProperties()
        {
            skyboxMaterial.SetFloat(ShaderParams.sunSize, skyboxDefinition.sunSize);
			if (sun.useColorTemperature)
			{
                skyboxMaterial.SetColor(ShaderParams.sunColor, skyboxDefinition.sunColor * Mathf.CorrelatedColorTemperatureToRGB(sun.colorTemperature));
            }
			else
			{
                skyboxMaterial.SetColor(ShaderParams.sunColor, skyboxDefinition.sunColor * sun.color);
            }
            
            skyboxMaterial.SetFloat(ShaderParams.sunInfluenceSize, skyboxDefinition.sunInfluenceSize);
            skyboxMaterial.SetFloat(ShaderParams.sunInfluenceIntensity, skyboxDefinition.sunInfluenceIntensity);

            skyboxMaterial.SetTexture(ShaderParams.cloudTex1, skyboxDefinition.cloudTexture1);
            skyboxMaterial.SetTexture(ShaderParams.cloudTex2, skyboxDefinition.cloudTexture2);

            skyboxMaterial.SetVector(ShaderParams.cloudTex1ZenithTiling, skyboxDefinition.texture1ZenithTiling);
            skyboxMaterial.SetVector(ShaderParams.cloudTex2ZenithTiling, skyboxDefinition.texture2ZenithTiling);
            skyboxMaterial.SetVector(ShaderParams.cloudTex1HorizonTiling, skyboxDefinition.texture1HorizonTilingSterile);
            skyboxMaterial.SetVector(ShaderParams.cloudTex2HorizonTiling, skyboxDefinition.texture2HorizonTilingSterile);


            skyboxMaterial.SetFloat(ShaderParams.cloudSharpness, skyboxDefinition.cloudSharpness);
            skyboxMaterial.SetColor(ShaderParams.cloudColor, skyboxDefinition.cloudColor);
            skyboxMaterial.SetFloat(ShaderParams.cloudOpacity, skyboxDefinition.cloudOpacity);
            skyboxMaterial.SetColor(ShaderParams.cloudShadingColor, skyboxDefinition.cloudShadingColor);
            skyboxMaterial.SetFloat(ShaderParams.cloudShadingThreshold, skyboxDefinition.cloudShadingThreshold);
            skyboxMaterial.SetFloat(ShaderParams.cloudShadingSharpness, skyboxDefinition.cloudShadingSharpness);
            skyboxMaterial.SetFloat(ShaderParams.cloudShadingStrength, skyboxDefinition.cloudShadingStrength);

            skyboxMaterial.SetFloat(ShaderParams.cloudSpeed, skyboxDefinition.cloudSpeed);
            skyboxMaterial.SetFloat(ShaderParams.cloudThreshold, 1f - skyboxDefinition.cloudiness);

            skyboxMaterial.SetFloat(ShaderParams.alternateUVAtZenith, skyboxDefinition.alternateUVAtZenith);
            skyboxMaterial.SetFloat(ShaderParams.sunInfluence, skyboxDefinition.sunCloudInfluence);
            skyboxMaterial.SetFloat(ShaderParams.skyColorInfluence, skyboxDefinition.skyColorCloudInfluence);

            skyboxMaterial.SetFloat(ShaderParams.blueNoiseStrength, skyboxDefinition.ditherStrength);

            skyboxMaterial.SetFloat(ShaderParams.fogHeightPower, skyboxDefinition.fogHeightPower);

            Shader.SetGlobalFloat(ShaderParams.depthFogStart, skyboxDefinition.fogStart);
            Shader.SetGlobalFloat(ShaderParams.depthFogEnd, skyboxDefinition.fogEnd);
            Shader.SetGlobalFloat(ShaderParams.depthFogDithering, skyboxDefinition.fogDithering);
        }


        private static class ShaderParams
        {
            public static int sunDirection = Shader.PropertyToID("_SUN_DIRECTION");

            public static int horizonColor = Shader.PropertyToID("_HORIZONCOLOR");
            public static int zenithColor = Shader.PropertyToID("_ZENITHCOLOR");
            public static int groundColor = Shader.PropertyToID("_GROUNDCOLOR");

            public static int cloudThreshold = Shader.PropertyToID("_CLOUDTHRESHOLD");
            public static int cloudSpeed = Shader.PropertyToID("_CLOUDTEXTURESPEED");

            public static int nightLuminance = Shader.PropertyToID("_CLOUD_NIGHT_LUMINANCE_MULTIPLIER");

            public static int fogColor = Shader.PropertyToID("_FOG_COLOR");
            public static int volumetricsFogColor = Shader.PropertyToID("_VOLUMETRICS_FOG_COLOR");
            public static int zenithAmbientColor = Shader.PropertyToID("_ZENITH_AMBIENT_COLOR");
            public static int depthFogColor = Shader.PropertyToID("_DEPTH_FOG_COLOR");

            public static int depthFogStart = Shader.PropertyToID("_DEPTH_FOG_START");
            public static int depthFogEnd = Shader.PropertyToID("_DEPTH_FOG_END");
            public static int depthFogDithering = Shader.PropertyToID("_DEPTH_FOG_DITHERING");

            public static int sunSize = Shader.PropertyToID("_SUNSIZE");
            public static int sunColor = Shader.PropertyToID("_SUNCOLOR");
            public static int sunInfluenceSize = Shader.PropertyToID("_SUNINFLUENCESIZE");
            public static int sunInfluenceIntensity = Shader.PropertyToID("_SUNINFLUENCEINTENSITY");
            public static int sunIntensity = Shader.PropertyToID("_SUN_INTENSITY");

            public static int cloudTex1 = Shader.PropertyToID("_CLOUDTEXTURE1");
            public static int cloudTex2 = Shader.PropertyToID("_CLOUDTEXTURE2");

            public static int cloudTex1ZenithTiling = Shader.PropertyToID("_TEXTURE1ZENITHTILING");
            public static int cloudTex2ZenithTiling = Shader.PropertyToID("_TEXTURE2ZENITHTILING");
            public static int cloudTex1HorizonTiling = Shader.PropertyToID("_TEXTURE1HORIZONTILING");
            public static int cloudTex2HorizonTiling = Shader.PropertyToID("_TEXTURE2HORIZONTILING");

            public static int cloudSharpness = Shader.PropertyToID("_CLOUDSHARPNESS");
            public static int cloudColor = Shader.PropertyToID("_CLOUDCOLOR");
            public static int cloudOpacity = Shader.PropertyToID("_CLOUDOPACITY");
            public static int cloudShadingColor = Shader.PropertyToID("_CLOUDSHADINGCOLOR");
            public static int cloudShadingThreshold = Shader.PropertyToID("_CLOUDSHADINGTHRESHOLD");
            public static int cloudShadingSharpness = Shader.PropertyToID("_CLOUDSHADINGSHARPNESS");
            public static int cloudShadingStrength = Shader.PropertyToID("_CLOUDSHADINGSTRENGTH");

            public static int alternateUVAtZenith = Shader.PropertyToID("_ALTERNATEUVATZENITH");
            public static int sunInfluence = Shader.PropertyToID("_SUNINFLUENCE");
            public static int skyColorInfluence = Shader.PropertyToID("_SKYCOLORINFLUENCE");

            public static int blueNoiseStrength = Shader.PropertyToID("_BLUENOISESTRENGTH");
            public static int fogHeightPower = Shader.PropertyToID("_FOG_HEIGHT_POWER");
        }

        public void UpdateActiveShaderProperties()
        {
            skyboxMaterial.SetColor(ShaderParams.fogColor, fogColor);
            Shader.SetGlobalColor(ShaderParams.volumetricsFogColor, fogColor);
            Shader.SetGlobalColor(ShaderParams.zenithAmbientColor, zenithAmbient);

            Shader.SetGlobalColor(ShaderParams.depthFogColor, fogColor);
            
            if(sun != null)
                Shader.SetGlobalVector(ShaderParams.sunDirection, sun.transform.forward);
        }

        // Update is called once per frame
        public void Update()
        {
            Profiler.BeginSample("TimeOfDayManager: Update Execution");
            if (!setupCorrect)
                return;

            if (skyboxDefinition == null)
                return;

            if (skyboxMaterial == null)
            {
                skyboxMaterial = GetSkyboxMaterial();
                SetLightingEnvironment(skyboxMaterial);
            }

            if (Application.isPlaying)
            {
                SetTimeOfDay();
            }
            else
            {
                HandleSunLight();
                SetPeriod();
                SetSkyColors();
            }


            if(Mathf.Abs(skyboxDefinition.activeTimeOfDay - cachedTimeOfDay) > 0.01f)
            {
                cachedTimeOfDay = skyboxDefinition.activeTimeOfDay;
                HandleSunLight();
                SetPeriod();
                SetSkyColors();
            }

            UpdateActiveShaderProperties();
            UpdateStaticShaderProperties();

            Profiler.EndSample();
        }


        private void SetTimeOfDay()
        {
            if (skyboxDefinition.activeTimeOfDay > 24f)
            {
                skyboxDefinition.activeTimeOfDay = skyboxDefinition.activeTimeOfDay - 24f;
            }

            if(skyboxDefinition.realSecondsToGameHours > 0f)
            {
                float t = Time.deltaTime * skyboxDefinition.realSecondsToGameHours; // One second in real life is one hour in-game. This can be modified with the realSecondsToGameHours variable (e.g., if realSecondsToGameHours is set to 2, then 1 second in real life corresponds to 2 hours in-game). 
                skyboxDefinition.activeTimeOfDay += t;
                timeHours += t; // Tracking timehours separately for sampling perlin noise (we don't reset it at 24).
            }
        }

        void HandleSunLight()
        {
            if (sun == null)
                return;

            SetSunLightDirection();

			if (skyboxDefinition.sunColorAutomatic)
			{
                float horizonAngle = 10f;
                bool updated = StaticTimeOfDayManager.UpdateDirectionalLightProperties(ref sun, skyboxDefinition.sunLightIntensity, horizonAngle);
                if (updated)
                {
                    StaticTimeOfDayManager.OnSunLightIntensityChangeEvent(sun.intensity / skyboxDefinition.sunLightIntensity);
                }
            }
            
            float cloudLuminance = Helpers.Remap(sun.intensity, 0f, skyboxDefinition.sunLightIntensity, skyboxDefinition.cloudNightLuminanceMultiplier, 1f);

            skyboxMaterial.SetFloat(ShaderParams.nightLuminance, cloudLuminance);
            skyboxMaterial.SetFloat(ShaderParams.sunIntensity, SunLightIntensityRelative);
        }


        // Basically, the light direction is derived from the current time of day such that the sun is perfectly horizontal at 6am and 6pm for a 12hr "day" cycle and 12hr "night" cycle.
        // Not currently configurable.
        private void SetSunLightDirection()
        {
            if (sun == null)
                return;

            float remappedTimeOfDay = skyboxDefinition.activeTimeOfDay / 24f;
            float sunAngle = (remappedTimeOfDay * 360f) - 90f;
            sun.transform.rotation = Quaternion.Euler(sunAngle, skyboxDefinition.sunBaseRotationY, 0f); // Sun rotation is based on time of day
        }


        // The SetSkyColors function depends on this to set the correct period.
        // Normally, this is straightforward. Transitioning from one day to the next is the tricky bit.
        private void SetPeriod()
        {
            if (Application.isPlaying)
            {
                HandlePeriod(skyboxDefinition.activeTimeOfDay);
            }
            else
            {
                HandlePeriod(skyboxDefinition.timeOfDay);
            }

            nextPeriodIndex = periodIndex + 1;
            if (nextPeriodIndex >= skyboxDefinition.periodsOfDay.Count)
            {
                nextPeriodIndex = 0;
            }

            previousPeriodIndex = periodIndex - 1;
            if(previousPeriodIndex < 0)
			{
                previousPeriodIndex = skyboxDefinition.periodsOfDay.Count - 1;
			}
        }

        void HandlePeriod(float time)
        {
            if (time < skyboxDefinition.periodsOfDay[0].startTime)
            {
                periodIndex = skyboxDefinition.periodsOfDay.Count - 1;
            }
            else
            {
                for (int i = 0; i < skyboxDefinition.periodsOfDay.Count; i++)
                {
                    if (time >= skyboxDefinition.periodsOfDay[i].startTime)
                    {
                        periodIndex = i;
                    }
                }
            }
        }


        // Sets the Horizon and Zenith colors based on the current time of day.
        // As we approach the next time of day, we transition smoothly to the Horizon and Zenith colors for that time of day.
        private void SetSkyColors()
        {
            if (Application.isPlaying)
            {
                HandleSkyColor(skyboxDefinition.activeTimeOfDay);
            }
            else
            {
                HandleSkyColor(skyboxDefinition.timeOfDay);
            }
        }


        private void HandleSkyColor(float time)
        {
            // Handle day/night rollover

            float startTime = skyboxDefinition.periodsOfDay[periodIndex].startTime;
            if(startTime > skyboxDefinition.periodsOfDay[nextPeriodIndex].startTime)
			{
                startTime -= 24f;
			}

            if(time > skyboxDefinition.periodsOfDay[nextPeriodIndex].startTime)
			{
                time -= 24f;
			}


            // Calculate and set colors.
            float t = Helpers.Remap(time, startTime, skyboxDefinition.periodsOfDay[nextPeriodIndex].startTime, 0, 1);

            Color horizonTemp = Color.Lerp(skyboxDefinition.periodsOfDay[periodIndex].horizonColor, skyboxDefinition.periodsOfDay[nextPeriodIndex].horizonColor, t);
            Color zenithTemp = Color.Lerp(skyboxDefinition.periodsOfDay[periodIndex].zenithColor, skyboxDefinition.periodsOfDay[nextPeriodIndex].zenithColor, t);
            Color groundTemp = Color.Lerp(skyboxDefinition.periodsOfDay[periodIndex].groundColor, skyboxDefinition.periodsOfDay[nextPeriodIndex].groundColor, t);

            skyboxMaterial.SetColor(ShaderParams.horizonColor, horizonTemp);
            skyboxMaterial.SetColor(ShaderParams.zenithColor, zenithTemp);
            skyboxMaterial.SetColor(ShaderParams.groundColor, groundTemp);


            // Set fog and ambient colors
            fogColor = Color.Lerp(skyboxDefinition.baseFogColor, horizonTemp, skyboxDefinition.fogColorBlend);
            zenithAmbient = Color.Lerp(horizonTemp, zenithTemp, 0.5f);
        }
    }

    // Internal class used to include all PeriodOfDay information.
    // I plan to replace this later with ScriptableObjects for ease of use.
    [System.Serializable]
    public class PeriodOfDay
    {
        [SerializeField]
        [Tooltip("(Optional) Descriptive Name")]
        public string description;
        [SerializeField, Range(0f, 24f)]
        [Tooltip("Set the Start Time for this Period of Day")]
        public float startTime;
        [SerializeField, ColorUsage(false, true)]
        [Tooltip("Set the Horizon Color for this Period of Day")]
        public Color horizonColor;
        [SerializeField, ColorUsage(false, true)]
        [Tooltip("Set the Zenith Color for this Period of Day")]
        public Color zenithColor;
        [SerializeField, ColorUsage(false, true)]
        [Tooltip("Set the Ground Color for this Period of Day")]
        public Color groundColor;

        public PeriodOfDay(string desc, float start, Color horizon, Color zenith, Color ground)
        {
            description = desc;
            startTime = start;
            horizonColor = horizon;
            zenithColor = zenith;
            groundColor = ground;
        }
    }

}
