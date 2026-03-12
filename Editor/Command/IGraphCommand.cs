namespace HGraph.Editor
{
    /// <summary>
    /// 图编辑命令接口，负责正向执行与撤销。
    /// </summary>
    public interface IGraphCommand
    {
        /// <summary>
        /// 命令描述，用于调试或后续扩展的命令展示。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 命令执行完成后建议触发的刷新模式。
        /// </summary>
        GraphCommandRefreshMode RefreshMode { get; }

        /// <summary>
        /// 正向执行命令。
        /// </summary>
        /// <param name="context">命令上下文。</param>
        /// <returns>是否执行成功。</returns>
        bool Execute(GraphCommandContext context);

        /// <summary>
        /// 撤销命令带来的数据变更。
        /// </summary>
        /// <param name="context">命令上下文。</param>
        void Undo(GraphCommandContext context);
    }
}
