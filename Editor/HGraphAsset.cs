using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// ScriptedImporter 的主输出产物，存储 .hgraph 文件的基础元数据。
    /// Inspector 绘制时按需反序列化获取完整图数据。
    /// </summary>
    public class HGraphAsset : ScriptableObject
    {
        /// <summary>
        /// 图类型全名（HGraphData 子类的 FullName）。
        /// </summary>
        public string GraphTypeName;

        /// <summary>
        /// 导入是否成功。
        /// </summary>
        public bool ImportSuccess;

        /// <summary>
        /// 导入失败时的错误信息。
        /// </summary>
        public string ErrorMessage;
    }
}
