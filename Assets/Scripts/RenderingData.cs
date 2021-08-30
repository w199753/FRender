using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace frp
{
    public class RenderingData : IDisposable
    {
        public FRPRenderSettings settings;
        public CullingResults cullingResults;
        public Dictionary<int, LightingData> lightingData;
        public Dictionary<int, ShadowData> shadowData;
        public Matrix4x4 sourceViewMatrix;
        public Matrix4x4 sourceProjectionMatrix;
        public RenderTargetIdentifier ColorTarget;
        public RenderTargetIdentifier DepthTarget;

        public void Dispose()
        {

        }

    }

    public struct LightingData
    {
        public Vector4 pos_type;
        public Vector4 geometry;
        public Vector4 color;
    }

    public struct ShadowData
    {

    }

}
