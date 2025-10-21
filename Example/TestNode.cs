using System;
using UnityEngine;

namespace HGraph.Example
{
    [HNode(typeof(TestGraph),Name = "测试节点")]
    public class TestNode : HNodeBase
    {
        public InputPort<string> input = new InputPort<string>();

        public OutputPort<string> output = new OutputPort<string>();

        [Port(AllowMultiple = true)]
        public OutputPort<int> output2 = new OutputPort<int>();

        public int intVal;

        public string stringVal;

        [NonSerialized]
        public float floatVal;

        [SerializeField]
        private bool boolVal;

        public HNodePieceList<ChoicePiece> pieces1 = new HNodePieceList<ChoicePiece>();
        public HNodePieceList<ChoicePiece> pieces2 = new HNodePieceList<ChoicePiece>();
    }

    [Serializable]
    public class ChoicePiece:HNodePieceBase
    {
        public string choice;

        [Port(AllowMultiple = true)]
        public OutputPort<string> output = new OutputPort<string>();

        [Port(AllowMultiple = true)]
        public InputPort<string> input = new InputPort<string>();
    }
}