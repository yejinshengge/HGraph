using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace HGraph.Editor
{
    /// <summary>
    /// 图节点视图，负责节点标题、端口与 Inspector 面板的渲染。
    /// </summary>
    public class HNodeView : NodeView
    {
        /// <summary>
        /// 当前节点绑定的数据对象。
        /// </summary>
        public HNode NodeData { get; private set; }

        /// <summary>
        /// Odin 属性树，用于绘制节点扩展面板中的字段。
        /// </summary>
        private PropertyTree _propertyTree;

        /// <summary>
        /// 记录被识别为端口的成员名，用于在 Inspector 中过滤显示。
        /// </summary>
        private readonly HashSet<string> _portMemberNames = new HashSet<string>();

        /// <summary>
        /// 输入端口索引（静态 + 动态），便于按端口 ID 恢复连线。
        /// </summary>
        private readonly Dictionary<string, HPortView> _inputPorts = new Dictionary<string, HPortView>();

        /// <summary>
        /// 输出端口索引（静态 + 动态），便于按端口 ID 恢复连线。
        /// </summary>
        private readonly Dictionary<string, HPortView> _outputPorts = new Dictionary<string, HPortView>();

        /// <summary>
        /// 动态输入端口集合，与 <see cref="_inputPorts"/> 共享视图实例，
        /// 但单独跟踪以支持局部刷新时精确移除。
        /// </summary>
        private readonly List<HPortView> _dynamicInputPortViews = new List<HPortView>();

        /// <summary>
        /// 动态输出端口集合，与 <see cref="_outputPorts"/> 共享视图实例，
        /// 但单独跟踪以支持局部刷新时精确移除。
        /// </summary>
        private readonly List<HPortView> _dynamicOutputPortViews = new List<HPortView>();

        private readonly Action<HPortView, HPortView> _onConnectRequested;

        private readonly Action<HNode, Vector2, Vector2> _onMoveRequested;

        private readonly Action<HNode, HNode, HNode> _onInspectorChanged;

        /// <summary>
        /// 上一帧动态端口描述符的签名缓存，用于检测 Odin 列表结构性变更。
        /// </summary>
        private int _lastDynamicPortSignature;

        /// <summary>
        /// 从模型同步位置时为 true，用来阻止 `SetPosition` 把这次刷新误判为用户拖拽。
        /// </summary>
        private bool _isApplyingPositionFromModel;

        /// <summary>
        /// 拖拽开始时的节点位置。鼠标抬起后用它生成 MoveNodeCommand。
        /// </summary>
        private Vector2? _dragStartPosition;

        /// <summary>
        /// 构建一个节点视图并初始化端口与 Inspector。
        /// </summary>
        /// <param name="nodeData">节点数据。</param>
        public HNodeView(
            HNode nodeData,
            Action<HPortView, HPortView> onConnectRequested,
            Action<HNode, Vector2, Vector2> onMoveRequested,
            Action<HNode, HNode, HNode> onInspectorChanged)
        {
            NodeData = nodeData;
            _onConnectRequested = onConnectRequested;
            _onMoveRequested = onMoveRequested;
            _onInspectorChanged = onInspectorChanged;
            title = nodeData.GetType().Name;
            _applyPositionWithoutNotify(nodeData.GraphPosition);

            style.minWidth = 200;
            _lastDynamicPortSignature = nodeData.ComputeDynamicPortSignature();
            _setupPorts();
            _setupInspector();
            RegisterCallback<MouseDownEvent>(_onMouseDown);
            RegisterCallback<MouseUpEvent>(_onMouseUp);
        }

        /// <summary>
        /// 扫描节点上的端口特性并创建输入/输出端口视图（静态端口 + 动态端口）。
        /// </summary>
        private void _setupPorts()
        {
            foreach (var member in HGraphNodePortUtility.GetPortMembers(NodeData.GetType()))
            {
                var input = member.GetCustomAttribute<InputAttribute>(true);
                if (input != null)
                {
                    _portMemberNames.Add(member.Name);
                    if (NodeData.TryGetStaticPort(member.Name, out var inputPortData))
                    {
                        var inputPort = HPortView.Create(inputPortData, member, input, UnityEditor.Experimental.GraphView.Direction.Input, _onConnectRequested);
                        _inputPorts[inputPort.PortId] = inputPort;
                        inputContainer.Add(inputPort);
                    }
                }

                var output = member.GetCustomAttribute<OutputAttribute>(true);
                if (output != null)
                {
                    _portMemberNames.Add(member.Name);
                    if (NodeData.TryGetStaticPort(member.Name, out var outputPortData))
                    {
                        var outputPort = HPortView.Create(outputPortData, member, output, UnityEditor.Experimental.GraphView.Direction.Output, _onConnectRequested);
                        _outputPorts[outputPort.PortId] = outputPort;
                        outputContainer.Add(outputPort);
                    }
                }
            }

            _setupDynamicPorts();
            RefreshPorts();
        }

        /// <summary>
        /// 根据节点当前的动态端口数据创建端口视图，并注册到全局端口索引中。
        /// </summary>
        private void _setupDynamicPorts()
        {
            foreach (var (descriptor, portData) in NodeData.GetDynamicPortsWithData())
            {
                var direction = descriptor.Direction == PortDirection.Input
                    ? UnityEditor.Experimental.GraphView.Direction.Input
                    : UnityEditor.Experimental.GraphView.Direction.Output;

                var portView = HPortView.CreateDynamic(portData, descriptor, direction, _onConnectRequested);

                if (direction == UnityEditor.Experimental.GraphView.Direction.Input)
                {
                    _inputPorts[portView.PortId] = portView;
                    _dynamicInputPortViews.Add(portView);
                    inputContainer.Add(portView);
                }
                else
                {
                    _outputPorts[portView.PortId] = portView;
                    _dynamicOutputPortViews.Add(portView);
                    outputContainer.Add(portView);
                }
            }
        }

        /// <summary>
        /// 重建动态端口视图：
        /// <list type="number">
        ///   <item>从容器与索引中移除旧动态端口视图；</item>
        ///   <item>调用 <see cref="HNode.RebuildDynamicPorts"/> 同步数据层；</item>
        ///   <item>依据最新数据重建视图并注册到端口索引；</item>
        ///   <item>通知 GraphView 刷新端口布局。</item>
        /// </list>
        /// </summary>
        public void RefreshDynamicPorts()
        {
            // 从索引和容器中移除旧动态输入端口
            foreach (var portView in _dynamicInputPortViews)
            {
                _inputPorts.Remove(portView.PortId);
                inputContainer.Remove(portView);
            }
            _dynamicInputPortViews.Clear();

            // 从索引和容器中移除旧动态输出端口
            foreach (var portView in _dynamicOutputPortViews)
            {
                _outputPorts.Remove(portView.PortId);
                outputContainer.Remove(portView);
            }
            _dynamicOutputPortViews.Clear();

            // 同步数据层（已在 EditNodeStateCommand 中调用过，此处再次调用保证幂等）
            NodeData.RebuildDynamicPorts();

            // 重建视图
            _setupDynamicPorts();
            RefreshPorts();
        }

        /// <summary>
        /// 初始化节点扩展区的 Inspector 容器与生命周期回调。
        /// </summary>
        private void _setupInspector()
        {
            _propertyTree = PropertyTree.Create(NodeData);

            var inspectorContainer = new IMGUIContainer(() =>
            {
                _drawInspectorWithoutPortMembers();
            });
            // 添加背景
            extensionContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);
            extensionContainer.style.paddingTop = 4;
            extensionContainer.style.paddingBottom = 4;
            extensionContainer.style.paddingLeft = 4;
            extensionContainer.style.paddingRight = 4;
            extensionContainer.Add(inspectorContainer);
            RefreshExpandedState();

            //UIElements销毁时回调
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _propertyTree?.Dispose();
                _propertyTree = null;
            });
        }

        /// <summary>
        /// 绘制 Inspector，并过滤掉已经作为端口展示的成员。
        /// </summary>
        private void _drawInspectorWithoutPortMembers()
        {
            if (_propertyTree == null)
            {
                return;
            }

            // Inspector 编辑使用“前后快照”生成命令，
            // 这样不用为每个字段手写撤销逻辑，也能兼容未来的节点子类。
            var beforeSnapshot = GraphCommandSnapshotUtility.CreateCopy(NodeData);
            EditorGUI.BeginChangeCheck();
            _propertyTree.BeginDraw(false);
            for (var i = 0; i < _propertyTree.RootPropertyCount; i++)
            {
                var rootProperty = _propertyTree.GetRootProperty(i);
                var memberName = rootProperty?.Info?.PropertyName;

                if (!string.IsNullOrEmpty(memberName) && _portMemberNames.Contains(memberName))
                {
                    continue;
                }

                rootProperty?.Draw();
            }
            _propertyTree.EndDraw();

            var changed = EditorGUI.EndChangeCheck();

            // Odin 对 List 的结构性操作（增删元素）可能不触发 EndChangeCheck，
            // 通过动态端口签名变化来补充检测。
            if (!changed)
            {
                changed = NodeData.ComputeDynamicPortSignature() != _lastDynamicPortSignature;
            }

            if (changed)
            {
                _lastDynamicPortSignature = NodeData.ComputeDynamicPortSignature();
                var afterSnapshot = GraphCommandSnapshotUtility.CreateCopy(NodeData);
                if (!GraphCommandSnapshotUtility.AreEquivalent(beforeSnapshot, afterSnapshot))
                {
                    _onInspectorChanged?.Invoke(NodeData, beforeSnapshot, afterSnapshot);
                }
            }
        }

        /// <summary>
        /// 节点拖拽后同步保存节点在图中的位置。
        /// </summary>
        /// <param name="newPos">新的节点矩形区域。</param>
        public override void SetPosition(Rect newPos)
        {
            if (_isApplyingPositionFromModel)
            {
                base.SetPosition(newPos);
                return;
            }

            base.SetPosition(newPos);

            // 拖拽过程中实时把视图位置写回模型，避免后续刷新把节点“弹回旧位置”。
            NodeData.GraphPosition = newPos.position;
        }

        /// <summary>
        /// 仅在命令系统回放/刷新时使用，从模型同步位置到视图，不生成新的移动命令。
        /// </summary>
        public void RefreshPositionFromModel()
        {
            if ((GetPosition().position - NodeData.GraphPosition).sqrMagnitude < 0.0001f)
            {
                return;
            }

            _applyPositionWithoutNotify(NodeData.GraphPosition);
        }

        /// <summary>
        /// 按端口 ID 获取输入端口视图。
        /// </summary>
        public bool TryGetInputPort(string portId, out HPortView portView)
        {
            return _inputPorts.TryGetValue(portId, out portView);
        }

        /// <summary>
        /// 按端口 ID 获取输出端口视图。
        /// </summary>
        public bool TryGetOutputPort(string portId, out HPortView portView)
        {
            return _outputPorts.TryGetValue(portId, out portView);
        }

        private void _applyPositionWithoutNotify(Vector2 position)
        {
            _isApplyingPositionFromModel = true;
            try
            {
                base.SetPosition(new Rect(position, Vector2.zero));
            }
            finally
            {
                _isApplyingPositionFromModel = false;
            }
        }

        private void _onMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _dragStartPosition = NodeData.GraphPosition;
        }

        private void _onMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0 || !_dragStartPosition.HasValue)
            {
                return;
            }

            var dragStartPosition = _dragStartPosition.Value;
            _dragStartPosition = null;
            var currentPosition = GetPosition().position;
            if ((currentPosition - dragStartPosition).sqrMagnitude < 0.0001f)
            {
                return;
            }

            // 拖拽结束后只提交一次移动命令，避免拖拽每一帧都污染命令栈。
            _onMoveRequested?.Invoke(NodeData, dragStartPosition, currentPosition);
        }
    }
}
