using System;

namespace HGraph
{
    /// <summary>
    /// 动态端口方向。
    /// </summary>
    public enum PortDirection
    {
        /// <summary>输入方向。</summary>
        Input,

        /// <summary>输出方向。</summary>
        Output
    }

    /// <summary>
    /// 动态端口描述符，由节点的 <see cref="HNode.GetDynamicPorts"/> 方法返回，
    /// 描述一个运行时决定的端口应具备的显示与行为属性。
    /// </summary>
    public struct DynamicPortDescriptor
    {
        /// <summary>
        /// 稳定的业务唯一标识（如选项的 GUID）。
        /// 用于建立"业务 ID → 端口 GUID"的持久化映射，保证选项改名后连线不丢失。
        /// </summary>
        public string Key;

        /// <summary>
        /// 端口在编辑器中显示的名称。
        /// </summary>
        public string Label;

        /// <summary>
        /// 端口方向（输入 / 输出）。
        /// </summary>
        public PortDirection Direction;

        /// <summary>
        /// 是否允许同时连接多条边。
        /// </summary>
        public bool AllowMultiple;

        /// <summary>
        /// 端口的值类型，用于编辑器连线兼容性判断。默认为 <see cref="object"/>。
        /// </summary>
        public Type ValueType;
    }
}
