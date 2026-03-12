using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 删除单条连线。
    /// </summary>
    public sealed class DeleteLinkCommand : IGraphCommand
    {
        /// <summary>
        /// 待删除的连线对象。
        /// </summary>
        private readonly HLink _link;

        /// <summary>
        /// 删除前连线所在索引。
        /// </summary>
        private int _removedIndex = -1;

        public string Description => "Delete Link";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建删除连线命令。
        /// </summary>
        /// <param name="link">目标连线。</param>
        public DeleteLinkCommand(HLink link)
        {
            _link = link;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_link == null)
            {
                return false;
            }

            _removedIndex = HGraphCommandHelper.FindLinkIndex(context.Graph, _link);
            if (_removedIndex < 0)
            {
                return false;
            }

            context.Graph.Links.RemoveAt(_removedIndex);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            var insertIndex = Mathf.Clamp(_removedIndex, 0, context.Graph.Links.Count);
            context.Graph.Links.Insert(insertIndex, _link);
        }
    }
}
