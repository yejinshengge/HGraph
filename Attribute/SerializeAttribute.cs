using System;

namespace HGraph
{
    /// <summary>
    /// 不需要序列化的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NotSerializeAttribute : HGraphBaseAttribute
    {
        
    }

    /// <summary>
    /// 私有字段需要强制序列化
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ForceSerializeAttribute : HGraphBaseAttribute
    {
        
    }

    /// <summary>
    /// 只在编辑器下序列化
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SerializeInEditorAttribute : HGraphBaseAttribute
    {
        
    }
}