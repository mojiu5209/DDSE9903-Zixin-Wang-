using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace OccaSoftware.Altos
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    [RequireComponent(typeof(Light))]
    public class MoonObject : MonoBehaviour
    {
        public MoonDefinitionScriptableObject moonDefinition;
        private VisualEffect visualEffect;

        private class ShaderParams
        {
            public static int moonBlendAmount = Shader.PropertyToID("_Celestial_Merge_Moon_Blend_Amount");

            public static int moonSize = Shader.PropertyToID("Moon Size");
            public static int phaseOffset = Shader.PropertyToID("Phase Offset");
            public static int moonAlbedo = Shader.PropertyToID("Moon Albedo");
            public static int moonFadeDist = Shader.PropertyToID("Moon Fade Distance");
            public static int phaseAxis = Shader.PropertyToID("Phase Axis");
            public static int phasePassthroughSpeed = Shader.PropertyToID("Phase Passthrough Speed");
            public static int upVector = Shader.PropertyToID("Up");
            public static int forwardVector = Shader.PropertyToID("Forward");
            public static int rightVector = Shader.PropertyToID("Right");
            public static int moonAlbedoMap = Shader.PropertyToID("Moon Albedo Map");
            public static int moonNormalMap = Shader.PropertyToID("Moon Normal Map");
            public static int rotationSpeed = Shader.PropertyToID("Rotation Speed");
            public static int moonNormalStrength = Shader.PropertyToID("Moon Normal Strength");
        }
        
        public void SetMoonDefinition(MoonDefinitionScriptableObject input)
        {
            moonDefinition = input;
        }

        void Start()
        {
            GetVisualEffectComponent();
            
            if(visualEffect != null)
            {
                visualEffect.Reinit();
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            transform.position = camera.transform.position;
        }

        private void GetVisualEffectComponent()
        {
            if (TryGetComponent(out VisualEffect temp))
            {
                visualEffect = temp;
            }
        }

        private void Update()
        {
            DoRotation();
            ApplyParams();
        }

        private void DoRotation()
        {
            if (Application.isPlaying && visualEffect != null && moonDefinition != null)
            {
                if (moonDefinition.earthRotationSpeed != 0f)
                    transform.Rotate(transform.right * moonDefinition.earthRotationSpeed * Time.deltaTime);
            }
        }


        private void ApplyParams()
        {
            if (moonDefinition == null)
                return;

            Shader.SetGlobalFloat(ShaderParams.moonBlendAmount, moonDefinition.atmosphereBlend); // Used by Merge Pass
            
            //  -- > Only update on change
            if (visualEffect != null)
            {
                #if VFXGRAPH_EXISTS
                visualEffect.SetFloat(ShaderParams.moonSize, moonDefinition.size);
                visualEffect.SetFloat(ShaderParams.phaseOffset, moonDefinition.phaseOffset);
                visualEffect.SetVector4(ShaderParams.moonAlbedo, moonDefinition.moonAlbedo);
                visualEffect.SetFloat(ShaderParams.moonFadeDist, moonDefinition.horizonFadeDistance);
                visualEffect.SetFloat(ShaderParams.phaseAxis, moonDefinition.phaseAxisRotation);
                visualEffect.SetFloat(ShaderParams.phasePassthroughSpeed, moonDefinition.phasePassthroughSpeed);
                visualEffect.SetVector3(ShaderParams.upVector, transform.up);
                visualEffect.SetVector3(ShaderParams.forwardVector, transform.forward);
                visualEffect.SetVector3(ShaderParams.rightVector, transform.right);
                visualEffect.SetTexture(ShaderParams.moonAlbedoMap, moonDefinition.albedoMap);
                visualEffect.SetTexture(ShaderParams.moonNormalMap, moonDefinition.normalMap);
                visualEffect.SetFloat(ShaderParams.rotationSpeed, moonDefinition.moonRotationSpeed);
                visualEffect.SetFloat(ShaderParams.moonNormalStrength, moonDefinition.normalStrength);
                #endif
            }
        }
    }
}
