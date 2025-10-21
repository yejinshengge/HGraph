
using System;
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