
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HGraph
{
    public abstract class HNodeBase:SerializedScriptableObject
    {
        /// <summary>
        /// 唯一ID
        /// </summary>
        [ReadOnly]
        public string GUID;

        /// <summary>
        /// 节点名
        /// </summary>
        public string NodeName;

        /// <summary>
        /// 节点位置
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 大小
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// 输入端口
        /// </summary>
        [ReadOnly]
        public Dictionary<string,HNodePortBase> InputPorts = new Dictionary<string,HNodePortBase>();

        /// <summary>
        /// 输出端口
        /// </summary>
        [ReadOnly]
        public Dictionary<string,HNodePortBase> OutputPorts = new Dictionary<string,HNodePortBase>();

        /// <summary>
        /// 获取输入端口
        /// </summary>
        /// <param name="portId"></param>
        /// <returns></returns>
        public HNodePortBase GetInputPort(string portId)
        {
            if(InputPorts.TryGetValue(portId, out var port))
            {
                return port;
            }
            return null;
        }

        /// <summary>
        /// 获取输出端口
        /// </summary>
        /// <param name="portId"></param>
        /// <returns></returns>
        public HNodePortBase GetOutputPort(string portId)
        {
            if(OutputPorts.TryGetValue(portId, out var port))
            {
                return port;
            }
            return null;
        }

        /// <summary>
        /// 获取有效的输出端口
        /// </summary>
        /// <returns></returns>
        public List<HNodePortBase> GetValidOutputPorts()
        {   
            var result = new List<HNodePortBase>();
            foreach(var port in OutputPorts.Values)
            {
                if(port.IsValid())
                {
                    result.Add(port);
                }
            }
            return result;
        }

        #region 生命周期

        internal void Enter()
        {
            OnEnter();
        }

        internal void Exit()
        {
            foreach (var item in InputPorts.Values)
            {
                item.Clear();
            }
            foreach (var item in OutputPorts.Values)
            {
                item.Clear();
            }
            OnExit();
        }

        public virtual void OnEnter(){}

        public virtual void OnExit(){}

        #endregion
    }

    #region Attributes

    /// <summary>
    /// 节点特性
    /// </summary>
    public class HNodeAttribute:HGraphBaseAttribute
    {
        public string Name = string.Empty;

        public string Group = string.Empty;

        public Type NodeOf;

        public HNodeAttribute(Type nodeOf)
        {
            NodeOf = nodeOf;
        }
    }

    #endregion
}