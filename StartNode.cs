namespace HGraph
{
    public class StartNode:HNodeBase
    {
        [Port(AllowMultiple = true)]
        public OutputPort<EmptyValue> output;
    }
}