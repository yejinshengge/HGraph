using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace HGraph
{
    /// <summary>
    /// 节点片段
    /// </summary>
    public abstract class HNodePieceBase
    {

    }

    /// <summary>
    /// 节点片段列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [InlineProperty]
    public class HNodePieceList<T> where T:HNodePieceBase,new()
    {
        [HideLabel]
        [ListDrawerSettings(CustomAddFunction = nameof(_addPiece), ShowFoldout = false, HideAddButton = false)]
        public List<T> pieces = new List<T>();

        private T _addPiece()
        {
            return new T();
        }
    }
}