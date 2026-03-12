using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    /// <summary>
    /// 节点端口视图，负责将端口元数据映射到 GraphView Port。
    /// </summary>
    public class HPortView : Port
    {
        /// <summary>
        /// 端口主色（R）。
        /// </summary>
        private const float PortColorR = 0.10f;

        /// <summary>
        /// 端口主色（G）。
        /// </summary>
        private const float PortColorG = 0.83f;

        /// <summary>
        /// 端口主色（B）。
        /// </summary>
        private const float PortColorB = 0.92f;

        /// <summary>
        /// 端口对应的节点成员名。
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// 端口绑定的数据对象。
        /// </summary>
        public HPort PortData { get; }

        /// <summary>
        /// 端口所属节点 ID。
        /// </summary>
        public string NodeGuid => PortData.NodeGUID;

        /// <summary>
        /// 用于持久化连线的稳定端口 ID。
        /// </summary>
        public string PortId => PortData.GUID;

        /// <summary>
        /// 端口特性元数据（包含连接容量等配置）。
        /// </summary>
        public HGraphPortAttribute PortAttribute { get; }

        /// <summary>
        /// 拖拽成功后的端口连接请求回调。
        /// </summary>
        private readonly Action<HPortView, HPortView> _onConnectRequested;

        /// <summary>
        /// 初始化端口视图实例。
        /// </summary>
        /// <param name="memberName">成员名。</param>
        /// <param name="portData">端口数据。</param>
        /// <param name="portAttribute">端口配置特性。</param>
        /// <param name="orientation">端口朝向。</param>
        /// <param name="direction">端口方向（输入/输出）。</param>
        /// <param name="capacity">端口容量（单连线/多连线）。</param>
        /// <param name="type">端口值类型。</param>
        private HPortView(
            string memberName,
            HPort portData,
            HGraphPortAttribute portAttribute,
            Orientation orientation,
            Direction direction,
            Capacity capacity,
            Type type,
            Action<HPortView, HPortView> onConnectRequested)
            : base(orientation, direction, capacity, type)
        {
            MemberName = memberName;
            PortData = portData;
            PortAttribute = portAttribute;
            _onConnectRequested = onConnectRequested;
            portName = $"{ObjectNames.NicifyVariableName(memberName)}";

            portColor = new Color(PortColorR, PortColorG, PortColorB, 1f);
            style.minHeight = 24;
            style.paddingTop = 2;
            style.paddingBottom = 2;
            style.unityTextAlign = TextAnchor.MiddleLeft;

            if (direction == Direction.Output)
            {
                style.unityTextAlign = TextAnchor.MiddleRight;
            }

            this.AddManipulator(new EdgeConnector<Edge>(new HEdgeConnectorListener(_onConnectRequested)));
        }

        /// <summary>
        /// 根据成员信息与端口配置创建端口视图。
        /// </summary>
        /// <param name="portData">端口数据。</param>
        /// <param name="member">声明端口的成员。</param>
        /// <param name="portAttribute">端口特性配置。</param>
        /// <param name="direction">端口方向。</param>
        /// <param name="onConnectRequested">连接成功后的回调。</param>
        /// <returns>创建完成的端口视图。</returns>
        public static HPortView Create(
            HPort portData,
            MemberInfo member,
            HGraphPortAttribute portAttribute,
            Direction direction,
            Action<HPortView, HPortView> onConnectRequested)
        {
            var valueType = _getMemberType(member);
            var capacity = portAttribute.AllowMultiple ? Capacity.Multi : Capacity.Single;

            return new HPortView(
                member.Name,
                portData,
                portAttribute,
                Orientation.Horizontal,
                direction,
                capacity,
                valueType,
                onConnectRequested);
        }

        /// <summary>
        /// 从字段或属性中推断端口类型。
        /// </summary>
        /// <param name="member">成员信息。</param>
        /// <returns>端口值类型；无法推断时返回 object。</returns>
        private static Type _getMemberType(MemberInfo member)
        {
            var field = member as FieldInfo;
            if (field != null)
            {
                return field.FieldType;
            }

            var property = member as PropertyInfo;
            if (property != null)
            {
                return property.PropertyType;
            }

            return typeof(object);
        }

        /// <summary>
        /// 为端口提供标准的拖拽连线交互。
        /// </summary>
        private sealed class HEdgeConnectorListener : IEdgeConnectorListener
        {
            /// <summary>
            /// 连接落地时回调到上层视图的委托。
            /// </summary>
            private readonly Action<HPortView, HPortView> _onConnectRequested;

            /// <summary>
            /// 创建端口连线监听器。
            /// </summary>
            /// <param name="onConnectRequested">连接成功后的回调。</param>
            public HEdgeConnectorListener(Action<HPortView, HPortView> onConnectRequested)
            {
                _onConnectRequested = onConnectRequested;
            }

            /// <summary>
            /// 拖拽到空白区域时不执行任何操作。
            /// </summary>
            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }

            /// <summary>
            /// 拖拽连接成功后把端口配对结果转交给上层视图处理。
            /// </summary>
            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (edge.output is HPortView outputPort && edge.input is HPortView inputPort)
                {
                    // Port 只负责把拖拽结果翻译成“请求连接哪两个端口”，
                    // 真正的数据写入与撤销/重做交给命令系统处理。
                    _onConnectRequested?.Invoke(outputPort, inputPort);
                }
            }
        }
    }
}
