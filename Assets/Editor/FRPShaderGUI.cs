using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class FRPShaderGUI : ShaderGUI
{
    private MaterialEditor m_MaterialEditor;
    private Object[] m_materials;
    private MaterialProperty[] m_properties;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        m_MaterialEditor = materialEditor;
        m_properties = properties;
        m_materials = materialEditor.targets;

        Material targetMat = m_MaterialEditor.target as Material;
        var normal = targetMat.GetTexture("_Normal");
        var roughness = targetMat.GetTexture("_RoughnessTex");
        if(normal == null)
        {
            targetMat.DisableKeyword("_NormalTexOn");
        }
        else
        {
            targetMat.EnableKeyword("_NormalTexOn");
        }

        if(roughness == null)
        {
            targetMat.DisableKeyword("_RoughnessTexOn");
        }
        else
        {
            targetMat.EnableKeyword("_RoughnessTexOn");
        }
    }
}
