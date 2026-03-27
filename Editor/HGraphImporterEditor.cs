using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// HGraphImporter 的自定义 Inspector 编辑器。
    /// 在 Inspector 面板中以只读树形结构展示 .hgraph 文件的反序列化内容。
    /// 使用 Odin PropertyTree 自动渲染图数据，替代手写 IMGUI。
    /// </summary>
    [CustomEditor(typeof(HGraphImporter))]
    public class HGraphImporterEditor : ScriptedImporterEditor
    {

        // ============ 缓存 ============

        private HGraphData _cachedGraph;
        private string _cachedPath;
        private Hash128 _cachedHash;
        private PropertyTree _propertyTree;

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

            if (!asset.ImportSuccess)
            {
                EditorGUILayout.HelpBox(
                    $"导入失败：{asset.ErrorMessage}", MessageType.Error);
                ApplyRevertGUI();
                return;
            }

            EnsureGraphCache(assetPath);

            if (_cachedGraph == null)
            {
                EditorGUILayout.HelpBox("无法反序列化图数据。", MessageType.Warning);
                ApplyRevertGUI();
                return;
            }

            DrawOpenEditorButton(assetPath);

            EditorGUILayout.Space(4);

            DrawOverview();

            EditorGUILayout.Space(4);

            // 用 Odin PropertyTree 只读渲染图数据。
            // 注意：PropertyTree.Draw() 内部已等价于 BeginDraw + DrawPropertiesInTree + EndDraw，
            // 不要再在外层套 BeginDraw/EndDraw，否则会导致嵌套/漏配，Inspector 上表现为整块不绘制。
            if (_propertyTree != null)
            {
                // 目标对象在缓存重建后可能替换引用，每帧同步属性树；Odin 4 建议与 Draw 同帧调用。
                _propertyTree.UpdateTree();

                // 使用 DisabledScope：部分 Odin 版本在 GUI.enabled=false 时会跳过绘制或表现异常。
                using (new EditorGUI.DisabledScope(true))
                {
                    _propertyTree.Draw(false);
                }
            }

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

            _cachedPath = assetPath;
            _cachedHash = hash;
            _cachedGraph = null;
            _disposePropertyTree();

            try
            {
                _cachedGraph = HGraphEditorUtility.LoadGraph(assetPath);
                if (_cachedGraph != null)
                {
                    // 必须指定 Odin 后端：默认 Unity 后端不支持抽象/多态列表（如 List<HNodeData>），
                    // 会导致根属性下无任何可绘制成员，Inspector 上表现为整块空白。
                    // Odin 4 在自定义 Editor 的 IMGUI 中绘制 PropertyTree 时，需 SetUpForIMGUIDrawing。
                    _propertyTree = PropertyTree.Create(_cachedGraph, SerializationBackend.Odin)
                        .SetUpForIMGUIDrawing();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HGraphImporterEditor] 反序列化失败: {e.Message}");
            }
        }

        private void _disposePropertyTree()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
        }

        // ============ 生命周期 ============

        public override void OnDisable()
        {
            _disposePropertyTree();
            base.OnDisable();
        }

        // ============ 打开编辑器按钮 ============

        private static void DrawOpenEditorButton(string assetPath)
        {
            if (GUILayout.Button("在图编辑器中打开", GUILayout.Height(24)))
            {
                HGraphWindow.OpenGraphFile(assetPath);
            }
        }

        // ============ 图概览 ============

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
    }
}
