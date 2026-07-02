using UnityEngine;
using UnityEngine.Rendering;

namespace OccaSoftware.Altos
{
    public class VolumetricCloudVolume : MonoBehaviour
    {
        public VolumetricCloudsDefinitionScriptableObject cloudData;

        private Matrix4x4 gpuVPLast;
        private Camera cam;

        private void Start()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            cam = Camera.main;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {

            var gpuProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);

            Shader.SetGlobalMatrix("_InvCamProj", gpuProj.inverse);
            var gpuView = cam.worldToCameraMatrix;
            var gpuVP = gpuProj * gpuView;

            if (gpuVPLast != null)
            {
                Shader.SetGlobalMatrix("_PrevViewProjM", gpuVPLast);
            }
            gpuVPLast = gpuVP;
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }
    }
}
