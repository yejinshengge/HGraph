namespace HGraph
{
    public class StartNode:HNodeBase
    {
        [Port(AllowMultiple = false)]
        public OutputPort<EmptyValue> output;
    }
}