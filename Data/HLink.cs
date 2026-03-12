namespace HGraph
{
    /// <summary>
    /// 图中的一条有向连线，完全通过节点与端口 GUID 描述连接关系。
    /// </summary>
    public class HLink
    {
        /// <summary>
        /// 输出端所在节点的 GUID。
        /// </summary>
        public string FromNodeId { get; private set; }

        /// <summary>
        /// 输入端所在节点的 GUID。
        /// </summary>
        public string ToNodeId { get; private set; }

        /// <summary>
        /// 输出端口的 GUID。
        /// </summary>
        public string FromPortId { get; private set; }

        /// <summary>
        /// 输入端口的 GUID。
        /// </summary>
        public string ToPortId { get; private set; }

        /// <summary>
        /// 构造一条连接两个端口的连线记录。
        /// </summary>
        /// <param name="fromNodeId">输出端所在节点 GUID。</param>
        /// <param name="toNodeId">输入端所在节点 GUID。</param>
        /// <param name="fromPortId">输出端口 GUID。</param>
        /// <param name="toPortId">输入端口 GUID。</param>
        public HLink(string fromNodeId, string toNodeId, string fromPortId, string toPortId)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            FromPortId = fromPortId;
            ToPortId = toPortId;
        }
    }
}