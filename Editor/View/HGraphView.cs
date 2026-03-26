using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    /// <summary>
    /// 图可视化主视图，负责节点/连线渲染、交互采集以及命令派发。
    /// </summary>
    public class HGraphView : GraphView
    {
        /// <summary>
        /// 当前绑定的图数据模型。
        /// </summary>
        private readonly HGraphData _graphData;

        /// <summary>
        /// 所属编辑器窗口，用于执行图命令。
        /// </summary>
        private readonly HGraphWindow _window;

        /// <summary>
        /// 右键创建节点时使用的搜索窗口。
        /// </summary>
        private HNodeSearchWindow _searchWindow;

        /// <summary>
        /// 节点 GUID 到节点视图的映射表。
        /// </summary>
        private readonly Dictionary<string, HNodeView> _nodeViewsByGuid = new Dictionary<string, HNodeView>();

        /// <summary>
        /// 当前视图中实际存在的 Edge。
        /// 数据源仍然是 _graph.Links，这里只是为了做局部刷新时统一清理旧 edge。
        /// </summary>
        private readonly List<Edge> _edgeViews = new List<Edge>();

        /// <summary>
        /// 打开创建节点搜索窗口时记录的屏幕坐标。
        /// 选中搜索项时使用该坐标，而不是搜索窗口内部的点击坐标。
        /// </summary>
        private Vector2 _lastNodeCreationScreenPosition;

        /// <summary>
        /// 创建图视图并初始化默认交互组件。
        /// </summary>
        /// <param name="graphData">图数据模型。</param>
        /// <param name="window">所属窗口。</param>
        public HGraphView(HGraphData graphData, HGraphWindow window)
        {
            _graphData = graphData;
            _window = window;

            styleSheets.Add(Resources.Load<StyleSheet>("GraphBackground"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            _initSearchWindow();
            _createNodes();
            _createLinks();
            RegisterCallback<KeyDownEvent>(_onKeyDown);
        }

        /// <summary>
        /// 初始化节点搜索窗口和节点创建请求回调。
        /// </summary>
        private void _initSearchWindow()
        {
            _searchWindow = ScriptableObject.CreateInstance<HNodeSearchWindow>();
            _searchWindow.Init(this, _graphData.GetType());

            nodeCreationRequest = ctx =>
            {
                _lastNodeCreationScreenPosition = ctx.screenMousePosition;
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
            };
        }

#region NodeView

        /// <summary>
        /// 为当前图中的所有节点创建视图。
        /// </summary>
        private void _createNodes()
        {
            foreach (var item in _graphData.Nodes)
            {
                _createNodeView(item);
            }
        }

        /// <summary>
        /// 为单个节点创建并注册视图。
        /// </summary>
        /// <param name="nodeDataData">节点数据。</param>
        /// <returns>创建完成的节点视图。</returns>
        private HNodeView _createNodeView(HNodeData nodeDataData)
        {
            var node = new HNodeView(
                nodeDataData,
                _requestConnectPorts,
                _requestMoveNode,
                _requestInspectorChanged);
            _nodeViewsByGuid[nodeDataData.GUID] = node;
            AddElement(node);
            return node;
        }

        /// <summary>
        /// 在最近一次记录的鼠标位置创建指定类型的节点。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        public void CreateNode(Type nodeType)
        {
            var windowMousePosition = _lastNodeCreationScreenPosition - _window.position.position;
            var localMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
            if (_window.ExecuteGraphCommand(new CreateNodeCommand(nodeType, localMousePosition)))
            {
                // 创建节点后立刻补一轮结构刷新，避免依赖异步调度导致节点延迟出现。
                RefreshStructureFromModel();
            }
        }

#endregion

        /// <summary>
        /// 按 Graph 数据恢复视图中的所有连线。
        /// </summary>
        private void _createLinks()
        {
            foreach (var link in _graphData.Links)
            {
                if (!_tryGetPortView(link.FromNodeId, link.FromPortId, Direction.Output, out var outputPort))
                {
                    continue;
                }

                if (!_tryGetPortView(link.ToNodeId, link.ToPortId, Direction.Input, out var inputPort))
                {
                    continue;
                }

                if (_hasConnection(outputPort, inputPort))
                {
                    continue;
                }

                _createEdgeView(outputPort, inputPort);
            }
        }

        /// <summary>
        /// 返回起始端口可连接的候选端口列表。
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (port == startPort)
                {
                    return;
                }

                if (port.node == startPort.node)
                {
                    return;
                }

                if (port.direction == startPort.direction)
                {
                    return;
                }

                if (!_arePortTypesCompatible(startPort.portType, port.portType))
                {
                    return;
                }

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        /// <summary>
        /// 创建一条连接输出端与输入端的 Edge 视图。
        /// </summary>
        private Edge _createEdgeView(HPortView outputPort, HPortView inputPort)
        {
            var edge = new Edge
            {
                output = outputPort,
                input = inputPort
            };

            edge.output.Connect(edge);
            edge.input.Connect(edge);
            AddElement(edge);
            _edgeViews.Add(edge);
            return edge;
        }

        /// <summary>
        /// 节点结构刷新：
        /// 1. 删除数据层已不存在的 NodeView
        /// 2. 创建缺失的 NodeView
        /// 3. 同步位置
        /// 4. 重新生成连线视图
        /// </summary>
        public void RefreshStructureFromModel()
        {
            _removeMissingNodeViews();
            _createMissingNodeViews();
            RefreshNodePositionsFromModel();
            RefreshLinksFromModel();
        }

        /// <summary>
        /// 连线是纯视图对象，当前采用“清空再按数据重建”的方式保证一致性。
        /// </summary>
        public void RefreshLinksFromModel()
        {
            _clearEdgeViews();
            _createLinks();
        }

        /// <summary>
        /// 将节点位置从模型同步到所有节点视图。
        /// </summary>
        public void RefreshNodePositionsFromModel()
        {
            foreach (var nodeView in _nodeViewsByGuid.Values)
            {
                nodeView.RefreshPositionFromModel();
            }
        }

        /// <summary>
        /// 重建所有节点视图的动态端口，并在之后刷新连线视图。
        /// 当 Inspector 编辑导致某节点动态端口数量变化时由窗口调用。
        /// </summary>
        public void RefreshNodePortsFromModel()
        {
            foreach (var nodeView in _nodeViewsByGuid.Values)
            {
                nodeView.RefreshDynamicPorts();
            }

            // 端口视图重建后，连线视图中的端口引用已失效，需要整体重建
            RefreshLinksFromModel();
        }

        /// <summary>
        /// 根据节点 GUID、端口 GUID 与方向查找端口视图。
        /// </summary>
        private bool _tryGetPortView(string nodeGuid, string portId, Direction direction, out HPortView portView)
        {
            portView = null;
            if (!_nodeViewsByGuid.TryGetValue(nodeGuid, out var nodeView))
            {
                return false;
            }

            return direction == Direction.Input
                ? nodeView.TryGetInputPort(portId, out portView)
                : nodeView.TryGetOutputPort(portId, out portView);
        }

        /// <summary>
        /// 将 Edge 视图转换为可持久化的连线数据。
        /// </summary>
        private bool _tryBuildLink(Edge edge, out HLinkData linkData)
        {
            linkData = null;

            var outputPort = edge?.output as HPortView;
            var inputPort = edge?.input as HPortView;
            if (outputPort == null || inputPort == null)
            {
                return false;
            }

            linkData = new HLinkData(outputPort.NodeGuid, inputPort.NodeGuid, outputPort.PortId, inputPort.PortId);
            return true;
        }

        /// <summary>
        /// 判断两个端口之间是否已经存在可视连接。
        /// </summary>
        private bool _hasConnection(HPortView outputPort, HPortView inputPort)
        {
            return outputPort.connections.Any(connection => connection.input == inputPort);
        }

        /// <summary>
        /// 判断两个端口的数据类型是否允许互连。
        /// </summary>
        private static bool _arePortTypesCompatible(Type startType, Type candidateType)
        {
            if (startType == null || candidateType == null)
            {
                return true;
            }

            return startType == candidateType
                   || startType.IsAssignableFrom(candidateType)
                   || candidateType.IsAssignableFrom(startType);
        }

        /// <summary>
        /// 处理端口连接请求，并根据冲突情况选择创建或替换连线。
        /// </summary>
        private void _requestConnectPorts(HPortView outputPort, HPortView inputPort)
        {
            var newLink = new HLinkData(outputPort.NodeGuid, inputPort.NodeGuid, outputPort.PortId, inputPort.PortId);
            if (HGraphCommandHelper.ContainsLink(_graphData, newLink))
            {
                return;
            }

            var conflictingLinks = new List<LinkRecord>();
            if (outputPort.capacity == Port.Capacity.Single)
            {
                conflictingLinks.AddRange(_graphData.Links
                    .Select((link, index) => new LinkRecord(link, index))
                    .Where(item => string.Equals(item.LinkData.FromPortId, outputPort.PortId, StringComparison.Ordinal)));
            }

            if (inputPort.capacity == Port.Capacity.Single)
            {
                conflictingLinks.AddRange(_graphData.Links
                    .Select((link, index) => new LinkRecord(link, index))
                    .Where(item => string.Equals(item.LinkData.ToPortId, inputPort.PortId, StringComparison.Ordinal)));
            }

            var distinctConflicts = conflictingLinks
                .GroupBy(item => item.LinkData, ReferenceEqualityComparer<HLinkData>.Instance)
                .Select(group => group.First())
                .ToList();

            if (distinctConflicts.Count > 0)
            {
                // 单容量端口重连时，需要把“删旧边 + 建新边”作为一次原子命令。
                _window.ExecuteGraphCommand(new ReplaceLinkCommand(newLink, distinctConflicts));
                return;
            }

            _window.ExecuteGraphCommand(new CreateLinkCommand(newLink));
        }

        /// <summary>
        /// 提交节点移动命令。
        /// </summary>
        private void _requestMoveNode(HNodeData nodeData, Vector2 from, Vector2 to)
        {
            _window.ExecuteGraphCommand(new MoveNodeCommand(nodeData, from, to));
        }

        /// <summary>
        /// 提交节点 Inspector 状态变更命令。
        /// </summary>
        private void _requestInspectorChanged(HNodeData nodeData, HNodeData beforeState, HNodeData afterState)
        {
            _window.ExecuteGraphCommand(new EditNodeStateCommand(nodeData, beforeState, afterState));
        }

        /// <summary>
        /// 处理 Delete / Backspace 删除当前选中内容。
        /// </summary>
        private void _onKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace)
            {
                return;
            }

            var deleteCommand = _buildDeleteSelectionCommand();
            if (deleteCommand == null)
            {
                return;
            }

            evt.StopImmediatePropagation();
            _window.ExecuteGraphCommand(deleteCommand);
        }

        /// <summary>
        /// 根据当前选择构建删除命令。
        /// </summary>
        private IGraphCommand _buildDeleteSelectionCommand()
        {
            var selectedNodeViews = selection
                .OfType<HNodeView>()
                .Distinct()
                .OrderByDescending(nodeView => _graphData.Nodes.IndexOf(nodeView.NodeDataData))
                .ToList();

            var selectedNodeIds = new HashSet<string>(selectedNodeViews.Select(nodeView => nodeView.NodeDataData.GUID));
            var commands = new List<IGraphCommand>();
            commands.AddRange(selectedNodeViews.Select(nodeView => new DeleteNodeCommand(nodeView.NodeDataData)));

            foreach (var edge in selection.OfType<Edge>())
            {
                if (!_tryBuildLink(edge, out var link))
                {
                    continue;
                }

                if (selectedNodeIds.Contains(link.FromNodeId) || selectedNodeIds.Contains(link.ToNodeId))
                {
                    continue;
                }

                commands.Add(new DeleteLinkCommand(link));
            }

            if (commands.Count == 0)
            {
                return null;
            }

            return new CompositeGraphCommand("Delete Selection", commands);
        }

        /// <summary>
        /// 删除模型中已不存在的节点视图。
        /// </summary>
        private void _removeMissingNodeViews()
        {
            var validNodeIds = new HashSet<string>(_graphData.Nodes.Select(node => node.GUID));
            var staleNodeIds = _nodeViewsByGuid.Keys
                .Where(nodeId => !validNodeIds.Contains(nodeId))
                .ToList();

            foreach (var nodeId in staleNodeIds)
            {
                var nodeView = _nodeViewsByGuid[nodeId];
                _nodeViewsByGuid.Remove(nodeId);
                RemoveElement(nodeView);
            }
        }

        /// <summary>
        /// 为模型中新出现的节点补建视图。
        /// </summary>
        private void _createMissingNodeViews()
        {
            foreach (var node in _graphData.Nodes)
            {
                if (_nodeViewsByGuid.ContainsKey(node.GUID))
                {
                    continue;
                }

                _createNodeView(node);
            }
        }

        /// <summary>
        /// 清空当前所有 Edge 视图，并同步端口连接状态。
        /// </summary>
        private void _clearEdgeViews()
        {
            for (var i = _edgeViews.Count - 1; i >= 0; i--)
            {
                var edge = _edgeViews[i];
                // 先从端口连接列表里断开，再移除视觉元素，否则端口会残留“已连接”状态。
                edge.output?.Disconnect(edge);
                edge.input?.Disconnect(edge);
                RemoveElement(edge);
            }

            _edgeViews.Clear();
        }

        /// <summary>
        /// 引用相等比较器，用于按对象实例进行去重。
        /// </summary>
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

            /// <summary>
            /// 判断两个对象是否引用同一实例。
            /// </summary>
            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            /// <summary>
            /// 返回对象的原始哈希值。
            /// </summary>
            public int GetHashCode(T obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }
    }
}
