using System;
using System.Collections.Generic;

namespace HGraph.Example
{
    /// <summary>
    /// 选择节点示例，演示动态端口的用法。
    /// 每个 <see cref="ChoiceOption"/> 对应一个输出端口；
    /// 在 Inspector 中增删选项后，端口会自动同步增减，且已有连线不受影响。
    /// </summary>
    [HGraphNode(NodeOf = typeof(DialogueGraph))]
    public class ChoiceNode : HNode
    {
        /// <summary>
        /// 节点输入端口，接收来自上一个对话节点的流。
        /// </summary>
        [Input]
        public string Input;

        /// <summary>
        /// 当前节点展示给玩家的提示文本。
        /// </summary>
        public string PromptText = "请做出选择：";

        /// <summary>
        /// 选项列表，每个选项对应一个动态输出端口。
        /// </summary>
        public List<ChoiceOption> Choices = new List<ChoiceOption>();

        /// <summary>
        /// 根据当前选项列表生成动态输出端口描述符。
        /// Key 使用选项的稳定 ID，保证改名后连线不丢失。
        /// </summary>
        protected override IEnumerable<DynamicPortDescriptor> GetDynamicPorts()
        {
            foreach (var choice in Choices)
            {
                yield return new DynamicPortDescriptor
                {
                    Key = choice.Id,
                    Label = string.IsNullOrEmpty(choice.Text) ? "(空选项)" : choice.Text,
                    Direction = PortDirection.Output,
                    AllowMultiple = false,
                    ValueType = typeof(string),
                };
            }
        }
    }

    /// <summary>
    /// 对话选项数据，包含稳定 ID 与显示文本。
    /// </summary>
    [Serializable]
    public class ChoiceOption
    {
        /// <summary>
        /// 选项的稳定唯一标识，创建后不应更改，用于维持端口连线的稳定性。
        /// </summary>
        public string Id = Guid.NewGuid().ToString();

        /// <summary>
        /// 选项的显示文本，同时作为对应端口的标签。
        /// </summary>
        public string Text = "新选项";
    }
}
