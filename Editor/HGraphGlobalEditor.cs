using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace HGraph.Editor
{
    [CustomEditor(typeof(HGraphBase), true)]
    public class HGraphGlobalEditor:OdinEditor
    {
        // 在inspector中添加编辑按钮
        public override void OnInspectorGUI()
        {            
            if (GUILayout.Button("Edit graph", GUILayout.Height(40))) {
                HGraphWindow.Open(serializedObject.targetObject as HGraphBase);
            }
            
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(HNodeBase), true)]
    public class HNodeGlobalEditor:OdinEditor
    {
        // 在inspector中添加编辑按钮
        public override void OnInspectorGUI()
        {            
            var node = serializedObject.targetObject as HNodeBase;
            var graph = _getParentGraph(node);
            
            // 显示所属图表信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("所属图表", EditorStyles.boldLabel);
            if (graph != null)
            {
                EditorGUILayout.ObjectField("Graph", graph, typeof(HGraphBase), false);
            }
            else
            {
                EditorGUILayout.HelpBox("无法找到该节点所属的图表", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 编辑图表按钮
            GUI.enabled = graph != null;
            if (GUILayout.Button("Edit Graph", GUILayout.Height(40))) {
                if (graph != null)
                {
                    HGraphWindow.Open(graph);
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(5);
            
            base.OnInspectorGUI();
        }
        
        /// <summary>
        /// 获取节点所属的父图表
        /// </summary>
        private HGraphBase _getParentGraph(HNodeBase node)
        {
            if (node == null) return null;
            
            // 获取节点资产的路径
            var assetPath = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(assetPath)) return null;
            
            // 加载主资产（HGraphBase是主资产，HNodeBase是子资产）
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return mainAsset as HGraphBase;
        }
    }
}