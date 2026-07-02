using UnityEngine;

namespace OccaSoftware.Altos
{
    [ExecuteAlways]
    public class SunLampObject : MonoBehaviour
    {
        private class ShaderParams
        {
            public static int sunDirection = Shader.PropertyToID("_SUNDIRECTION");
            public static int sunIntensityForVolumetrics = Shader.PropertyToID("_SUN_INTENSITY_VOLUMETRICS");
        }

        private TimeOfDayManager timeOfDayManager = null;
        private float volumetricsIntensity = 1f;

        private void OnEnable()
        {
            StaticTimeOfDayManager.UpdateSunIntensityEvent += UpdateVolumetricsIntensity;
        }

        private void OnDisable()
        {
            StaticTimeOfDayManager.UpdateSunIntensityEvent -= UpdateVolumetricsIntensity;
        }

        private void Start()
        {
            transform.parent.TryGetComponent(out timeOfDayManager);

            if (timeOfDayManager != null)
                UpdateVolumetricsIntensity(timeOfDayManager.SunLightIntensityRelative);
        }

        private void Update()
        {
            Shader.SetGlobalVector(ShaderParams.sunDirection, -transform.forward);
            Shader.SetGlobalFloat(ShaderParams.sunIntensityForVolumetrics, volumetricsIntensity);
        }

        void UpdateVolumetricsIntensity(float relativeBrightness)
        {
            volumetricsIntensity = relativeBrightness * relativeBrightness;
        }
    }

}