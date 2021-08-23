using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(fileName = "PostEffectRenderAsset", menuName = "FRP/RenderPass/postEffectPass")]
    public class PostEffectRenderAsset : FRenderPassAsset
    {
        public override FRenderPass CreateRenderPass()
        {
            return new PostEffectRenderPass(this);
        }
    }

    public class PostEffectRenderPass : FRenderPassRender<PostEffectRenderAsset>
    {
        public PostEffectRenderPass(PostEffectRenderAsset asset):base(asset)
        {

        }
        private static readonly string BUFFER_NAME = "PostEffectPass";
        public FRPRenderSettings settings;
        CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };


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
static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
    public static Mesh m_mesh;
        public override void Render()
        {
            if(camera != Camera.main)return;
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            settings = renderingData.settings;

context.SetupCameraProperties(camera);

// buffer.GetTemporaryRT(cameraColorTextureId,camera.pixelWidth, camera.pixelHeight, 0);
// buffer.SetRenderTarget(cameraColorTextureId);
// buffer.ClearRenderTarget(true,true,Color.black);
// context.ExecuteCommandBuffer(buffer);
// buffer.Clear(); 
// buffer.SetRenderTarget(renderingData.ColorTarget);
// buffer.SetGlobalTexture("_MainTex",cameraColorTextureId);
// buffer.DrawProcedural(Matrix4x4.identity,new Material(Shader.Find("Unlit/Blur")),0,MeshTopology.Triangles,3);
// context.ExecuteCommandBuffer(buffer);
// buffer.Clear(); 

    int screenImage = Shader.PropertyToID("_ScreenImage");
    int src = screenImage;
    int dst = Shader.PropertyToID("_PostprocessRT_0");
        CommandBuffer cmd = CommandBufferPool.Get("Postprocess Pass");

        cmd.BeginSample("Postprocess Pass");

        context.ExecuteCommandBuffer(buffer);
        cmd.Clear(); 
                int rawImage = Shader.PropertyToID("_RawScreenImage");
                cmd.GetTemporaryRT(rawImage, camera.pixelWidth, camera.pixelHeight, 0);
                cmd.GetTemporaryRT(screenImage, camera.pixelWidth, camera.pixelHeight, 0);
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, rawImage);
                cmd.Blit(rawImage, screenImage, new Material(Shader.Find("Unlit/Blur")), 0);
                cmd.ReleaseTemporaryRT(rawImage);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                src = screenImage;

                cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
                cmd.ReleaseTemporaryRT(screenImage);
                cmd.ReleaseTemporaryRT(src);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.EndSample("Postprocess Pass");
buffer.ReleaseTemporaryRT(cameraColorTextureId);
            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear(); 
        }


        public override void Cleanup()
        {
        }
    }

}
