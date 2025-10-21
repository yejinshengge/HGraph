using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    /// <summary>
    /// 用于显示属性面板
    /// </summary>
    public class HPropertyView
    {
        /// <summary>
        /// Odin属性树
        /// </summary>
        private PropertyTree _propertyTree;

        /// <summary>
        /// IMGUI容器
        /// </summary>
        private IMGUIContainer _imguiContainer;

        /// <summary>
        /// 目标对象
        /// </summary>
        private object _target;

        public HPropertyView(object target)
        {
            // 创建Odin属性树
            _propertyTree = PropertyTree.Create(target);
            _target = target;
        }

        /// <summary>
        /// 刷新视图
        /// </summary>
        public void Update()
        {
            // 更新PropertyTree以反映Undo后的数据
            _propertyTree?.UpdateTree();
            
            // 强制重绘IMGUI容器
            _imguiContainer?.MarkDirtyRepaint();
        }

        public IMGUIContainer Init()
        {
            if (_propertyTree == null || _target == null)
                return null;
            var propertyList = new List<InspectorProperty>();   

            // 只绘制非端口字段
            foreach (var property in _propertyTree.EnumerateTree(false))
            {
                // 跳过标记为Input或Output的端口字段
                var field = _target.GetType().GetField(property.Name, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                // 如果找不到对应的字段，跳过（可能是Odin内部生成的虚拟属性）
                if (field == null)
                    continue;
                
                var hasInput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(InputPort<>);
                var hasOutput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(OutputPort<>);
  
                // 跳过公开但非序列化字段
                var isPublicNonSerialized = field.IsPublic && field.GetCustomAttribute(typeof(System.NonSerializedAttribute)) != null;
                // 跳过非公开但没指定序列化字段
                var isPrivateNotSerialized = field.IsPrivate && field.GetCustomAttribute(typeof(SerializeField)) == null;
                // 跳过基类字段（包括HNodeBase及其所有父类的字段）
                var isBaseClassField = field.DeclaringType != _target.GetType();
                // 跳过 HNodePieceList 字段
                var isHNodePieceList = _isHNodePieceList(field.FieldType);
                
                if (hasInput || hasOutput || isPublicNonSerialized || isPrivateNotSerialized || isBaseClassField || isHNodePieceList)
                    continue;
                
                propertyList.Add(property);
            } 

            if(propertyList.Count == 0)
                return null;
            // 检查目标对象是否继承自 UnityEngine.Object
            bool isUnityObject = _target is UnityEngine.Object;
            
            // 使用IMGUIContainer嵌入Odin绘制
            _imguiContainer = new IMGUIContainer(() =>
            {
                if (_propertyTree == null || _target == null)
                    return;
                    
                // 只有当目标继承自 UnityEngine.Object 时才启用自动 Undo
                // HNodeBase 继承自 SerializedScriptableObject，最终继承自 UnityEngine.Object，可以启用
                // 但 HNodePieceBase 不继承自 UnityEngine.Object，需要禁用自动 Undo
                _propertyTree.BeginDraw(isUnityObject);
                
                // 只绘制非端口字段
                foreach (var property in propertyList)
                {   
                    // 绘制字段
                    property.Draw();
                }
                
                _propertyTree.EndDraw();
                
            });

            // 为节点级别的字段设置样式
            // _imguiContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 1f));
            _imguiContainer.style.paddingTop = 5;
            _imguiContainer.style.paddingBottom = 5;
            _imguiContainer.style.paddingLeft = 5;
            _imguiContainer.style.paddingRight = 5;
            _imguiContainer.style.borderTopLeftRadius = 4;
            _imguiContainer.style.borderTopRightRadius = 4;
            _imguiContainer.style.borderBottomLeftRadius = 4;
            _imguiContainer.style.borderBottomRightRadius = 4;
            
            return _imguiContainer;
        }

        public void Dispose()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
            _imguiContainer = null;
            _target = null;
        }
        
        /// <summary>
        /// 检查字段类型是否为包含端口的 HNodePieceList
        /// </summary>
        private bool _isHNodePieceList(System.Type type)
        {
            // 检查是否为 HNodePieceList<T> 类型
            if (!type.IsGenericType) return false;
            var genericTypeDef = type.GetGenericTypeDefinition();
            if (genericTypeDef != typeof(HNodePieceList<>)) return false;
            
            // // 获取泛型参数类型（即 T，如 ChoicePiece）
            // var pieceType = type.GetGenericArguments()[0];
            
            // // 检查 pieceType 中是否有 Input 或 Output 字段
            // var pieceFields = pieceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // return pieceFields.Any(f => 
            //     f.GetCustomAttribute(typeof(InputAttribute)) != null || 
            //     f.GetCustomAttribute(typeof(OutputAttribute)) != null
            // );
            return true;
        }
    }
}