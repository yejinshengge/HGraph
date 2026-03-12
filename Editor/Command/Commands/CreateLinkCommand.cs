namespace HGraph.Editor
{
    /// <summary>
    /// 创建单条连线。
    /// </summary>
    public sealed class CreateLinkCommand : IGraphCommand
    {
        /// <summary>
        /// 待创建的连线对象。
        /// </summary>
        private readonly HLink _link;

        public string Description => "Create Link";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建连线命令。
        /// </summary>
        /// <param name="link">目标连线。</param>
        public CreateLinkCommand(HLink link)
        {
            _link = link;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_link == null || HGraphCommandHelper.ContainsLink(context.Graph, _link))
            {
                return false;
            }

            context.Graph.Links.Add(_link);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            HGraphCommandHelper.RemoveLink(context.Graph, _link);
        }
    }
}
