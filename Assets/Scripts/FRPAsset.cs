using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace frp
{
    [System.Serializable]
    public enum ShadowType
    {
        SM,
        PCF,
        ESM,
        VSM,
        EVSM,
        NONE,
    }
    [System.Serializable]
    public class FShadowSetting
    {
        [SerializeField]
        public ShadowType shadowType = ShadowType.SM;
        [Range(0.1f, 1)]
        public float shadowStrengh = 1;
        [Range(10, 2000)]
        public float shadowDistance = 2000;
        public int shadowResolution = 1024;
    }

    [System.Serializable]
    public class FRPRenderSettings
    {
        [SerializeField]
        public FShadowSetting shadowSetting;
        public bool useDepthPeeling = false;
        [Range(1,10)]
        public int peelingDepth = 5;
        [Range(0.5f, 2f)]
        public float shExposure = 1;
        public bool enableUnuniformBlur = false;
        public bool enableSSPR = false;
        public bool enableSSAO = false;

        [SerializeField]
        [HideInInspector]
        List<FRenderPassAsset> m_renderPassAssets = new List<FRenderPassAsset>();
        public List<FRenderPassAsset> RenderPassAssets => m_renderPassAssets;
    }

[CreateAssetMenu(menuName = "FRP/Create new asset")]
public class FRPAsset : RenderPipelineAsset
{
    [SerializeField]
    public FRPRenderSettings renderSettings;

    private Lazy<Shader> m_defaultShader = new Lazy<Shader>(()=>Shader.Find("FRP/Default"));
    private Material m_defualtMaterial;

    protected override RenderPipeline CreatePipeline()
    {
        return new FRP(renderSettings);
    }

    public override Material defaultMaterial
    {
        get{
            if(m_defualtMaterial == null)
            {
                m_defualtMaterial = new Material(m_defaultShader.Value);
            }
            return m_defualtMaterial;
        }
    }
}

}
    