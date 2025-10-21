
using System;
using Sirenix.OdinInspector;

namespace HGraph
{
    [Serializable]
    public class HNodeLink
    {
        /// <summary>
        /// 起始节点GUID
        /// </summary>
        [ReadOnly]
        public string BaseNodeGUID;

        /// <summary>
        /// 目标节点GUID
        /// </summary>
        [ReadOnly]
        public string TargetNodeGUID;

        /// <summary>
        /// 起始端口名称
        /// </summary>
        public string BasePortName;

        /// <summary>
        /// 目标端口名称
        /// </summary>
        public string TargetPortName;
    }
}