using System;

namespace HGraph
{
    /// <summary>
    /// 端口实例数据。
    /// 端口本身不直接保存成员反射信息，只通过稳定 GUID 参与连线与持久化。
    /// </summary>
    public class HPort
    {
        /// <summary>
        /// 端口唯一标识。
        /// </summary>
        [ForceSerialize] private string _guid;
        public string GUID => _guid;

        /// <summary>
        /// 当前端口所属节点的 GUID。
        /// </summary>
        [ForceSerialize] private string _nodeGuid;
        public string NodeGUID => _nodeGuid;

        /// <summary>
        /// 创建一个归属于指定节点的端口实例。
        /// </summary>
        /// <param name="nodeGuid">所属节点 GUID。</param>
        public HPort(string nodeGuid)
        {
            _guid = Guid.NewGuid().ToString();
            _nodeGuid = nodeGuid;
        }
    }
}