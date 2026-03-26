using System;

namespace HGraph.Editor
{
    /// <summary>
    /// 图结构辅助方法，集中处理连线判等与查找。
    /// </summary>
    internal static class HGraphCommandHelper
    {
        /// <summary>
        /// 查找目标连线在图中的索引。
        /// </summary>
        /// <param name="graphData">目标图。</param>
        /// <param name="target">待查找连线。</param>
        /// <returns>索引；不存在时返回 -1。</returns>
        public static int FindLinkIndex(HGraphData graphData, HLinkData target)
        {
            for (var i = 0; i < graphData.Links.Count; i++)
            {
                if (AreSameLink(graphData.Links[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 判断图中是否已存在等价连线。
        /// </summary>
        /// <param name="graphData">目标图。</param>
        /// <param name="target">待判断连线。</param>
        /// <returns>是否存在。</returns>
        public static bool ContainsLink(HGraphData graphData, HLinkData target)
        {
            return FindLinkIndex(graphData, target) >= 0;
        }

        /// <summary>
        /// 从图中移除目标连线。
        /// </summary>
        /// <param name="graphData">目标图。</param>
        /// <param name="target">待删除连线。</param>
        public static void RemoveLink(HGraphData graphData, HLinkData target)
        {
            var index = FindLinkIndex(graphData, target);
            if (index >= 0)
            {
                graphData.Links.RemoveAt(index);
            }
        }

        /// <summary>
        /// 判断两条连线是否在逻辑上指向同一组节点与端口。
        /// </summary>
        /// <param name="left">左侧连线。</param>
        /// <param name="right">右侧连线。</param>
        /// <returns>是否等价。</returns>
        public static bool AreSameLink(HLinkData left, HLinkData right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(left.FromNodeId, right.FromNodeId, StringComparison.Ordinal)
                   && string.Equals(left.ToNodeId, right.ToNodeId, StringComparison.Ordinal)
                   && string.Equals(left.FromPortId, right.FromPortId, StringComparison.Ordinal)
                   && string.Equals(left.ToPortId, right.ToPortId, StringComparison.Ordinal);
        }
    }
}
