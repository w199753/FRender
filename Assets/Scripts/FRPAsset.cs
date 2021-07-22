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
        [SerializeField]
        public ShadowType shadowType = ShadowType.SM;
        [Range(0.1f, 1)]
        public float shadowStrengh = 1;
        [Range(10, 300)]
        public float shadowDistance = 100;
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
    }

    [CreateAssetMenu(menuName = "FRP/Create new asset")]
    public class FRPAsset : RenderPipelineAsset
    {

        [SerializeField]
        FRPRenderSettings renderSettings;
        protected override RenderPipeline CreatePipeline()
        {
            FRenderResourcePool.TestFRenderResourcePool();
            return new FRP(renderSettings);
        }




        [HideInInspector]
        private Material defaultMat;
        public override Material defaultMaterial
        {
            get
            {
                if (defaultMat == null)
                {
                    defaultMat = new Material(Shader.Find("FRP/Default"));
                }
                return defaultMat;
            }
        }
    }
}

