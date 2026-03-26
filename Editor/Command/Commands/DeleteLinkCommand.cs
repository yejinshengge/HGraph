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
        private readonly HLinkData _linkData;

        /// <summary>
        /// 删除前连线所在索引。
        /// </summary>
        private int _removedIndex = -1;

        public string Description => "Delete Link";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建删除连线命令。
        /// </summary>
        /// <param name="linkData">目标连线。</param>
        public DeleteLinkCommand(HLinkData linkData)
        {
            _linkData = linkData;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_linkData == null)
            {
                return false;
            }

            _removedIndex = HGraphCommandHelper.FindLinkIndex(context.GraphData, _linkData);
            if (_removedIndex < 0)
            {
                return false;
            }

            context.GraphData.Links.RemoveAt(_removedIndex);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            var insertIndex = Mathf.Clamp(_removedIndex, 0, context.GraphData.Links.Count);
            context.GraphData.Links.Insert(insertIndex, _linkData);
        }
    }
}
