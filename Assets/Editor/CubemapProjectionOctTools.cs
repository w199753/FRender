using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class CubemapProjectionOctTools : EditorWindow
{
    public enum TexSize : int
    {
        _128X128 = 7,
        _256X256 = 8,
        _512X512 = 9,
        _1024X1024 = 10,
        _2048X2048 = 11,
    }
    public Cubemap cubemap;
    public TexSize textureSize = TexSize._2048X2048;
    private Texture previewTex;
    [MenuItem("Tools/优化检测工具/Cubemap映射到OctMap")]
    public static void ShowWindow()
    {
        Rect windowRect = new Rect(0, 0, 300, 500);
        CubemapProjectionOctTools window = (CubemapProjectionOctTools)EditorWindow.GetWindowWithRect(typeof(CubemapProjectionOctTools), windowRect, true, "CubemapProjectionOctTools");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        textureSize = (TexSize)EditorGUILayout.EnumPopup(textureSize);
        cubemap = EditorGUILayout.ObjectField("Cubemap", cubemap, typeof(Cubemap), true) as Cubemap;


        if(GUILayout.Button("Generate",GUILayout.Width(300)))
        {
            DoProjection();
            DoImporterSettings();
        }

        if(previewTex != null)
        {
            GUI.DrawTexture(new Rect(0,250,150,150), previewTex);
        }
        GUILayout.EndVertical();
    }

    private void DoProjection()
    {
        int resolution = (int)Mathf.Pow(2, (int)textureSize);
        resolution = 1536;
        var mat = new Material(Shader.Find("Unlit/CubemapProjectionOct"));

        mat.SetTexture(Shader.PropertyToID("_Cubemap"),cubemap);

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGB32, 0);
        descriptor.autoGenerateMips = false;
        descriptor.useMipMap = false;
        descriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

        Texture2D resultTex = new Texture2D( resolution, resolution, TextureFormat.RGBA32, false, false);
        var rt = RenderTexture.GetTemporary(descriptor);
        Graphics.Blit(null,rt,mat,0);
        Graphics.SetRenderTarget(rt);
        resultTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        resultTex.Apply();

        byte[] bytes = resultTex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/AmplifyShaderEditor/AAARes.png", bytes);

        Graphics.SetRenderTarget(null);
        RenderTexture.ReleaseTemporary(rt);
        AssetDatabase.Refresh();

    }

    private void DoImporterSettings()
    {
        var texture = AssetDatabase.LoadAssetAtPath("Assets/AmplifyShaderEditor/AAARes.png",typeof(Texture2D)) as Texture2D;
        var path = AssetDatabase.GetAssetPath(texture);
        Debug.Log("fzy path:"+path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer)
        {
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;

            {
                importer.ClearPlatformTextureSettings("Android");
                TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                settings.name = "Android";
                settings.overridden = true;
                settings.format = TextureImporterFormat.ETC2_RGB4;
                settings.maxTextureSize = 2048;
                settings.allowsAlphaSplitting = false;
                settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                importer.SetPlatformTextureSettings(settings);
            }
            {
                importer.ClearPlatformTextureSettings("iOS");
                TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                settings.name = "iOS";
                settings.overridden = true;
                settings.format = TextureImporterFormat.ETC2_RGB4;
                settings.maxTextureSize = 2048;
                settings.allowsAlphaSplitting = false;
                settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                importer.SetPlatformTextureSettings(settings);
            }
        }
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
    }
}
