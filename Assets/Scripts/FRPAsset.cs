using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "FRP/Create new asset")]
public class FRPAsset : RenderPipelineAsset
{
    public bool MSAA = false;
    public bool TAA = false;
    private Material _defaultMaterial;


    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new FRP(this);
    }

    public override Material GetDefaultMaterial()
    {
        if(_defaultMaterial == null)
        {
            _defaultMaterial = new Material(Shader.Find("FRP/Default"));
        }
        return _defaultMaterial;
    }


}
