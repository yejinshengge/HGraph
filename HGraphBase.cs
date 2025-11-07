using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HGraph
{
    /// <summary>
    /// HGraph序列化数据
    /// </summary>
    public class HGraphBase : SerializedScriptableObject
    {
        /// <summary>
        /// 节点
        /// </summary>
        public List<HNodeBase> nodes = new List<HNodeBase>();

        /// <summary>
        /// 节点连接
        /// </summary>
        // [HideInInspector]
        public List<HNodeLink> links = new List<HNodeLink>();

        /// <summary>
        /// 黑板属性
        /// </summary>
        public List<HBlackBoardProperty> exposedProperties = new List<HBlackBoardProperty>();

        /// <summary>
        /// 当前节点队列
        /// </summary>
        [NonSerialized]
        private Queue<HNodeBase> _curNodes = new();

        /// <summary>
        /// 避免重复
        /// </summary>
        [NonSerialized]
        private HashSet<HNodeBase> _curNodesSet = new();



#region 节点        

        /// <summary>
        /// 获取第一个节点
        /// </summary>
        /// <returns></returns>
        public HNodeBase GetFirstNode()
        {
            var startNode = _getStartNode() as StartNode;
            if(startNode == null)
                return null;
            return GetPortNextNode(startNode.output);
        }

        // 获取起始节点
        private HNodeBase _getStartNode()
        {
            return nodes.FirstOrDefault(n => n is StartNode);
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="nodeGUID"></param>
        /// <returns></returns>
        public HNodeBase GetNode(string nodeGUID)
        {
            return nodes.FirstOrDefault(n => n.GUID == nodeGUID);
        }

        /// <summary>
        /// 获取当前端口连接的下一个节点
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public HNodeBase GetPortNextNode(HNodePortBase port)
        {
            var nextNodes = GetPortNextNodes(port);
            if(nextNodes.Count == 0)
                return null;
            return nextNodes[0];
        }

        /// <summary>
        /// 获取当前端口连接的所有节点
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public List<HNodeBase> GetPortNextNodes(HNodePortBase port)
        {
            var result = new List<HNodeBase>();
            foreach(var link in links)
            {
                if(link.BasePortGUID != port.GUID)
                    continue;
                var targetNode = GetNode(link.TargetNodeGUID);
                if(targetNode == null)
                    continue;
                result.Add(targetNode);
            }
            return result;
        }

#endregion        

#region 端口

        /// <summary>
        /// 检查是否存在连接
        /// </summary>
        /// <param name="basePortGUID"></param>
        /// <param name="targetPortGUID"></param>
        /// <returns></returns>
        public bool CheckLinkContains(string basePortGUID, string targetPortGUID)
        {
            return links.Any(link => link.BasePortGUID == basePortGUID && link.TargetPortGUID == targetPortGUID);
        }

        /// <summary>
        /// 获取当前输出端口连接的下一个输入端口
        /// </summary>
        /// <param name="outputPort"></param>
        /// <returns></returns>
        public HNodePortBase GetNextPort(HNodePortBase outputPort)
        {
            var nextPorts = GetNextPorts(outputPort);
            if(nextPorts.Count == 0)
                return null;
            return nextPorts[0];
        }
        /// <summary>
        /// 获取当前输出端口连接的所有输入端口
        /// </summary>
        /// <param name="outputPort"></param>
        /// <returns></returns>
        public List<HNodePortBase> GetNextPorts(HNodePortBase outputPort)
        {
            var result = new List<HNodePortBase>();
            foreach(var link in links)
            {
                if(link.BasePortGUID != outputPort.GUID)
                    continue;
                var targetNode = GetNode(link.TargetNodeGUID);
                if(targetNode == null)
                    continue;
                var targetPort = targetNode.GetInputPort(link.TargetPortGUID);
                if(targetPort == null)
                    continue;
                result.Add(targetPort);
            }
            return result;
        }

        /// <summary>
        /// 获取当前输入端口连接的下一个输出端口GUID
        /// </summary>
        /// <param name="currentPortGUID"></param>
        /// <returns></returns>
        public string GetNextPortId(string currentPortGUID)
        {
            var nextPortsIds = GetNextPortsIds(currentPortGUID);
            if(nextPortsIds.Count == 0)
            {
                return string.Empty;
            }
            return nextPortsIds[0];
        }

        /// <summary>
        /// 获取当前输入端口连接的所有输出端口GUID
        /// </summary>
        /// <param name="currentPortGUID"></param>
        /// <returns></returns>
        public List<string> GetNextPortsIds(string currentPortGUID)
        {
            var result = new List<string>();
            foreach(var link in links)
            {
                if(link.BasePortGUID == currentPortGUID)
                {
                    result.Add(link.TargetPortGUID);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取当前输入端口连接的下一个输出端口GUID
        /// </summary>
        /// <param name="currentPortGUID"></param>
        /// <param name="targetNodeId"></param>
        /// <returns></returns>
        public List<string> GetNextPortsIds(string currentPortGUID,string targetNodeId)
        {
            var result = new List<string>();
            foreach(var link in links)
            {
                if(link.BasePortGUID == currentPortGUID && link.TargetNodeGUID == targetNodeId)
                {
                    result.Add(link.TargetPortGUID);
                }
            }
            return result;
        }
#endregion

#region 生命周期

        public void Init()
        {
            var firstNode = GetFirstNode();
            _curNodes.Append(firstNode);
            _curNodesSet.Add(firstNode);
            OnInit();
        }

        protected virtual void OnInit(){}

        public bool DoCurNode()
        {
            bool execute = false;
            var cnt = _curNodes.Count;
            for(var i = 0; i < cnt; i++)
            {
                execute = true;
                var node = _curNodes.Dequeue();
                _curNodesSet.Remove(node);
                // 执行当前节点
                node.Enter();
                // 获取输出
                var ports = node.GetValidOutputPorts();
                // 获取下一节点
                foreach(var port in ports)
                {
                    // 添加到队列
                    var nodes = GetPortNextNodes(port);

                    foreach (var nextNode in nodes)
                    {
                        if(!_curNodesSet.Contains(nextNode))
                        {
                            _curNodes.Enqueue(nextNode);
                            _curNodesSet.Add(nextNode);
                        }
                    }

                    // 端口赋值
                    var nextPorts = GetNextPorts(port);
                    foreach (var nextPort in nextPorts)
                    {
                        nextPort.SetValue(port);
                    }
                }
                // 退出节点
                node.Exit();
            }
            return execute;
        }

        public void CleanUp()
        {
            while(_curNodes.Count > 0)
            {
                var node = _curNodes.Dequeue();
                node.Exit();
            }
            _curNodes.Clear();
            _curNodesSet.Clear();
            OnCleanUp();
        }

        protected virtual void OnCleanUp(){}

#endregion

#if UNITY_EDITOR

        /// <summary>
        /// 开启前处理
        /// </summary>
        public void OnBeforeOpen()
        {
            if(nodes.FirstOrDefault(n => n is StartNode) == null)
            {
                var startNode = SerializedScriptableObject.CreateInstance<StartNode>();
                startNode.GUID = Guid.NewGuid().ToString();
                startNode.Position = new UnityEngine.Vector2(100, 200);
                startNode.NodeName = "Start";
                startNode.name = "Start";
                
                // 只添加到列表，子资产的保存在资产持久化后处理
                nodes.Add(startNode);
                
                // 如果资产已经存在（已持久化），则添加为子资产
                if(UnityEditor.AssetDatabase.Contains(this))
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(startNode, this);
                }
            }
        }
        
#endif
    }

    #region Attributes

    public class HGraphBaseAttribute : Attribute
    {

    }


    public class HGraphAttribute : HGraphBaseAttribute
    {
        /// <summary>
        /// 是否显示黑板
        /// </summary>
        public bool ShowBlackBoard = false;

        /// <summary>
        /// 是否显示小地图
        /// </summary>
        public bool ShowMiniMap = true;
    }

    #endregion
}