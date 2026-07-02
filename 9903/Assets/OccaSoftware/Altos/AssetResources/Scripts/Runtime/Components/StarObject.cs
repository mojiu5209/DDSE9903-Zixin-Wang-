using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace OccaSoftware.Altos
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public class StarObject : MonoBehaviour
    {
        private VisualEffect visualEffect;
        public StarDefinitionScriptableObject starDefinition;
        
        public void SetStarDefinition(StarDefinitionScriptableObject input)
        {
            starDefinition = input;
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            StaticTimeOfDayManager.UpdateSunIntensityEvent += UpdateStarBrightness;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            StaticTimeOfDayManager.UpdateSunIntensityEvent -= UpdateStarBrightness;
        }

        // Start is called before the first frame update
        void Start()
        {
            Setup();
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            transform.position = camera.transform.position;
        }


        public void Setup()
        {
            if(TryGetComponent(out VisualEffect temp))
            {
                visualEffect = temp;
            }

            if(visualEffect != null)
            {
                visualEffect.Reinit();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(starDefinition != null)
            {
                UpdateShaderProperties();
            }
        }

        private void UpdateStarBrightness(float relativeBrightness)
        {
            if (starDefinition != null)
            {
                #if VFXGRAPH_EXISTS
                visualEffect.SetFloat("Daytime Brightness", Mathf.Lerp(1.0f, starDefinition.daytimeBrightness, relativeBrightness));
                #endif
            }
        }

        private void UpdateShaderProperties()
        {
            #if VFXGRAPH_EXISTS
            visualEffect.SetFloat("Star Count", starDefinition.count);
            visualEffect.SetTexture("Star Texture", starDefinition.texture);
            visualEffect.SetGradient("Star Color Gradient", starDefinition.color);
            visualEffect.SetFloat("Star Size", starDefinition.size);
            visualEffect.SetFloat("Star Size Max Multiplier", starDefinition.sizeRange);
            visualEffect.SetFloat("Peak Brightness Multiplier", starDefinition.peakBrightness);
            visualEffect.SetVector2("Range of Brightness Oscillations per Second", starDefinition.flickerFrequencyMinMax);
            visualEffect.SetFloat("Randomize Star Angle", starDefinition.textureAngleRandom);
            visualEffect.SetFloat("Star Horizon Fade Distance", starDefinition.horizonFadeDistance);
            visualEffect.SetFloat("Rotation Speed", starDefinition.rotationSpeed);
            #endif
        }

    }

}
