using System;

namespace HGraph
{
    /// <summary>
    /// HGraph 自定义特性的公共基类，便于统一标记与扩展。
    /// </summary>
    public class HGraphBaseAttribute : Attribute
    {
    }

    /// <summary>
    /// 标记节点可被哪一种图类型创建与使用。
    /// </summary>
    public class HGraphNodeAttribute : HGraphBaseAttribute
    {
        /// <summary>
        /// 当前节点所属的图类型。
        /// </summary>
        public Type NodeOf { get; set; }
    }
}