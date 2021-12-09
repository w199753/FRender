using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateCubeMapTools : EditorWindow
{
    public enum TexSize : int
    {
        _128X128 = 7,
        _256X256 = 8,
        _512X512 = 9,
        _1024X1024 = 10,
        _2048X2048 = 11,
    }
    public Texture2D leftTex;
    public Texture2D rightTex;
    public Texture2D topTex;
    public Texture2D bottomTex;
    public Texture2D frontTex;
    public Texture2D backTex;

    public TexSize textureSize = TexSize._1024X1024;
    [MenuItem("FTools/Generate Cubemap")]
    public static void ShowWindow()
    {
        Rect windowRect = new Rect(0, 0, 300, 500);
        GenerateCubeMapTools window = (GenerateCubeMapTools)EditorWindow.GetWindowWithRect(typeof(GenerateCubeMapTools), windowRect, true, "GenerateCubeMapTools");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("每张texture生成的size");
        textureSize = (TexSize)EditorGUILayout.EnumPopup(textureSize);
        GUILayout.EndHorizontal();

        leftTex = EditorGUILayout.ObjectField("Left", leftTex, typeof(Texture2D), true) as Texture2D;
        rightTex = EditorGUILayout.ObjectField("Right", rightTex, typeof(Texture2D), true) as Texture2D;
        topTex = EditorGUILayout.ObjectField("Top", topTex, typeof(Texture2D), true) as Texture2D;
        bottomTex = EditorGUILayout.ObjectField("Bottom", bottomTex, typeof(Texture2D), true) as Texture2D;
        frontTex = EditorGUILayout.ObjectField("Front", frontTex, typeof(Texture2D), true) as Texture2D;
        backTex = EditorGUILayout.ObjectField("Back", backTex, typeof(Texture2D), true) as Texture2D;

        if(GUILayout.Button("Generate",GUILayout.Width(300)))
        {
            GenerateCubemap();
        }
        GUILayout.EndVertical();
    }

    private void GenerateCubemap()
    {
        // var material = new Material(Shader.Find("Unlit/GenCubemapTest"));
        var texList = new List<Texture2D>();
        texList.Add(leftTex);
        texList.Add(rightTex);
        texList.Add(topTex);
        texList.Add(bottomTex);
        texList.Add(frontTex);
        texList.Add(backTex);
        int resolution = (int)Mathf.Pow(2, (int)textureSize);
        //Debug.Log("fzy 123:"+(int)textureSize + "   "+resolution);

        // Vector2Int[] res = new Vector2Int[6]{
        //     new Vector2Int(resolution*,0),
        //     new Vector2Int(resolution*,0),
        //     new Vector2Int(resolution*,0),
        //     new Vector2Int(resolution*,0),
        //     new Vector2Int(resolution*,0),
        //     new Vector2Int(resolution*,0),
        // };

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGBFloat, 0);
        descriptor.autoGenerateMips = false;
        descriptor.useMipMap = false;
        descriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

        Texture2D resultCubemap = new Texture2D(6 * resolution, resolution, TextureFormat.RGB24, false, false);
        var rt = RenderTexture.GetTemporary(descriptor);

        for (int i = 0; i < texList.Count; i++)
        {
            //material.SetTexture("_EnvMap",texList[i]);
            Graphics.Blit(texList[i], rt);
            Texture2D singleFace = new Texture2D(resolution, resolution, TextureFormat.RGB24, false, false);
            Graphics.SetRenderTarget(rt);
            singleFace.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            singleFace.Apply();

            resultCubemap.SetPixels(resolution * i, 0, resolution, resolution, singleFace.GetPixels(), 0);
        }

        resultCubemap.Apply(false);
        byte[] resbytes = resultCubemap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/AmplifyShaderEditor/Res.png", resbytes);

        Graphics.SetRenderTarget(null);
        RenderTexture.ReleaseTemporary(rt);
        AssetDatabase.Refresh();
    }



}
