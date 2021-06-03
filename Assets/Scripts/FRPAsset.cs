using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    [CreateAssetMenu(menuName = "FRP/Create new asset")]
    public class FRPAsset : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new FRP();
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

