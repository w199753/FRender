using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FRenderResourcePool 
{
    public struct ResObject
    {
        public ResObject(GameObject obj,bool isUsing)
        {
            this.obj = obj;
            this._isUsing = isUsing;
            obj.SetActive(isUsing);
        }
        public GameObject obj;
        private bool _isUsing;
        public bool IsUsing
        {
            get
            {
                return _isUsing;
            }
            set
            {
                _isUsing = value;
                if(obj) obj.SetActive(value);
            }
        }
        public void SetUsing(bool v)
        {
            _isUsing = v;
        }
        public void Clear()
        {
            GameObject.DestroyImmediate(obj);
            _isUsing = false;
        }
    }


    public static void TestFRenderResourcePool()
    {
        
        foreach(var item in objPool)
        {
            item.SetUsing(false);
            item.Clear();
        }
        objPool.Clear();
    }
    //public static readonly FRenderResourcePool instance = new FRenderResourcePool();
    private static Queue<ResObject> objPool = new Queue<ResObject>();
    private static Dictionary<int,Component> _resDic = new Dictionary<int, Component>();

    //最前面放的是可以直接用的，get到要取出来插入到队尾
    public static GameObject CreateObject(string name)
    {
        var find = GameObject.Find(name);
        if(find)return find;
        if(objPool.Count == 0)
        {
            objPool.Enqueue(new ResObject(new GameObject(name),true));
            return objPool.Peek().obj;
        }
        else
        {
            if(objPool.Peek().IsUsing == true)
            {
                var tmp = new ResObject(new GameObject(name),true);
                objPool.Enqueue(tmp);
                return tmp.obj;
            }
            else
            {
                var tmp = objPool.Dequeue();
                objPool.Enqueue(tmp);
                return tmp.obj;
            }
        }
    }

    public static T AddComponent<T>(ref GameObject obj) where T :UnityEngine.Component
    {
        obj.TryGetComponent(out T comp);
        if(comp == null)
            comp = obj.AddComponent<T>();
        return comp;
    }

    public static void Disposexx()
    {
        foreach(var item in objPool)
        {
            item.SetUsing(false);
        }
    }
}
