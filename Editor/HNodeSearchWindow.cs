using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    public class HNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private HGraphView _graphView;

        private EditorWindow _editorWindow;

        public void Init(HGraphView graphView, EditorWindow editorWindow)
        {
            _graphView = graphView;
            _editorWindow = editorWindow;
        }

        /// <summary>
        /// 创建搜索树
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var nodeTypeMap = HGraphCache.GetNodeTypeMap(_graphView.GetGraphType());
            // 分组
            var groupSet = new HashSet<string>();
            var groupMap = new Dictionary<string, List<Type>>();

            foreach (var nodeType in nodeTypeMap)
            {
                var group = nodeType.Value.Group;
                if(string.IsNullOrEmpty(group))
                    group = "Default";
                groupSet.Add(group);
                if(!groupMap.ContainsKey(group))
                    groupMap[group] = new List<Type>();
                groupMap[group].Add(nodeType.Key);
            }
            // 排序
            var groupList = groupSet.ToList();
            groupList.Sort();

            foreach(var item in groupMap)
            {
                item.Value.Sort((a, b) => {
                    var nodeAttrA = nodeTypeMap[a];
                    var nodeAttrB = nodeTypeMap[b];
                    var aName = nodeAttrA.Name;
                    var bName = nodeAttrB.Name;
                    if(string.IsNullOrEmpty(aName))
                        aName = a.Name;
                    if(string.IsNullOrEmpty(bName))
                        bName = b.Name;
                    return string.Compare(aName, bName);
                });
            }

            // 构建搜索树
            var treeList = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            for(int i = 0; i < groupList.Count; i++)
            {
                treeList.Add(new SearchTreeGroupEntry(new GUIContent(groupList[i]), 1));
                foreach(var nodeType in groupMap[groupList[i]])
                {
                    treeList.Add(new SearchTreeEntry(new GUIContent(nodeTypeMap[nodeType].Name))
                    {
                        userData = nodeType,
                        level = 2
                    });
                }

            }
            return treeList;
        }

        /// <summary>
        /// 选中回调
        /// </summary>
        /// <param name="SearchTreeEntry"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            // 将鼠标位置转换为世界坐标
            var worldMousePosition = _editorWindow.rootVisualElement.
                ChangeCoordinatesTo(_editorWindow.rootVisualElement.parent, 
                context.screenMousePosition - _editorWindow.position.position);
            var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);

            // 创建节点实例
            var nodeType = SearchTreeEntry.userData as Type;
            var nodeData = SerializedScriptableObject.CreateInstance(nodeType) as HNodeBase;
            
            // 获取节点特性名称作为默认名称
            var nodeAttr = HGraphCache.GetAttribute<HNodeAttribute>(nodeType);
            var defaultName = !string.IsNullOrEmpty(nodeAttr?.Name) ? nodeAttr.Name : nodeType.Name;
            
            // 设置节点属性
            nodeData.NodeName = defaultName;
            nodeData.GUID = Guid.NewGuid().ToString();
            nodeData.Position = localMousePosition;
            nodeData.name = defaultName; // 同步资产名称

            _graphView.AddNode(nodeData);
            return true;
        }
    }
}