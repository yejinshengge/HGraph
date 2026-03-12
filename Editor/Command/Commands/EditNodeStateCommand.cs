namespace HGraph.Editor
{
    /// <summary>
    /// 节点状态快照命令，用于承载 Inspector 字段编辑。
    /// </summary>
    public sealed class EditNodeStateCommand : IGraphCommand
    {
        /// <summary>
        /// 需要被应用状态回放的真实节点对象。
        /// </summary>
        private readonly HNode _targetNode;

        /// <summary>
        /// 编辑前的节点快照。
        /// </summary>
        private readonly HNode _beforeState;

        /// <summary>
        /// 编辑后的节点快照。
        /// </summary>
        private readonly HNode _afterState;

        public string Description => $"Edit {_targetNode.GetType().Name}";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建节点状态编辑命令。
        /// </summary>
        /// <param name="targetNode">真实节点对象。</param>
        /// <param name="beforeState">编辑前快照。</param>
        /// <param name="afterState">编辑后快照。</param>
        public EditNodeStateCommand(HNode targetNode, HNode beforeState, HNode afterState)
        {
            _targetNode = targetNode;
            _beforeState = beforeState;
            _afterState = afterState;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_targetNode == null || _beforeState == null || _afterState == null)
            {
                return false;
            }

            if (GraphCommandSnapshotUtility.AreEquivalent(_beforeState, _afterState))
            {
                return false;
            }

            GraphCommandSnapshotUtility.ApplyState(_targetNode, _afterState);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            GraphCommandSnapshotUtility.ApplyState(_targetNode, _beforeState);
        }
    }
}
