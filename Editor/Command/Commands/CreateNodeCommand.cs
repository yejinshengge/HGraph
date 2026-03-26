using System;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 创建节点命令。首次执行时构建节点实例，后续 redo 复用原对象以保持 GUID 稳定。
    /// </summary>
    public sealed class CreateNodeCommand : IGraphCommand
    {
        /// <summary>
        /// 待创建节点的运行时类型。
        /// </summary>
        private readonly Type _nodeType;

        /// <summary>
        /// 节点初始位置。
        /// </summary>
        private readonly Vector2 _position;

        /// <summary>
        /// 缓存的节点实例，供 redo 时复用。
        /// </summary>
        private HNodeData _nodeData;

        /// <summary>
        /// 节点插入索引，便于撤销后恢复原顺序。
        /// </summary>
        private int _insertIndex = -1;

        public string Description => $"Create {_nodeType.Name}";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Structure | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 初始化一个新增节点命令。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        /// <param name="position">初始位置。</param>
        public CreateNodeCommand(Type nodeType, Vector2 position)
        {
            _nodeType = nodeType;
            _position = position;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_nodeData == null)
            {
                _nodeData = (HNodeData)Activator.CreateInstance(_nodeType);
                _nodeData.GraphPosition = _position;
                HGraphNodePortUtility.EnsureStaticPorts(_nodeData);
                // 初始化动态端口，使节点一创建就拥有正确的端口实例
                _nodeData.RebuildDynamicPorts();
            }

            _insertIndex = Mathf.Clamp(_insertIndex < 0 ? context.GraphData.Nodes.Count : _insertIndex, 0, context.GraphData.Nodes.Count);
            if (context.GraphData.Nodes.Contains(_nodeData))
            {
                return false;
            }

            context.GraphData.Nodes.Insert(_insertIndex, _nodeData);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            _insertIndex = context.GraphData.Nodes.IndexOf(_nodeData);
            context.GraphData.Nodes.Remove(_nodeData);
        }
    }
}
