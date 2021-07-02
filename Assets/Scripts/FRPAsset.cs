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
    }
    [System.Serializable]
    public class FShadowSetting
    {
        public Texture hhh;
        [SerializeField]
        public ShadowType shadowType = ShadowType.SM;
        [Range(0.1f,1)]
        public float shadowStrengh = 1;
        [Range(10,300)]
        public float shadowDistance = 100;
        public int shadowResolution = 1024;
    }
    
    [CreateAssetMenu(menuName = "FRP/Create new asset")]
    public class FRPAsset : RenderPipelineAsset
    {
        [SerializeField]
        FShadowSetting shadowSetting;
        protected override RenderPipeline CreatePipeline()
        {
            FRenderResourcePool.TestFRenderResourcePool();
            return new FRP(shadowSetting);
        }


        

        [HideInInspector]
        private Material defaultMat;
        public override Material defaultMaterial 
        {
            get
            {
                if(defaultMat == null)
                {
                    defaultMat = new Material(Shader.Find("FRP/Default"));
                }
                return defaultMat;
            }
        }
    }
}

