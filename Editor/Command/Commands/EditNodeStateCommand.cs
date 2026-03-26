namespace HGraph.Editor
{
    /// <summary>
    /// 节点状态快照命令，用于承载 Inspector 字段编辑。
    /// 若编辑导致动态端口数量变化，会自动升级刷新模式以驱动端口视图重建。
    /// </summary>
    public sealed class EditNodeStateCommand : IGraphCommand
    {
        /// <summary>
        /// 需要被应用状态回放的真实节点对象。
        /// </summary>
        private readonly HNodeData _targetNodeData;

        /// <summary>
        /// 编辑前的节点快照。
        /// </summary>
        private readonly HNodeData _beforeState;

        /// <summary>
        /// 编辑后的节点快照。
        /// </summary>
        private readonly HNodeData _afterState;

        /// <summary>
        /// 当前命令执行或撤销后所需的刷新模式，会在 Execute/Undo 内动态调整。
        /// </summary>
        private GraphCommandRefreshMode _refreshMode = GraphCommandRefreshMode.Repaint;

        public string Description => $"Edit {_targetNodeData.GetType().Name}";

        /// <summary>
        /// 刷新模式在 Execute / Undo 调用后由框架读取，
        /// 若动态端口发生变化则自动包含 <see cref="GraphCommandRefreshMode.NodePorts"/> 与 <see cref="GraphCommandRefreshMode.Links"/>。
        /// </summary>
        public GraphCommandRefreshMode RefreshMode => _refreshMode;

        /// <summary>
        /// 创建节点状态编辑命令。
        /// </summary>
        /// <param name="targetNodeData">真实节点对象。</param>
        /// <param name="beforeState">编辑前快照。</param>
        /// <param name="afterState">编辑后快照。</param>
        public EditNodeStateCommand(HNodeData targetNodeData, HNodeData beforeState, HNodeData afterState)
        {
            _targetNodeData = targetNodeData;
            _beforeState = beforeState;
            _afterState = afterState;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_targetNodeData == null || _beforeState == null || _afterState == null)
            {
                return false;
            }

            if (GraphCommandSnapshotUtility.AreEquivalent(_beforeState, _afterState))
            {
                return false;
            }

            GraphCommandSnapshotUtility.ApplyState(_targetNodeData, _afterState);
            _updateRefreshModeAfterApply();
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            GraphCommandSnapshotUtility.ApplyState(_targetNodeData, _beforeState);
            _updateRefreshModeAfterApply();
        }

        /// <summary>
        /// 应用快照后调用 <see cref="HNodeData.RebuildDynamicPorts"/>，
        /// 若端口集合发生变化则将刷新模式升级为包含端口与连线重建。
        /// </summary>
        private void _updateRefreshModeAfterApply()
        {
            var portsChanged = _targetNodeData.RebuildDynamicPorts();
            _refreshMode = portsChanged
                ? GraphCommandRefreshMode.NodePorts | GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint
                : GraphCommandRefreshMode.Repaint;
        }
    }
}
