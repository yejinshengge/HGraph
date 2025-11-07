using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HGraph
{

    public abstract class HNodePortBase
    {
        [ReadOnly]
        public string GUID;

        public abstract void Clear();

        public abstract bool IsValid();

        public abstract void SetValue(HNodePortBase sourcePort);
    }

    [Serializable]
    public abstract class HNodePortBase<T>:HNodePortBase
    {
        public T Value {get;set;}

        [NonSerialized]
        private bool _hasVal;

        public override void SetValue(HNodePortBase sourcePort)
        {
            var source = sourcePort as HNodePortBase<T>;
            if(source == null)
            {
                Debug.LogError($"源端口类型不匹配");
                return;
            }
            Value = source.Value;
            _hasVal = true;
        }

        public override void Clear()
        {
            _hasVal = false;
            Value = default;
        }

        public override bool IsValid()
        {
            return _hasVal;
        }
    }
    
    [Serializable]
    public class InputPort<T> : HNodePortBase<T>
    {
        
    }

    [Serializable]
    public class OutputPort<T> : HNodePortBase<T>
    {
        
    }

    public struct EmptyValue{}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PortAttribute:HGraphBaseAttribute
    {
        /// <summary>
        /// 是否允许多连接
        /// </summary>
        public bool AllowMultiple = true;
    }
}