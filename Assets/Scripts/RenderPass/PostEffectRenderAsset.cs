using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "PostEffectRenderAsset", menuName = "FRP/RenderPass/postEffectPass")]
    public class PostEffectRenderAsset : FRenderPassAsset
    {
        [SerializeField]
        public ComputeShader ssprComputeShader;
        public override FRenderPass CreateRenderPass()
        {
            return new PostEffectRenderPass(this);
        }
    }

    public class PostEffectRenderPass : FRenderPassRender<PostEffectRenderAsset>
    {
        private class ShaderPropertyID
        {
            public int dest;
            public int source;
            public ShaderPropertyID()
            {
                dest = Shader.PropertyToID("_DestImage");
                source = Shader.PropertyToID("_SrcImage");
            }
        }

        public int Compute_KernelID;
        public int Compute_KernelDepthTestID;
        private PostEffectRenderAsset renderAsset;
        public PostEffectRenderPass(PostEffectRenderAsset asset) : base(asset)
        {
            renderAsset = asset;
            if(renderAsset.ssprComputeShader == null)
            {
                renderAsset.ssprComputeShader = Resources.Load<ComputeShader>("Shader/SSPRCompute");
            }
            
        }
        private static readonly string BUFFER_NAME = "PostEffectPass";
        public FRPRenderSettings settings;
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };
        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();

        public static Mesh mesh
        {
            get
            {
                if (m_mesh != null)
                    return m_mesh;
                m_mesh = new Mesh();
                m_mesh.vertices = new Vector3[] {
                new Vector3(-1,-1,0.5f),
                new Vector3(-1,1,0.5f),
                new Vector3(1,1,0.5f),
                new Vector3(1,-1,0.5f)
            };
                m_mesh.uv = new Vector2[] {
                new Vector2(0,1),
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1)
            };

                m_mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
                return m_mesh;
            }
        }

        public Material ssprMat;

        RenderTargetIdentifier screenSrcID = new RenderTargetIdentifier(shaderPropertyID.source);
        RenderTargetIdentifier screenDestID = new RenderTargetIdentifier(shaderPropertyID.dest);
        public static Mesh m_mesh;
        public override void Render()
        {
            if (camera != Camera.main) return;
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            Compute_KernelID = renderAsset.ssprComputeShader.FindKernel("GenRelfectMap");
            Compute_KernelDepthTestID = renderAsset.ssprComputeShader.FindKernel("GenReflectMapDepthTest");
            settings = renderingData.settings;
            context.SetupCameraProperties(camera);
            ssprMat = MaterialPool.GetMaterial("Unlit/SSPR");

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();


            buffer.GetTemporaryRT(shaderPropertyID.source, camera.pixelWidth, camera.pixelHeight, 0);
            buffer.GetTemporaryRT(shaderPropertyID.dest, camera.pixelWidth, camera.pixelHeight, 0);
            buffer.Blit(renderingData.ColorTarget, screenSrcID);
            //buffer.Blit(rawImage, screenImage, new Material(Shader.Find("Unlit/Blur")), 0);
            // start postEffect
            SSPRRender();

            buffer.Blit(screenDestID, renderingData.ColorTarget);
            buffer.ReleaseTemporaryRT(shaderPropertyID.source);
            buffer.ReleaseTemporaryRT(shaderPropertyID.dest);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }


        private void SSPRRender()
        {
            int size = 2048;
            int res = Shader.PropertyToID("_Result");
            int camPID = Shader.PropertyToID("_CamearP");
            int screenColor = Shader.PropertyToID("_ScreenColor");
            int srcScreenSizeInfo = Shader.PropertyToID("_ScreenSizeInfo");
            var cameraInv_P = GL.GetGPUProjectionMatrix(renderingData.sourceProjectionMatrix,false)* renderingData.sourceViewMatrix;
            RenderTextureDescriptor desc = new RenderTextureDescriptor(size,size,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 32);
            desc.enableRandomWrite = true;
            desc.sRGB = false;
            //Debug.Log("fzy com:"+Compute_KernelID)
            buffer.GetTemporaryRT(res,desc,FilterMode.Bilinear);
            buffer.SetGlobalTexture("_CameraDepthTex",renderingData.DepthTarget);
            buffer.SetComputeMatrixParam(renderAsset.ssprComputeShader,camPID, cameraInv_P.inverse);
            buffer.SetComputeVectorParam(renderAsset.ssprComputeShader,srcScreenSizeInfo,new Vector4(camera.pixelWidth,camera.pixelHeight,1.0f/(float)camera.pixelWidth,1.0f/(float)camera.pixelHeight));
            buffer.SetComputeMatrixParam(renderAsset.ssprComputeShader,"_CamearVP", cameraInv_P);

            buffer.SetComputeTextureParam(renderAsset.ssprComputeShader,Compute_KernelID,screenColor,screenSrcID);
            buffer.SetComputeTextureParam(renderAsset.ssprComputeShader,Compute_KernelID,res,res);
            buffer.DispatchCompute(renderAsset.ssprComputeShader,Compute_KernelID,size/8,size/8,1);

            buffer.SetComputeTextureParam(renderAsset.ssprComputeShader,Compute_KernelDepthTestID,screenColor,screenSrcID);
            buffer.SetComputeTextureParam(renderAsset.ssprComputeShader,Compute_KernelDepthTestID,res,res);
            buffer.DispatchCompute(renderAsset.ssprComputeShader,Compute_KernelDepthTestID,size/8,size/8,1);
            
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.SetGlobalTexture("_Test",res); 
            buffer.ReleaseTemporaryRT(res);
            buffer.Blit(screenSrcID, screenDestID, ssprMat);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public override void Cleanup()
        {
        }
    }

}
