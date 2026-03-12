namespace HGraph
{
    /// <summary>
    /// 图资源持久化接口，约定图对象的保存与加载方式。
    /// </summary>
    public interface IHGraphPersistence
    {
        /// <summary>
        /// 将图数据保存到指定路径。
        /// </summary>
        /// <param name="path">保存路径。</param>
        /// <param name="graph">待保存的图对象。</param>
        /// <param name="isEditor">是否保存编辑器态数据。</param>
        void Save(string path, HGraph graph, bool isEditor = false);

        /// <summary>
        /// 从指定路径加载图数据。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <returns>反序列化得到的图对象。</returns>
        HGraph Load(string path);
    }
}