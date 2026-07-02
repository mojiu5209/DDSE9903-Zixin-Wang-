using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Altos
{

    public class DepthFogRenderFeature : ScriptableRendererFeature
    {
        class DepthFogRenderPass : ScriptableRenderPass
        {
            private Material depthFogMaterial;
            private RenderTargetHandle depthFogRenderTexture;
            private const string featureDescription = "Render Depth Fog OS";
            private const string depthFogShaderPath = "Shader Graphs/DepthFogShader_OS";

            public DepthFogRenderPass()
            {
                if (depthFogMaterial == null) depthFogMaterial = CoreUtils.CreateEngineMaterial(depthFogShaderPath);
                depthFogRenderTexture.Init(featureDescription);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                cmd.GetTemporaryRT(depthFogRenderTexture.id, rtDescriptor);
                ConfigureTarget(depthFogRenderTexture.Identifier());
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
                CommandBuffer cmd = CommandBufferPool.Get(featureDescription);
                Blit(cmd, source, depthFogRenderTexture.Identifier(), depthFogMaterial);
                Blit(cmd, depthFogRenderTexture.Identifier(), source);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(depthFogRenderTexture.id);
            }
        }

        DepthFogRenderPass depthFogRenderPass;
        TimeOfDayManager todm;

        private void OnEnable()
        {
            Helpers.RenderFeatureOnEnable(Recreate);
        }

        private void OnDisable()
        {
            Helpers.RenderFeatureOnDisable(Recreate);
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        public override void Create()
        {
            todm = FindObjectOfType<TimeOfDayManager>();

            if (todm != null)
            {
                depthFogRenderPass = new DepthFogRenderPass();
                depthFogRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents - 1;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            if (todm != null)
            {
                renderer.EnqueuePass(depthFogRenderPass);
            }
        }
    }
}