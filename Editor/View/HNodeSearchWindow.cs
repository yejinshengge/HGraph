using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 节点搜索窗口，负责为当前图类型提供可创建的节点列表。
    /// </summary>
    public class HNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        /// <summary>
        /// 当前所属的图视图。
        /// </summary>
        private HGraphView _graphView;

        /// <summary>
        /// 当前图视图对应的图类型。
        /// </summary>
        private Type _graphType;

        /// <summary>
        /// 初始化搜索窗口上下文。
        /// </summary>
        /// <param name="graphView">当前图视图。</param>
        /// <param name="graphType">当前图类型。</param>
        public void Init(HGraphView graphView, Type graphType)
        {
            _graphView = graphView;
            _graphType = graphType;
        }

        /// <summary>
        /// 构建搜索树内容。
        /// </summary>
        /// <param name="context">搜索窗口上下文。</param>
        /// <returns>GraphView 搜索树条目列表。</returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("添加节点"))
            };

            var nodeTypes = HGraphEditorUtility.GetNodeTypesForGraph(_graphType);
            foreach (var type in nodeTypes)
            {
                tree.Add(new SearchTreeEntry(new GUIContent(type.Name))
                {
                    level = 1,
                    userData = type
                });
            }

            if (nodeTypes.Count == 0)
            {
                tree.Add(new SearchTreeEntry(new GUIContent("（无可用节点类型）"))
                {
                    level = 1
                });
            }

            return tree;
        }

        /// <summary>
        /// 处理搜索项选择，并通知图视图创建节点。
        /// </summary>
        /// <param name="entry">被选中的搜索项。</param>
        /// <param name="context">搜索窗口上下文。</param>
        /// <returns>是否成功消费本次选择。</returns>
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is Type nodeType)
            {
                _graphView.CreateNode(nodeType);
                return true;
            }
            return false;
        }
    }
}
