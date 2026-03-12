namespace HGraph
{
    /// <summary>
    /// 端口声明特性的基类，用于描述成员在图编辑器中的端口行为。
    /// </summary>
    public class HGraphPortAttribute : HGraphBaseAttribute
    {
        /// <summary>
        /// 是否允许该端口同时连接多条边。
        /// </summary>
        public bool AllowMultiple { get; set; }
    }

    /// <summary>
    /// 将字段或属性标记为输入端口。
    /// </summary>
    public class InputAttribute : HGraphPortAttribute
    {
    }

    /// <summary>
    /// 将字段或属性标记为输出端口。
    /// </summary>
    public class OutputAttribute : HGraphPortAttribute
    {
    }
}