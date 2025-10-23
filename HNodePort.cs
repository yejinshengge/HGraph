using System;
using Sirenix.OdinInspector;

namespace HGraph
{

    public abstract class HNodePortBase
    {
        [ReadOnly]
        public string GUID;
    }

    [Serializable]
    public abstract class HNodePortBase<T>:HNodePortBase
    {
        public T Value {get;set;}
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