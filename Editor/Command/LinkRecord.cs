namespace HGraph.Editor
{
    /// <summary>
    /// 连线及其原始索引，用于删除后精确恢复顺序。
    /// </summary>
    public readonly struct LinkRecord
    {
        /// <summary>
        /// 被记录的连线对象。
        /// </summary>
        public HLinkData LinkData { get; }

        /// <summary>
        /// 连线原始索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 创建一条连线恢复记录。
        /// </summary>
        /// <param name="linkData">连线对象。</param>
        /// <param name="index">原始索引。</param>
        public LinkRecord(HLinkData linkData, int index)
        {
            LinkData = linkData;
            Index = index;
        }
    }
}
