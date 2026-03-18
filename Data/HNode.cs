using System;
using System.Collections.Generic;
using UnityEngine;

namespace HGraph
{
    /// <summary>
    /// 图节点基类，负责维护节点标识、位置以及实例端口集合。
    /// 支持两类端口：
    /// <list type="bullet">
    ///   <item><term>静态端口</term><description>通过 <see cref="InputAttribute"/>/<see cref="OutputAttribute"/> 特性声明在成员上，数量固定。</description></item>
    ///   <item><term>动态端口</term><description>通过重写 <see cref="GetDynamicPorts"/> 方法按需生成，数量随节点数据变化。</description></item>
    /// </list>
    /// </summary>
    public abstract class HNode
    {
        /// <summary>
        /// 静态端口绑定记录，用于保存"成员名 -> 端口 GUID"的稳定映射。
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
        /// 动态端口绑定记录，用于保存"业务 Key -> 端口 GUID"的稳定映射。
        /// Key 由子类在 <see cref="GetDynamicPorts"/> 中提供，通常为选项的 GUID 等不变标识。
        /// </summary>
        [Serializable]
        private sealed class DynamicPortBinding
        {
            /// <summary>
            /// 动态端口的业务唯一标识（由子类提供，需保持稳定）。
            /// </summary>
            public string Key;

            /// <summary>
            /// 对应端口实例的 GUID。
            /// </summary>
            public string PortGuid;

            /// <summary>
            /// 创建一条动态端口绑定关系。
            /// </summary>
            /// <param name="key">业务 Key。</param>
            /// <param name="portGuid">端口 GUID。</param>
            public DynamicPortBinding(string key, string portGuid)
            {
                Key = key;
                PortGuid = portGuid;
            }
        }

        // ============ 标识与位置 ============

        /// <summary>
        /// 节点唯一标识。
        /// </summary>
        public string GUID { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 节点在图画布中的位置。
        /// </summary>
        [HideInInspector, SerializeInEditor]
        public Vector2 GraphPosition;

        // ============ 端口存储 ============

        /// <summary>
        /// 节点持有的全部端口实例（静态 + 动态）。
        /// </summary>
        protected List<HPort> Ports = new List<HPort>();

        /// <summary>
        /// 静态端口成员名到端口 GUID 的映射。
        /// </summary>
        private readonly List<StaticPortBinding> _staticPortBindings = new List<StaticPortBinding>();

        /// <summary>
        /// 动态端口业务 Key 到端口 GUID 的映射，随节点数据变化增删。
        /// </summary>
        private readonly List<DynamicPortBinding> _dynamicPortBindings = new List<DynamicPortBinding>();

        // ============ 公开访问 ============

        /// <summary>
        /// 只读访问节点端口。
        /// </summary>
        public IReadOnlyList<HPort> PortCollection => Ports;

        // ============ 静态端口 API ============

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

        // ============ 动态端口 API ============

        /// <summary>
        /// 返回当前节点希望拥有的动态端口描述符集合。
        /// 子类重写此方法，根据自身数据（如选项列表）按需生成描述符。
        /// 框架会调用 <see cref="RebuildDynamicPorts"/> 将描述符同步为实际端口实例。
        /// </summary>
        /// <returns>动态端口描述符集合；基类默认返回空集合。</returns>
        protected virtual IEnumerable<DynamicPortDescriptor> GetDynamicPorts()
        {
            yield break;
        }

        /// <summary>
        /// 根据当前 <see cref="GetDynamicPorts"/> 返回的 Key 集合计算轻量签名，
        /// 用于编辑器快速判断动态端口结构是否发生变化。
        /// </summary>
        public int ComputeDynamicPortSignature()
        {
            unchecked
            {
                var hash = 17;
                foreach (var descriptor in GetDynamicPorts())
                {
                    hash = hash * 31 + (descriptor.Key?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        /// <summary>
        /// 将 <see cref="GetDynamicPorts"/> 的最新结果同步到 <see cref="Ports"/> 与绑定列表：
        /// <list type="bullet">
        ///   <item>新增描述符 → 创建 <see cref="HPort"/> 并记录绑定。</item>
        ///   <item>移除描述符 → 删除对应 <see cref="HPort"/> 与绑定。</item>
        ///   <item>Key 不变    → 复用原 <see cref="HPort"/>，保持连线稳定。</item>
        /// </list>
        /// </summary>
        /// <returns>若端口集合发生变化则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool RebuildDynamicPorts()
        {
            var descriptors = new List<DynamicPortDescriptor>(GetDynamicPorts());
            var changed = false;

            // 收集本次仍有效的 key 集合
            var validKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var descriptor in descriptors)
            {
                validKeys.Add(descriptor.Key);
            }

            // 移除已不存在的动态端口绑定及对应的 HPort
            for (var i = _dynamicPortBindings.Count - 1; i >= 0; i--)
            {
                var binding = _dynamicPortBindings[i];
                if (validKeys.Contains(binding.Key))
                {
                    continue;
                }

                Ports.RemoveAll(p => string.Equals(p.GUID, binding.PortGuid, StringComparison.Ordinal));
                _dynamicPortBindings.RemoveAt(i);
                changed = true;
            }

            // 为新增的描述符创建端口
            foreach (var descriptor in descriptors)
            {
                var existing = _dynamicPortBindings.Find(b => string.Equals(b.Key, descriptor.Key, StringComparison.Ordinal));
                if (existing != null)
                {
                    // key 已存在，复用原端口，无需任何操作
                    continue;
                }

                var newPort = new HPort(GUID);
                Ports.Add(newPort);
                _dynamicPortBindings.Add(new DynamicPortBinding(descriptor.Key, newPort.GUID));
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// 按业务 Key 查找动态端口。
        /// </summary>
        /// <param name="key">动态端口的业务 Key。</param>
        /// <param name="port">查找到的端口。</param>
        /// <returns>是否查找成功。</returns>
        public bool TryGetDynamicPort(string key, out HPort port)
        {
            var binding = _dynamicPortBindings.Find(b => string.Equals(b.Key, key, StringComparison.Ordinal));
            if (binding != null && TryGetPort(binding.PortGuid, out port))
            {
                return true;
            }

            port = null;
            return false;
        }

        /// <summary>
        /// 返回所有动态端口描述符及其对应端口实例的配对列表，供视图层渲染使用。
        /// 仅返回已通过 <see cref="RebuildDynamicPorts"/> 完成绑定的端口。
        /// </summary>
        /// <returns>描述符与端口实例的配对集合。</returns>
        public IEnumerable<(DynamicPortDescriptor Descriptor, HPort Port)> GetDynamicPortsWithData()
        {
            foreach (var descriptor in GetDynamicPorts())
            {
                if (TryGetDynamicPort(descriptor.Key, out var port))
                {
                    yield return (descriptor, port);
                }
            }
        }
    }
}
