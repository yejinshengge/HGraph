using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    /// <summary>
    /// HGraph 主编辑器窗口，负责文件操作、命令系统接入以及 GraphView 容器管理。
    /// </summary>
    public class HGraphWindow : OdinEditorWindow
    {
        /// <summary>
        /// 打开图编辑器窗口。
        /// </summary>
        [MenuItem("HGraph/HGraph Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<HGraphWindow>(false, "HGraph Editor");
            window.Show();
        }

        // ============ 状态 ============

        /// <summary>
        /// 当前图资源对应的文件路径。
        /// </summary>
        [SerializeField, HideInInspector] private string _currentFilePath;

        /// <summary>
        /// 当前正在编辑的图对象。
        /// </summary>
        [NonSerialized, OdinSerialize, HideInInspector] private HGraph _currentGraph;

        /// <summary>
        /// 当前窗口是否存在未保存修改。
        /// </summary>
        [SerializeField, HideInInspector] private bool _isDirty;

        /// <summary>
        /// 当前承载图内容的 GraphView。
        /// </summary>
        private HGraphView _graphView;

        /// <summary>
        /// GraphView 创建后是否需要提升到最前层显示。
        /// </summary>
        private bool _graphViewNeedsBringToFront;

        /// <summary>
        /// 当前图的命令服务。
        /// </summary>
        private GraphCommandService _commandService;

        /// <summary>
        /// 命令执行后先累积刷新意图，再在合适时机统一刷新 GraphView，
        /// 避免一次用户操作触发多次重建/闪屏。
        /// </summary>
        private GraphCommandRefreshMode _pendingRefreshMode;

        /// <summary>
        /// UIElements 下一帧刷新调度标记，避免重复注册 schedule。
        /// </summary>
        private bool _isGraphRefreshScheduled;
        /// <summary>
        /// 当前窗口是否已加载图对象。
        /// </summary>
        private bool HasGraph => _currentGraph != null;

        // ============ Odin Inspector ============

        [HorizontalGroup("FileInfo"), ShowInInspector, ReadOnly, LabelText("当前文件"), PropertyOrder(0)]
        /// <summary>
        /// Inspector 中展示的当前文件路径。
        /// </summary>
        private string DisplayFilePath => _currentFilePath ?? "无";

        [HorizontalGroup("FileInfo"), ShowInInspector, ReadOnly, LabelText("图类型"), PropertyOrder(1)]
        [ShowIf("HasGraph")]
        /// <summary>
        /// Inspector 中展示的图类型名。
        /// </summary>
        private string CurrentGraphTypeName => _currentGraph?.GetType().Name ?? "无";

        // ============ 生命周期 ============

        /// <summary>
        /// 窗口启用时恢复选择监听，并在必要时重建命令服务与图视图。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            Selection.selectionChanged += OnSelectionChanged;
            if (_currentGraph != null)
            {
                _createCommandService();
                InitGraphView();
            }
        }

        /// <summary>
        /// 窗口销毁时清理事件订阅与图视图。
        /// </summary>
        protected override void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            _unsubscribeCommandService();
            RemoveGraphView();
            base.OnDestroy();
        }

        // ============ Toolbar 绘制 ============

        /// <summary>
        /// Inspector 绘制开始前处理快捷键并绘制工具栏。
        /// </summary>
        protected override void OnBeginDrawEditors()
        {
            _handleShortcuts(Event.current);
            DrawToolbar();
            base.OnBeginDrawEditors();
        }

        /// <summary>
        /// Inspector 绘制结束后消费刷新请求并更新 GraphView 布局。
        /// </summary>
        protected override void OnEndDrawEditors()
        {
            base.OnEndDrawEditors();
            _applyPendingGraphRefresh();
            LayoutGraphView();
        }

        /// <summary>
        /// 绘制顶部工具栏。
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (ToolbarButton("CreateAddNew", " 新建", 60))
                CreateNew();

            using (new EditorGUI.DisabledScope(!HasGraph))
            {
                if (ToolbarButton("SaveAs", " 保存", 60))
                    Save();
                if (ToolbarButton("SaveAs@2x", " 另存为", 70))
                    SaveAs();
                GUILayout.Space(8);
                using (new EditorGUI.DisabledScope(_commandService == null || !_commandService.CanUndo))
                {
                    if (ToolbarButton("TreeEditor.Trash", " 撤销", 60))
                        UndoCommand();
                }
                using (new EditorGUI.DisabledScope(_commandService == null || !_commandService.CanRedo))
                {
                    if (ToolbarButton("TreeEditor.Duplicate", " 重做", 60))
                        RedoCommand();
                }
            }

            GUILayout.FlexibleSpace();

            if (_isDirty)
                GUILayout.Label("未保存", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制统一样式的工具栏按钮。
        /// </summary>
        private static bool ToolbarButton(string iconName, string label, float width)
        {
            var content = new GUIContent(label, EditorGUIUtility.IconContent(iconName).image);
            return GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(width));
        }

        /// <summary>
        /// 让 GraphView 填充当前可用编辑区域。
        /// </summary>
        private void LayoutGraphView()
        {
            if (_currentGraph != null && (_graphView == null || _graphView.parent == null))
                InitGraphView();

            var rect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_graphView != null && Event.current.type == EventType.Repaint)
            {
                _graphView.style.top = rect.yMin;
                _graphView.style.left = rect.xMin;
                _graphView.style.width = rect.width;
                _graphView.style.height = rect.height;

                if (_graphViewNeedsBringToFront)
                {
                    _graphView.BringToFront();
                    _graphViewNeedsBringToFront = false;
                }
            }
        }

        // ============ 命令 ============

        /// <summary>
        /// 让用户选择图类型并创建新图实例。
        /// </summary>
        private void CreateNew()
        {
            var graphTypes = HGraphEditorUtility.GetAllHGraphTypes();
            if (graphTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 HGraph 的具体实现类型，请先创建 HGraph 的子类。", "确定");
                return;
            }

            var menu = new GenericMenu();
            foreach (var type in graphTypes)
            {
                var captured = type;
                menu.AddItem(new GUIContent(captured.Name), false, () => OnSelectNewGraphType(captured));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 保存当前图；若尚未有路径则先选择保存位置。
        /// </summary>
        private void Save()
        {
            if (_currentGraph == null) return;

            if (string.IsNullOrEmpty(_currentFilePath))
            {
                var path = ChooseSavePath();
                if (path == null) return;
                _currentFilePath = path;
            }

            CommitSave();
        }

        /// <summary>
        /// 以新路径保存当前图。
        /// </summary>
        private void SaveAs()
        {
            if (_currentGraph == null) return;

            var path = ChooseSavePath();
            if (path == null) return;

            _currentFilePath = path;
            CommitSave();
        }

        // ============ 事件处理 ============

        /// <summary>
        /// 监听项目窗口选择变化，支持通过选中 .hgraph 资源切换当前图。
        /// </summary>
        private void OnSelectionChanged()
        {
            var selected = Selection.activeObject;
            if (selected == null) return;

            var assetPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith(".hgraph", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(assetPath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
                return;

            if (!ConfirmDiscardChanges()) return;

            LoadGraphFromPath(assetPath);
            Repaint();
        }

        /// <summary>
        /// 处理新图类型的选择结果。
        /// </summary>
        /// <param name="graphType">用户选中的图类型。</param>
        private void OnSelectNewGraphType(Type graphType)
        {
            if (!ConfirmDiscardChanges()) return;

            try
            {
                _currentGraph = (HGraph)Activator.CreateInstance(graphType);
                _currentFilePath = null;
                _isDirty = true;
                _createCommandService();
                UpdateTitle();
                InitGraphView();
            }
            catch (Exception e)
            {
                Debug.LogError($"[HGraphWindow] 创建 {graphType.Name} 失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"创建 {graphType.Name} 实例失败:\n{e.Message}", "确定");
            }
        }

        // ============ GraphView 管理 ============

        /// <summary>
        /// 为当前图重建 GraphView。
        /// </summary>
        private void InitGraphView()
        {
            RemoveGraphView();
            if (_currentGraph == null) return;

            _graphView = new HGraphView(_currentGraph, this);
            _graphView.style.position = Position.Absolute;
            rootVisualElement.Add(_graphView);
            _graphViewNeedsBringToFront = true;
        }

        /// <summary>
        /// 移除当前 GraphView。
        /// </summary>
        private void RemoveGraphView()
        {
            if (_graphView == null) return;
            if (rootVisualElement.Contains(_graphView))
                rootVisualElement.Remove(_graphView);
            _graphView = null;
        }

        // ============ 辅助 ============

        /// <summary>
        /// 手动将窗口标记为脏状态。
        /// </summary>
        public void MarkDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            UpdateTitle();
        }

        /// <summary>
        /// 通过命令系统执行图操作。
        /// </summary>
        /// <param name="command">待执行命令。</param>
        /// <returns>是否执行成功。</returns>
        public bool ExecuteGraphCommand(IGraphCommand command)
        {
            if (_commandService == null || command == null)
            {
                return false;
            }

            return _commandService.Execute(command);
        }

        /// <summary>
        /// 撤销上一条命令。
        /// </summary>
        private void UndoCommand()
        {
            _commandService?.Undo();
        }

        /// <summary>
        /// 重做下一条命令。
        /// </summary>
        private void RedoCommand()
        {
            _commandService?.Redo();
        }

        /// <summary>
        /// 从指定资源路径加载图并刷新窗口状态。
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private void LoadGraphFromPath(string assetPath)
        {
            var graph = HGraphEditorUtility.LoadGraph(assetPath);
            if (graph != null)
            {
                _currentGraph = graph;
                _currentFilePath = assetPath;
                _isDirty = false;
                _createCommandService();
                _commandService?.MarkSaved();
                UpdateTitle();
                InitGraphView();
            }
            else
            {
                Debug.LogWarning($"[HGraphWindow] 无法加载文件: {assetPath}");
            }
        }

        /// <summary>
        /// 执行实际保存逻辑，并同步窗口脏状态。
        /// </summary>
        private void CommitSave()
        {
            HGraphEditorUtility.SaveGraph(_currentFilePath, _currentGraph);
            _commandService?.MarkSaved();
            _isDirty = false;
            UpdateTitle();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 弹出保存路径选择框。
        /// </summary>
        /// <returns>项目内相对路径或绝对路径；取消时返回 null。</returns>
        private static string ChooseSavePath()
        {
            var absolutePath = EditorUtility.SaveFilePanel("保存 HGraph", "Assets", "NewGraph", "hgraph");
            if (string.IsNullOrEmpty(absolutePath)) return null;

            var relativePath = FileUtil.GetProjectRelativePath(absolutePath);
            return string.IsNullOrEmpty(relativePath) ? absolutePath : relativePath;
        }

        /// <summary>
        /// 根据脏状态更新窗口标题。
        /// </summary>
        private void UpdateTitle()
        {
            titleContent = new GUIContent(_isDirty ? "HGraph Editor *" : "HGraph Editor");
        }

        /// <summary>
        /// 基于当前图重建命令服务并重新订阅事件。
        /// </summary>
        private void _createCommandService()
        {
            _unsubscribeCommandService();
            if (_currentGraph == null)
            {
                _commandService = null;
                return;
            }

            _commandService = new GraphCommandService(_currentGraph);
            _commandService.StateChanged += _onCommandStateChanged;
        }

        /// <summary>
        /// 取消订阅旧命令服务事件。
        /// </summary>
        private void _unsubscribeCommandService()
        {
            if (_commandService == null)
            {
                return;
            }

            _commandService.StateChanged -= _onCommandStateChanged;
        }

        /// <summary>
        /// 响应命令系统状态变化，并把刷新请求转成窗口级调度。
        /// </summary>
        /// <param name="commandEvent">命令事件数据。</param>
        private void _onCommandStateChanged(GraphCommandEvent commandEvent)
        {
            _pendingRefreshMode |= commandEvent.RefreshMode;
            _isDirty = commandEvent.IsDirty || (HasGraph && string.IsNullOrEmpty(_currentFilePath) && !_commandService.CanUndo && !_commandService.CanRedo);
            UpdateTitle();
            _scheduleGraphRefresh();
            Repaint();
        }

        /// <summary>
        /// 统一消费命令产生的刷新请求。
        /// `Structure` 表示节点集合发生变化，
        /// `Links` 表示仅连线变化，
        /// `NodePositions` 表示只需同步节点位置。
        /// </summary>
        private void _applyPendingGraphRefresh()
        {
            if (_pendingRefreshMode == GraphCommandRefreshMode.None)
            {
                return;
            }

            var refreshMode = _pendingRefreshMode;
            _pendingRefreshMode = GraphCommandRefreshMode.None;

            if ((refreshMode & GraphCommandRefreshMode.Structure) != 0)
            {
                if (_graphView == null || _graphView.parent == null)
                {
                    InitGraphView();
                }
                else
                {
                    _graphView.RefreshStructureFromModel();
                }
            }
            else
            {
                // NodePorts 需要重建受影响节点的动态端口视图，完成后连线视图也需一并刷新
                if ((refreshMode & GraphCommandRefreshMode.NodePorts) != 0)
                {
                    _graphView?.RefreshNodePortsFromModel();
                }
                else if ((refreshMode & GraphCommandRefreshMode.Links) != 0)
                {
                    _graphView?.RefreshLinksFromModel();
                }
            }

            if ((refreshMode & GraphCommandRefreshMode.NodePositions) != 0)
            {
                _graphView?.RefreshNodePositionsFromModel();
            }

            if ((refreshMode & GraphCommandRefreshMode.Repaint) != 0)
            {
                Repaint();
            }
        }

        /// <summary>
        /// 处理窗口级撤销/重做快捷键。
        /// </summary>
        /// <param name="currentEvent">当前 IMGUI 事件。</param>
        private void _handleShortcuts(Event currentEvent)
        {
            var isActionKeyPressed = currentEvent != null && (currentEvent.command || currentEvent.control);
            if (!HasGraph || currentEvent == null || currentEvent.type != EventType.KeyDown || !isActionKeyPressed)
            {
                return;
            }

            if (currentEvent.keyCode != KeyCode.Z)
            {
                return;
            }

            if (currentEvent.shift)
            {
                if (_commandService != null && _commandService.CanRedo)
                {
                    RedoCommand();
                    currentEvent.Use();
                }

                return;
            }

            if (_commandService != null && _commandService.CanUndo)
            {
                UndoCommand();
                currentEvent.Use();
            }
        }

        /// <summary>
        /// 在下一帧调度图刷新，避免同一帧重复执行。
        /// </summary>
        private void _scheduleGraphRefresh()
        {
            if (_isGraphRefreshScheduled)
            {
                return;
            }

            _isGraphRefreshScheduled = true;
            rootVisualElement.schedule.Execute(() =>
            {
                _isGraphRefreshScheduled = false;
                _applyPendingGraphRefresh();
            }).StartingIn(0);
        }

        /// <summary>
        /// 在切换图或新建图前确认是否丢弃当前未保存修改。
        /// </summary>
        /// <returns>是否允许继续后续操作。</returns>
        private bool ConfirmDiscardChanges()
        {
            if (!_isDirty || _currentGraph == null) return true;

            // 0 = 保存, 1 = 取消, 2 = 不保存
            var choice = EditorUtility.DisplayDialogComplex(
                "未保存的更改", "当前图有未保存的更改，如何处理？",
                "保存", "取消", "不保存");

            if (choice == 0) Save();
            return choice != 1;
        }
    }
}
