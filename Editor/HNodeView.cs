using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using Object = UnityEngine.Object;


namespace HGraph.Editor
{
    /// <summary>
    /// 节点视图
    /// </summary>
    public class HNodeView : NodeView
    {
        /// <summary>
        /// 节点数据
        /// </summary>
        public HNodeBase Node;

        // 属性视图
        private HPropertyView _propertyView;
        
        // 片段列表视图（支持多个）
        private List<HNodePieceListView> _pieceListViews = new List<HNodePieceListView>();

        public string GUID => Node?.GUID ?? string.Empty;

        public void Init(HNodeBase nodeData)
        {
            title = nodeData.NodeName;
            Node = nodeData;
            _initNodeView();
            _registerEvent();
        }

#region 事件

        // 注册事件
        private void _registerEvent()
        {
            RegisterCallback<GeometryChangedEvent>(_onGeometryChanged);
            
            // 注册Undo/Redo事件，实现即时刷新
            UnityEditor.Undo.undoRedoPerformed += _onUndoRedoPerformed;
            
            // 延迟注册重命名回调，确保UI元素已创建
            this.schedule.Execute(() =>
            {
                var titleLabel = this.Q<Label>("title-label");
                if (titleLabel != null)
                {
                    titleLabel.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.clickCount == 2) // 双击标题重命名
                        {
                            _startRenaming();
                        }
                    });
                }
            }).ExecuteLater(0);
        }
        
        /// <summary>
        /// 开始重命名节点
        /// </summary>
        private void _startRenaming()
        {
            var titleLabel = this.Q<Label>("title-label");
            if (titleLabel == null) return;
            
            // 创建TextField用于编辑
            var textField = new TextField
            {
                value = title,
                name = "title-field"
            };
            
            // 设置样式使其看起来像标题
            textField.style.fontSize = titleLabel.style.fontSize;
            textField.style.unityTextAlign = TextAnchor.MiddleCenter;
            textField.style.marginLeft = 0;
            textField.style.marginRight = 0;
            textField.style.marginTop = 0;
            textField.style.marginBottom = 0;
            
            // 获取输入框元素并设置样式
            var input = textField.Q(className: "unity-text-field__input");
            if (input != null)
            {
                input.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));
                input.style.borderLeftWidth = 1;
                input.style.borderRightWidth = 1;
                input.style.borderTopWidth = 1;
                input.style.borderBottomWidth = 1;
                input.style.borderLeftColor = new StyleColor(Color.gray);
                input.style.borderRightColor = new StyleColor(Color.gray);
                input.style.borderTopColor = new StyleColor(Color.gray);
                input.style.borderBottomColor = new StyleColor(Color.gray);
            }
            
            // 替换Label为TextField
            titleLabel.parent.Insert(titleLabel.parent.IndexOf(titleLabel), textField);
            titleLabel.style.display = DisplayStyle.None;
            
            // 延迟聚焦和全选，确保TextField已完全创建
            textField.schedule.Execute(() =>
            {
                textField.Focus();
                textField.SelectAll();
            }).ExecuteLater(0);
            
            // 处理编辑完成（失去焦点）
            textField.RegisterCallback<BlurEvent>(evt =>
            {
                _finishRenaming(textField, titleLabel);
            });
            
            // 处理键盘输入
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    _finishRenaming(textField, titleLabel);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    textField.value = title; // 恢复原值
                    _finishRenaming(textField, titleLabel);
                    evt.StopPropagation();
                }
            });
        }
        
        /// <summary>
        /// 完成重命名
        /// </summary>
        private void _finishRenaming(TextField textField, Label titleLabel)
        {
            if (textField == null || titleLabel == null) return;
            
            var newName = textField.value;
            if (!string.IsNullOrWhiteSpace(newName) && newName != title)
            {
                // 记录Undo
                UnityEditor.Undo.RecordObject(Node, "Rename Node");
                
                // 更新节点名称
                Node.NodeName = newName;
                title = newName;
                
                // 同步更新资产名称
                Node.name = newName;
                
                // 标记为脏并保存资产
                UnityEditor.EditorUtility.SetDirty(Node);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            
            // 恢复Label显示
            titleLabel.style.display = DisplayStyle.Flex;
            textField.RemoveFromHierarchy();
        }
        
        /// <summary>
        /// Undo/Redo操作后的回调
        /// </summary>
        private void _onUndoRedoPerformed()
        {
            if(Node == null) return;

            // 更新属性视图
            _propertyView?.Update();
            
            // 更新所有片段列表视图
            foreach (var pieceListView in _pieceListViews)
            {
                pieceListView?.Update();
            }
            
            // 更新标题（如果节点名称被Undo/Redo改变）
            if (title != Node.NodeName)
            {
                title = Node.NodeName;
                
                // 同步更新资产名称
                Node.name = Node.NodeName;
                UnityEditor.AssetDatabase.SaveAssets();
            }
            
        }

        // 位置、大小、缩放变化回调
        private void _onGeometryChanged(GeometryChangedEvent evt)
        {
            _updatePosition();
        }

#endregion

#region 视图
        // 创建节点内容视图
        private void _initNodeView()
        {            
            // 起始节点不可移动,不可删除
            if(Node is StartNode)
            {
                capabilities &= ~Capabilities.Movable;
                capabilities &= ~Capabilities.Deletable;
            }
            capabilities |= Capabilities.Renamable;
            SetPosition(new Rect(Node.Position, Node.Size));
            
            // 获取所有字段
            var fields = Node.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                // 过滤掉基类成员
                .Where(f => f.DeclaringType != typeof(HNodeBase))
                .ToList();
            
            // 第一步：添加节点级别的端口（非 HNodePieceList 的 Input/Output 字段）
            fields.ForEach(field =>
            {
                if (!_isHNodePieceListType(field.FieldType))
                {
                    _createNodePortView(field, Node);
                }
            });
        
            // 第二步：创建 Odin 字段视图容器（包括列表编辑功能，显示在扩展容器底部）
            _createOdinFieldsView();
            // 第三步：为 HNodePieceList 创建视图（显示在扩展容器顶部）
            fields.ForEach(field =>
            {
                if (_isHNodePieceListType(field.FieldType))
                {
                    _createPieceListView(field);
                }
            });

            extensionContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 1f));
            RefreshPorts();
            RefreshExpandedState();
        }

        // 创建Odin字段视图
        private void _createOdinFieldsView()
        {
            // 创建Odin属性树
            _propertyView = new HPropertyView(Node);
            var pv = _propertyView.Init();
            if(pv == null) return;
            // 添加到扩展容器
            extensionContainer.Add(pv);
        }


        // 创建端口视图（仅用于节点级别的 Input/Output 字段）
        private void _createNodePortView(FieldInfo field,Object target)
        {
            // 端口
            var isInput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(InputPort<>);
            var isOutput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(OutputPort<>);
            var port = HGraphUtility.CreatePort(this, field, target, Orientation.Horizontal);
            
            if(isInput)
            {
                inputContainer.Add(port);
            }
            if(isOutput)
            {
                outputContainer.Add(port);
            }
        }
        
        // 检查是否为 HNodePieceList<T> 类型
        private bool _isHNodePieceListType(Type type)
        {
            if (!type.IsGenericType) return false;
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(HNodePieceList<>);
        }
        
        // 创建 HNodePieceList 视图
        private void _createPieceListView(FieldInfo field)
        {            
            var pieceListView = new HNodePieceListView(field, Node, this);
            var container = pieceListView.Init();
            if(container != null)
            {
                _pieceListViews.Add(pieceListView);
                extensionContainer.Add(container);
            }
        }

#endregion        

        // 更新位置大小
        private void _updatePosition()
        {
            if (Node == null) return;
            
            var newPosition = GetPosition().position;
            var newSize = GetPosition().size;
            
            // 只有在位置或大小真正改变时才记录Undo
            if (Node.Position != newPosition || Node.Size != newSize)
            {
                UnityEditor.Undo.RecordObject(Node, "Move/Resize Node");
                Node.Position = newPosition;
                Node.Size = newSize;
                UnityEditor.EditorUtility.SetDirty(Node);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 取消注册Undo/Redo事件
            UnityEditor.Undo.undoRedoPerformed -= _onUndoRedoPerformed;
            _propertyView?.Dispose();
            
            // 清理所有片段列表视图
            foreach (var pieceListView in _pieceListViews)
            {
                pieceListView?.Dispose();
            }
            _pieceListViews.Clear();
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~HNodeView()
        {
            Dispose();
        }
    }
}