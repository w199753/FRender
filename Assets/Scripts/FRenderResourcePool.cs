using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FRenderResourcePool 
{
    public static readonly FRenderResourcePool instance = new FRenderResourcePool();

    private static Dictionary<int,Object> _resDic = new Dictionary<int, Object>();
    public static T CreateRes<T>(string name) where T:UnityEngine.Component
    {
        var obj = new GameObject(name);
        var t = obj.AddComponent<T>();
        if(!_resDic.ContainsKey(obj.GetHashCode()))
            _resDic.Add(obj.GetHashCode(),obj);
        return t;
    }

    public static void Dispose()
    {
        foreach(var item in _resDic)
        {
            UnityEngine.Object.Destroy(item.Value);
        }
        _resDic.Clear();
    }
}
