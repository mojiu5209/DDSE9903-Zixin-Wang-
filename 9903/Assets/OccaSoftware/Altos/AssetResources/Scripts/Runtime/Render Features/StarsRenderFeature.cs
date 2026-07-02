using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace OccaSoftware.Altos
{

    public class StarsRenderFeature : ScriptableRendererFeature
    {

        class StarRenderPass : ScriptableRenderPass
        {
            private RenderTargetHandle starsRT;

            private const string featureDescription = "RenderStarsOS";
            private const string starInputTexID = "_Celestial_Stars_Input";
            private const string starRenderTargetName = "RenderStars";

            GameObject starGameObject = null;

            public void GetStarObject()
            {
                if (starGameObject == null)
                {
                    StarObject temp = FindObjectOfType<StarObject>();

                    if (temp != null)
                        starGameObject = temp.gameObject;
                }
            }

            public StarRenderPass()
            {
                starsRT.Init(starRenderTargetName);
            }


            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                rtDescriptor.msaaSamples = 1;
                rtDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                cmd.GetTemporaryRT(starsRT.id, rtDescriptor);

                ConfigureTarget(starsRT.id);
                ConfigureClear(ClearFlag.All, Color.black);

                GetStarObject();
            }


            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (starGameObject == null)
                    return;

                #region Define Drawing and Filtering Settings
                DrawingSettings drawingSettings = CreateDrawingSettings(RenderPassHelpers.shaderTagIds, ref renderingData, SortingCriteria.None);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent, RenderPassHelpers.FindMaskInt(starGameObject));
                #endregion

                CommandBuffer cmd = CommandBufferPool.Get(featureDescription);

                #region Write Stars to Star Render Texture
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                cmd.SetGlobalTexture(starInputTexID, starsRT.Identifier());
                #endregion

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

        }


        class MoonRenderPass : ScriptableRenderPass
        {
            RenderTargetHandle moonRT;
            MoonObject moonObject = null;
            private const string moonRTHandleName = "Moon RT";
            private const string moonRenderID = "_Celestial_Moon_Input";
            private const string featureDescription = "Render Moon OS";
            private const RenderTextureFormat targetFormat = RenderTextureFormat.DefaultHDR;

            public MoonRenderPass()
            {
                moonRT.Init(moonRTHandleName);
                RenderPassHelpers.GetMoonObject(ref moonObject);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderPassHelpers.Configure(ref cmd, cameraTextureDescriptor, targetFormat, moonRT, this, moonObject);

            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderPassHelpers.Execute(moonObject, featureDescription, moonRenderID, true, moonRT, ref context, ref renderingData, this);
            }


            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(moonRT.id);
            }
        }

        class MaskRenderPass : ScriptableRenderPass
        {
            RenderTargetHandle maskRT;

            MoonObject moonObject = null;
            private const string maskRTHandleName = "Mask RT";
            private const string moonMaskTexID = "_Celestial_Merge_Mask";
            private const string featureDescription = "Render Mask OS";
            private const RenderTextureFormat targetFormat = RenderTextureFormat.R8;


            public MaskRenderPass()
            {
                maskRT.Init(maskRTHandleName);
                RenderPassHelpers.GetMoonObject(ref moonObject);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderPassHelpers.Configure(ref cmd, cameraTextureDescriptor, targetFormat, maskRT, this, moonObject);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderPassHelpers.Execute(moonObject, featureDescription, moonMaskTexID, false, maskRT, ref context, ref renderingData, this);
            }


            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(maskRT.id);
            }

        }

        class MergeCelestialRenderPass : ScriptableRenderPass
        {
            private const string featureDescription = "Merge Celestial OS";
            private const string mergeRenderTargetName = "CelestialMergeTargetOS";
            private const string mergeShaderPath = "Shader Graphs/Celestial Merge Pass_OS";
            private Material starMergeMaterial = null;
            private RenderTargetHandle starMergeTarget;

            public MergeCelestialRenderPass()
            {
                starMergeTarget.Init(mergeRenderTargetName);
                if (starMergeMaterial == null) starMergeMaterial = CoreUtils.CreateEngineMaterial(mergeShaderPath);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor descriptor = cameraTextureDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                cmd.GetTemporaryRT(starMergeTarget.id, descriptor);
                ConfigureTarget(starMergeTarget.id);
                ConfigureClear(ClearFlag.Color, Color.black);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(featureDescription);

                RenderPassHelpers.SetFadeFalloff(starMergeMaterial, 0.2f);
                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

                #region Merge Stars to Screen
                Blit(cmd, source, starMergeTarget.Identifier(), starMergeMaterial);
                Blit(cmd, starMergeTarget.Identifier(), source);
                #endregion

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(starMergeTarget.id);
            }
        }

        StarRenderPass starRenderPass;
        MoonRenderPass moonRenderPass;
        MaskRenderPass maskRenderPass;
        MergeCelestialRenderPass mergeCelestialRenderPass;

        StarObject starObject;
        MoonObject moonObject;


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
            starObject = FindObjectOfType<StarObject>();
            moonObject = FindObjectOfType<MoonObject>();
            if(starObject != null && moonObject != null)
            {
                starRenderPass = new StarRenderPass();
                moonRenderPass = new MoonRenderPass();
                maskRenderPass = new MaskRenderPass();
                mergeCelestialRenderPass = new MergeCelestialRenderPass();


                // Configures where the render pass should be injected.
                starRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
                moonRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 2;
                maskRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 3;
                mergeCelestialRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 4;
            }
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(starObject != null && moonObject != null)
            {
                Camera camera = renderingData.cameraData.camera;
                if (camera.name == "Preview Camera" || camera.name == "Preview Scene Camera")
                    return;

                if (camera.cameraType == CameraType.Reflection)
                    return;

                renderer.EnqueuePass(starRenderPass);
                renderer.EnqueuePass(moonRenderPass);
                renderer.EnqueuePass(maskRenderPass);

                renderer.EnqueuePass(mergeCelestialRenderPass);
            }
        }


        private static class RenderPassHelpers
        {
            public static List<ShaderTagId> shaderTagIds = new List<ShaderTagId>()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForwardOnly")
        };

            public static int FindMaskInt(GameObject gameObject)
            {
                return LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            }

            public static class ShaderParam
            {
                public static int overrideMaskEnabled = Shader.PropertyToID("_OVERRIDE_MATERIAL_MOON_ENABLED");
                public static int fadeFalloff = Shader.PropertyToID("_Celestial_Merge_Fade_Falloff");
            }

            public static void SetFadeFalloff(Material mat, float val)
            {
                mat.SetFloat(ShaderParam.fadeFalloff, val);
            }

            public static void Configure(ref CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor, RenderTextureFormat targetFormat, RenderTargetHandle rtHandle, ScriptableRenderPass pass, MoonObject moonObject)
            {
                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                rtDescriptor.colorFormat = targetFormat;
                cmd.GetTemporaryRT(rtHandle.id, rtDescriptor);
                pass.ConfigureTarget(rtHandle.id);
                pass.ConfigureClear(ClearFlag.Color, Color.black);
            }

            public static void Execute(MoonObject moonObject, string desc, string targetTex, bool overrideMask, RenderTargetHandle rtHandle, ref ScriptableRenderContext context, ref RenderingData renderingData, ScriptableRenderPass pass)
            {
                if (moonObject == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(desc);

                #region Define Drawing and Filtering Settings
                DrawingSettings drawingSettings = pass.CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.None);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, FindMaskInt(moonObject.gameObject));
                #endregion

                #region Write Moon to Moon RT
                cmd.SetGlobalInt(ShaderParam.overrideMaskEnabled, overrideMask ? 1 : 0);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                cmd.SetGlobalTexture(targetTex, rtHandle.Identifier());
                #endregion

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public static void GetMoonObject(ref MoonObject moonObject)
            {
                if (moonObject == null)
                {
                    moonObject = FindObjectOfType<MoonObject>();
                }
            }
        }
    }
}