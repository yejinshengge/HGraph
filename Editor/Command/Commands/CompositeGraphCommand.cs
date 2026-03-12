using System.Collections.Generic;
using System.Linq;

namespace HGraph.Editor
{
    /// <summary>
    /// 组合命令，用于把"删除节点 + 删除关联边"之类的复合操作包装成单次撤销。
    /// </summary>
    public sealed class CompositeGraphCommand : IGraphCommand
    {
        /// <summary>
        /// 子命令集合，按顺序执行、按逆序撤销。
        /// </summary>
        private readonly List<IGraphCommand> _commands;

        /// <summary>
        /// 组合命令描述。
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 聚合后的刷新模式。
        /// </summary>
        public GraphCommandRefreshMode RefreshMode { get; }

        /// <summary>
        /// 创建组合命令。
        /// </summary>
        /// <param name="description">命令描述。</param>
        /// <param name="commands">子命令序列。</param>
        public CompositeGraphCommand(string description, IEnumerable<IGraphCommand> commands)
        {
            Description = description;
            _commands = commands?.Where(command => command != null).ToList() ?? new List<IGraphCommand>();
            RefreshMode = _commands.Aggregate(GraphCommandRefreshMode.None, (current, command) => current | command.RefreshMode);
        }

        /// <summary>
        /// 顺序执行全部子命令；若中途失败，会回滚已执行部分。
        /// </summary>
        /// <param name="context">命令上下文。</param>
        /// <returns>是否执行成功。</returns>
        public bool Execute(GraphCommandContext context)
        {
            var executedCount = 0;
            try
            {
                foreach (var command in _commands)
                {
                    if (!command.Execute(context))
                    {
                        return false;
                    }

                    executedCount++;
                }

                return _commands.Count > 0;
            }
            catch
            {
                for (var i = executedCount - 1; i >= 0; i--)
                {
                    _commands[i].Undo(context);
                }

                throw;
            }
        }

        /// <summary>
        /// 逆序撤销全部子命令。
        /// </summary>
        /// <param name="context">命令上下文。</param>
        public void Undo(GraphCommandContext context)
        {
            for (var i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo(context);
            }
        }
    }
}
