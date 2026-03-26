namespace HGraph
{
    /// <summary>
    /// 图中的一条有向连线，完全通过节点与端口 GUID 描述连接关系。
    /// </summary>
    public class HLinkData
    {
        /// <summary>
        /// 输出端所在节点的 GUID。
        /// </summary>
        [ForceSerialize] private string _fromNodeId;
        public string FromNodeId => _fromNodeId;

        /// <summary>
        /// 输入端所在节点的 GUID。
        /// </summary>
        [ForceSerialize] private string _toNodeId;
        public string ToNodeId => _toNodeId;

        /// <summary>
        /// 输出端口的 GUID。
        /// </summary>
        [ForceSerialize] private string _fromPortId;
        public string FromPortId => _fromPortId;

        /// <summary>
        /// 输入端口的 GUID。
        /// </summary>
        [ForceSerialize] private string _toPortId;
        public string ToPortId => _toPortId;

        /// <summary>
        /// 构造一条连接两个端口的连线记录。
        /// </summary>
        /// <param name="fromNodeId">输出端所在节点 GUID。</param>
        /// <param name="toNodeId">输入端所在节点 GUID。</param>
        /// <param name="fromPortId">输出端口 GUID。</param>
        /// <param name="toPortId">输入端口 GUID。</param>
        public HLinkData(string fromNodeId, string toNodeId, string fromPortId, string toPortId)
        {
            _fromNodeId = fromNodeId;
            _toNodeId = toNodeId;
            _fromPortId = fromPortId;
            _toPortId = toPortId;
        }
    }
}