using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "PostEffectRenderAsset", menuName = "FRP/RenderPass/postEffectPass")]
    public class PostEffectRenderAsset : FRenderPassAsset
    {
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
        public PostEffectRenderPass(PostEffectRenderAsset asset) : base(asset)
        {
            if(asset.ssprComputeShader == null)
            {
                asset.ssprComputeShader = Resources.Load<ComputeShader>("Shader/SSPRCompute");
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
            Compute_KernelID = asset.ssprComputeShader.FindKernel("GenRelfectMap");
            settings = renderingData.settings;
            context.SetupCameraProperties(camera);
            ssprMat = MaterialPool.GetMaterial("Unlit/SSPR");


            buffer.BeginSample("Postprocess Pass");
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();


            buffer.GetTemporaryRT(shaderPropertyID.source, camera.pixelWidth, camera.pixelHeight, 0);
            buffer.GetTemporaryRT(shaderPropertyID.dest, camera.pixelWidth, camera.pixelHeight, 0);
            buffer.Blit(BuiltinRenderTextureType.CameraTarget, screenSrcID);
            //buffer.Blit(rawImage, screenImage, new Material(Shader.Find("Unlit/Blur")), 0);
            // start postEffect
            SSPRRender();

            buffer.Blit(screenDestID, BuiltinRenderTextureType.CameraTarget);
            buffer.ReleaseTemporaryRT(shaderPropertyID.source);
            buffer.ReleaseTemporaryRT(shaderPropertyID.dest);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            buffer.EndSample("Postprocess Pass");

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }


        private void SSPRRender()
        {
            int res = Shader.PropertyToID("_Result");
            buffer.GetTemporaryRT(res,2048,2048,32,FilterMode.Bilinear);
            buffer.SetComputeTextureParam(asset.ssprComputeShader,Compute_KernelID,"_DepthNormal",new RenderTargetIdentifier(Shader.PropertyToID("_DepthNormal")));
            buffer.SetComputeTextureParam(asset.ssprComputeShader,Compute_KernelID,res,res);
            buffer.DispatchCompute(asset.ssprComputeShader,Compute_KernelID,2048/8,2048/8,1);
            buffer.SetGlobalTexture("_Test",res);
            buffer.ReleaseTemporaryRT(res);
            buffer.Blit(screenSrcID, screenDestID, ssprMat);
        }

        public override void Cleanup()
        {
        }
    }

}
