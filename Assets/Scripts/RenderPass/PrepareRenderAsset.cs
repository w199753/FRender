using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace frp
{
    //-负责搞lighting数据和shadow数据,要放在renderpass的第一个
    [CreateAssetMenu(fileName = "PrepareRenderAsset", menuName = "FRP/RenderPass/preParePass")]
    public class PrepareRenderAsset : FRenderPassAsset
    {
        [SerializeField]
        public Cubemap shCubemap;
        public Cubemap prefilterMap;
        [SerializeField]
        public ComputeShader shComputeShader;
        public override FRenderPass CreateRenderPass()
        {
            return new PrepareRenderPass(this);
        }
    }
    public class CSMCorners
    {
        public Vector3[] Far = new Vector3[4];
        public Vector3[] Near = new Vector3[4];

        public static CSMCorners Copy(CSMCorners corners)
        {
            CSMCorners temp = new CSMCorners();
            for (int i = 0; i < 4; i++)
            {
                temp.Near[i] = new Vector3(corners.Near[i].x, corners.Near[i].y, corners.Near[i].z);
                temp.Far[i] = new Vector3(corners.Far[i].x, corners.Far[i].y, corners.Far[i].z);
            }
            return temp;
        }
    }
    public class PrepareRenderPass : FRenderPassRender<PrepareRenderAsset>
    {
        private class ShaderPropertyID
        {
            public int lightData;
            public int lightCount;

            public int shSampleDirsID;
            public int shOutputResultID;
            public int shCoeffBufferID;
            public int shCubemapID;

            public int smShadowMap;
            public int smVPArray;
            public int smSplitNears;
            public int smSplitFars;
            public int smType;
            public int smTempDepth;

            public int depthNormalTex;
            public ShaderPropertyID()
            {
                lightData = Shader.PropertyToID("_LightData");
                lightCount = Shader.PropertyToID("_LightCount");

                shSampleDirsID = Shader.PropertyToID("SampleDirs");
                shOutputResultID = Shader.PropertyToID("Result");
                shCoeffBufferID = Shader.PropertyToID("sh_coeff");
                shCubemapID = Shader.PropertyToID("CubeMap");

                smType = Shader.PropertyToID("_ShadowType");
                smShadowMap = Shader.PropertyToID("_SMShadowMap");
                smVPArray = Shader.PropertyToID("_LightVPArray");
                smSplitNears = Shader.PropertyToID("_LightSplitNear");
                smSplitFars = Shader.PropertyToID("_LightSplitFar");
                smTempDepth = Shader.PropertyToID("_TempDepth");

                depthNormalTex = Shader.PropertyToID("_DepthNormal");
            }
        }

        RenderTargetIdentifier cameraTargetID = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        RenderTargetIdentifier sourceTempTexID = new RenderTargetIdentifier(Shader.PropertyToID("u_Source22Texture"));
        RenderTargetIdentifier sourceTexID = new RenderTargetIdentifier(Shader.PropertyToID("u_SourceTexture"));
        RenderTargetIdentifier smid = new RenderTargetIdentifier(shaderPropertyID.smShadowMap);
        RenderTargetIdentifier smtempID = new RenderTargetIdentifier(shaderPropertyID.smTempDepth);
        private const int SHORDER = 2;
        private const string BUFFER_NAME = "PreRender";
        FRPRenderSettings settings;
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };
        ComputeBuffer lightDataBuffer;
        ComputeBuffer shOutputBuffer;
        ComputeBuffer shDirBuffer;
        ComputeBuffer shCoeffBuffer;
        List<LightingData> lightDataList;
        private int prevLightCount = -1;
        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
        private int SH_Compute_KernelID;
        private PrepareRenderAsset renderAsset;
        Material boxFilterMat ;
        Material guassFilterMat ;

        private int SHCoeffLength
        {
            get { return (SHORDER + 1) * (SHORDER + 1); }
        }

        Vector2[] drawOrder = new Vector2[4]{
                new Vector2(-1,-1),
                new Vector2(-1,1),
                new Vector2(1,1),
                new Vector2(1,-1)
            };
        Matrix4x4[] vpArray = new Matrix4x4[4];
        public PrepareRenderPass(PrepareRenderAsset asset) : base(asset)
        {
            renderAsset = asset;

            prevLightCount = -1;
            if (renderAsset.shCubemap == null)
            {
                renderAsset.shCubemap = Resources.Load<Cubemap>("Test3");
            }
            if (renderAsset.prefilterMap == null)
            {
                renderAsset.prefilterMap = Resources.Load<Cubemap>("Test4");
            }
            if (renderAsset.shComputeShader == null)
            {
                renderAsset.shComputeShader = Resources.Load<ComputeShader>("Shader/SHCompute");
            }
            shOutputBuffer = new ComputeBuffer(SHCoeffLength, Marshal.SizeOf(typeof(Vector3)));
            shDirBuffer = new ComputeBuffer(32 * 32, Marshal.SizeOf(typeof(Vector3)));
            shCoeffBuffer = new ComputeBuffer(SHCoeffLength, Marshal.SizeOf(typeof(Vector3)));
            shDirBuffer.SetData(StaticData.dirs);
            InitShadowData();
        }


        public override void Render()
        {
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            settings = renderingData.settings;
            renderingData.lightingData.Clear();
            renderingData.shadowData.Clear();

            RenderPrepareLight();
            RenderPrepareSH();
            RenderPrepareShadow();
            RenderPrepareDepthNormal();


            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        private void RenderPrepareLight()
        {
            int idx = 0;
            NativeArray<VisibleLight> visibleLights = renderingData.cullingResults.visibleLights;
            int lightCount = visibleLights.Length;
            if (lightCount == 0) return;
            if (lightCount != prevLightCount)
            {
                if (lightDataBuffer != null) lightDataBuffer.Release();
                lightDataBuffer = new ComputeBuffer(lightCount, Marshal.SizeOf(typeof(LightingData)));
                lightDataList = new List<LightingData>(lightCount);
            }
            foreach (var light in visibleLights)
            {
                Matrix4x4 local2world = light.localToWorldMatrix;
                LightingData data = new LightingData();
                if (light.lightType == LightType.Directional)
                {
                    data.geometry = local2world.GetColumn(2).normalized;
                    data.pos_type = -data.geometry;
                    data.geometry.w = float.MaxValue;
                    data.pos_type.w = 1;
                    data.color = light.finalColor;
                }
                else if (light.lightType == LightType.Point)
                {
                    data.geometry = local2world.GetColumn(2).normalized;
                    data.geometry.w = light.range;
                    data.pos_type = new Vector4(local2world.m03, local2world.m13, local2world.m23, 2);
                    data.color = light.finalColor;
                }
                else if (light.lightType == LightType.Spot)
                {

                }
                renderingData.lightingData.Add(idx++, data);
            }
            lightDataBuffer.SetData<LightingData>(renderingData.lightingData.Values.GetValueList<int, LightingData>());
            buffer.SetGlobalInt(shaderPropertyID.lightCount, lightCount);
            buffer.SetGlobalBuffer(shaderPropertyID.lightData, lightDataBuffer);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            prevLightCount = lightCount;
        }

        private void RenderPrepareSH()
        {
            SH_Compute_KernelID = renderAsset.shComputeShader.FindKernel("SHCompute");
            Vector3Int[] zero = new Vector3Int[9];
            for (int i = 0; i < zero.Length; i++)
            {
                zero[i] = Vector3Int.zero;
            }
            Vector3Int[] res = new Vector3Int[9];

            renderAsset.shComputeShader.SetTexture(SH_Compute_KernelID, shaderPropertyID.shCubemapID, renderAsset.shCubemap);
            buffer.SetGlobalTexture(shaderPropertyID.shCubemapID, renderAsset.shCubemap);
            buffer.SetGlobalTexture(Shader.PropertyToID("TestPrefilter"), renderAsset.prefilterMap);
            renderAsset.shComputeShader.SetBuffer(SH_Compute_KernelID, shaderPropertyID.shOutputResultID, shOutputBuffer);
            renderAsset.shComputeShader.SetBuffer(SH_Compute_KernelID, shaderPropertyID.shSampleDirsID, shDirBuffer);
            shOutputBuffer.SetData(zero);
            //camera.RenderToCubemap(cb);
            renderAsset.shComputeShader.Dispatch(SH_Compute_KernelID, 1, 1, 1);
            shOutputBuffer.GetData(res);
            Vector3[] res_f = new Vector3[9];
            for (int i = 0; i < 9; i++)
            {
                res_f[i] = res[i];
                res_f[i] = res_f[i] * 1.22718359375e-6f;
            }
            shCoeffBuffer.SetData(res_f);
            buffer.SetGlobalBuffer(shaderPropertyID.shCoeffBufferID, shCoeffBuffer);
        }

        CSMCorners[] cameraCorners = new CSMCorners[4];
        CSMCorners[] lightCorners = new CSMCorners[4];

        Vector3[] SplitPosition = new Vector3[4];
        Quaternion[] SplitRotate = new Quaternion[4];
        Matrix4x4[] SplitMatrix = new Matrix4x4[4];

        private void InitShadowData()
        {
            for (int i = 0; i < 4; i++)
            {
                cameraCorners[i] = new CSMCorners();
                lightCorners[i] = new CSMCorners();
                for (int j = 0; j < 4; j++)
                {
                    lightCorners[i].Near[j] = new Vector3();
                    lightCorners[i].Far[j] = new Vector3();
                }
                SplitPosition[i] = new Vector3();
                SplitRotate[i] = new Quaternion();
                SplitMatrix[i] = new Matrix4x4();
                SplitMatrix[i] = Matrix4x4.identity;
            }
        }

        //感觉太耗了，目前每种光源只支持一个shadow!
        private void RenderPrepareShadow()
        {

            if (camera.cameraType != CameraType.Game) camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            if (renderingData.cullingResults.visibleLights.Length == 0) return;
            bool hasDirLightShadow = false;
            bool hasPointLightShadow = false;
            bool hasSpotLightShadow = false;
            foreach (var light in renderingData.cullingResults.visibleLights)
            {
                if (light.lightType == LightType.Directional && hasDirLightShadow == false)
                {
                    hasDirLightShadow = true;
                    PrepareDirectionLightShadow();
                }
                else if (light.lightType == LightType.Point && hasPointLightShadow == false)
                {
                    hasPointLightShadow = true;
                }
                else if (light.lightType == LightType.Spot && hasSpotLightShadow == false)
                {
                    hasSpotLightShadow = true;
                }
            }
        }

        private void DrawShadows(Matrix4x4 project, Matrix4x4 viewMatrix, int cascadeIndex)
        {
            CullingResults m_cullingResults = new CullingResults();
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
            {
                cullParam.isOrthographic = true;
                for (int i = 0; i < cullParam.cullingPlaneCount; i++)
                {
                    cullParam.SetCullingPlane(i, GetCullingPlane(i, cascadeIndex));
                }
                m_cullingResults = context.Cull(ref cullParam);
            }
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(settings.shadowSetting.shadowResolution, settings.shadowSetting.shadowResolution, GraphicsFormat.R32G32B32A32_SFloat, 32);
            renderTextureDescriptor.useMipMap = true;
            renderTextureDescriptor.autoGenerateMips = true;
            renderTextureDescriptor.mipCount = (int)Mathf.Log(settings.shadowSetting.shadowResolution, 2) + 1;
            buffer.GetTemporaryRT(shaderPropertyID.smTempDepth, renderTextureDescriptor, FilterMode.Point);
            //buffer.GetTemporaryRT(shaderPropertyID.smTempDepth, settings.shadowSetting.shadowResolution, settings.shadowSetting.shadowResolution, 32, FilterMode.Point, GraphicsFormat.R32G32B32A32_SFloat);
            buffer.SetRenderTarget(shaderPropertyID.smTempDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            //filteringSettings.sortingLayerRange = SortingLayerRange.all;
            DrawingSettings drawingSettings = new DrawingSettings() { sortingSettings = sortingSettings };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            if (settings.shadowSetting.shadowType == ShadowType.PCF || settings.shadowSetting.shadowType == ShadowType.SM)
            {
                // drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_ShadowCaster_SM"));
                drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_ShadowCaster_SM"));
                buffer.CopyTexture(shaderPropertyID.smTempDepth, 0, 0, shaderPropertyID.smShadowMap, cascadeIndex, 0);
            }
            else if (settings.shadowSetting.shadowType == ShadowType.VSM)
            {
                drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_ShadowCaster_VSM"));
                //buffer.CopyTexture(shaderPropertyID.smTempDepth, 0, 0, shaderPropertyID.smShadowMap, cascadeIndex, 0);
                int sourceTex = Shader.PropertyToID("u_SourceTexture");
                PrefilterShadowMap(sourceTex);
                buffer.CopyTexture(sourceTex, 0, 0, shaderPropertyID.smShadowMap, cascadeIndex, 0);
                buffer.ReleaseTemporaryRT(sourceTex);
            }
            else if (settings.shadowSetting.shadowType == ShadowType.ESM)
            {
                drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_ShadowCaster_ESM"));

                int sourceTex = Shader.PropertyToID("u_SourceTexture");
                PrefilterShadowMap(sourceTex);
                buffer.CopyTexture(sourceTex, 0, 0, shaderPropertyID.smShadowMap, cascadeIndex, 0);
                buffer.ReleaseTemporaryRT(sourceTex);
            }
            else if (settings.shadowSetting.shadowType == ShadowType.EVSM)
            {

            }
            sortingSettings.criteria = SortingCriteria.RenderQueue;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.all;
            drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
            context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);


            var proj = GL.GetGPUProjectionMatrix(project, false);
            vpArray[cascadeIndex] = proj * viewMatrix;
            buffer.SetRenderTarget(cameraTargetID, cameraTargetID);
            //buffer.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.ReleaseTemporaryRT(shaderPropertyID.smTempDepth);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

        }

        private void PrefilterShadowMap(int sourceTex)
        {
            Profiler.BeginSample("Blur ShadowMap");
            if(guassFilterMat == null) guassFilterMat = MaterialPool.GetMaterial("Unlit/Blur");
            int mipCount = (int)Mathf.Log(settings.shadowSetting.shadowResolution, 2) + 1;
            int sourceTempTex = Shader.PropertyToID("u_Source22Texture");
            RenderTextureDescriptor desc = new RenderTextureDescriptor(settings.shadowSetting.shadowResolution, settings.shadowSetting.shadowResolution, GraphicsFormat.R32G32B32A32_SFloat, 32);
            desc.useMipMap = true;
            desc.autoGenerateMips = true;
            desc.mipCount = mipCount;
            buffer.GetTemporaryRT(sourceTex, desc, FilterMode.Bilinear);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.SetGlobalTexture("u_CoarserTexture", shaderPropertyID.smTempDepth);
            int size = 1;
            buffer.SetGlobalVector("g_focus", new Vector4(0, 0, 0, 0));
            //using (new ProfilingScope(buffer, new ProfilingSampler("Blur ShadowMap")))
            {
                if(boxFilterMat == null) boxFilterMat = MaterialPool.GetMaterial("Unlit/NewBoxFilter");

                if (settings.enableSSAO == true)
                {
                    for (int i = mipCount - 1; i >= 0; i--)
                    {
                        buffer.GetTemporaryRT(sourceTempTex, size, size, 32, FilterMode.Bilinear, GraphicsFormat.R32G32B32A32_SFloat);
                        buffer.SetGlobalInt("g_level", i);
                        buffer.SetGlobalTexture("u_SourceTexture", sourceTex);
                        buffer.Blit(sourceTexID, sourceTempTexID, boxFilterMat);
                        buffer.CopyTexture(sourceTempTexID, 0, 0, sourceTexID, 0, i);
                        size <<= 1;
                        buffer.ReleaseTemporaryRT(sourceTempTex);
                    }
                }
                else
                {
                    buffer.Blit(smtempID, sourceTexID, guassFilterMat);
                }
                Profiler.EndSample();
            }
        }

        private void PrepareDirectionLightShadow()
        {
            //计算视椎体四个顶点
            float near = camera.nearClipPlane;
            float far = Mathf.Clamp(settings.shadowSetting.shadowDistance, settings.shadowSetting.shadowDistance, camera.farClipPlane);
            float[] nears = { near, far * 0.067f + near, far * 0.133f + far * 0.067f + near, far * 0.267f + far * 0.133f + far * 0.067f + near };
            float[] fars = { far * 0.067f + near, far * 0.133f + far * 0.067f + near, far * 0.267f + far * 0.133f + far * 0.067f + near, far };
            float height = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float width = height * camera.aspect;

            Matrix4x4 cameraLocal2World = camera.transform.localToWorldMatrix;

            var hasShadow = renderingData.cullingResults.GetShadowCasterBounds(0, out Bounds bounds);
            DrawBound(bounds, Color.red);
            //四个点顺序：左下，左上，右上，右下(下面的都同理)
            //var ttBounds = new Vector3[8];
            //for (int x = -1, i = 0; x <= 1; x += 2)
            //    for (int y = -1; y <= 1; y += 2)
            //        for (int z = -1; z <= 1; z += 2)
            //            ttBounds[i++] = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            if (hasShadow == false) return;
            var dirLight = renderingData.cullingResults.visibleLights[0].light;
            //变换包围盒
            var casterBoundVerts = new CSMCorners();
            casterBoundVerts.Near = new Vector3[4];
            casterBoundVerts.Far = new Vector3[4];
            casterBoundVerts.Near[0] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1)));
            casterBoundVerts.Near[1] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, -1)));
            casterBoundVerts.Near[2] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, -1)));
            casterBoundVerts.Near[3] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, -1)));
            casterBoundVerts.Far[0] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, 1)));
            casterBoundVerts.Far[1] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, 1)));
            casterBoundVerts.Far[2] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, 1)));
            casterBoundVerts.Far[3] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, 1)));
            for (int idx = 0; idx < 4; idx++)
            {
                casterBoundVerts.Near[idx] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(casterBoundVerts.Near[idx]);
                casterBoundVerts.Far[idx] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(casterBoundVerts.Far[idx]);
            }
            //计算出包围盒在在灯光空间下的样子
            Vector3[] nn = new Vector3[4];
            Vector3[] ff = new Vector3[4];
            for (int idx = 0; idx < 4; idx++)
            {
                var pNear = casterBoundVerts.Near[idx];
                var pFar = casterBoundVerts.Far[idx];
                nn[0] = new Vector3(Mathf.Min(nn[0].x, pNear.x, pFar.x), Mathf.Min(nn[0].y, pNear.y, pFar.y), Mathf.Min(nn[0].z, pNear.z, pFar.z));
                nn[1] = new Vector3(Mathf.Min(nn[1].x, pNear.x, pFar.x), Mathf.Max(nn[1].y, pNear.y, pFar.y), Mathf.Min(nn[1].z, pNear.z, pFar.z));
                nn[2] = new Vector3(Mathf.Max(nn[2].x, pNear.x, pFar.x), Mathf.Max(nn[2].y, pNear.y, pFar.y), Mathf.Min(nn[2].z, pNear.z, pFar.z));
                nn[3] = new Vector3(Mathf.Max(nn[3].x, pNear.x, pFar.x), Mathf.Min(nn[3].y, pNear.y, pFar.y), Mathf.Min(nn[3].z, pNear.z, pFar.z));

                ff[0] = new Vector3(Mathf.Min(ff[0].x, pNear.x, pFar.x), Mathf.Min(ff[0].y, pNear.y, pFar.y), Mathf.Max(ff[0].z, pNear.z, pFar.z));
                ff[1] = new Vector3(Mathf.Min(ff[1].x, pNear.x, pFar.x), Mathf.Max(ff[1].y, pNear.y, pFar.y), Mathf.Max(ff[1].z, pNear.z, pFar.z));
                ff[2] = new Vector3(Mathf.Max(ff[2].x, pNear.x, pFar.x), Mathf.Max(ff[2].y, pNear.y, pFar.y), Mathf.Max(ff[2].z, pNear.z, pFar.z));
                ff[3] = new Vector3(Mathf.Max(ff[3].x, pNear.x, pFar.x), Mathf.Min(ff[3].y, pNear.y, pFar.y), Mathf.Max(ff[3].z, pNear.z, pFar.z));
            }
            for (int idx = 0; idx < 4; idx++)
            {
                casterBoundVerts.Near[idx] = dirLight.transform.localToWorldMatrix.MultiplyPoint(nn[idx]);
                casterBoundVerts.Far[idx] = dirLight.transform.localToWorldMatrix.MultiplyPoint(ff[idx]);
            }
            //DrawAABB(casterBoundVerts);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var pNear = new Vector3(width * drawOrder[j].x, height * drawOrder[j].y, 1) * nears[i];
                    var pFar = new Vector3(width * drawOrder[j].x, height * drawOrder[j].y, 1) * fars[i];
                    cameraCorners[i].Near[j] = cameraLocal2World.MultiplyPoint(pNear);
                    cameraCorners[i].Far[j] = cameraLocal2World.MultiplyPoint(pFar);
                }
            }

            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(settings.shadowSetting.shadowResolution, settings.shadowSetting.shadowResolution, GraphicsFormat.R32G32B32A32_SFloat, 32);
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.autoGenerateMips = false;
            //renderTextureDescriptor.mipCount = 12;
            renderTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            renderTextureDescriptor.volumeDepth = 4;
            renderTextureDescriptor.enableRandomWrite = false;
            buffer.GetTemporaryRT(shaderPropertyID.smShadowMap, renderTextureDescriptor, FilterMode.Point);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var mainNearCor = cameraCorners[i].Near[j];
                    var mainFarCor = cameraCorners[i].Far[j];
                    lightCorners[i].Near[j] = SplitMatrix[i].inverse.MultiplyPoint(mainNearCor);
                    lightCorners[i].Far[j] = SplitMatrix[i].inverse.MultiplyPoint(mainFarCor);
                }

                var farDist = Vector3.Distance(lightCorners[i].Far[0], lightCorners[i].Far[2]);
                var crossDist = Vector3.Distance(lightCorners[i].Near[0], lightCorners[i].Far[2]);
                var maxDist = Mathf.Max(farDist,crossDist);

                //更新灯光视椎的八个顶点为boundingbox的点
                nn = new Vector3[4];
                ff = new Vector3[4];
                for (int idx = 0; idx < 4; idx++)
                {
                    var pNear = lightCorners[i].Near[idx];
                    var pFar = lightCorners[i].Far[idx];
                    nn[0] = new Vector3(Mathf.Min(nn[0].x, pNear.x, pFar.x), Mathf.Min(nn[0].y, pNear.y, pFar.y), Mathf.Min(nn[0].z, pNear.z, pFar.z));
                    nn[1] = new Vector3(Mathf.Min(nn[1].x, pNear.x, pFar.x), Mathf.Max(nn[1].y, pNear.y, pFar.y), Mathf.Min(nn[1].z, pNear.z, pFar.z));
                    nn[2] = new Vector3(Mathf.Max(nn[2].x, pNear.x, pFar.x), Mathf.Max(nn[2].y, pNear.y, pFar.y), Mathf.Min(nn[2].z, pNear.z, pFar.z));
                    nn[3] = new Vector3(Mathf.Max(nn[3].x, pNear.x, pFar.x), Mathf.Min(nn[3].y, pNear.y, pFar.y), Mathf.Min(nn[3].z, pNear.z, pFar.z));
                    //
                    ff[0] = new Vector3(Mathf.Min(ff[0].x, pNear.x, pFar.x), Mathf.Min(ff[0].y, pNear.y, pFar.y), Mathf.Max(ff[0].z, pNear.z, pFar.z));
                    ff[1] = new Vector3(Mathf.Min(ff[1].x, pNear.x, pFar.x), Mathf.Max(ff[1].y, pNear.y, pFar.y), Mathf.Max(ff[1].z, pNear.z, pFar.z));
                    ff[2] = new Vector3(Mathf.Max(ff[2].x, pNear.x, pFar.x), Mathf.Max(ff[2].y, pNear.y, pFar.y), Mathf.Max(ff[2].z, pNear.z, pFar.z));
                    ff[3] = new Vector3(Mathf.Max(ff[3].x, pNear.x, pFar.x), Mathf.Min(ff[3].y, pNear.y, pFar.y), Mathf.Max(ff[3].z, pNear.z, pFar.z));
                }
                for (int idx = 0; idx < 4; idx++)
                {
                    lightCorners[i].Near[idx] = nn[idx];
                    lightCorners[i].Far[idx] = ff[idx];
                }


                //把拆分后的相机包围盒计算后再变回到世界空间（保持变换后的boundingbox一致）然后求两平面距离，就是要移动z轴的距离
                var tLight = CSMCorners.Copy(lightCorners[i]);
                for (int idx = 0; idx < 4; idx++)
                {
                    tLight.Near[idx] = SplitMatrix[i].MultiplyPoint(tLight.Near[idx]);
                    tLight.Far[idx] = SplitMatrix[i].MultiplyPoint(tLight.Far[idx]);
                }
                var dis = Vector3.Distance(tLight.Near[0], casterBoundVerts.Near[0]);
                var normalDir1 = Vector3.Cross(tLight.Near[0] - tLight.Near[1], tLight.Near[2] - tLight.Near[1]).normalized;
                var normalDir2 = Vector3.Cross(casterBoundVerts.Near[0] - casterBoundVerts.Near[1], casterBoundVerts.Near[2] - casterBoundVerts.Near[1]).normalized;
                //根据平面方程求两平面间的距离
                var p = tLight.Near[0];
                var D1 = -Vector3.Dot(normalDir1, tLight.Near[0]);
                var D2 = -Vector3.Dot(normalDir2, casterBoundVerts.Near[0]);
                dis = Mathf.Abs(D1 - D2) / Vector3.Magnitude(normalDir1);
                //Debug.Log("fzy dis:" + dis);

                nn = new Vector3[4];
                for (int idx = 0; idx < 4; idx++)
                {
                    var pNear = lightCorners[i].Near[idx];
                    var pFar = lightCorners[i].Far[idx];
                    nn[0] = new Vector3(Mathf.Min(nn[0].x, pNear.x, pFar.x), Mathf.Min(nn[0].y, pNear.y, pFar.y), Mathf.Min(nn[0].z, pNear.z, pFar.z));
                    nn[1] = new Vector3(Mathf.Min(nn[1].x, pNear.x, pFar.x), Mathf.Max(nn[1].y, pNear.y, pFar.y), Mathf.Min(nn[1].z, pNear.z, pFar.z));
                    nn[2] = new Vector3(Mathf.Max(nn[2].x, pNear.x, pFar.x), Mathf.Max(nn[2].y, pNear.y, pFar.y), Mathf.Min(nn[2].z, pNear.z, pFar.z));
                    nn[3] = new Vector3(Mathf.Max(nn[3].x, pNear.x, pFar.x), Mathf.Min(nn[3].y, pNear.y, pFar.y), Mathf.Min(nn[3].z, pNear.z, pFar.z));
                }
                for (int idx = 0; idx < 4; idx++)
                {
                    lightCorners[i].Near[idx] = nn[idx];
                    if (D2 - D1 > 1)
                    {
                        lightCorners[i].Near[idx].z = nn[idx].z - dis;
                    }
                    else
                    {
                        lightCorners[i].Near[idx].z = nn[idx].z + dis;
                    }
                }

                var center = PixelAlignment(i, maxDist);
                SplitRotate[i] = dirLight.transform.rotation;
                SplitPosition[i] = SplitMatrix[i].MultiplyPoint(center);
                for (int idx = 0; idx < 4; idx++)
                {
                    tLight.Near[idx] = SplitMatrix[i].inverse.MultiplyPoint(tLight.Near[idx]);
                    tLight.Far[idx] = SplitMatrix[i].inverse.MultiplyPoint(tLight.Far[idx]);
                }
                SplitMatrix[i] = GetModelMatrix(SplitPosition[i], SplitRotate[i]);
                var viewMatrix = Matrix4x4.TRS(SplitPosition[i], SplitRotate[i], Vector3.one).inverse;
                var t = Matrix4x4.identity;
                t.m22 = -1;
                viewMatrix = t * viewMatrix;
                //if (SystemInfo.usesReversedZBuffer)
                //{
                //    //Debug.Log("123");
                //    viewMatrix.m20 = -viewMatrix.m20;
                //    viewMatrix.m21 = -viewMatrix.m21;
                //    viewMatrix.m22 = -viewMatrix.m22;
                //    viewMatrix.m23 = -viewMatrix.m23;
                //}
                var project = Matrix4x4.Ortho(-maxDist * 0.5f, maxDist * 0.5f, -maxDist * 0.5f, maxDist * 0.5f, lightCorners[i].Near[0].z, lightCorners[i].Far[0].z);
                buffer.SetViewProjectionMatrices(viewMatrix, project);

                DrawShadows(project, viewMatrix, i);
            }
            //DD();
            buffer.SetGlobalInt(shaderPropertyID.smType, (int)settings.shadowSetting.shadowType);
            buffer.SetGlobalVector(shaderPropertyID.smSplitNears, new Vector4(nears[0], nears[1], nears[2], nears[3]));
            buffer.SetGlobalVector(shaderPropertyID.smSplitFars, new Vector4(fars[0], fars[1], fars[2], fars[3]));
            buffer.SetGlobalMatrixArray(shaderPropertyID.smVPArray, vpArray);
            buffer.SetGlobalTexture(shaderPropertyID.smShadowMap, smid);
            buffer.ReleaseTemporaryRT(shaderPropertyID.smShadowMap);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            //reset camera params
            buffer.SetViewProjectionMatrices(renderingData.sourceViewMatrix, renderingData.sourceProjectionMatrix);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void PreparePointLightShadow()
        {

        }

        private void PrepareSpotLightShadow()
        {

        }


        private Vector3 GetPlaneNormal(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return Vector3.Cross(p0 - p1, p2 - p1).normalized;
        }

        //前后，上下，左右
        private Plane GetCullingPlane(int planeIndex, int cascadeIndex)
        {
            var tLight = lightCorners[cascadeIndex];

            var normal = new Vector3();
            var point = new Vector3();
            var mat = SplitMatrix[cascadeIndex];
            var nears = new Vector3[4];
            var fars = new Vector3[4];
            //return new Plane(Vector3.one,Vector3.one);
            for (int i = 0; i < 4; i++)
            {
                var near = mat.MultiplyPoint(tLight.Near[i]);
                nears[i] = new Vector3(near.x, near.y, near.z);

                var far = mat.MultiplyPoint(tLight.Far[i]);
                fars[i] = new Vector3(far.x, far.y, far.z);
            }

            if (planeIndex == 0)
            {
                normal = GetPlaneNormal(nears[0], nears[1], nears[2]);
                point = nears[0];
            }
            else if (planeIndex == 1)
            {
                normal = -GetPlaneNormal(fars[0], fars[1], fars[2]);
                point = fars[0];
            }
            else if (planeIndex == 2)
            {
                normal = GetPlaneNormal(nears[1], fars[1], fars[2]);
                point = nears[1];
            }
            else if (planeIndex == 3)
            {
                normal = -GetPlaneNormal(nears[0], fars[0], fars[3]);
                point = nears[0];
            }
            else if (planeIndex == 4)
            {
                normal = GetPlaneNormal(fars[0], fars[1], nears[1]);
                point = fars[0];
            }
            else if (planeIndex == 5)
            {
                normal = -GetPlaneNormal(fars[3], fars[2], nears[2]);
                point = fars[3];
            }
            return new Plane(normal, point);
        }

        private Vector3 PixelAlignment(int i, float maxDist)
        {
            //防止边缘抖动
            float minX = lightCorners[i].Near[0].x;
            float maxX = lightCorners[i].Near[2].x;
            float minY = lightCorners[i].Near[0].y;
            float maxY = lightCorners[i].Near[2].y;
            float minZ = lightCorners[i].Near[0].z;
            float unitPerTex = maxDist / (float)settings.shadowSetting.shadowResolution;
            var posx = (minX + maxX) * 0.5f;
            posx /= unitPerTex;
            posx = Mathf.FloorToInt(posx);
            posx *= unitPerTex;

            var posy = (minY + maxY) * 0.5f;
            posy /= unitPerTex;
            posy = Mathf.FloorToInt(posy);
            posy *= unitPerTex;

            var posz = minZ;
            posz /= unitPerTex;
            posz = Mathf.FloorToInt(posz);
            posz *= unitPerTex;
            return new Vector3(posx, posy, posz);
        }
        private Matrix4x4 GetModelMatrix(Vector3 position, Quaternion rotate)
        {
            float x = rotate.x;
            float y = rotate.y;
            float z = rotate.z;
            float w = rotate.w;
            var q00 = 1 - 2 * y * y - 2 * z * z;
            var q01 = 2 * x * y - 2 * z * w;
            var q02 = 2 * x * z + 2 * y * w;
            var q10 = 2 * x * y + 2 * z * w;
            var q11 = 1 - 2 * x * x - 2 * z * z;
            var q12 = 2 * y * z - 2 * x * w;
            var q20 = 2 * x * z - 2 * y * w;
            var q21 = 2 * y * z + 2 * x * w;
            var q22 = 1 - 2 * x * x - 2 * y * y;
            var modelMatrix =
                new Matrix4x4(
                new Vector4(q00, q10, q20, 0),
                new Vector4(q01, q11, q21, 0),
                new Vector4(q02, q12, q22, 0),
                new Vector4(position.x, position.y, position.z, 1)
                );
            return modelMatrix;
        }

        public void DrawBound(Bounds bounds, Color color)
        {
            var verts = new Vector2[]
            {
                    new Vector2(-1, -1),
                    new Vector2(1, -1),
                    new Vector2(1, 1),
                    new Vector2(-1, 1),
                    new Vector2(-1, -1),
            };
            for (var i = 0; i < 4; i++)
            {
                Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, 1)), color);
                Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, -1)), color);

                Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), color);
            }
        }

        private void DD()
        {
            CSMCorners[] fcs = new CSMCorners[4];
            for (int k = 0; k < 4; k++)
            {
                fcs[k] = new CSMCorners();
                Debug.DrawLine(cameraCorners[k].Near[1], cameraCorners[k].Near[2], Color.white, 0.1f);
                Debug.DrawLine(cameraCorners[k].Near[2], cameraCorners[k].Near[3], Color.white, 0.1f);
                Debug.DrawLine(cameraCorners[k].Near[3], cameraCorners[k].Near[0], Color.white, 0.1f);
                Debug.DrawLine(cameraCorners[k].Near[0], cameraCorners[k].Near[1], Color.white, 0.1f);

                Debug.DrawLine(cameraCorners[k].Far[1], cameraCorners[k].Far[2], Color.blue, 0.1f);
                Debug.DrawLine(cameraCorners[k].Far[2], cameraCorners[k].Far[3], Color.blue, 0.1f);
                Debug.DrawLine(cameraCorners[k].Far[3], cameraCorners[k].Far[0], Color.blue, 0.1f);
                Debug.DrawLine(cameraCorners[k].Far[0], cameraCorners[k].Far[1], Color.blue, 0.1f);

                fcs[k].Near = new Vector3[4];
                fcs[k].Far = new Vector3[4];

                for (int i = 0; i < 4; i++)
                {
                    //fcs[k].nearCorners[i] = lightSplitObject[k].transform.TransformPoint(lightCorners[k].nearCorners[i]);//dirLightCameraSplits[k].transform.TransformPoint(lightCorners[k].nearCorners[i]);
                    //fcs[k].farCorners[i] = lightSplitObject[k].transform.TransformPoint(lightCorners[k].farCorners[i]); //dirLightCameraSplits[k].transform.TransformPoint(lightCorners[k].farCorners[i]);
                    fcs[k].Near[i] = SplitMatrix[k].MultiplyPoint(lightCorners[k].Near[i]);
                    fcs[k].Far[i] = SplitMatrix[k].MultiplyPoint(lightCorners[k].Far[i]);
                    //fcs[k].nearCorners[i] = lightSplitObject[k].transform.TransformPoint( lightCorners[k].nearCorners[i]);
                    //fcs[k].farCorners[i] =  lightSplitObject[k].transform.TransformPoint( lightCorners[k].farCorners[i]); 
                }

                Debug.DrawLine(fcs[k].Near[0], fcs[k].Near[1], Color.red, 0.1f);
                Debug.DrawLine(fcs[k].Near[1], fcs[k].Near[2], Color.red, 0.1f);
                Debug.DrawLine(fcs[k].Near[2], fcs[k].Near[3], Color.red, 0.1f);
                Debug.DrawLine(fcs[k].Near[3], fcs[k].Near[0], Color.red, 0.1f);

                Debug.DrawLine(fcs[k].Far[0], fcs[k].Far[1], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Far[1], fcs[k].Far[2], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Far[2], fcs[k].Far[3], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Far[3], fcs[k].Far[0], Color.green, 0.1f);

                Debug.DrawLine(fcs[k].Near[0], fcs[k].Far[0], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Near[1], fcs[k].Far[1], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Near[2], fcs[k].Far[2], Color.green, 0.1f);
                Debug.DrawLine(fcs[k].Near[3], fcs[k].Far[3], Color.green, 0.1f);

            }
        }
        
        public void RenderPrepareDepthNormal()
        {
            var depthNormalCmd = CommandBufferPool.Get("DepthNormalPass");
            depthNormalCmd.BeginSample("DepthNormalPass");
            context.ExecuteCommandBuffer(depthNormalCmd);
            depthNormalCmd.Clear();
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(2048,2048, GraphicsFormat.R8G8B8A8_SRGB, 32);
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.autoGenerateMips = false;
            depthNormalCmd.GetTemporaryRT(shaderPropertyID.depthNormalTex, renderTextureDescriptor, FilterMode.Point);
            depthNormalCmd.SetRenderTarget(shaderPropertyID.depthNormalTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            depthNormalCmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(depthNormalCmd);
            depthNormalCmd.Clear();

            SortingSettings sortingSettings = new SortingSettings(camera);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            //filteringSettings.sortingLayerRange = SortingLayerRange.all;
            DrawingSettings drawingSettings = new DrawingSettings() { sortingSettings = sortingSettings };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            drawingSettings.SetShaderPassName(0,new ShaderTagId("FRP_DepthNormalPass"));
            sortingSettings.criteria = SortingCriteria.RenderQueue;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.all;
            context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
            depthNormalCmd.SetGlobalTexture(shaderPropertyID.depthNormalTex,shaderPropertyID.depthNormalTex);
            depthNormalCmd.ReleaseTemporaryRT(shaderPropertyID.depthNormalTex);
            depthNormalCmd.SetRenderTarget(cameraTargetID, cameraTargetID);
            depthNormalCmd.EndSample("DepthNormalPass");
            context.ExecuteCommandBuffer(depthNormalCmd);
            depthNormalCmd.Clear();
            CommandBufferPool.Release(depthNormalCmd);
        }

        #region TestFunc
        private void GetCameraSplitBox(int i)
        {
            var cor = cameraCorners[i];
            float[] xs = { cor.Near[0].x, cor.Near[1].x, cor.Near[2].x, cor.Near[3].x,
                   cor.Far[0].x, cor.Far[1].x, cor.Far[2].x, cor.Far[3].x };

            float[] ys = { cor.Near[0].y, cor.Near[1].y, cor.Near[2].y, cor.Near[3].y,
                   cor.Far[0].y, cor.Far[1].y, cor.Far[2].y, cor.Far[3].y };

            float[] zs = { cor.Near[0].z, cor.Near[1].z, cor.Near[2].z, cor.Near[3].z,
                   cor.Far[0].z, cor.Far[1].z, cor.Far[2].z, cor.Far[3].z };

            float minX = Mathf.Min(xs);
            float maxX = Mathf.Max(xs);

            float minY = Mathf.Min(ys);
            float maxY = Mathf.Max(ys);

            float minZ = Mathf.Min(zs);
            float maxZ = Mathf.Max(zs);

            CSMCorners debugCor = new CSMCorners();
            debugCor.Near = new Vector3[4];
            debugCor.Far = new Vector3[4];
            debugCor.Near[0] = new Vector3(minX, minY, minZ);
            debugCor.Near[1] = new Vector3(maxX, minY, minZ);
            debugCor.Near[2] = new Vector3(maxX, maxY, minZ);
            debugCor.Near[3] = new Vector3(minX, maxY, minZ);

            debugCor.Far[0] = new Vector3(minX, minY, maxZ);
            debugCor.Far[1] = new Vector3(maxX, minY, maxZ);
            debugCor.Far[2] = new Vector3(maxX, maxY, maxZ);
            debugCor.Far[3] = new Vector3(minX, maxY, maxZ);
            Debug.DrawLine(debugCor.Near[0], debugCor.Near[1], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Near[1], debugCor.Near[2], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Near[2], debugCor.Near[3], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Near[3], debugCor.Near[0], Color.blue, 0.01f);

            Debug.DrawLine(debugCor.Far[0], debugCor.Far[1], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[1], debugCor.Far[2], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[2], debugCor.Far[3], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[3], debugCor.Far[0], Color.blue, 0.01f);

            Debug.DrawLine(debugCor.Far[0], debugCor.Near[0], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[1], debugCor.Near[1], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[2], debugCor.Near[2], Color.blue, 0.01f);
            Debug.DrawLine(debugCor.Far[3], debugCor.Near[3], Color.blue, 0.01f);

        }

        private void DrawAABB(CSMCorners debugCor)
        {
            Debug.DrawLine(debugCor.Near[0], debugCor.Near[1], Color.magenta);
            Debug.DrawLine(debugCor.Near[1], debugCor.Near[2], Color.magenta);
            Debug.DrawLine(debugCor.Near[2], debugCor.Near[3], Color.magenta);
            Debug.DrawLine(debugCor.Near[3], debugCor.Near[0], Color.magenta);

            Debug.DrawLine(debugCor.Far[0], debugCor.Far[1], Color.blue);
            Debug.DrawLine(debugCor.Far[1], debugCor.Far[2], Color.blue);
            Debug.DrawLine(debugCor.Far[2], debugCor.Far[3], Color.blue);
            Debug.DrawLine(debugCor.Far[3], debugCor.Far[0], Color.blue);

            Debug.DrawLine(debugCor.Far[0], debugCor.Near[0], Color.blue);
            Debug.DrawLine(debugCor.Far[1], debugCor.Near[1], Color.blue);
            Debug.DrawLine(debugCor.Far[2], debugCor.Near[2], Color.blue);
            Debug.DrawLine(debugCor.Far[3], debugCor.Near[3], Color.blue);
        }

        private void DrawAABBMul(CSMCorners debugCor, Matrix4x4 trans, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                debugCor.Near[i] = trans.MultiplyPoint(debugCor.Near[i]);
                debugCor.Far[i] = trans.MultiplyPoint(debugCor.Far[i]);

            }
            Debug.DrawLine(debugCor.Near[0], debugCor.Near[1], Color.magenta);
            Debug.DrawLine(debugCor.Near[1], debugCor.Near[2], Color.magenta);
            Debug.DrawLine(debugCor.Near[2], debugCor.Near[3], Color.magenta);
            Debug.DrawLine(debugCor.Near[3], debugCor.Near[0], Color.magenta);

            Debug.DrawLine(debugCor.Far[0], debugCor.Far[1], color);
            Debug.DrawLine(debugCor.Far[1], debugCor.Far[2], color);
            Debug.DrawLine(debugCor.Far[2], debugCor.Far[3], color);
            Debug.DrawLine(debugCor.Far[3], debugCor.Far[0], color);

            Debug.DrawLine(debugCor.Far[0], debugCor.Near[0], color);
            Debug.DrawLine(debugCor.Far[1], debugCor.Near[1], color);
            Debug.DrawLine(debugCor.Far[2], debugCor.Near[2], color);
            Debug.DrawLine(debugCor.Far[3], debugCor.Near[3], color);
        }

        public override void Cleanup()
        {

        }
        #endregion

    }

}
