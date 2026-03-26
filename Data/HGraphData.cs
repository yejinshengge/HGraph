using System.Collections.Generic;

namespace HGraph
{
    /// <summary>
    /// 图数据根对象，负责持有节点集合与连线集合。
    /// 具体图类型通常通过继承它来声明自己的节点体系。
    /// </summary>
    public abstract class HGraphData
    {
        /// <summary>
        /// 当前图中包含的全部节点。
        /// </summary>
        public List<HNodeData> Nodes = new List<HNodeData>();

        /// <summary>
        /// 当前图中包含的全部连线。
        /// </summary>
        public List<HLinkData> Links = new List<HLinkData>();
    }
}