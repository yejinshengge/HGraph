
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    /// <summary>
    /// 图表视图
    /// </summary>
    public class HGraphView : GraphView
    {
        // 图表数据
        private HGraphBase _graph;

        // 小地图
        private MiniMap _miniMap;

        // 搜索窗口
        private HNodeSearchWindow _nodeSearchWindow;

        // 黑板
        private Blackboard _blackboard;

        // 黑板属性
        private List<HBlackBoardProperty> _exposedProperties = new List<HBlackBoardProperty>();


        public HGraphView(EditorWindow editorWindow,HGraphBase graph)
        {
            this.name = graph.name;
            _graph = graph;
            styleSheets.Add(Resources.Load<StyleSheet>("GraphBackground"));
            // 缩放
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // _addStartNode();
            _createNodes();
            _createConnections();
            _createMiniMap();
            _createBlackBoard();
            _addSearchWindow(editorWindow);
            
            _registerEvent();
        }
        
#region 事件
        // 注册事件
        private void _registerEvent()
        {
            // 注册回调
            deleteSelection = _onDeleteSelectedNode; // 删除节点
            graphViewChanged = _onGraphViewChanged; // 图表变化回调
            
            RegisterCallback<GeometryChangedEvent>(evt => _updateMiniMapPosition()); // 更新小地图位置
            
            // 注册Undo/Redo事件，实现即时刷新
            Undo.undoRedoPerformed += _onUndoRedoPerformed;
        }

        // Undo/Redo操作后的回调
        private void _onUndoRedoPerformed()
        {
            // 重建整个图表视图以保证UI与数据同步
            _rebuildGraphView();
        }

        // 图表变化回调
        private GraphViewChange _onGraphViewChanged(GraphViewChange changes)
        {
            // 处理添加的边
            if (changes.edgesToCreate != null)
            {
                foreach (var edge in changes.edgesToCreate)
                {
                    if (edge.output?.node is HNodeView outputNode && edge.input?.node is HNodeView inputNode)
                    {
                        // 记录Undo
                        Undo.RecordObject(_graph, "Create Edge");
                        var link = HGraphUtility.LinkPort(outputNode.GUID, inputNode.GUID, edge.output, edge.input);
                        // 添加到图表数据
                        _graph.links.Add(link);
                    }
                }
                
                // 标记为脏，触发序列化
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssets();
            }
            
            // 处理移除的边
            if (changes.elementsToRemove != null)
            {
                bool hasChanges = false;
                foreach (var element in changes.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        if (edge.output?.node is HNodeView outputNode && edge.input?.node is HNodeView inputNode)
                        {
                            if (!hasChanges)
                            {
                                // 记录Undo（只记录一次）
                                Undo.RecordObject(_graph, "Remove Edge");
                                hasChanges = true;
                            }
                            
                            // 从连接列表中移除对应的连接
                            _graph.links.RemoveAll(link => 
                                link.BaseNodeGUID == outputNode.GUID && 
                                link.TargetNodeGUID == inputNode.GUID &&
                                link.BasePortGUID == edge.output.name &&
                                link.TargetPortGUID == edge.input.name);
                        }
                    }
                }
                
                if (hasChanges)
                {
                    // 标记为脏，触发序列化
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssets();
                }
            }

            return changes;
        }
        
        /// <summary>
        /// 重建图表视图
        /// </summary>
        private void _rebuildGraphView()
        {
            if (_graph == null) return;
            
            // 清除所有现有节点视图
            var nodesToRemove = nodes.ToList().Cast<HNodeView>().ToList();
            foreach (var nodeView in nodesToRemove)
            {
                nodeView.Dispose();
                RemoveElement(nodeView);
            }
            
            // 清除所有边
            var edgesToRemove = edges.ToList();
            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }
            
            // 重新创建节点和连接
            _createNodes();
            _createConnections();
        }
#endregion        
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 取消注册Undo/Redo事件
            Undo.undoRedoPerformed -= _onUndoRedoPerformed;
            
            // 清理所有节点视图
            var nodesToRemove = nodes.ToList().Cast<HNodeView>().ToList();
            foreach (var nodeView in nodesToRemove)
            {
                nodeView.Dispose();
            }
        }

        /// <summary>
        /// 获取图类型
        /// </summary>
        /// <returns></returns>
        public Type GetGraphType()
        {
            return _graph.GetType();
        }

        // 添加搜索窗口
        private void _addSearchWindow(EditorWindow editorWindow)
        {
            _nodeSearchWindow = ScriptableObject.CreateInstance<HNodeSearchWindow>();
            _nodeSearchWindow.Init(this,editorWindow);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition),_nodeSearchWindow);
        }

#region nodes
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(HNodeBase node)
        {
            // 记录Undo
            Undo.RecordObject(_graph, "Add Node");
            
            _graph.nodes.Add(node);
            AssetDatabase.AddObjectToAsset(node, _graph);
            // 标记为脏，触发序列化
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            CreateNodeView(node);
        }

        /// <summary>
        /// 创建节点视图
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        public HNodeView CreateNodeView(HNodeBase nodeData)
        {
            // 视图
            var node = new HNodeView();
            node.Init(nodeData);
            AddElement(node);
            return node;
        }

        // 创建节点视图
        private void _createNodes()
        {
            foreach(var item in _graph.nodes)
            {
                CreateNodeView(item);
            }
        }

        // 创建连接
        private void _createConnections()
        {
            var _nodes = nodes.ToList().Cast<HNodeView>().ToList();
            foreach(var node in _nodes)
            {
                // 当前节点的所有连接
                var connections = _graph.links.Where(x => x.BaseNodeGUID == node.GUID).ToList();
                for(var i = 0; i < connections.Count; i++)
                {
                    // 目标节点
                    var targetNode = _nodes.First(x => x.GUID == connections[i].TargetNodeGUID);
                    var link = connections[i];

                    _linkNodes(node.mainContainer.Q<Port>(link.BasePortGUID),targetNode.mainContainer.Q<Port>(link.TargetPortGUID));
                }

            }
        }

        // 连接节点
        private void _linkNodes(Port outputPort, Port inputPort)
        {
            var edge = new Edge()
            {
                output = outputPort,
                input = inputPort
            };
            edge.input?.Connect(edge);
            edge.output?.Connect(edge);
            AddElement(edge);
        }

        // 删除选中节点
        private void _onDeleteSelectedNode(string operationName, AskUser askUser)
        {
            // 获取要删除的节点
            var nodesToDelete = new List<HNodeView>();
            
            foreach (var element in selection)
            {
                if (element is HNodeView nodeView)
                {
                    nodesToDelete.Add(nodeView);
                }
            }
            
            // 如果有节点要删除，记录Undo
            if (nodesToDelete.Count > 0)
            {
                Undo.RecordObject(_graph, "Delete Node");
            }
            
            // 收集所有要删除的边（连接到要删除节点的边）
            var edgesToDelete = new List<Edge>();
            foreach (var nodeView in nodesToDelete)
            {
                var nodeEdges = edges.ToList().Where(edge => 
                    edge.output?.node == nodeView || edge.input?.node == nodeView).ToList();
                edgesToDelete.AddRange(nodeEdges);
            }
            
            // 删除节点配置和数据中的连接
            foreach (var nodeView in nodesToDelete)
            {
                // 找到对应的节点数据
                var nodeData = _graph.nodes.FirstOrDefault(n => n.GUID == nodeView.GUID);
                if (nodeData != null)
                {
                    // 从节点列表中移除
                    _graph.nodes.Remove(nodeData);
                    
                    // 删除与该节点相关的所有连接（作为起始节点或目标节点）
                    _graph.links.RemoveAll(link => 
                        link.BaseNodeGUID == nodeView.GUID || 
                        link.TargetNodeGUID == nodeView.GUID);
                    
                    // 从资产中移除节点子对象
                    AssetDatabase.RemoveObjectFromAsset(nodeData);
                }
                
                // 释放节点视图资源
                nodeView.Dispose();
            }
            
            // 如果有节点被删除，标记为脏并保存
            if (nodesToDelete.Count > 0)
            {
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssets();
            }
            
            // 先删除所有相关的边，并断开端口连接
            foreach (var edge in edgesToDelete)
            {
                // 断开端口连接
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                // 从视图中移除边
                RemoveElement(edge);
            }
            
            // 执行默认的删除操作（从视图中删除节点）
            DeleteSelection();
        }

        // 筛选可以连接的端口
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            if(!HGraphUtility.CheckPortValid(startPort)) return compatiblePorts;
            var startPortData = startPort.userData as HNodePortBase;
            foreach (var endPort in ports)
            {
                if(!HGraphUtility.CheckPortValid(endPort)) continue;
                var endPortData = endPort.userData as HNodePortBase;
                // 不是同一个端口，不是同一个节点
                if (startPort == endPort || startPort.node == endPort.node)
                    continue;
                // 端口间已存在连接    
                if(_graph.CheckLinkContains(startPortData.GUID, endPortData.GUID))
                    continue;
                // 输入端口只能连接输出端口
                if (startPort.direction == endPort.direction)
                    continue;
                var startType = startPort.portType.GetGenericArguments()[0];
                var endType = endPort.portType.GetGenericArguments()[0];
                // 端口类型检查
                if(startType != endType)
                    continue;
                
                compatiblePorts.Add(endPort);
                
            }
            return compatiblePorts;
        }
        

#endregion

#region miniMap
        // 创建小地图
        private void _createMiniMap()
        {
            if(HGraphCache.GetAttribute<HGraphAttribute>(_graph.GetType())?.ShowMiniMap == false) return;

            _miniMap = new MiniMap(){anchored = true};
            _updateMiniMapPosition();
            Add(_miniMap);
        }

        // 更新miniMap位置到右下角
        private void _updateMiniMapPosition()
        {
            if (_miniMap == null) return;
            // 将miniMap定位到右下角
            var windowWidth = contentRect.width;
            var windowHeight = contentRect.height;
            var miniMapWidth = 200;
            var miniMapHeight = 140;
            var margin = 10; // 边距
            
            // 计算右下角位置 (窗口宽度 - miniMap宽度 - 边距, 窗口高度 - miniMap高度 - 边距)
            var x = windowWidth - miniMapWidth - margin;
            var y = windowHeight - miniMapHeight - margin;
            
            _miniMap.SetPosition(new Rect(x, y, miniMapWidth, miniMapHeight));
        }
#endregion

#region blackBoard
        // 创建黑板
        private void _createBlackBoard()
        {
            if(HGraphCache.GetAttribute<HGraphAttribute>(_graph.GetType())?.ShowBlackBoard == false) return;

            var blackBoard = new Blackboard(this);
            blackBoard.Add(new BlackboardSection(){ title = "Exposed Properties" });
            // 添加一条属性
            blackBoard.addItemRequested = bd => {AddPropertyToBlackboard(new HBlackBoardProperty(){Name = "New Property", Value = "New Value"});};
            // 修改属性
            blackBoard.editTextRequested = (bd,element,newVal)=>{
                var oldName = ((BlackboardField)element).text;
                if(_exposedProperties.Any(x => x.Name == newVal))
                {
                    EditorUtility.DisplayDialog("提示", "属性名已存在", "确定");
                    return;
                }

                var index = _exposedProperties.FindIndex(x => x.Name == oldName);
                _exposedProperties[index].Name = newVal;
                ((BlackboardField)element).text = newVal;
            };
            
            blackBoard.SetPosition(new Rect(10, 30, 200, 300));
            Add(blackBoard);
            _blackboard = blackBoard;
        }

        /// <summary>
        /// 添加属性到黑板
        /// </summary>
        /// <param name="bd"></param>
        public void AddPropertyToBlackboard(HBlackBoardProperty property)
        {
            var curName = property.Name;
            var curValue = property.Value;

            // 防止重名
            while(_exposedProperties.Any(x => x.Name == curName))
                curName = $"{curName}(1)";
            
            var newProperty = new HBlackBoardProperty(){Name = curName, Value = curValue};
            _exposedProperties.Add(newProperty);

            var container = new VisualElement();
            // 名称
            var blackBoardField = new BlackboardField(){text = curName, typeText = "string"};
            container.Add(blackBoardField);

            // 值
            var blackBoardValueField = new TextField(){value = curValue};
            blackBoardValueField.RegisterValueChangedCallback(evt => {
                var index = _exposedProperties.FindIndex(x => x.Name == newProperty.Name);
                _exposedProperties[index].Value = evt.newValue;
            });
            
            var blackBoardValueRow = new BlackboardRow(blackBoardField,blackBoardValueField);
            container.Add(blackBoardValueRow);

            _blackboard?.Add(container);
        }

        /// <summary>
        /// 清除黑板
        /// </summary>
        public void ClearBlackBoard()
        {
            _exposedProperties.Clear();
            _blackboard?.Clear();
        }
#endregion
    }
}