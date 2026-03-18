using System;

namespace HGraph.Editor
{
    /// <summary>
    /// 命令执行后窗口需要进行的刷新类型。
    /// </summary>
    [Flags]
    public enum GraphCommandRefreshMode
    {
        /// <summary>
        /// 不需要执行任何刷新。
        /// </summary>
        None = 0,

        /// <summary>
        /// 请求窗口重绘。
        /// </summary>
        Repaint = 1 << 0,

        /// <summary>
        /// 仅同步节点位置。
        /// </summary>
        NodePositions = 1 << 1,

        /// <summary>
        /// 仅刷新连线视图。
        /// </summary>
        Links = 1 << 2,

        /// <summary>
        /// 节点结构发生变化，需要刷新节点与连线。
        /// </summary>
        Structure = 1 << 3,

        /// <summary>
        /// 某节点的动态端口数量发生变化，需要重建受影响节点的端口视图并刷新连线。
        /// </summary>
        NodePorts = 1 << 4,
    }
}
