using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 编辑器工具：类型发现 & 持久化
    /// </summary>
    public static class HGraphEditorUtility
    {
        /// <summary>
        /// 扫描当前 AppDomain 中所有可实例化的图类型。
        /// </summary>
        /// <returns>按名称排序后的图类型列表。</returns>
        public static List<Type> GetAllHGraphTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(HGraph)))
                .OrderBy(t => t.Name)
                .ToList();
        }

        /// <summary>
        /// 获取指定图类型允许创建的全部节点类型。
        /// 节点归属关系通过 <see cref="HGraphNodeAttribute.NodeOf"/> 声明。
        /// </summary>
        /// <param name="graphType">目标图类型。</param>
        /// <returns>按名称排序后的节点类型列表。</returns>
        public static List<Type> GetNodeTypesForGraph(Type graphType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                {
                    if (!t.IsClass || t.IsAbstract || !t.IsSubclassOf(typeof(HNode))) return false;
                    var attrs = t.GetCustomAttributes(typeof(HGraphNodeAttribute), false);
                    if (attrs.Length == 0) return false;
                    return ((HGraphNodeAttribute)attrs[0]).NodeOf == graphType;
                })
                .OrderBy(t => t.Name)
                .ToList();
        }

        /// <summary>
        /// 保存图资源。
        /// 当前仅保留统一入口，后续会委托给持久化实现。
        /// </summary>
        /// <param name="path">目标路径。</param>
        /// <param name="graph">待保存的图对象。</param>
        public static void SaveGraph(string path, HGraph graph)
        {
            Debug.Log($"[HGraph] 保存图到: {path} (类型: {graph.GetType().Name})");
            HGraphPersistenceRegistry.Current.Save(path, graph, isEditor: true);
        }

        /// <summary>
        /// 从指定路径加载图资源。
        /// 当前为占位入口，后续会委托给持久化实现。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <returns>加载得到的图对象；当前未实现时返回 null。</returns>
        public static HGraph LoadGraph(string path)
        {
            Debug.Log($"[HGraph] 从文件加载图: {path}");
            var graph = HGraphPersistenceRegistry.Current.Load(path);

            if (graph != null)
            {
                foreach (var node in graph.Nodes)
                {
                    HGraphNodePortUtility.EnsureStaticPorts(node);
                    node.RebuildDynamicPorts();
                }
            }

            return graph;
        }
    }
}
