namespace HGraph.Example
{
    /// <summary>
    /// 对话节点示例，展示普通字段与输入输出端口的组合写法。
    /// </summary>
    [HGraphNode(NodeOf = typeof(DialogueGraph))]
    public class DialogueNode : HNode
    {
        /// <summary>
        /// 当前节点要展示的对话文本。
        /// </summary>
        public string DialogueText;

        /// <summary>
        /// 对话节点的输入端口。
        /// </summary>
        [Input]
        public string Input;

        /// <summary>
        /// 对话节点的输出端口，允许连接到多个后继节点。
        /// </summary>
        [Output(AllowMultiple = true)]
        public string Output;
    }
}