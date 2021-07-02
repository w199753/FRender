using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class PreFilterEnvMapEditor : EditorWindow
{
    [MenuItem("FTools/Prefilter")]
    static void AddWindow()
    {
        Rect wr = new Rect(0, 0, 300, 500);
        PreFilterEnvMapEditor window = (PreFilterEnvMapEditor)EditorWindow.GetWindowWithRect(typeof(PreFilterEnvMapEditor), wr, true, "PrefilterEnv");
        window.Show();
    }
    public const int Irradiance_Resolution = 128;
    public const int Prefilter_Resolution = 512;
    public const int BRDF_Resolution = 512;
    public Material mat;
    public Cubemap envCubemap;

    public Texture2D previewTex;

    private bool AssertResource()
    {
        if (mat == null) { this.ShowNotification(new GUIContent("Mat 不能为空!")); return false; };
        if (envCubemap == null) { this.ShowNotification(new GUIContent("Cubemap 不能为空!")); return false; };
        return true;
    }
    private void OnGUI()
    {
        GUILayout.BeginVertical();
        mat = EditorGUILayout.ObjectField("Material", mat, typeof(Material), true) as Material;
        envCubemap = EditorGUILayout.ObjectField("Cubemap", envCubemap, typeof(Cubemap), true) as Cubemap;

        if (GUILayout.Button("Gen Irradiance Map", GUILayout.Width(300)))
        {
            if(AssertResource()) GenIrradiance();
        }

        if(GUILayout.Button("Gen Prefilter Map", GUILayout.Width(300)))
        {
            if(AssertResource()) GenPrefilterMap();
        }

        if(GUILayout.Button("Gen IntegrateBRDF Map", GUILayout.Width(300)))
        {
            if(mat == null) { this.ShowNotification(new GUIContent("Mat 不能为空!")); return; };
            GenIntegrateBRDF();
        }
        GUILayout.EndVertical();
    }

    private static int 
        cubeMapID = Shader.PropertyToID("_EnvMap"),
        currentRoughness = Shader.PropertyToID("_Roughness"),
        //*based right hand coordinate system*  0:(right) 1:(left) 2:(up) 3:(down) 4:(front) 5:(back)
        currentFaceID = Shader.PropertyToID("_FaceID"); 
        
    string[] faceName = new string[6]{"right","left","up","down","front","back"};

    CubemapFace[] cubemapFaces = new CubemapFace[6]{
        CubemapFace.NegativeX,
        CubemapFace.PositiveX,
        CubemapFace.NegativeY,
        CubemapFace.PositiveY,
        CubemapFace.PositiveZ,
        CubemapFace.NegativeZ,
    };
    Vector2Int[] posListx = new Vector2Int[6]{
        new Vector2Int(128*0,128*1),
        new Vector2Int(128*2,128*1),
        new Vector2Int(128*1,128*2),
        new Vector2Int(128*1,128*0),
        new Vector2Int(128*1,128*1),
        new Vector2Int(128*3,128*1),
    };

    private Vector2Int[] GetPosList(int resolution)
    {
        Vector2Int[] res = new Vector2Int[6]{
            new Vector2Int(resolution*0,resolution*1),
            new Vector2Int(resolution*2,resolution*1),
            new Vector2Int(resolution*1,resolution*2),
            new Vector2Int(resolution*1,resolution*0),
            new Vector2Int(resolution*1,resolution*1),
            new Vector2Int(resolution*3,resolution*1),
        };
        return res;
    }
    private void GenIrradiance()
    {
        mat.SetTexture(cubeMapID,envCubemap);

        RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(128,128,RenderTextureFormat.ARGB32);
        Texture2D resultCubemap = new Texture2D(Irradiance_Resolution*4, Irradiance_Resolution*3, TextureFormat.RGBAFloat, false, true);
        
        var posList = posListx;
        var rt = RenderTexture.GetTemporary(rtDesc);
        for(int i=0;i<6;i++)
        {
            mat.SetInt(currentFaceID,i);
            Graphics.Blit(null,rt,mat,0);

            Texture2D irradiance = new Texture2D(Irradiance_Resolution, Irradiance_Resolution, TextureFormat.RGBAFloat, false, true);
            Graphics.SetRenderTarget(rt);
            irradiance.ReadPixels(new Rect(0, 0, Irradiance_Resolution, Irradiance_Resolution), 0, 0);
            
            irradiance.Apply();
            previewTex = irradiance;
            resultCubemap.SetPixels(posList[i].x,posList[i].y,Irradiance_Resolution,Irradiance_Resolution,irradiance.GetPixels(),0);
            //resultCubemap.SetPixels(posList[i].x,posList[i].y,64,64,lutTex.GetPixels(1),1);

            // resultCubemap.SetPixels(lutTex.GetPixels(),faceList[i]);
            // resultCubemap.Apply();
            

            byte[] bytes = irradiance.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            File.WriteAllBytes(Application.dataPath + $"/Texture/Irradiance/face_{faceName[i]}.exr", bytes);
        }
        
        resultCubemap.Apply(false);
        byte[] resbytes = resultCubemap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
        File.WriteAllBytes(Application.dataPath + $"/Texture/Irradiance/Res.exr", resbytes);
    

        RenderTexture.ReleaseTemporary(rt);
        Graphics.SetRenderTarget(null);
        AssetDatabase.Refresh();
    }

    private void GenPrefilterMap()
    {
        //想搞在tex2D的mipmap里，但不知道怎么能存上

        //-预先搞一张Tex2D出来
        //Texture2D tmpTex = new Texture2D(Prefilter_Resolution*4, Prefilter_Resolution*3, TextureFormat.RGBA32, false, true);
        //tmpTex.Apply();
        //byte[] tmpBytes = tmpTex.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + $"/Texture/Prefilter/Test.png", tmpBytes);


        // Texture2D result = AssetDatabase.LoadAssetAtPath("Assets/Texture/Prefilter/Test.png",typeof(Texture2D)) as Texture2D;

        // //-逐渐降采样去生成mipmap
        // int mipLevel = 0;
        // for(int mipResolution = Prefilter_Resolution; mipResolution>=1; mipResolution>>=1)
        // {
        //     RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(mipResolution,mipResolution,RenderTextureFormat.ARGB32);
        //     Texture2D resultCubemap = new Texture2D(mipResolution*4, mipResolution*3,TextureFormat.ARGB32,false, true) ;
        //     var rt = RenderTexture.GetTemporary(rtDesc);
        //     var posList = GetPosList(mipResolution);
        //     mat.SetTexture(cubeMapID,envCubemap);
        //     for(int i=0; i<6;i++)
        //     {
        //         mat.SetInt(currentFaceID,i);
        //         Graphics.Blit(null,rt,mat,0);

        //         Texture2D prefiltertemp = new Texture2D(mipResolution, mipResolution,TextureFormat.ARGB32,false, true) ;
        //         Graphics.SetRenderTarget(rt);
        //         prefiltertemp.ReadPixels(new Rect(0, 0, mipResolution, mipResolution), 0, 0);
        //         prefiltertemp.Apply();

        //         var pix = prefiltertemp.GetPixels();
        //         if(mipLevel == 2)
        //         {
        //             for(int j=0;j<pix.Length;j++)
        //             {
        //                 pix[j] = Color.red;
        //             }
        //         }
        //         result.SetPixels(posList[i].x,posList[i].y,mipResolution,mipResolution,pix,mipLevel);
        //     }
        //     mipLevel++ ;
        //     Graphics.SetRenderTarget(null);
        //     RenderTexture.ReleaseTemporary(rt);
        // }
        // result.Apply(false);
        // AssetDatabase.CreateAsset(result,"Assets/Texture/Prefilter/Test.cubemap");
        // AssetDatabase.SaveAssets();
        // AssetDatabase.Refresh();
        float roughnesDelta = 1.0f/(Mathf.Log(Prefilter_Resolution,2)-1);
        Debug.Log("fzy delta:"+roughnesDelta+" "+Mathf.Log(Prefilter_Resolution,2));
        float roughness = 0;
        int mipLevel = 0;
        Cubemap result = new Cubemap(Prefilter_Resolution,TextureFormat.RGBA32,(int)Mathf.Log(Prefilter_Resolution,2));
        for(int mipResolution = Prefilter_Resolution; mipResolution>1; mipResolution>>=1)
        {
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(mipResolution,mipResolution,RenderTextureFormat.ARGB32);
            Texture2D resultCubemap = new Texture2D(mipResolution*4, mipResolution*3,TextureFormat.ARGB32,false, true) ;
            var rt = RenderTexture.GetTemporary(rtDesc);
            mat.SetTexture(cubeMapID,envCubemap);
            mat.SetFloat(currentRoughness,roughness);
            var posList = GetPosList(mipResolution);
            for(int i=0; i<6;i++)
            {
                mat.SetInt(currentFaceID,i);
                Graphics.Blit(null,rt,mat,1);
                Texture2D prefiltertemp = new Texture2D(mipResolution, mipResolution,TextureFormat.ARGB32,false, true) ;
                Graphics.SetRenderTarget(rt);
                prefiltertemp.ReadPixels(new Rect(0, 0, mipResolution, mipResolution), 0, 0,false);
                prefiltertemp.Apply();
                var pix = prefiltertemp.GetPixels(0,0,mipResolution,mipResolution);
                result.SetPixels(pix,cubemapFaces[i],mipLevel);
            }
            Debug.Log("F:"+mipLevel+" "+roughness);
            roughness += roughnesDelta;
            mipLevel++ ;
            Graphics.SetRenderTarget(null);
            RenderTexture.ReleaseTemporary(rt);
        }
        result.Apply(false);
        AssetDatabase.CreateAsset(result,"Assets/Texture/Prefilter/Test.mat");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void GenIntegrateBRDF()
    {
        RenderTexture rt=RenderTexture.GetTemporary(BRDF_Resolution, BRDF_Resolution, 0, RenderTextureFormat.ARGBFloat);
        Graphics.Blit(null, rt, mat,2);

        Texture2D lutTex = new Texture2D(BRDF_Resolution, BRDF_Resolution, TextureFormat.RGBAFloat, true, true);
        Graphics.SetRenderTarget(rt);
        lutTex.ReadPixels(new Rect(0, 0, BRDF_Resolution, BRDF_Resolution), 0, 0);
        lutTex.Apply();

        byte[] bytes = lutTex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
        File.WriteAllBytes(Application.dataPath + "/Texture/BRDF/BRDFLut.exr", bytes);

        RenderTexture.ReleaseTemporary(rt);
        Graphics.SetRenderTarget(null);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
