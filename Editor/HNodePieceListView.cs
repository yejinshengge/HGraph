using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.GraphView;

namespace HGraph.Editor
{
    /// <summary>
    /// 节点片段列表视图
    /// </summary>
    public class HNodePieceListView
    {
        private VisualElement _container;
        private FieldInfo _field;
        private Object _target;
        private HNodeView _view;

        public HNodePieceListView(
            FieldInfo field,
            Object target,
            HNodeView view)
        {
            _container = new VisualElement();
            _field = field;
            _target = target;
            _view = view;
        }

        public VisualElement Init()
        {
            if (!_field.FieldType.IsGenericType) return null;
            var genericTypeDef = _field.FieldType.GetGenericTypeDefinition();
            if(genericTypeDef != typeof(HNodePieceList<>)) return null;

            // 获取泛型参数类型（即 T，如 ChoicePiece）
            var pieceType = _field.FieldType.GetGenericArguments()[0];
            
            // 检查 pieceType 中是否有 Input 或 Output 字段
            var pieceFields = pieceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var hasPortFields = pieceFields.Any(f => 
                f.FieldType.IsSubclassOf(typeof(HNodePortBase<>))
            );

            // 获取 pieces 列表数据
            var pieceListValue = _field.GetValue(_target);
            if (pieceListValue == null) return null;
            
            var piecesField = _field.FieldType.GetField("pieces");
            if (piecesField == null) return null;
            
            var piecesList = piecesField.GetValue(pieceListValue) as IList;
            if (piecesList == null) return null;

            // 创建 piece 列表的标题容器（更明显的样式）
            var listContainer = new IMGUIContainer(() =>
            {                
                GUILayout.BeginHorizontal(GUI.skin.box);
                var boldStyle = new GUIStyle(GUI.skin.label);
                boldStyle.fontStyle = FontStyle.Bold;
                boldStyle.fontSize = 12;
                GUILayout.Label($"{_field.Name}", boldStyle, GUILayout.Width(100));
                GUILayout.Label($"{piecesList.Count} items", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(20)))
                {
                    UnityEditor.Undo.RecordObject(_target, "Add Piece");
                    var newPiece = Activator.CreateInstance(pieceType);
                    piecesList.Add(newPiece);
                    UnityEditor.EditorUtility.SetDirty(_target);
                    Update();
                }
                GUILayout.EndHorizontal();
            });
            // 设置固定高度以避免布局偏移
            listContainer.style.height = 28;
            listContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.24f, 0.33f, 1f)); // 灰蓝色
            listContainer.style.paddingTop = 2;
            listContainer.style.paddingBottom = 2;
            listContainer.style.paddingLeft = 2;
            listContainer.style.paddingRight = 2;
            listContainer.style.borderTopLeftRadius = 4;
            listContainer.style.borderTopRightRadius = 4;
            listContainer.style.borderBottomLeftRadius = 4;
            listContainer.style.borderBottomRightRadius = 4;
            listContainer.style.marginBottom = 0;

            _container.Add(listContainer);

            // 为每个 piece 创建视图
            for (int i = 0; i < piecesList.Count; i++)
            {
                var piece = piecesList[i];
                var pieceIndex = i; // 捕获索引
                var pieceView = _createPieceElementView(_field.Name, pieceIndex, piece, pieceType, () =>
                {
                    // 删除回调
                    UnityEditor.Undo.RecordObject(_target, "Remove Piece");
                    piecesList.RemoveAt(pieceIndex);
                    UnityEditor.EditorUtility.SetDirty(_target);
                    Update();
                });
                if (pieceView != null)
                {
                    _container.Add(pieceView);
                }
            }

            return _container;
        }

        // 为单个 piece 元素创建视图
        private VisualElement _createPieceElementView(string listFieldName, int index, object piece, Type pieceType, Action onDelete)
        {
            // 创建主容器 - 垂直布局
            var mainContainer = new VisualElement();
            mainContainer.name = $"{listFieldName}_piece_{index}";
            mainContainer.style.marginBottom = 2;
            
            // 交替背景色，更深的颜色
            var bgColor = index % 2 == 0 
                ? new Color(0.18f, 0.18f, 0.18f, 1f)  // 偶数项：更深
                : new Color(0.20f, 0.20f, 0.20f, 1f);  // 奇数项：稍浅
            mainContainer.style.backgroundColor = new StyleColor(bgColor);
            
            mainContainer.style.paddingTop = 0;
            mainContainer.style.paddingBottom = 0;
            mainContainer.style.paddingLeft = 5;
            mainContainer.style.paddingRight = 5;
            mainContainer.style.borderTopLeftRadius = 4;
            mainContainer.style.borderTopRightRadius = 4;
            mainContainer.style.borderBottomLeftRadius = 4;
            mainContainer.style.borderBottomRightRadius = 4;
            
            // 遍历 piece 的字段
            var fields = pieceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // 为每个端口字段创建一行
            foreach (var field in fields)
            {
                var isInput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(InputPort<>);
                var isOutput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(OutputPort<>);
                
                if (isInput || isOutput )
                {
                    // 创建端口字段行
                    var fieldRow = new VisualElement();
                    fieldRow.style.flexDirection = FlexDirection.Row;
                    fieldRow.style.alignItems = Align.Center;
                    fieldRow.style.justifyContent = Justify.SpaceBetween;
                    fieldRow.style.marginBottom = 2;
                    fieldRow.style.minHeight = 24;
                    
                    if (isInput)
                    {
                        var port = HGraphUtility.CreatePort(_view, field,
                            piece, Orientation.Horizontal);
                        fieldRow.Add(port);
                    }
                    
                    // 中间添加占位符，让端口分布在两侧
                    var spacer = new VisualElement();
                    spacer.style.flexGrow = 1;
                    fieldRow.Add(spacer);
                    
                    if (isOutput)
                    {
                        var port = HGraphUtility.CreatePort(_view, field,
                            piece, Orientation.Horizontal);
                        fieldRow.Add(port);
                    }
                    
                    mainContainer.Add(fieldRow);
                }
            }
            
            // 创建 piece 属性视图（显示非端口字段）
            var propertyView = new HPropertyView(piece);
            var propertyContainer = propertyView.Init();
            if (propertyContainer != null)
            {
                mainContainer.Add(propertyContainer);
            }
            
            // 底部添加删除按钮
            var deleteButtonContainer = new VisualElement();
            deleteButtonContainer.style.flexDirection = FlexDirection.Row;
            deleteButtonContainer.style.justifyContent = Justify.FlexEnd;
            deleteButtonContainer.style.marginTop = 4;
            
            var deleteButton = new Button(onDelete);
            deleteButton.text = "Delete";
            deleteButton.style.width = 60;
            deleteButton.style.height = 20;
            deleteButton.style.fontSize = 10;
            deleteButton.style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f, 0.8f));
            deleteButtonContainer.Add(deleteButton);
            
            mainContainer.Add(deleteButtonContainer);
            
            return mainContainer;
        }

        // 更新UI
        public void Update()
        {
            if(_field == null) return;
            _container?.Clear();
            Init();
            _view.RefreshPorts();
            _view.RefreshExpandedState();
        }

        public void Dispose()
        {
            _container?.Clear();
            _container = null;
            _field = null;
            _target = null;
            _view = null;
        }
    }
}