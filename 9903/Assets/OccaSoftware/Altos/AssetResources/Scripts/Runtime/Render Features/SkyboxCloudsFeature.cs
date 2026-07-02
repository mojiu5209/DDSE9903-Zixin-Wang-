using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Altos
{
    public class SkyboxCloudsFeature : ScriptableRendererFeature
    {
        class VolumetricCloudsRenderPass : ScriptableRenderPass
        {
            #region RT Handles
            private RenderTargetHandle cloudTarget;
            private RenderTargetHandle temporalTarget;
            private RenderTargetHandle upscaleHalfRes;
            private RenderTargetHandle upscaleQuarterRes;
            private RenderTargetHandle mergeTarget;
            private RenderTargetHandle depthTex;
            private RenderTargetHandle depthQuarterRes;
            #endregion

            #region Input vars
            private const string profilerTag = "Render Volumetric Clouds OS";
            private VolumetricCloudVolume cloudVolume;
            public VolumetricCloudVolume CloudVolume
			{
                get => cloudVolume;
                set => cloudVolume = value;
			}
            #endregion

            #region Shader Variable References
            private const string mergePassInputTextureShaderReference = "_MERGE_PASS_INPUT_TEX";
            private const string colorHistoryId = "_PREVIOUS_TAA_CLOUD_RESULTS";
            private const string depthId = "_DitheredDepthTex";
            #endregion

            #region Texture Ids
            private const string cloudId = "_CloudRenderPass";
            private const string upscaleHalfId = "_CloudUpscaleHalfResTarget";
            private const string upscaleQuarterId = "_CloudUpscaleQuarterResTarget";
            private const string taaId = "_CloudTemporalIntegration";
            private const string mergeId = "_CloudSceneMergeTarget";
            #endregion

            #region Shader Paths
            private const string upscaleShaderpath = "Shader Graphs/UpscaleClouds_OS";
            private const string temporalAntiAliasingShaderpath = "Shader Graphs/TemporalIntegration_OS";
            private const string cameraMergeShaderpath = "Shader Graphs/MergeClouds_OS";
            private const string renderpassShaderpath = "Shader Graphs/RenderClouds_OS";
            private const string ditherDepthShaderpath = "Shader Graphs/DitherDepth_OS";
            #endregion

            #region Materials
            private Material cloudRenderPassMaterial;
            private Material cloudTAAMaterial;
            private Material cloudMergeMaterial;
            private Material upscaleMaterial;
            private Material ditherDepth;
            #endregion

            // RT Desc.
            RenderTextureDescriptor cloudRenderDescriptor;

            // TAA Class
            TemporalAA taa;
            
            


            public VolumetricCloudsRenderPass()
            {
                // Create TAA handler
                taa = new TemporalAA();


                // Setup RT Handles
                cloudTarget.Init(cloudId);
                upscaleHalfRes.Init(upscaleHalfId);
                upscaleQuarterRes.Init(upscaleQuarterId);
                temporalTarget.Init(taaId);
                mergeTarget.Init(mergeId);
                depthTex.Init(depthId);
            }

            public void Setup(VolumetricCloudVolume cloudVolume)
			{
                this.cloudVolume = cloudVolume;

                // Setup Materials
                if (cloudRenderPassMaterial == null) cloudRenderPassMaterial = CoreUtils.CreateEngineMaterial(renderpassShaderpath);
                if (cloudMergeMaterial == null) cloudMergeMaterial = CoreUtils.CreateEngineMaterial(cameraMergeShaderpath);
                if (cloudTAAMaterial == null) cloudTAAMaterial = CoreUtils.CreateEngineMaterial(temporalAntiAliasingShaderpath);
                if (upscaleMaterial == null) upscaleMaterial = CoreUtils.CreateEngineMaterial(upscaleShaderpath);
                if (ditherDepth == null) ditherDepth = CoreUtils.CreateEngineMaterial(ditherDepthShaderpath);
            }

            


            private static class TimeManager
            {
                private static float managedTime = 0;
                private static uint frameCount = 0;

                public static float ManagedTime
                {
                    get => managedTime;
                }

                public static uint FrameCount
                {
                    get => frameCount;
                }

                public static void Update()
                {
                    float unityRealtimeSinceStartup = Time.realtimeSinceStartup;
                    uint unityFrameCount = (uint)Time.frameCount;

                    bool newFrame;
                    if (Application.isPlaying)
                    {
                        newFrame = frameCount != unityFrameCount;
                        frameCount = unityFrameCount;
                    }
                    else
                    {
                        newFrame = (unityRealtimeSinceStartup - managedTime) > 0.0166f;
                        if (newFrame)
                            frameCount++;
                    }

                    if (newFrame)
                    {
                        managedTime = unityRealtimeSinceStartup;
                    }
                }
            }

            private class TemporalAA
			{
                public TemporalAA()
				{
                    //
				}

                private Dictionary<Camera, TAACameraData> temporalData = new Dictionary<Camera, TAACameraData>();
                public Dictionary<Camera, TAACameraData> TemporalData
				{
                    get => temporalData;
				}


                public void Cleanup()
				{
                    CleanupDictionary();
                }


                internal class TAACameraData
                {
                    private uint lastFrameUsed;
                    private RenderTexture colorTexture;
                    private string cameraName;
                    private Matrix4x4 prevViewProj;

                    public TAACameraData(uint lastFrameUsed, RenderTexture colorTexture, string cameraName)
                    {
                        LastFrameUsed = lastFrameUsed;
                        ColorTexture = colorTexture;
                        CameraName = cameraName;
                        prevViewProj = Matrix4x4.identity;
                    }

                    public uint LastFrameUsed
                    {
                        get => lastFrameUsed;
                        set => lastFrameUsed = value;
                    }

                    public RenderTexture ColorTexture
                    {
                        get => colorTexture;
                        set => colorTexture = value;
                    }


                    public string CameraName
                    {
                        get => cameraName;
                        set => cameraName = value;
                    }

                    public Matrix4x4 PrevViewProj
					{
                        get => prevViewProj;
                        set => prevViewProj = value;
					}
                }

                public bool IsTemporalDataValid(Camera camera, RenderTextureDescriptor descriptor)
                {
                    if (temporalData.TryGetValue(camera, out TAACameraData cameraData))
                    {
                        bool isColorTexValid = IsRenderTextureValid(descriptor, cameraData.ColorTexture);

                        if (isColorTexValid)
                            return true;
                    }

                    return false;


                    bool IsRenderTextureValid(RenderTextureDescriptor desc, RenderTexture rt)
                    {
                        if (rt == null)
                        {
                            return false;
                        }

                        bool rtWrongSize = (rt.width != desc.width || rt.height != desc.height) ? true : false;
                        if (rtWrongSize)
                        {
                            return false;
                        }

                        return true;
                    }
                }


                public void SetupTemporalData(Camera camera, RenderTextureDescriptor descriptor)
                {
                    SetupColorTexture(camera, descriptor, out RenderTexture color);

                    if (temporalData.ContainsKey(camera))
                    {
                        if (temporalData[camera].ColorTexture != null)
                            temporalData[camera].ColorTexture.Release();

                        temporalData[camera].ColorTexture = color;
                    }
                    else
                    {
                        temporalData.Add(camera, new TAACameraData(TimeManager.FrameCount, color, camera.name));
                    }

                    void SetupColorTexture(Camera camera, RenderTextureDescriptor descriptor, out RenderTexture renderTexture)
                    {
                        descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                        descriptor.depthBufferBits = 0;
                        descriptor.msaaSamples = 1;
                        descriptor.useDynamicScale = false;

                        renderTexture = new RenderTexture(descriptor);

                        ClearTexture(renderTexture);

                        renderTexture.name = camera.name + " Color History";
                        renderTexture.filterMode = FilterMode.Point;
                        renderTexture.wrapMode = TextureWrapMode.Clamp;

                        renderTexture.Create();
                    }
                }

                void ClearTexture(RenderTexture textureToClear)
                {
                    RenderTexture activeTexture = RenderTexture.active;
                    RenderTexture.active = textureToClear;
                    GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
                    RenderTexture.active = activeTexture;
                }


                void CleanupDictionary()
                {
                    List<Camera> removeTargets = new List<Camera>();
                    foreach (KeyValuePair<Camera, TAACameraData> entry in temporalData)
                    {
                        if (entry.Value.LastFrameUsed < TimeManager.FrameCount - 10)
                        {
                            if (entry.Value.ColorTexture != null)
                                entry.Value.ColorTexture.Release();

                            removeTargets.Add(entry.Key);
                        }
                    }

                    for (int i = 0; i < removeTargets.Count; i++)
                    {
                        temporalData.Remove(removeTargets[i]);
                    }
                }

                public Matrix4x4 GetCurrentViewProjection(Camera camera)
                {
                    var proj = camera.nonJitteredProjectionMatrix;
                    var view = camera.worldToCameraMatrix;
                    var viewProj = proj * view;
                    return viewProj;
                }

                public Matrix4x4 GetPreviousViewProjection(Camera camera)
                {
                    if (temporalData.TryGetValue(camera, out TAACameraData data))
                    {
                        return data.PrevViewProj;
                    }
                    else
                    {
                        return Matrix4x4.identity;
                    }
                }

                public void SetPreviousViewProjection(Camera camera, Matrix4x4 currentViewProjection)
                {
                    if (temporalData.ContainsKey(camera))
                    {
                        temporalData[camera].PrevViewProj = currentViewProjection;
                    }
                }
            }


            private void AssignDefaultDescriptorSettings(ref RenderTextureDescriptor desc, RenderTextureFormat format = RenderTextureFormat.DefaultHDR)
            {
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                desc.useDynamicScale = false;
                desc.colorFormat = format;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                CloudShaderParamHandler.SetCloudMaterialSettings(cloudVolume, cloudRenderPassMaterial);

                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                AssignDefaultDescriptorSettings(ref rtDescriptor);


                // Clouds
                cloudRenderDescriptor = rtDescriptor;
                cloudRenderDescriptor.height = (int)(cloudRenderDescriptor.height * cloudVolume.cloudData.renderScale);
                cloudRenderDescriptor.width = (int)(cloudRenderDescriptor.width * cloudVolume.cloudData.renderScale);
                CloudShaderParamHandler.SetRenderScale(cloudVolume.cloudData.renderScale);
                cmd.GetTemporaryRT(cloudTarget.id, cloudRenderDescriptor, FilterMode.Point);


                // Dithered Depth
                RenderTextureDescriptor depthDescriptor = cameraTextureDescriptor;
                AssignDefaultDescriptorSettings(ref depthDescriptor, RenderTextureFormat.RFloat);
                cmd.GetTemporaryRT(depthTex.id, depthDescriptor, FilterMode.Point);

                // Upscale
                if(cloudVolume.cloudData.renderScaleSelection == RenderScaleSelection.Half || cloudVolume.cloudData.renderScaleSelection == RenderScaleSelection.Quarter)
				{
                    cmd.GetTemporaryRT(upscaleHalfRes.id, rtDescriptor, FilterMode.Point);
                }

                if(cloudVolume.cloudData.renderScaleSelection == RenderScaleSelection.Quarter)
				{
                    RenderTextureDescriptor halfResDescriptor = rtDescriptor;
                    halfResDescriptor.width = (int)(rtDescriptor.width * 0.5f);
                    halfResDescriptor.height = (int)(rtDescriptor.height * 0.5f);
                    cmd.GetTemporaryRT(upscaleQuarterRes.id, halfResDescriptor, FilterMode.Point);
                }

                
                // TAA
                cmd.GetTemporaryRT(temporalTarget.id, rtDescriptor, FilterMode.Point);

                // Merge
                cmd.GetTemporaryRT(mergeTarget.id, rtDescriptor, FilterMode.Point);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                TimeManager.Update();

                Camera camera = renderingData.cameraData.camera;

                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);


                #region Render Clouds
                CloudShaderParamHandler.SetDepthCulling(cloudVolume, cloudRenderPassMaterial);
                CloudShaderParamHandler.SetDepthCulling(cloudVolume, cloudMergeMaterial);

                int noiseId = (int)TimeManager.FrameCount % 64;
                Texture2D n = Resources.Load<Texture2D>($"BlueNoise/LDR_LLL1_{noiseId}");
                cmd.SetGlobalTexture("_BLUE_NOISE", n);
                cmd.SetGlobalInt("_FrameId", (int)TimeManager.FrameCount);
                cmd.SetGlobalFloat("_CLOUD_RENDER_SCALE", cloudVolume.cloudData.renderScale);

                bool useDitheredDepth = cloudVolume.cloudData.depthCullOptions == DepthCullOptions.RenderLocal && cloudVolume.cloudData.renderScaleSelection != RenderScaleSelection.Full ? true : false;
                cmd.SetGlobalInt("_USE_DITHERED_DEPTH", useDitheredDepth ? 1 : 0);
                Blit(cmd, source, depthTex.Identifier(), ditherDepth);
                cmd.SetGlobalTexture("_DitheredDepthTexture", depthTex.Identifier());
                
                Blit(cmd, source, cloudTarget.Identifier(), cloudRenderPassMaterial);
                #endregion


                #region Upscale
                RenderTargetIdentifier taaInput = cloudTarget.Identifier();
                if(cloudVolume.cloudData.renderScaleSelection != RenderScaleSelection.Full)
				{
                    if(cloudVolume.cloudData.renderScaleSelection == RenderScaleSelection.Quarter)
					{
                        cmd.SetGlobalFloat("_UPSCALE_SOURCE_RENDER_SCALE", 0.25f);
                        Blit(cmd, cloudTarget.Identifier(), upscaleQuarterRes.Identifier(), upscaleMaterial);

                        cmd.SetGlobalFloat("_UPSCALE_SOURCE_RENDER_SCALE", 0.5f);
                        Blit(cmd, upscaleQuarterRes.Identifier(), upscaleHalfRes.Identifier(), upscaleMaterial);
                        taaInput = upscaleHalfRes.Identifier();
                    }

                    if(cloudVolume.cloudData.renderScaleSelection == RenderScaleSelection.Half)
					{
                        cmd.SetGlobalFloat("_UPSCALE_SOURCE_RENDER_SCALE", 0.5f);
                        Blit(cmd, cloudTarget.Identifier(), upscaleHalfRes.Identifier(), upscaleMaterial);
                        taaInput = upscaleHalfRes.Identifier();
                    }
                }
                #endregion
                


                #region TAA
                if (cloudVolume.cloudData.taaEnabled && cloudVolume.cloudData.taaBlendFactor < 1.0f)
                {
                    Matrix4x4 viewProj = taa.GetCurrentViewProjection(camera);
                    cmd.SetGlobalMatrix("_ViewProjM", viewProj);
                    cmd.SetGlobalMatrix("_PrevViewProjM", taa.GetPreviousViewProjection(camera));

                    taa.SetPreviousViewProjection(camera, viewProj);

                    bool isTemporalDataValid = taa.IsTemporalDataValid(camera, renderingData.cameraData.cameraTargetDescriptor);
					if (!isTemporalDataValid)
					{
                        taa.SetupTemporalData(camera, renderingData.cameraData.cameraTargetDescriptor);
                        CloudShaderParamHandler.IgnoreTAAThisFrame(cloudVolume, cloudTAAMaterial);
                    }
					else
					{
                        CloudShaderParamHandler.ConfigureTAAParams(cloudVolume, cloudTAAMaterial);
                        cmd.SetGlobalTexture(colorHistoryId, taa.TemporalData[camera].ColorTexture);
                        taa.TemporalData[camera].LastFrameUsed = TimeManager.FrameCount;
                    }

                    Blit(cmd, taaInput, temporalTarget.Identifier(), cloudTAAMaterial);
                    Blit(cmd, temporalTarget.Identifier(), taa.TemporalData[camera].ColorTexture);

                    cmd.SetGlobalTexture(mergePassInputTextureShaderReference, temporalTarget.Identifier());
                    
                    taa.TemporalData[camera].LastFrameUsed = TimeManager.FrameCount;
                }
				else
				{
                    cmd.SetGlobalTexture(mergePassInputTextureShaderReference, taaInput);
				}
				#endregion
                

				#region Merge with Scene View
				Blit(cmd, source, mergeTarget.Identifier(), cloudMergeMaterial);
                Blit(cmd, mergeTarget.Identifier(), source);
                #endregion

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                Profiler.EndSample();
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                taa.Cleanup();
                cmd.ReleaseTemporaryRT(depthTex.id);

                cmd.ReleaseTemporaryRT(cloudTarget.id);

                cmd.ReleaseTemporaryRT(upscaleHalfRes.id);
                cmd.ReleaseTemporaryRT(upscaleQuarterRes.id);

                cmd.ReleaseTemporaryRT(temporalTarget.id);

                cmd.ReleaseTemporaryRT(mergeTarget.id);
            }
        }


        VolumetricCloudsRenderPass cloudRenderPass;

        #region Excluded Camera Targets
        private const string previewCameraName = "Preview Camera";
        private const string previewSceneCameraName = "Preview Scene Camera";
        #endregion

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
            cloudRenderPass = new VolumetricCloudsRenderPass();
            cloudRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        private bool IsValid(Camera camera, ref VolumetricCloudVolume cloudVolume)
		{
            if (cloudVolume == null || cloudVolume.cloudData == null)
                return false;

            if (camera.name == previewCameraName || camera.name == previewSceneCameraName)
                return false;

            if (camera.cameraType == CameraType.SceneView && !cloudVolume.cloudData.renderInSceneView)
                return false;

            if (camera.cameraType == CameraType.Reflection)
                return false;

            return true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VolumetricCloudVolume cloudVolume = FindObjectOfType<VolumetricCloudVolume>();

            if (IsValid(renderingData.cameraData.camera, ref cloudVolume))
            {
                cloudRenderPass.Setup(cloudVolume);
                renderer.EnqueuePass(cloudRenderPass);
            }
        }

        private class CloudShaderParamHandler
        {
            public static class ShaderParams
            {
                public static int renderScaleShaderReference = Shader.PropertyToID("_CLOUD_RENDER_SCALE");
                public static int depthCullReference = Shader.PropertyToID("_CLOUD_DEPTH_CULL_ON");
                public static int taaBlendFactorReference = Shader.PropertyToID("_TAA_BLEND_FACTOR");

                public static class CloudData
                {
                    public static int _CLOUD_STEP_COUNT = Shader.PropertyToID("_CLOUD_STEP_COUNT");
                    public static int _CLOUD_BLUE_NOISE_STRENGTH = Shader.PropertyToID("_CLOUD_BLUE_NOISE_STRENGTH");
                    public static int _CLOUD_BASE_TEX = Shader.PropertyToID("_CLOUD_BASE_TEX");
                    public static int _CLOUD_DETAIL1_TEX = Shader.PropertyToID("_CLOUD_DETAIL1_TEX");
                    public static int _CLOUD_EXTINCTION_COEFFICIENT = Shader.PropertyToID("_CLOUD_EXTINCTION_COEFFICIENT");
                    public static int _CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_COVERAGE");
                    public static int _CLOUD_SUN_COLOR_MASK = Shader.PropertyToID("_CLOUD_SUN_COLOR_MASK");
                    public static int _CLOUD_LAYER_HEIGHT = Shader.PropertyToID("_CLOUD_LAYER_HEIGHT");
                    public static int _CLOUD_LAYER_THICKNESS = Shader.PropertyToID("_CLOUD_LAYER_THICKNESS");
                    public static int _CLOUD_FADE_DIST = Shader.PropertyToID("_CLOUD_FADE_DIST");
                    public static int _CLOUD_BASE_SCALE = Shader.PropertyToID("_CLOUD_BASE_SCALE");
                    public static int _CLOUD_DETAIL1_SCALE = Shader.PropertyToID("_CLOUD_DETAIL1_SCALE");
                    public static int _CLOUD_DETAIL1_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL1_STRENGTH");
                    public static int _CLOUD_BASE_TIMESCALE = Shader.PropertyToID("_CLOUD_BASE_TIMESCALE");
                    public static int _CLOUD_DETAIL1_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL1_TIMESCALE");
                    public static int _CLOUD_FOG_POWER = Shader.PropertyToID("_CLOUD_FOG_POWER");
                    public static int _CLOUD_MAX_LIGHTING_DIST = Shader.PropertyToID("_CLOUD_MAX_LIGHTING_DIST");
                    public static int _CLOUD_PLANET_RADIUS = Shader.PropertyToID("_CLOUD_PLANET_RADIUS");

                    public static int _CLOUD_CURL_TEX = Shader.PropertyToID("_CLOUD_CURL_TEX");
                    public static int _CLOUD_CURL_SCALE = Shader.PropertyToID("_CLOUD_CURL_SCALE");
                    public static int _CLOUD_CURL_STRENGTH = Shader.PropertyToID("_CLOUD_CURL_STRENGTH");
                    public static int _CLOUD_CURL_TIMESCALE = Shader.PropertyToID("_CLOUD_CURL_TIMESCALE");
                    public static int _CLOUD_CURL_ADJUSTMENT_BASE = Shader.PropertyToID("_CLOUD_CURL_ADJUSTMENT_BASE");

                    public static int _CLOUD_DETAIL2_TEX = Shader.PropertyToID("_CLOUD_DETAIL2_TEX");
                    public static int _CLOUD_DETAIL2_SCALE = Shader.PropertyToID("_CLOUD_DETAIL2_SCALE");
                    public static int _CLOUD_DETAIL2_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL2_TIMESCALE");
                    public static int _CLOUD_DETAIL2_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL2_STRENGTH");

                    public static int _CLOUD_HGFORWARD = Shader.PropertyToID("_CLOUD_HGFORWARD");
                    public static int _CLOUD_HGBACK = Shader.PropertyToID("_CLOUD_HGBACK");
                    public static int _CLOUD_HGBLEND = Shader.PropertyToID("_CLOUD_HGBLEND");
                    public static int _CLOUD_HGSTRENGTH = Shader.PropertyToID("_CLOUD_HGSTRENGTH");

                    public static int _CLOUD_AMBIENT_EXPOSURE = Shader.PropertyToID("_CLOUD_AMBIENT_EXPOSURE");
                    public static int _CLOUD_DISTANT_COVERAGE_START_DEPTH = Shader.PropertyToID("_CLOUD_DISTANT_COVERAGE_START_DEPTH");
                    public static int _CLOUD_DISTANT_CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_DISTANT_CLOUD_COVERAGE");
                    public static int _CLOUD_DETAIL1_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL1_HEIGHT_REMAP");

                    public static int _CLOUD_DETAIL1_INVERT = Shader.PropertyToID("_CLOUD_DETAIL1_INVERT");
                    public static int _CLOUD_DETAIL2_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL2_HEIGHT_REMAP");
                    public static int _CLOUD_DETAIL2_INVERT = Shader.PropertyToID("_CLOUD_DETAIL2_INVERT");
                    public static int _CLOUD_HEIGHT_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_HEIGHT_DENSITY_INFLUENCE");
                    public static int _CLOUD_COVERAGE_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_COVERAGE_DENSITY_INFLUENCE");

                    public static int _CLOUD_HIGHALT_TEX_1 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_1");
                    public static int _CLOUD_HIGHALT_TEX_2 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_2");
                    public static int _CLOUD_HIGHALT_TEX_3 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_3");

                    public static int _CLOUD_HIGHALT_OFFSET1 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET1");
                    public static int _CLOUD_HIGHALT_OFFSET2 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET2");
                    public static int _CLOUD_HIGHALT_OFFSET3 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET3");
                    public static int _CLOUD_HIGHALT_SCALE1 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE1");
                    public static int _CLOUD_HIGHALT_SCALE2 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE2");
                    public static int _CLOUD_HIGHALT_SCALE3 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE3");
                    public static int _CLOUD_HIGHALT_COVERAGE = Shader.PropertyToID("_CLOUD_HIGHALT_COVERAGE");
                    public static int _CLOUD_HIGHALT_INFLUENCE1 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE1");
                    public static int _CLOUD_HIGHALT_INFLUENCE2 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE2");
                    public static int _CLOUD_HIGHALT_INFLUENCE3 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE3");
                    public static int _CLOUD_BASE_RGBAInfluence = Shader.PropertyToID("_CLOUD_BASE_RGBAInfluence");
                    public static int _CLOUD_DETAIL1_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL1_RGBAInfluence");
                    public static int _CLOUD_DETAIL2_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL2_RGBAInfluence");
                    public static int _CLOUD_HIGHALT_EXTINCTION = Shader.PropertyToID("_CLOUD_HIGHALT_EXTINCTION");

                    public static int _CLOUD_HIGHALT_SHAPE_POWER = Shader.PropertyToID("_CLOUD_HIGHALT_SHAPE_POWER");
                    public static int _CLOUD_SCATTERING_AMPGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_AMPGAIN");
                    public static int _CLOUD_SCATTERING_DENSITYGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_DENSITYGAIN");
                    public static int _CLOUD_SCATTERING_OCTAVES = Shader.PropertyToID("_CLOUD_SCATTERING_OCTAVES");

                    public static int _CLOUD_SUBPIXEL_JITTER_ON = Shader.PropertyToID("_CLOUD_SUBPIXEL_JITTER_ON");
                    public static int _CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_CLOUD_WEATHERMAP_TEX");
                    public static int _CLOUD_WEATHERMAP_VELOCITY = Shader.PropertyToID("_CLOUD_WEATHERMAP_VELOCITY");
                    public static int _CLOUD_WEATHERMAP_SCALE = Shader.PropertyToID("_CLOUD_WEATHERMAP_SCALE");
                    public static int _CLOUD_WEATHERMAP_VALUE_RANGE = Shader.PropertyToID("_CLOUD_WEATHERMAP_VALUE_RANGE");
                    public static int _USE_CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_USE_CLOUD_WEATHERMAP_TEX");

                    public static int _CLOUD_DENSITY_CURVE_TEX = Shader.PropertyToID("_CLOUD_DENSITY_CURVE_TEX");
                }
            }


            public static void SetRenderScale(float renderScale)
            {
                Shader.SetGlobalFloat(ShaderParams.renderScaleShaderReference, renderScale);
            }

            public static void SetDepthCulling(VolumetricCloudVolume cloudVolume, Material material)
            {
                if (cloudVolume == null)
                    return;

                int renderLocal = 0;
                if (cloudVolume.cloudData.depthCullOptions == DepthCullOptions.RenderLocal)
                    renderLocal = 1;

                material.SetInt(ShaderParams.depthCullReference, renderLocal);
            }

            public static void ConfigureTAAParams(VolumetricCloudVolume cloudVolume, Material material)
            {
                if (cloudVolume == null)
                    return;

                material.SetFloat(ShaderParams.taaBlendFactorReference, cloudVolume.cloudData.taaBlendFactor);
            }

            public static void IgnoreTAAThisFrame(VolumetricCloudVolume cloudVolume, Material material)
            {
                if (cloudVolume == null)
                    return;

                material.SetFloat(ShaderParams.taaBlendFactorReference, 1f);
            }

            public static void SetCloudMaterialSettings(VolumetricCloudVolume cloudVolume, Material cloudRenderMaterial)
            {
                if (cloudVolume == null)
                    return;

                VolumetricCloudsDefinitionScriptableObject cloudData = cloudVolume.cloudData;

                cloudRenderMaterial.SetFloat(ShaderParams.renderScaleShaderReference, cloudData.renderScale);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_AMBIENT_EXPOSURE, cloudData.ambientExposure);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_RGBAInfluence, cloudData.baseTextureRGBAInfluence);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_SCALE, cloudData.baseTextureScale);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_BASE_TEX, cloudData.baseTexture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_TIMESCALE, cloudData.baseTextureTimescale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_BLUE_NOISE_STRENGTH, cloudData.blueNoise);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_COVERAGE, cloudData.cloudiness);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_COVERAGE_DENSITY_INFLUENCE, cloudData.cloudinessDensityInfluence);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_SCALE, cloudData.curlTextureScale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_STRENGTH, cloudData.curlTextureInfluence);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_CURL_TEX, cloudData.curlTexture);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_TIMESCALE, cloudData.curlTextureTimescale);

                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_HEIGHT_REMAP, cloudData.detail1TextureHeightRemap);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_RGBAInfluence, cloudData.detail1TextureRGBAInfluence);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_SCALE, cloudData.detail1TextureScale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DETAIL1_STRENGTH, cloudData.detail1TextureInfluence);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_DETAIL1_TEX, cloudData.detail1Texture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_TIMESCALE, cloudData.detail1TextureTimescale);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DISTANT_CLOUD_COVERAGE, cloudData.distantCoverageAmount);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DISTANT_COVERAGE_START_DEPTH, cloudData.distantCoverageDepth);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_EXTINCTION_COEFFICIENT, cloudData.extinctionCoefficient);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_FADE_DIST, cloudData.cloudFadeDistance);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_FOG_POWER, cloudData.fogPower);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HEIGHT_DENSITY_INFLUENCE, cloudData.heightDensityInfluence);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGFORWARD, cloudData.HGEccentricityForward);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGBACK, cloudData.HGEccentricityBackward);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGBLEND, cloudData.HGBlend);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGSTRENGTH, cloudData.HGStrength);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_COVERAGE, cloudData.highAltCloudiness);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_EXTINCTION, cloudData.highAltExtinctionCoefficient);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE1, cloudData.highAltStrength1);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE2, cloudData.highAltStrength2);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE3, cloudData.highAltStrength3);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET1, cloudData.highAltTimescale1);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET2, cloudData.highAltTimescale2);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET3, cloudData.highAltTimescale3);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE1, cloudData.highAltScale1);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE2, cloudData.highAltScale2);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE3, cloudData.highAltScale3);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_1, cloudData.highAltTex1);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_2, cloudData.highAltTex2);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_3, cloudData.highAltTex3);


                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_LAYER_HEIGHT, cloudData.cloudLayerHeight);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_LAYER_THICKNESS, cloudData.cloudLayerThickness);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_MAX_LIGHTING_DIST, cloudData.maxLightingDistance);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_PLANET_RADIUS, cloudData.planetRadius);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_SCATTERING_AMPGAIN, cloudData.multipleScatteringAmpGain);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_SCATTERING_DENSITYGAIN, cloudData.multipleScatteringDensityGain);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_SCATTERING_OCTAVES, cloudData.multipleScatteringOctaves);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_STEP_COUNT, cloudData.stepCount);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_SUN_COLOR_MASK, cloudData.sunColor);


                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_SUBPIXEL_JITTER_ON, cloudData.subpixelJitterEnabled == true ? 1 : 0);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_WEATHERMAP_TEX, cloudData.weathermapTexture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_WEATHERMAP_VELOCITY, cloudData.weathermapVelocity);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_WEATHERMAP_SCALE, cloudData.weathermapScale);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._USE_CLOUD_WEATHERMAP_TEX, cloudData.weathermapType == WeathermapType.Texture ? 1 : 0);

                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_DENSITY_CURVE_TEX, cloudData.curve.GetTexture());
            }
        }
    }

}
