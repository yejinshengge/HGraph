using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 原子替换连线，主要用于单容量端口"重连"。
    /// </summary>
    public sealed class ReplaceLinkCommand : IGraphCommand
    {
        /// <summary>
        /// 替换后要写入的新连线。
        /// </summary>
        private readonly HLinkData _newLinkData;

        /// <summary>
        /// 被替换掉的旧连线集合。
        /// </summary>
        private readonly List<LinkRecord> _replacedLinks;

        public string Description => "Replace Link";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建替换连线命令。
        /// </summary>
        /// <param name="newLinkData">新连线。</param>
        /// <param name="replacedLinks">需要移除并可恢复的旧连线。</param>
        public ReplaceLinkCommand(HLinkData newLinkData, IEnumerable<LinkRecord> replacedLinks)
        {
            _newLinkData = newLinkData;
            _replacedLinks = replacedLinks?.ToList() ?? new List<LinkRecord>();
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_newLinkData == null)
            {
                return false;
            }

            foreach (var link in _replacedLinks.OrderByDescending(item => item.Index))
            {
                var index = HGraphCommandHelper.FindLinkIndex(context.GraphData, link.LinkData);
                if (index >= 0)
                {
                    context.GraphData.Links.RemoveAt(index);
                }
            }

            if (!HGraphCommandHelper.ContainsLink(context.GraphData, _newLinkData))
            {
                context.GraphData.Links.Add(_newLinkData);
                return true;
            }

            return false;
        }

        public void Undo(GraphCommandContext context)
        {
            HGraphCommandHelper.RemoveLink(context.GraphData, _newLinkData);
            foreach (var link in _replacedLinks.OrderBy(item => item.Index))
            {
                var insertIndex = Mathf.Clamp(link.Index, 0, context.GraphData.Links.Count);
                if (!HGraphCommandHelper.ContainsLink(context.GraphData, link.LinkData))
                {
                    context.GraphData.Links.Insert(insertIndex, link.LinkData);
                }
            }
        }
    }
}
