using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 节点移动命令。拖拽过程中节点位置会实时写回模型，命令负责记录一次可撤销的位移。
    /// </summary>
    public sealed class MoveNodeCommand : IGraphCommand
    {
        /// <summary>
        /// 被移动的节点对象。
        /// </summary>
        private readonly HNode _node;

        /// <summary>
        /// 移动起点位置。
        /// </summary>
        private readonly Vector2 _from;

        /// <summary>
        /// 移动终点位置。
        /// </summary>
        private readonly Vector2 _to;

        public string Description => $"Move {_node.GetType().Name}";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.NodePositions | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建节点移动命令。
        /// </summary>
        /// <param name="node">目标节点。</param>
        /// <param name="from">起点位置。</param>
        /// <param name="to">终点位置。</param>
        public MoveNodeCommand(HNode node, Vector2 from, Vector2 to)
        {
            _node = node;
            _from = from;
            _to = to;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_node == null || _from == _to)
            {
                return false;
            }

            _node.GraphPosition = _to;
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            _node.GraphPosition = _from;
        }
    }
}
