using System;
using System.Collections.Generic;
using UnityEngine;

namespace HGraph
{
    /// <summary>
    /// 图节点基类，负责维护节点标识、位置以及实例端口集合。
    /// </summary>
    public abstract class HNode
    {
        /// <summary>
        /// 静态端口绑定记录，用于保存“成员名 -> 端口 GUID”的稳定映射。
        /// </summary>
        [Serializable]
        private sealed class StaticPortBinding
        {
            /// <summary>
            /// 声明端口的成员名称。
            /// </summary>
            public string MemberName;

            /// <summary>
            /// 对应端口实例的 GUID。
            /// </summary>
            public string PortGuid;

            /// <summary>
            /// 创建一条静态端口绑定关系。
            /// </summary>
            /// <param name="memberName">成员名称。</param>
            /// <param name="portGuid">端口 GUID。</param>
            public StaticPortBinding(string memberName, string portGuid)
            {
                MemberName = memberName;
                PortGuid = portGuid;
            }
        }

        /// <summary>
        /// 节点唯一标识。
        /// </summary>
        public string GUID { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 节点在图画布中的位置。
        /// </summary>
        [HideInInspector, SerializeInEditor]
        public Vector2 GraphPosition;

        /// <summary>
        /// 节点持有的全部端口实例。
        /// </summary>
        protected List<HPort> Ports = new List<HPort>();

        /// <summary>
        /// 静态端口成员名到端口 GUID 的映射。
        /// 端口自身不再保存成员标识，避免把“展示/声明信息”混入运行时端口数据。
        /// </summary>
        private readonly List<StaticPortBinding> _staticPortBindings = new List<StaticPortBinding>();

        /// <summary>
        /// 只读访问节点端口。
        /// </summary>
        public IReadOnlyList<HPort> PortCollection => Ports;

        /// <summary>
        /// 按端口 GUID 查找端口。
        /// </summary>
        /// <param name="portGuid">目标端口 GUID。</param>
        /// <param name="port">查找到的端口。</param>
        /// <returns>是否查找成功。</returns>
        public bool TryGetPort(string portGuid, out HPort port)
        {
            foreach (var item in Ports)
            {
                if (string.Equals(item.GUID, portGuid, StringComparison.Ordinal))
                {
                    port = item;
                    return true;
                }
            }

            port = null;
            return false;
        }

        /// <summary>
        /// 按成员名查找静态端口。
        /// </summary>
        /// <param name="memberName">声明端口的成员名。</param>
        /// <param name="port">查找到的端口。</param>
        /// <returns>是否查找成功。</returns>
        public bool TryGetStaticPort(string memberName, out HPort port)
        {
            var binding = _staticPortBindings.Find(item => string.Equals(item.MemberName, memberName, StringComparison.Ordinal));
            if (binding != null && TryGetPort(binding.PortGuid, out port))
            {
                return true;
            }

            port = null;
            return false;
        }

        /// <summary>
        /// 按成员名获取静态端口；不存在时自动创建。
        /// </summary>
        /// <param name="memberName">声明端口的成员名。</param>
        /// <returns>已存在或新建的端口实例。</returns>
        public HPort GetOrCreateStaticPort(string memberName)
        {
            if (TryGetStaticPort(memberName, out var port))
            {
                return port;
            }

            var createdPort = new HPort(GUID);
            Ports.Add(createdPort);
            _upsertStaticPortBinding(memberName, createdPort.GUID);
            return createdPort;
        }

        /// <summary>
        /// 新增或更新静态端口绑定，确保成员名始终映射到最新端口 GUID。
        /// </summary>
        /// <param name="memberName">成员名。</param>
        /// <param name="portGuid">端口 GUID。</param>
        private void _upsertStaticPortBinding(string memberName, string portGuid)
        {
            var binding = _staticPortBindings.Find(item => string.Equals(item.MemberName, memberName, StringComparison.Ordinal));
            if (binding != null)
            {
                binding.PortGuid = portGuid;
                return;
            }

            _staticPortBindings.Add(new StaticPortBinding(memberName, portGuid));
        }
    }
}