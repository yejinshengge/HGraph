using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// HGraphImporter 的自定义 Inspector 编辑器。
    /// 在 Inspector 面板中以只读树形结构展示 .hgraph 文件的反序列化内容。
    /// </summary>
    [CustomEditor(typeof(HGraphImporter))]
    public class HGraphImporterEditor : ScriptedImporterEditor
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // ============ 缓存 ============

        private HGraphData _cachedGraph;
        private string _cachedPath;
        private Hash128 _cachedHash;

        // ============ Foldout 状态 ============

        private bool _nodesFoldout;
        private bool _linksFoldout;
        private readonly Dictionary<string, bool> _nodeFoldouts = new();
        private readonly Dictionary<string, bool> _portsFoldouts = new();
        private readonly Dictionary<string, bool> _fieldsFoldouts = new();

        // ============ 主绘制 ============

        public override void OnInspectorGUI()
        {
            var importer = (HGraphImporter)target;
            var assetPath = importer.assetPath;
            var asset = AssetDatabase.LoadAssetAtPath<HGraphAsset>(assetPath);

            if (asset == null)
            {
                EditorGUILayout.HelpBox("无法加载 HGraphAsset 资源。", MessageType.Error);
                ApplyRevertGUI();
                return;
            }

            // 3.7 导入失败时显示错误信息
            if (!asset.ImportSuccess)
            {
                EditorGUILayout.HelpBox(
                    $"导入失败：{asset.ErrorMessage}", MessageType.Error);
                ApplyRevertGUI();
                return;
            }

            // 3.1 按需反序列化 + 3.2 缓存失效
            EnsureGraphCache(assetPath);

            if (_cachedGraph == null)
            {
                EditorGUILayout.HelpBox("无法反序列化图数据。", MessageType.Warning);
                ApplyRevertGUI();
                return;
            }

            // 3.6 "在图编辑器中打开" 按钮
            DrawOpenEditorButton(assetPath);

            EditorGUILayout.Space(4);

            // 3.3 图概览
            DrawOverview();

            EditorGUILayout.Space(4);

            // 3.4 节点列表
            DrawNodeList();

            EditorGUILayout.Space(2);

            // 3.5 连接列表
            DrawLinkList();

            ApplyRevertGUI();
        }

        // ============ 缓存管理 ============

        private void EnsureGraphCache(string assetPath)
        {
            var hash = AssetDatabase.GetAssetDependencyHash(assetPath);

            if (_cachedGraph != null
                && string.Equals(_cachedPath, assetPath, StringComparison.Ordinal)
                && _cachedHash == hash)
            {
                return;
            }

            // 缓存失效或首次加载
            _cachedPath = assetPath;
            _cachedHash = hash;
            _cachedGraph = null;

            try
            {
                _cachedGraph = HGraphEditorUtility.LoadGraph(assetPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HGraphImporterEditor] 反序列化失败: {e.Message}");
            }
        }

        // ============ 3.6 打开编辑器按钮 ============

        private static void DrawOpenEditorButton(string assetPath)
        {
            if (GUILayout.Button("在图编辑器中打开", GUILayout.Height(24)))
            {
                HGraphWindow.OpenGraphFile(assetPath);
            }
        }

        // ============ 3.3 图概览 ============

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("图概览", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("图类型", _cachedGraph.GetType().Name);
                EditorGUILayout.LabelField("节点总数", _cachedGraph.Nodes.Count.ToString());
                EditorGUILayout.LabelField("连接总数", _cachedGraph.Links.Count.ToString());
            }
        }

        // ============ 3.4 节点列表 ============

        private void DrawNodeList()
        {
            _nodesFoldout = EditorGUILayout.Foldout(_nodesFoldout, $"节点 ({_cachedGraph.Nodes.Count})", true);
            if (!_nodesFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var node in _cachedGraph.Nodes)
                {
                    DrawNode(node);
                }
            }
        }

        private void DrawNode(HNodeData node)
        {
            var nodeType = node.GetType();
            var label = $"{nodeType.Name}  [{node.GUID}]";

            _nodeFoldouts.TryAdd(node.GUID, false);
            _nodeFoldouts[node.GUID] = EditorGUILayout.Foldout(_nodeFoldouts[node.GUID], label, true);

            if (!_nodeFoldouts[node.GUID]) return;

            using (new EditorGUI.IndentLevelScope())
            {
                // 端口列表
                DrawNodePorts(node);

                // 自定义字段
                DrawNodeFields(node);
            }
        }

        private void DrawNodePorts(HNodeData node)
        {
            var portKey = node.GUID + "_ports";
            _portsFoldouts.TryAdd(portKey, false);

            var portCount = node.PortCollection.Count;
            _portsFoldouts[portKey] = EditorGUILayout.Foldout(
                _portsFoldouts[portKey], $"端口 ({portCount})", true);

            if (!_portsFoldouts[portKey]) return;

            using (new EditorGUI.IndentLevelScope())
            {
                // 静态端口
                foreach (var member in HGraphNodePortUtility.GetPortMembers(node.GetType()))
                {
                    if (!node.TryGetStaticPort(member.Name, out var port))
                        continue;

                    var direction = member.IsDefined(typeof(InputAttribute), true) ? "Input" : "Output";
                    EditorGUILayout.LabelField(
                        $"{member.Name} ({direction})",
                        port.GUID);
                }

                // 动态端口
                foreach (var (descriptor, port) in node.GetDynamicPortsWithData())
                {
                    var dirStr = descriptor.Direction == PortDirection.Input ? "Input" : "Output";
                    var displayName = string.IsNullOrEmpty(descriptor.Label) ? descriptor.Key : descriptor.Label;
                    EditorGUILayout.LabelField(
                        $"{displayName} ({dirStr}, Key={descriptor.Key})",
                        port.GUID);
                }
            }
        }

        private void DrawNodeFields(HNodeData node)
        {
            var fieldsKey = node.GUID + "_fields";
            _fieldsFoldouts.TryAdd(fieldsKey, false);

            var customFields = GetCustomFields(node);
            if (customFields.Count == 0) return;

            _fieldsFoldouts[fieldsKey] = EditorGUILayout.Foldout(
                _fieldsFoldouts[fieldsKey], $"自定义字段 ({customFields.Count})", true);

            if (!_fieldsFoldouts[fieldsKey]) return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (name, value) in customFields)
                {
                    EditorGUILayout.LabelField(name, FormatValue(value));
                }
            }
        }

        private static List<(string Name, object Value)> GetCustomFields(HNodeData node)
        {
            var result = new List<(string, object)>();
            var nodeType = node.GetType();
            var baseType = typeof(HNodeData);

            foreach (var field in nodeType.GetFields(FieldFlags))
            {
                // 跳过 HNodeData 基类声明的字段
                if (field.DeclaringType == baseType || (field.DeclaringType != null && field.DeclaringType.IsAssignableFrom(baseType)))
                    continue;

                // 跳过端口字段
                if (field.IsDefined(typeof(HGraphPortAttribute), true))
                    continue;

                // 跳过 NotSerialize
                if (field.IsDefined(typeof(NotSerializeAttribute), true))
                    continue;

                // 跳过编辑器专用标记字段
                if (field.IsDefined(typeof(SerializeInEditorAttribute), true))
                    continue;

                // 仅包含公共字段或 ForceSerialize 私有字段
                var isSerializable = field.IsPublic || field.IsDefined(typeof(ForceSerializeAttribute), true);
                if (!isSerializable)
                    continue;

                result.Add((field.Name, field.GetValue(node)));
            }

            return result;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "(null)";
            if (value is ICollection collection) return $"[Count: {collection.Count}]";
            return value.ToString();
        }

        // ============ 3.5 连接列表 ============

        private void DrawLinkList()
        {
            _linksFoldout = EditorGUILayout.Foldout(
                _linksFoldout, $"连接 ({_cachedGraph.Links.Count})", true);

            if (!_linksFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var link in _cachedGraph.Links)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("源节点", link.FromNodeId);
                    EditorGUILayout.LabelField("源端口", link.FromPortId);
                    EditorGUILayout.LabelField("目标节点", link.ToNodeId);
                    EditorGUILayout.LabelField("目标端口", link.ToPortId);
                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}
