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
        private readonly HNode _node;

        /// <summary>
        /// 删除前节点所在索引。
        /// </summary>
        private int _nodeIndex = -1;

        /// <summary>
        /// 删除节点时一并移除的连线记录。
        /// </summary>
        private List<LinkRecord> _removedLinks;

        public string Description => $"Delete {_node.GetType().Name}";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Structure | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建删除节点命令。
        /// </summary>
        /// <param name="node">目标节点。</param>
        public DeleteNodeCommand(HNode node)
        {
            _node = node;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_node == null)
            {
                return false;
            }

            _nodeIndex = context.Graph.Nodes.IndexOf(_node);
            if (_nodeIndex < 0)
            {
                return false;
            }

            _removedLinks = context.Graph.Links
                .Select((link, index) => new LinkRecord(link, index))
                .Where(record => string.Equals(record.Link.FromNodeId, _node.GUID, StringComparison.Ordinal)
                                 || string.Equals(record.Link.ToNodeId, _node.GUID, StringComparison.Ordinal))
                .ToList();

            context.Graph.Nodes.RemoveAt(_nodeIndex);
            foreach (var link in _removedLinks.OrderByDescending(record => record.Index))
            {
                context.Graph.Links.RemoveAt(link.Index);
            }

            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            var insertIndex = Mathf.Clamp(_nodeIndex, 0, context.Graph.Nodes.Count);
            context.Graph.Nodes.Insert(insertIndex, _node);
            foreach (var link in _removedLinks.OrderBy(record => record.Index))
            {
                var linkIndex = Mathf.Clamp(link.Index, 0, context.Graph.Links.Count);
                context.Graph.Links.Insert(linkIndex, link.Link);
            }
        }
    }
}
