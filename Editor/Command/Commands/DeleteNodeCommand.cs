using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 删除节点命令，同时缓存并移除该节点关联的所有连线。
    /// </summary>
    public sealed class DeleteNodeCommand : IGraphCommand
    {
        /// <summary>
        /// 待删除的节点对象。
        /// </summary>
        private readonly HNodeData _nodeData;

        /// <summary>
        /// 删除前节点所在索引。
        /// </summary>
        private int _nodeIndex = -1;

        /// <summary>
        /// 删除节点时一并移除的连线记录。
        /// </summary>
        private List<LinkRecord> _removedLinks;

        public string Description => $"Delete {_nodeData.GetType().Name}";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Structure | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建删除节点命令。
        /// </summary>
        /// <param name="nodeData">目标节点。</param>
        public DeleteNodeCommand(HNodeData nodeData)
        {
            _nodeData = nodeData;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_nodeData == null)
            {
                return false;
            }

            _nodeIndex = context.GraphData.Nodes.IndexOf(_nodeData);
            if (_nodeIndex < 0)
            {
                return false;
            }

            _removedLinks = context.GraphData.Links
                .Select((link, index) => new LinkRecord(link, index))
                .Where(record => string.Equals(record.LinkData.FromNodeId, _nodeData.GUID, StringComparison.Ordinal)
                                 || string.Equals(record.LinkData.ToNodeId, _nodeData.GUID, StringComparison.Ordinal))
                .ToList();

            context.GraphData.Nodes.RemoveAt(_nodeIndex);
            foreach (var link in _removedLinks.OrderByDescending(record => record.Index))
            {
                context.GraphData.Links.RemoveAt(link.Index);
            }

            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            var insertIndex = Mathf.Clamp(_nodeIndex, 0, context.GraphData.Nodes.Count);
            context.GraphData.Nodes.Insert(insertIndex, _nodeData);
            foreach (var link in _removedLinks.OrderBy(record => record.Index))
            {
                var linkIndex = Mathf.Clamp(link.Index, 0, context.GraphData.Links.Count);
                context.GraphData.Links.Insert(linkIndex, link.LinkData);
            }
        }
    }
}
