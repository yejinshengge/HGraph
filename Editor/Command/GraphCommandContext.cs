namespace HGraph.Editor
{
    /// <summary>
    /// 命令执行上下文，只暴露源数据图对象。
    /// </summary>
    public sealed class GraphCommandContext
    {
        /// <summary>
        /// 当前命令作用的图对象。
        /// </summary>
        public HGraph Graph { get; }

        /// <summary>
        /// 创建一个命令执行上下文。
        /// </summary>
        /// <param name="graph">命令要操作的图对象。</param>
        public GraphCommandContext(HGraph graph)
        {
            Graph = graph;
        }
    }
}
