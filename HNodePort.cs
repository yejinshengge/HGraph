using System;

namespace HGraph
{
    [Serializable]
    public abstract class HNodePortBase<T>
    {
        public string GUID;

        public T Value {get;set;}
    }
    
    [Serializable]
    public class InputPort<T> : HNodePortBase<T>
    {
        public string FromGUID;
    }

    [Serializable]
    public class OutputPort<T> : HNodePortBase<T>
    {
        public string ToGUID;
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