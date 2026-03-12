using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HGraph.Editor
{
    /// <summary>
    /// 节点端口声明与实例端口同步工具。
    /// </summary>
    public static class HGraphNodePortUtility
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// 获取节点类型上所有被端口特性标记的字段或属性。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        /// <returns>可用于创建端口的成员集合。</returns>
        public static IEnumerable<MemberInfo> GetPortMembers(Type nodeType)
        {
            return nodeType
                .GetMembers(MemberFlags)
                .Where(_isSupportedPortMember)
                .Where(member => member.IsDefined(typeof(HGraphPortAttribute), true));
        }

        /// <summary>
        /// 根据节点声明同步静态端口，确保每个端口成员都拥有稳定的端口实例。
        /// </summary>
        /// <param name="node">目标节点。</param>
        public static void EnsureStaticPorts(HNode node)
        {
            foreach (var member in GetPortMembers(node.GetType()))
            {
                node.GetOrCreateStaticPort(member.Name);
            }
        }

        /// <summary>
        /// 判断成员是否可以作为端口声明源。
        /// 目前仅支持普通字段与非索引属性。
        /// </summary>
        /// <param name="member">待判断成员。</param>
        /// <returns>是否属于受支持的端口成员。</returns>
        private static bool _isSupportedPortMember(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Field)
            {
                return true;
            }

            if (member.MemberType == MemberTypes.Property)
            {
                return member is PropertyInfo property && property.GetIndexParameters().Length == 0;
            }

            return false;
        }
    }
}
