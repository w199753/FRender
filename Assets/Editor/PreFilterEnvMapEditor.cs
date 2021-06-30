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
    public Material mat;
    public Cubemap envCubemap;

    public Texture2D previewTex;
    private void OnGUI()
    {
        GUILayout.BeginVertical();
        mat = EditorGUILayout.ObjectField("Material", mat, typeof(Material), true) as Material;
        envCubemap = EditorGUILayout.ObjectField("Cubemap", envCubemap, typeof(Cubemap), true) as Cubemap;

        if (GUILayout.Button("Gen Irradiance Map", GUILayout.Width(300)))
        {
            if (mat == null) { this.ShowNotification(new GUIContent("Mat 不能为空!")); return; };
            if (envCubemap == null) { this.ShowNotification(new GUIContent("Cubemap 不能为空!")); return; };
            GenIrradiance();
        }
        GUILayout.EndVertical();
    }

    private static int 
        cubeMapID = Shader.PropertyToID("_EnvMap"),
        
        //*based left hand coordinate system*  0:(right) 1:(left) 2:(up) 3:(down) 4:(front) 5:(back)
        currentFaceID = Shader.PropertyToID("_FaceID"); 

    string[] faceName = new string[6]{"right","left","up","down","front","back"};
    Vector2Int[] posList = new Vector2Int[6]{
        new Vector2Int(128*2,128*1),
        new Vector2Int(128*0,128*1),
        new Vector2Int(128*1,128*2),
        new Vector2Int(128*1,128*0),
        new Vector2Int(128*1,128*1),
        new Vector2Int(128*3,128*1),
    };
    private void GenIrradiance()
    {
        mat.SetTexture(cubeMapID,envCubemap);

        RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(128,128,RenderTextureFormat.ARGB32);
        //Cubemap resultCubemap = new Cubemap(128,TextureFormat.RGBAFloat,false);
        Texture2D resultCubemap = new Texture2D(128*4, 128*3, TextureFormat.RGBAFloat, false, true);
        
        var rt = RenderTexture.GetTemporary(rtDesc);
        for(int i=0;i<6;i++)
        {
            mat.SetInt(currentFaceID,i);
            Graphics.Blit(null,rt,mat);

            Texture2D lutTex = new Texture2D(128, 128, TextureFormat.RGBAFloat, false, true);
            Graphics.SetRenderTarget(rt);
            lutTex.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
            
            lutTex.Apply();
            previewTex = lutTex;
            resultCubemap.SetPixels(posList[i].x,posList[i].y,128,128,lutTex.GetPixels(),0);
            //resultCubemap.SetPixels(posList[i].x,posList[i].y,64,64,lutTex.GetPixels(1),1);

            // resultCubemap.SetPixels(lutTex.GetPixels(),faceList[i]);
            // resultCubemap.Apply();
            

            byte[] bytes = lutTex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            File.WriteAllBytes(Application.dataPath + $"/Texture/Test{faceName[i]}.exr", bytes);
        }
        
        resultCubemap.Apply(false);
        byte[] resbytes = resultCubemap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
        File.WriteAllBytes(Application.dataPath + $"/Texture/Res.exr", resbytes);
    

        RenderTexture.ReleaseTemporary(rt);
        Graphics.SetRenderTarget(null);
        AssetDatabase.Refresh();
    }
}
