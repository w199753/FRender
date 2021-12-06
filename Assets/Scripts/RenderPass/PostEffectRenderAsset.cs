using System;
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
            var hasPostEffect = settings.enableSSPR | settings.enableSSAO;
            //Debug.Log(hasPostEffect);
            if(settings.enableSSPR == true)
            {
                SSPRRender();
            }
            if(settings.enableSSAO == true)
            {
                SSAORender();
            }
            if(hasPostEffect == false)
            {
                buffer.Blit(screenSrcID, screenDestID);
            }

            

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
            RenderTextureDescriptor desc = new RenderTextureDescriptor(size,size,UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, 32);
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

/*
uint HaltonSequence(uint Index, uint base = 3)
{
	uint result = 0;
	uint f = 1;
	uint i = Index;
	
	[unroll(255)] 
	while (i > 0) {
		result += (f / base) * (i % base);
		i = floor(i / base);
	}
	return result;
}
*/

/*
	float E1 = frac((float)Index / NumSamples + float(Random.x & 0xffff) / (1 << 16));
	float E2 = float(ReverseBits32(Index) ^ Random.y) * 2.3283064365386963e-10;
	return float2(E1, E2);
*/

uint ReverseBits32(uint bits)
{
	bits = (bits << 16) | (bits >> 16);
	bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
	bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
	bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
	bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
	return bits;
}
private uint HaltonSequence(uint Index, uint b = 3)
{
    uint result = 0;
	uint f = 1;
	uint i = Index;
	
	while (i > 0) {
		result += (f / b) * (i % b);
		i = (uint)Mathf.Floor(i / b);
	}
	return result;
}
public float Frac(float value) { return value - (float)Math.Truncate(value); }
private Vector2 Hammersley(uint Index, uint NumSamples, uint Random)
{
    float E1 = Frac((float)Index / NumSamples + (float)(Random & 0xffff) / (1 << 16));
	float E2 = (float)(ReverseBits32(Index) ) * 2.3283064365386963e-10f;
    return new Vector2(E1, E2);
}

Vector4 CosineSampleHemisphere(Vector2 E) {
	float Phi = 2 * Mathf.PI * E.x;
	float CosTheta = Mathf.Sqrt(E.y);
	float SinTheta = Mathf.Sqrt(1 - CosTheta * CosTheta);

	Vector3 L = new Vector3( SinTheta * Mathf.Cos(Phi), SinTheta * Mathf.Sin(Phi), CosTheta );
	float PDF = CosTheta / Mathf.PI;

	return new Vector4(L.x,L.y,L.z, PDF);
}

Matrix4x4 GetTangentBasis(Vector3 TangentZ) {
	Vector3 UpVector = Mathf.Abs(TangentZ.z) < 0.999 ? Vector3.forward : Vector3.right;
	Vector3 TangentX = Vector3.Normalize(Vector3.Cross( UpVector, TangentZ));
	Vector3 TangentY = Vector3.Cross(TangentZ, TangentX);
    Matrix4x4 res = Matrix4x4.identity;
    res.SetColumn(0,TangentX);
    res.SetColumn(1,TangentY);
    res.SetColumn(2,TangentZ);
	return res;
}

Vector3 TangentToWorld(Vector3 Vec, Vector3 TangentZ)
{
    return GetTangentBasis(TangentZ).inverse.MultiplyPoint(Vec);
}


        private void SSAORender()
        {
            Vector3 N = Vector3.up; //used with z
            const uint SAMPLE_COUNT = 64u;
            float PDF = 0;
            for(uint idx=0;idx<SAMPLE_COUNT;idx++)
            {
                Vector2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
                Vector4 sm = CosineSampleHemisphere(Xi);
                PDF = sm.w;
                Vector3 H = Vector3.Normalize(TangentToWorld(new Vector3(sm.x,sm.y,sm.z),N));
                Debug.Log("fzy hh"+H);
                var t = new GameObject(idx.ToString());
                t.GetComponent<Transform>().position = H;
                t.GetComponent<Transform>().localScale = Vector3.one*0.1f;
            }
            settings.enableSSAO = false;
        }

        public override void Cleanup()
        {
        }
    }

}
