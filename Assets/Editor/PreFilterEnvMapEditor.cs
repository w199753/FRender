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

    public Texture previewTex;
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

    private void GenIrradiance()
    {
        mat.SetTexture(cubeMapID,envCubemap);
        mat.SetInt(currentFaceID,0);
        RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(128,128,RenderTextureFormat.ARGB32);
        var rt = RenderTexture.GetTemporary(rtDesc);
        Graphics.Blit(null,rt,mat);

        Texture2D lutTex = new Texture2D(128, 128, TextureFormat.RGBAFloat, true, true);
        Graphics.SetRenderTarget(rt);
        lutTex.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
        lutTex.Apply();
        previewTex = lutTex;

        byte[] bytes = lutTex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
        File.WriteAllBytes(Application.dataPath + "/Texture/Test.exr", bytes);

        RenderTexture.ReleaseTemporary(rt);
        Graphics.SetRenderTarget(null);
        AssetDatabase.Refresh();
    }
}
