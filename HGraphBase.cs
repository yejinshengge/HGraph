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