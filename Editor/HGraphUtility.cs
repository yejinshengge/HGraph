using System;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HGraph.Editor
{
    /// <summary>
    /// 存储和加载图标
    /// </summary>
    public static class HGraphUtility
    {

        /// <summary>
        /// 创建端口
        /// </summary>
        /// <param name="view"></param>
        /// <param name="field"></param>
        /// <param name="target"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public static Port CreatePort(HNodeView view, FieldInfo field, object target = null,
            Orientation orientation = Orientation.Horizontal)
        {
            var isInput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(InputPort<>);
            var isOutput = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(OutputPort<>);

            if(!isInput && !isOutput) return null;

            // 获取AllowMultiple字段
            var allowMultipleField = field.GetCustomAttribute<PortAttribute>();
            var allowMultiple = allowMultipleField != null && allowMultipleField.AllowMultiple;
            var capacity = allowMultiple ? Port.Capacity.Multi : Port.Capacity.Single;
            
            // 方向
            var dir = isInput ? Direction.Input : Direction.Output;
            var port = view.InstantiatePort(orientation, dir, capacity, field.FieldType);

            var data = field.GetValue(target) as HNodePortBase;
            if (data != null && !string.IsNullOrEmpty(data.GUID))
            {
                port.userData = data;
            }
            else
            {
                data = Activator.CreateInstance(field.FieldType) as HNodePortBase;
                data.GUID = Guid.NewGuid().ToString();
                port.userData = data;
                field.SetValue(target, data);
                Debug.Log($"创建端口 {data.GUID}");
                
                if(target is UnityEngine.Object unityObj)
                {
                    UnityEditor.EditorUtility.SetDirty(unityObj);
                }
            }
            port.name = data.GUID;
            port.portName = field.Name;

            var dict = isInput ? view.Node.InputPorts : view.Node.OutputPorts;
            dict[data.GUID] = data;

            return port;
        }

        public static void RemovePort()
        {

        }
        
        /// <summary>
        /// 校验端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CheckPortValid(Port port)
        {
            return port.portType.IsGenericType && 
            (port.portType.GetGenericTypeDefinition() == typeof(InputPort<>) || 
            port.portType.GetGenericTypeDefinition() == typeof(OutputPort<>));
        }

        /// <summary>
        /// 连接端口
        /// </summary>
        /// <param name="outputPort"></param>
        /// <param name="inputPort"></param>
        public static HNodeLink LinkPort(Port outputPort, Port inputPort)
        {
            var basePort = outputPort.userData as HNodePortBase;
            var targetPort = inputPort.userData as HNodePortBase;
            if (basePort == null || targetPort == null)
            {
                Debug.LogError("端口数据为空，无法创建连接");
                return null;
            }
            Debug.Log($"连接端口 {basePort.GUID} -> {targetPort.GUID}");

            var link = new HNodeLink()
            {
                BasePortGUID = basePort.GUID,
                TargetPortGUID = targetPort.GUID
            };

            return link;
        }

        public static HNodeLink LinkPort(string baseNodeGUID,string targetNodeGUID,Port outputPort, Port inputPort)
        {
            var link = LinkPort(outputPort, inputPort);
            if(link == null) return null;

            link.BaseNodeGUID = baseNodeGUID;
            link.TargetNodeGUID = targetNodeGUID;

            return link;
        }
    }
}