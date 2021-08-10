using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class NewTestEditor :Editor 
{
    [MenuItem("GameObject/Create Texture Array")]
    static void Create()
    {
        string filePattern = "Assets/smoke_{0:000}"; 
        Texture2DArray textureArray = new Texture2DArray(1024, 1024, 4, TextureFormat.ARGB32, false);
        string path = "Assets/Resources/SmokeTextureArray.asset";
        AssetDatabase.CreateAsset(textureArray,path);
        AssetDatabase.Refresh();
    }
}
